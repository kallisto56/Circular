namespace Circular
{
	using UnityEngine;
	using UnityEditor;



	public static class Utility
	{


		/// <summary>
		/// Finds nearest point on Biarc in 2d space.
		/// </summary>
		/// <param name="biarc">Target biarc</param>
		/// <param name="mousePosition">mouse position</param>
		/// <param name="distanceOnBiarc">[out] distance on biarc</param>
		/// <param name="pointOnBiarc">[out] point on biarc</param>
		/// <param name="vertices3d">Fixed array for storing vertices of biarc in local space</param>
		/// <param name="vertices2d">Fixed array for storing vertices of biarc in 2d space</param>
		public static void GetNearestPointOnBiarc (Biarc biarc, Vector2 mousePosition, out float distanceOnBiarc, out Vector3 pointOnBiarc, ref Vector3[] vertices3d, ref Vector2[] vertices2d)
		{
			float delta = biarc.totalLength / (float)(vertices3d.Length - 1);
			float alpha = 0.0f;

			for (int n = 0; n < vertices3d.Length; n++)
			{
				// Store point in vertices3d for later use
				vertices3d[n] = biarc.GetPoint(alpha);

				// Transform it into 2d space
				vertices2d[n] = HandleUtility.WorldToGUIPoint(vertices3d[n]);
				alpha += delta;
			}

			int index = 0;
			float nearestDistance = float.MaxValue;
			Vector2 nearestPoint = Vector2.zero;

			// Iterate over vertices as a set of lines. Line with a shortest
			// distance to mouse position wins.
			for (int n = 0; n < vertices2d.Length - 1; n++)
			{
				Vector2 point = Utility.GetClosestPointOnFiniteLine2d(mousePosition, vertices2d[n], vertices2d[n + 1]);
				float distance = Vector2.Distance(point, mousePosition);
				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearestPoint = point;
					index = n;
				}
			}

			// Next, find closest point in vertices3d
			float distanceToPoint = Vector2.Distance(vertices2d[index], nearestPoint);
			float length = Vector2.Distance(vertices2d[index], vertices2d[index + 1]);
			float t = distanceToPoint / length;

			Vector3 closestPoint = Vector3.Lerp(vertices3d[index], vertices3d[index + 1], t);

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
		/// <returns><see cref="Vector3" /> Closets point on finite line to specified point</returns>
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
		/// 2D Method for finding closest point on line for specified point
		/// </summary>
		/// <param name="point">Point, to which algorithm should find closest point on line</param>
		/// <param name="origin">Point, where line starts</param>
		/// <param name="destination">Point, where line ends</param>
		/// <returns><see cref="Vector2" /> Closets point on finite line to specified point</returns>
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
		/// Method for determining if there is an intersection between ray and sphere.
		/// </summary>
		/// <param name="ray">Ray</param>
		/// <param name="center">Center of sphere</param>
		/// <param name="radius">Radius of sphere</param>
		/// <returns>Returns true, if ray goes through sphere.</returns>
		public static bool RaycastSphere (Ray ray, Vector3 center, float radius)
		{
			// Source: https://answers.unity.com/questions/1825578/check-if-raycast-intersects-a-sphere-given-the-sph.html
			// Get the components of the vector coming from the point to the camera 
			float x = center.x - ray.origin.x;
			float y = center.y - ray.origin.y;
			float z = center.z - ray.origin.z;

			Vector3 dir = ray.direction;

			float t = (x * dir.x + y * dir.y + z * dir.z) / (dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);

			float D1 = (dir.x * dir.x + dir.y * dir.y + dir.z * dir.z) * (t * t);
			float D2 = (x * dir.x + y * dir.y + z * dir.z) * 2 * t;
			float D3 = (x * x + y * y + z * z);

			// Solve quadratic formula
			float D = D1 - D2 + D3;

			// Check if the ray is within the radius
			return (D < radius / 2.0f);
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
		/// Applies rounding to provided Vector3 with defined grid size and origin
		/// </summary>
		/// <param name="position">Input <see cref="Vector3" /></param>
		/// <param name="origin">Origin from which grid snapping should be applied</param>
		/// <param name="gridSize">Size of grid</param>
		/// <returns><see cref="Vector3" /></returns>
		public static Vector3 SnapVector (Vector3 position, Vector3 origin, float snapValue)
		{
			return new Vector3(
				origin.x + Mathf.Round((position.x - origin.x) / snapValue) * snapValue,
				origin.y + Mathf.Round((position.y - origin.y) / snapValue) * snapValue,
				origin.z + Mathf.Round((position.z - origin.z) / snapValue) * snapValue
			);
		}

		/// <summary>
		/// Applies rounding to provided Vector3 using Transform as origin
		/// </summary>
		/// <param name="position"></param>
		/// <param name="origin"></param>
		/// <param name="snapValue"></param>
		/// <returns><see cref="Vector3" /></returns>
		public static Vector3 SnapVector (Vector3 position, Transform origin, float snapValue)
		{
			position = origin.InverseTransformPoint(position);
			position = new Vector3(
				origin.localPosition.x + Mathf.Round((position.x - origin.localPosition.x) / snapValue) * snapValue,
				origin.localPosition.y + Mathf.Round((position.y - origin.localPosition.y) / snapValue) * snapValue,
				origin.localPosition.z + Mathf.Round((position.z - origin.localPosition.z) / snapValue) * snapValue
			);

			return origin.TransformPoint(position);
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



	}



}