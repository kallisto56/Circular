namespace Circular
{
	using UnityEngine;
	using UnityEditor;



	/// <summary>
	/// Set of commonly used functions.
	/// </summary>
	public static class Utility
	{


		/// <summary>
		/// Finds nearest point on Biarc in 2d space.
		/// </summary>
		/// <param name="biarc">Target biarc</param>
		/// <param name="transform">Transform for converting biarc points between world space and local space</param>
		/// <param name="mousePosition">Mouse position</param>
		/// <param name="distanceOnBiarc">[out] distance on biarc</param>
		/// <param name="pointOnBiarc">[out] point on biarc</param>
		/// <param name="vertices3d">Fixed array for storing vertices of biarc in local space</param>
		/// <param name="vertices2d">Fixed array for storing vertices of biarc in 2d space</param>
		public static void GetNearestPointOnBiarc (Biarc biarc, Transform transform, Vector2 mousePosition, out float distanceOnBiarc, out Vector3 pointOnBiarc, out float distanceOnBiarc2d, ref Vector3[] vertices3d, ref Vector2[] vertices2d)
		{
			// ...
			float alpha = 0.0f;
			float delta = biarc.totalLength / (float)(vertices3d.Length - 1);

			// ...
			for (int n = 0; n < vertices3d.Length; n++)
			{
				// Transform point to world space and store it in vertices3d for later use
				vertices3d[n] = transform.TransformPoint(biarc.GetPoint(alpha));

				// Transform it into 2d space
				vertices2d[n] = HandleUtility.WorldToGUIPoint(vertices3d[n]);

				// Advance
				alpha += delta;
			}

			int index = 0;
			distanceOnBiarc2d = float.MaxValue;
			Vector2 nearestPoint = Vector2.zero;

			// Iterate over vertices as a set of lines. Line with a shortest
			// distance to mouse position wins.
			for (int n = 0; n < vertices2d.Length - 1; n++)
			{
				// Find closest point on line segment
				Vector2 point = Utility.GetClosestPointOnFiniteLine2d(mousePosition, vertices2d[n], vertices2d[n + 1]);

				// Measure distance to mouse position
				float distance = Vector2.Distance(point, mousePosition);

				// ...
				if (distance < distanceOnBiarc2d)
				{
					distanceOnBiarc2d = distance;
					nearestPoint = point;
					index = n;
				}
			}

			// We already know index, we are required to find alpha,
			// that will be used to find point in vertices3d.
			float distanceToPoint = Vector2.Distance(vertices2d[index], nearestPoint);
			float length = Vector2.Distance(vertices2d[index], vertices2d[index + 1]);
			float t = distanceToPoint / length;

			// Interpolation between two points in local space
			Vector3 closestPoint = Vector3.Lerp(
				transform.InverseTransformPoint(vertices3d[index]),
				transform.InverseTransformPoint(vertices3d[index + 1]),
				t
			);

			// Last, since vertices3d is an approximation, find nearest point
			biarc.GetNearestPoint(closestPoint, out pointOnBiarc, out distanceOnBiarc);
		}

		/// <summary>
		/// Calculates intersection between two infinite lines.
		/// </summary>
		/// <param name="p0">First point</param>
		/// <param name="v0">Direction of first line</param>
		/// <param name="p1">Second point</param>
		/// <param name="v1">Direction of second line</param>
		/// <param name="intersection">Intersection between two infinite lines</param>
		/// <returns><see cref="bool" /> Returns true if intersection occurred</returns>
		public static bool LineLineIntersection (Vector3 p0, Vector3 v0, Vector3 p1, Vector3 v1, out Vector3 intersection)
		{
			// Source: https://stackoverflow.com/questions/59449628/check-when-two-vector3-lines-intersect-unity3d
			Vector3 direction = p1 - p0;
			Vector3 cross0 = Vector3.Cross(v0, v1);
			Vector3 cross1 = Vector3.Cross(direction, v1);

			float planarFactor = Vector3.Dot(direction, cross0);

			// is coplanar, and not parallel
			if (Mathf.Abs(planarFactor) < 0.0001f && cross0.sqrMagnitude > 0.0001f)
			{
				float length = Vector3.Dot(cross1, cross0) / cross0.sqrMagnitude;
				intersection = p0 + (v0 * length);
				return true;
			}
			else
			{
				intersection = Vector3.zero;
				return false;
			}
		}

		/// <summary>
		/// Method for finding closest point on line for specified point
		/// </summary>
		/// <param name="point">Point, to which algorithm should find closest point on line</param>
		/// <param name="origin">Point, where line starts</param>
		/// <param name="destination">Point, where line ends</param>
		/// <returns><see cref="Vector3" /> Closest point on finite line to specified point</returns>
		public static Vector3 GetClosestPointOnFiniteLine (Vector3 point, Vector3 origin, Vector3 destination)
		{
			// Source: https://stackoverflow.com/questions/51905268/how-to-find-closest-point-on-line
			Vector3 direction = destination - origin;
			float length = direction.magnitude;

			direction.Normalize();

			float distance = Mathf.Clamp(Vector3.Dot(point - origin, direction), 0f, length);
			return origin + direction * distance;
		}

		/// <summary>
		/// Method for finding closest point on infinite line for specified point
		/// </summary>
		/// <param name="point">Point, to which algorithm should find closest point on line</param>
		/// <param name="origin">Point, where line starts</param>
		/// <param name="direction">Direction of the line</param>
		/// <returns><see cref="Vector3" /> Closest point on infinite line to specified point</returns>
		public static Vector3 GetClosestPointOnInfiniteLine (Vector3 point, Vector3 origin, Vector3 direction)
		{
			// Source: https://stackoverflow.com/questions/51905268/how-to-find-closest-point-on-line
			return origin + Vector3.Project(point - origin, direction);
		}

		/// <summary>
		/// 2D Method for finding closest point on line for specified point
		/// </summary>
		/// <param name="point">Point, to which algorithm should find closest point on line</param>
		/// <param name="origin">Point, where line starts</param>
		/// <param name="destination">Point, where line ends</param>
		/// <returns><see cref="Vector2" /> Closest point on finite line to specified point</returns>
		public static Vector2 GetClosestPointOnFiniteLine2d (Vector2 point, Vector2 origin, Vector2 destination)
		{
			// Source: https://stackoverflow.com/questions/51905268/how-to-find-closest-point-on-line
			Vector2 direction = destination - origin;
			float length = direction.magnitude;

			direction.Normalize();

			float distance = Mathf.Clamp(Vector2.Dot(point - origin, direction), 0f, length);
			return origin + direction * distance;
		}

		/// <summary>
		/// Applies rounding to provided Vector3 with defined grid size
		/// </summary>
		/// <param name="position">Input <see cref="Vector3" /></param>
		/// <param name="gridSize">Size of grid</param>
		/// <returns><see cref="Vector3" /></returns>
		public static Vector3 SnapVector (Vector3 position, float gridSize)
		{
			return new Vector3(
				Mathf.Round(position.x / gridSize) * gridSize,
				Mathf.Round(position.y / gridSize) * gridSize,
				Mathf.Round(position.z / gridSize) * gridSize
			);
		}

		/// <summary>
		/// Applies rounding to provided Vector3 using Transform.
		/// </summary>
		/// <param name="position">Value to snap</param>
		/// <param name="transform">Transform, that will be used for converting between world space and local space</param>
		/// <param name="snapValue">The increment to snap to</param>
		/// <returns><see cref="Vector3" /></returns>
		public static Vector3 SnapVector (Vector3 position, Transform transform, float snapValue)
		{
			position = transform.InverseTransformPoint(position);

			position = new Vector3(
				transform.localPosition.x + Mathf.Round((position.x - transform.localPosition.x) / snapValue) * snapValue,
				transform.localPosition.y + Mathf.Round((position.y - transform.localPosition.y) / snapValue) * snapValue,
				transform.localPosition.z + Mathf.Round((position.z - transform.localPosition.z) / snapValue) * snapValue
			);

			return transform.TransformPoint(position);
		}

		/// <summary>
		/// Applies rounding to provided Vector3 using Transform and pivot.
		/// </summary>
		/// <param name="position">Value to snap</param>
		/// <param name="transform">Transform, that will be used for converting between world space and local space</param>
		/// <param name="pivot">Position, that will be used as origin, from which snapping will start.</param>
		/// <param name="snapValue">The increment to snap to</param>
		/// <returns><see cref="Vector3" /></returns>
		public static Vector3 SnapVector (Vector3 position, Transform transform, Vector3 pivot, float snapValue)
		{
			position = transform.InverseTransformPoint(position);

			position = new Vector3(
				transform.localPosition.x + pivot.x + Mathf.Round((position.x - transform.localPosition.x - pivot.x) / snapValue) * snapValue,
				transform.localPosition.y + pivot.y + Mathf.Round((position.y - transform.localPosition.y - pivot.y) / snapValue) * snapValue,
				transform.localPosition.z + pivot.z + Mathf.Round((position.z - transform.localPosition.z - pivot.z) / snapValue) * snapValue
			);

			return transform.TransformPoint(position);
		}

		/// <summary>
		/// Returns Vector3, which has smallest value in corresponding float variable.
		/// </summary>
		/// <param name="v0">First point</param>
		/// <param name="v1">Second point</param>
		/// <param name="v2">Third point</param>
		/// <param name="d0">First distance</param>
		/// <param name="d1">Second distance</param>
		/// <param name="d2">Third distance</param>
		/// <returns><see cref="Vector3" /></returns>
		public static Vector3 Min (Vector3 v0, Vector3 v1, Vector3 v2, float d0, float d1, float d2)
		{
			if (d0 < d1)
			{
				return (d0 < d2) ? v0 : v2;
			}
			else
			{
				return (d1 < d2) ? v1 : v2;
			}
		}

		/// <summary>
		/// Translates target quaternion to world space
		/// </summary>
		/// <param name="transform">Transform that will be used to translate to world space</param>
		/// <param name="rotation">Target quaternion</param>
		/// <returns>Quaternion, translated to world space</returns>
		public static Quaternion TransformRotation (Transform transform, Quaternion rotation)
		{
			// ...
			Vector3 forward = rotation * Vector3.forward;
			Vector3 upwards = rotation * Vector3.up;

			// ...
			forward = transform.TransformDirection(forward);
			upwards = transform.TransformDirection(upwards);

			// ...
			return Quaternion.LookRotation(forward, upwards);
		}


	}



}