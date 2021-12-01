namespace Circular
{
	using System;
	using UnityEngine;


	
	[Serializable]
	public class Arc
	{
		public Vector3 origin;
		public Vector3 tangent;
		public Vector3 destination;

		
		[NonSerialized] public bool isInitialized = false;
		[NonSerialized] public bool isValid = false;

		[NonSerialized] public Plane plane;
		[NonSerialized] public Bounds bounds;

		[NonSerialized] public float leftExtentLength;
		[NonSerialized] public float rightExtentLength;
		
		[NonSerialized] public float arcLength;
		[NonSerialized] public float totalLength;

		[NonSerialized] public float radius;
		[NonSerialized] public float sweepAngle;

		[NonSerialized] public Vector3 center;

		[NonSerialized] public Vector3 leftExtent;
		[NonSerialized] public Vector3 rightExtent;

		[NonSerialized] public Vector3 center2leftExtent;
		[NonSerialized] public Vector3 center2rightExtent;



		/// <summary>
		/// Constructor
		/// </summary>
		public Arc ()
		{
			
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="origin">Origin</param>
		/// <param name="tangent">Tangent</param>
		/// <param name="destination">Destination</param>
		public Arc (Vector3 origin, Vector3 tangent, Vector3 destination)
		{
			this.origin = origin;
			this.tangent = tangent;
			this.destination = destination;

			this.Initialize(true);
		}

		/// <summary>
		/// Initializes arc with provided points.
		/// </summary>
		/// <param name="origin">Origin</param>
		/// <param name="tangent">Tangent</param>
		/// <param name="destination">Destination</param>
		/// <param name="forceInitialize">if set true, initialization will be forced even if provided points are equal to local parameters.</param>
		public void Initialize (Vector3 origin, Vector3 tangent, Vector3 destination, bool forceInitialize = false)
		{
			forceInitialize = this.origin != origin ||
				this.tangent != tangent ||
				this.destination != destination ||
				forceInitialize == true;

			this.origin = origin;
			this.tangent = tangent;
			this.destination = destination;

			this.Initialize(forceInitialize);
		}

		/// <summary>
		/// Initializes arc
		/// </summary>
		/// <param name="forceInitialize">If true, it will force initialization with local parameters</param>
		public void Initialize (bool forceInitialize = false)
		{
			if (this.isInitialized == true && forceInitialize == false) return;
			this.isInitialized = true;

			// Construction of plane that will be used in later, for example:
			// - this.GetDistanceAtPoint(...)
			// - this.GetPerpendicular(...)
			// - this.GetClosestPointOnArc(...)
			this.plane = new Plane(this.origin, this.tangent, this.destination);

			// Extents, circle center and it's radius
			{
				// Find perpendiculars, that will be used to find center of circle
				Vector3 leftPerpendicular = Vector3.Cross(this.tangent - this.origin, this.plane.normal).normalized;
				Vector3 rightPerpendicular = Vector3.Cross(this.tangent - this.destination, this.plane.normal).normalized;

				// ...
				float leftDistance = Vector3.Distance(origin, tangent);
				float rightDistance = Vector3.Distance(destination, tangent);

				// Find maximum possible, equal distance, from each side.
				float length = Mathf.Min(leftDistance, rightDistance);

				// Position of start and end of arc
				this.leftExtent = Vector3.Lerp(this.tangent, this.origin, length / leftDistance);
				this.rightExtent = Vector3.Lerp(this.tangent, this.destination, length / rightDistance);

				// Perform intersection test, from each extent in direction perpendicular to it's side.
				this.isValid = Utility.LineLineIntersection(
					this.leftExtent, leftPerpendicular,
					this.rightExtent, rightPerpendicular,
					out this.center
				);

				// ...
				this.radius = Vector3.Distance(this.center, this.leftExtent);
			}

			// Additional information
			{
				this.leftExtentLength = Vector3.Distance(this.leftExtent, this.origin);
				this.rightExtentLength = Vector3.Distance(this.rightExtent, this.destination);

				if (this.isValid)
				{
					this.center2leftExtent = (this.leftExtent - this.center).normalized;
					this.center2rightExtent = (this.rightExtent - this.center).normalized;

					this.sweepAngle = Vector3.Angle(this.center2leftExtent, this.center2rightExtent);
					this.arcLength = this.radius * (Mathf.Deg2Rad * Mathf.Abs(this.sweepAngle));
					this.totalLength = this.arcLength + this.leftExtentLength + this.rightExtentLength;
				}
				else
				{
					// Because we were unable to construct and Arc, we have a straight
					// line from origin to destination. This line will be divided
					// into three parts, two extents and fake arc. Imagine you have
					// <see cref="Sample" /> which will sit at middle of the arc,
					// and in case, when it's a straight line it will be at the middle
					// of the line between origin and destination.
					this.totalLength = Vector3.Distance(this.origin, this.destination);

					this.center2leftExtent = Vector3.zero;
					this.center2rightExtent = Vector3.zero;

					this.radius = 0.0f;
					this.center = Vector3.zero;

					float oneThirdDistance = this.totalLength / 3.0f;
					float oneThirdAlpha = 1.0f / oneThirdDistance;
					
					this.leftExtent = Vector3.Lerp(this.origin, this.destination, oneThirdAlpha);
					this.rightExtent = Vector3.Lerp(this.destination, this.origin, oneThirdAlpha);
					
					this.leftExtentLength = oneThirdDistance;
					this.rightExtentLength = oneThirdDistance;

					this.sweepAngle = 0.0f;
					this.arcLength = oneThirdDistance;
				}
			}

			this.ComputeBounds();
		}

		/// <summary>
		/// Computes bounds for arc.
		/// </summary>
		/// <param name="countSegments">Count segments</param>
		public void ComputeBounds (int countSegments = 32)
		{
			this.bounds = new Bounds();
			
			float distanceOnArc = 0.0f;
			float delta = this.totalLength / (float)(countSegments - 1);
			
			for (int n = 0; n < countSegments; n++)
			{
				this.bounds.Encapsulate(this.GetPoint(distanceOnArc));
				distanceOnArc += delta;
			}
		}

		/// <summary>
		/// Returns point on arc for a given distance on arc.
		/// </summary>
		/// <param name="distanceOnArc">Distance on arc</param>
		/// <returns><see cref="Vector3" /> Point on arc</returns>
		public Vector3 GetPoint (float distanceOnArc)
		{
			if (this.isValid == false)
			{
				return Vector3.Lerp(this.origin, this.destination, distanceOnArc / this.totalLength);
			}

			if (distanceOnArc < this.leftExtentLength)
			{
				// Between origin and left extent
				float t = distanceOnArc / this.leftExtentLength;
				return Vector3.Lerp(this.origin, this.leftExtent, t);
			}
			else if (distanceOnArc > this.totalLength - this.rightExtentLength)
			{
				// Between right extent and destination
				float t = (distanceOnArc - (this.leftExtentLength + this.arcLength)) / this.rightExtentLength;
				return Vector3.Lerp(this.rightExtent, this.destination, t);
			}
			else
			{
				// Between start and end of arc
				float at = (distanceOnArc - this.leftExtentLength) / this.arcLength;
				return this.center + Vector3.Slerp(this.center2leftExtent, this.center2rightExtent, at) * this.radius;
			}
		}

		/// <summary>
		/// Returns distance on arc for a given point on arc.
		/// </summary>
		/// <param name="pointOnArc">Point on arc</param>
		/// <returns><see cref="float" /> Distance on arc</returns>
		public float GetDistanceAtPoint (Vector3 pointOnArc)
		{
			if (this.isValid == false)
			{
				return Vector3.Distance(this.origin, pointOnArc);
			}

			Vector3 centerToLeft = (this.center - this.leftExtent).normalized;
			Vector3 centerToRight = (this.center - this.rightExtent).normalized;
			Vector3 centerToTarget = (this.center - pointOnArc).normalized;
			Vector3 centerToMiddle = (this.center - this.tangent).normalized;

			float leftToTarget = Vector3.SignedAngle(centerToLeft, centerToTarget, this.plane.normal);
			float rightToTarget = Vector3.SignedAngle(centerToRight, centerToTarget, this.plane.normal);
			float leftToMiddle = Vector3.SignedAngle(centerToLeft, centerToMiddle, this.plane.normal);

			if ((leftToMiddle >= 0 && leftToTarget > 0.0f && rightToTarget < 0.0f) ||
				(leftToMiddle <= 0 && leftToTarget < 0.0f && rightToTarget > 0.0f))
			{
				float t = Mathf.Abs(leftToTarget) / (Mathf.Abs(leftToMiddle) * 2.0f);
				return this.leftExtentLength + (t * this.arcLength);
			}
			else
			{
				Vector3 origin2middle = (this.origin - this.tangent).normalized;
				Vector3 origin2point = (pointOnArc - this.tangent).normalized;
				if (Vector3.Dot(origin2middle, origin2point) > 0.999f)
				{
					return Vector3.Distance(this.origin, pointOnArc);
				}
				else
				{
					return (this.leftExtentLength + this.arcLength + Vector3.Distance(this.rightExtent, pointOnArc));
				}
			}
		}

		/// <summary>
		/// Returns direction, perpendicular to normal plane of arc at point and distance on arc.
		/// </summary>
		/// <param name="pointOnArc">Point on arc</param>
		/// <param name="distanceOnArc">Distance on arc</param>
		/// <returns><see cref="Vector3" /> Perpendicular to normal plane on arc</returns>
		public Vector3 GetPerpendicular (Vector3 pointOnArc, float distanceOnArc)
		{
			if (this.isValid == false)
			{
				Vector3 direction = (this.destination - this.origin).normalized;
				Vector3 crossProduct = Vector3.Cross(direction, Vector3.up).normalized;
				if (crossProduct == Vector3.zero)
				{
					return Vector3.Cross(direction, Vector3.right).normalized;
				}
				return crossProduct;
			}
			else
			{
				this.GetTangent(pointOnArc, distanceOnArc, out Vector3 leftTangent, out Vector3 rightTangent);
				Vector3 direction = (rightTangent - leftTangent).normalized;
				return Vector3.Cross(direction, this.plane.normal).normalized;
			}
		}

		/// <summary>
		/// Returns direction along the path at given point and distance on arc.
		/// </summary>
		/// <param name="pointOnArc">Point on arc</param>
		/// <param name="distanceOnArc">Distance on arc</param>
		/// <returns><see cref="Vector3" /> Direction on arc</returns>
		public Vector3 GetDirection (Vector3 pointOnArc, float distanceOnArc)
		{
			if (this.isValid == false)
			{
				return (this.destination - this.origin).normalized;
			}
			else
			{
				this.GetTangent(pointOnArc, distanceOnArc, out Vector3 leftTangent, out Vector3 rightTangent);
				return (rightTangent - leftTangent).normalized;
			}
		}

		/// <summary>
		/// Returns point on arc, that is closest to a given point on plane.
		/// </summary>
		/// <param name="pointOnPlane">Point on plane of this arc</param>
		/// <returns><see cref="Vector3" /> Point on arc</returns>
		public Vector3 GetNearestPointOnPlane (Vector3 pointOnPlane)
		{
			if (this.isValid == false)
			{
				return Utility.GetClosestPointOnFiniteLine(pointOnPlane, this.origin, this.destination);
			}

			Vector3 left = Utility.GetClosestPointOnFiniteLine(pointOnPlane, this.origin, this.leftExtent);
			Vector3 right = Utility.GetClosestPointOnFiniteLine(pointOnPlane, this.rightExtent, this.destination);

			float leftDistance = Vector3.Distance(left, pointOnPlane);
			float rightDistance = Vector3.Distance(right, pointOnPlane);

			if (this.GetClosestPointOnArc(pointOnPlane, out Vector3 arc) == true)
			{
				float arcDistance = Vector3.Distance(arc, pointOnPlane);
				return Utility.Min(left, right, arc, leftDistance, rightDistance, arcDistance);
			}

			return (leftDistance < rightDistance) ? left : right;
		}

		/// <summary>
		/// Finds closest point on arc itself, which is closest to a given point on plane.
		/// </summary>
		/// <param name="pointOnPlane">Point on plane of this arc</param>
		/// <param name="pointOnArc">Point on arc</param>
		/// <returns>If returns true, method found point on arc for a given point on plane.</returns>
		public bool GetClosestPointOnArc (Vector3 pointOnPlane, out Vector3 pointOnArc)
		{
			pointOnArc = default;
			bool bInside = false;

			Vector3 direction = (pointOnPlane - this.center).normalized;
			Vector3 pointOnCircle = this.center + direction * this.radius;

			float a = Vector3.SignedAngle(this.center2leftExtent, this.center2rightExtent, this.plane.normal);
			float b = Vector3.SignedAngle(this.center2rightExtent, direction, this.plane.normal);

			if (b < 0 && b > -a || b > 0 && b < -a)
			{
				bInside = true;
				pointOnArc = pointOnCircle;
			}

			return bInside;
		}

		/// <summary>
		/// Calculates tangent points at specified distance on arc. Note, that calculated tangents are positions and not directions.
		/// </summary>
		/// <param name="pointOnArc">Position on arc</param>
		/// <param name="distanceOnArc">Distance on arc</param>
		/// <param name="leftTangent">Position of left tangent</param>
		/// <param name="rightTangent">Position of right tangent</param>
		public void GetTangent (Vector3 pointOnArc, float distanceOnArc, out Vector3 leftTangent, out Vector3 rightTangent)
		{
			if (this.isValid == false)
			{
				leftTangent = this.origin;
				rightTangent = this.destination;
			}
			else if (distanceOnArc < this.leftExtentLength)
			{
				// Before left extent
				leftTangent = this.origin;
				rightTangent = this.tangent;
			}
			else if (distanceOnArc < this.leftExtentLength + this.arcLength)
			{
				// First, find two middle points between extents and point on arc.
				leftTangent = Vector3.Lerp(this.leftExtent, pointOnArc, 0.5f);
				rightTangent = Vector3.Lerp(this.rightExtent, pointOnArc, 0.5f);
				
				// Define two lines against which we will do an intersection
				Vector3 origin2tangent = (this.tangent - this.origin).normalized;
				Vector3 tangent2destination = (this.destination - this.tangent).normalized;

				// Define two directions from center to each middle point
				Vector3 center2left = (leftTangent - this.center).normalized;
				Vector3 center2right = (rightTangent - this.center).normalized;

				// Result of intersections are tangents
				Utility.LineLineIntersection(this.center, center2left, this.origin, origin2tangent, out leftTangent);
				Utility.LineLineIntersection(this.center, center2right, this.tangent, tangent2destination, out rightTangent);
			}
			else
			{
				// After right extent
				leftTangent = this.tangent;
				rightTangent = this.destination;
			}
		}

		/// <summary>
		/// Splits arc into two at specified distance on arc.
		/// </summary>
		/// <param name="distanceOnArc">Distance on arc, where split will occur.</param>
		/// <param name="left">Arc before specified distance</param>
		/// <param name="right">Arc after specified distance</param>
		public void Split (float distanceOnArc, out Arc left, out Arc right)
		{
			Vector3 pointOnArc = this.GetPoint(distanceOnArc);

			this.GetTangent(pointOnArc, distanceOnArc, out Vector3 leftTangent, out Vector3 rightTangent);
			
			left = new Arc(this.origin, leftTangent, pointOnArc);
			right = new Arc(pointOnArc, rightTangent, this.destination);
		}



	}



}