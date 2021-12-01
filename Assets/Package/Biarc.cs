namespace Circular
{
	using System;
	using UnityEngine;



	[Serializable]
	public class Biarc
	{
		public Arc leftArc = new Arc();
		public Arc rightArc = new Arc();

		public int identifier;

		public Vector3 origin
		{
			get { return this.leftArc.origin; }
			set { this.leftArc.origin = value; }
		}

		public Vector3 leftTangent
		{
			get { return this.leftArc.tangent; }
			set { this.leftArc.tangent = value; }
		}

		public Vector3 leftMidpoint
		{
			get { return this.leftArc.destination; }
			set { this.leftArc.destination = value; }
		}

		public Vector3 rightMidpoint
		{
			get { return this.rightArc.origin; }
			set { this.rightArc.origin = value; }
		}

		public Vector3 rightTangent
		{
			get { return this.rightArc.tangent; }
			set { this.rightArc.tangent = value; }
		}

		public Vector3 destination
		{
			get { return this.rightArc.destination; }
			set { this.rightArc.destination = value; }
		}

		public MidpointBehaviour leftMidpointBehaviour = MidpointBehaviour.auto;
		public MidpointBehaviour rightMidpointBehaviour = MidpointBehaviour.auto;
		public float leftMidpointOffset;
		public float rightMidpointOffset;

		[NonSerialized] public bool isInitialized = false;

		[NonSerialized] public float midpointsLength;
		[NonSerialized] public float totalLength;

		[NonSerialized] public Bounds bounds;



		/// <summary>
		/// Constructor
		/// </summary>
		public Biarc ()
		{

		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="origin">Origin</param>
		/// <param name="leftTangent">Left tangent</param>
		/// <param name="leftMidpoint">Left midpoint</param>
		/// <param name="rightMidpoint">Right midpoint</param>
		/// <param name="rightTangent">Right tangent</param>
		/// <param name="destination">Destination</param>
		public Biarc (Vector3 origin, Vector3 leftTangent, Vector3 leftMidpoint, Vector3 rightMidpoint, Vector3 rightTangent, Vector3 destination)
		{
			this.origin = origin;
			this.leftTangent = leftTangent;
			this.leftMidpoint = leftMidpoint;
			this.rightMidpoint = rightMidpoint;
			this.rightTangent = rightTangent;
			this.destination = destination;

			this.AdjustMidpoints();
			this.Initialize(true);
		}

		/// <summary>
		/// Initializes Biarc with given points
		/// </summary>
		/// <param name="origin">Origin</param>
		/// <param name="leftTangent">Left tangent</param>
		/// <param name="leftMidpoint">Left midpoint</param>
		/// <param name="rightMidpoint">Right midpoint</param>
		/// <param name="rightTangent">Right tangent</param>
		/// <param name="destination">Destination</param>
		public void Initialize (Vector3 origin, Vector3 leftTangent, Vector3 leftMidpoint, Vector3 rightMidpoint, Vector3 rightTangent, Vector3 destination)
		{
			this.origin = origin;
			this.leftTangent = leftTangent;
			this.leftMidpoint = leftMidpoint;
			this.rightMidpoint = rightMidpoint;
			this.rightTangent = rightTangent;
			this.destination = destination;

			this.AdjustMidpoints();
			this.Initialize(true);
		}

		/// <summary>
		/// Initializes Biarc
		/// </summary>
		/// <param name="forceInitialize">If true, it will force initialization with local parameters</param>
		public void Initialize (bool forceInitialize = false)
		{
			if (this.isInitialized == true && forceInitialize == false) return;
			this.isInitialized = true;

			this.leftArc.Initialize(true);
			this.rightArc.Initialize(true);

			this.midpointsLength = Vector3.Distance(this.leftMidpoint, this.rightMidpoint);
			this.totalLength = this.leftArc.totalLength + this.midpointsLength + this.rightArc.totalLength;

			this.bounds = new Bounds();
			this.bounds.Encapsulate(this.leftArc.bounds);
			this.bounds.Encapsulate(this.rightArc.bounds);
		}

		/// <summary>
		/// Adjusts position of midpoints, so that they lie between tangents and do not cross each other.
		/// </summary>
		public void AdjustMidpoints ()
		{
			Vector3 direction = (this.rightTangent - this.leftTangent).normalized;
			float distance = Vector3.Distance(this.leftTangent, this.rightTangent);
			float middle = distance / 2.0f;

			float leftOffset = 0.0f;
			float rightOffset = 0.0f;
			
			// Left midpoint
			if (this.leftMidpointBehaviour == MidpointBehaviour.stayAtMiddle)
			{
				leftOffset = middle;
			}
			else if (this.leftMidpointBehaviour == MidpointBehaviour.offsetFromTangent)
			{
				leftOffset = this.leftMidpointOffset;
			}
			else if (this.leftMidpointBehaviour == MidpointBehaviour.offsetFromMiddle)
			{
				leftOffset = middle - this.leftMidpointOffset;
			}
			else if (this.leftMidpointBehaviour == MidpointBehaviour.auto)
			{
				leftOffset = Vector3.Distance(this.leftTangent, this.leftMidpoint);
			}
			
			// Right midpoint
			if (this.rightMidpointBehaviour == MidpointBehaviour.stayAtMiddle)
			{
				rightOffset = middle;
			}
			else if (this.rightMidpointBehaviour == MidpointBehaviour.offsetFromTangent)
			{
				rightOffset = this.rightMidpointOffset;
			}
			else if (this.rightMidpointBehaviour == MidpointBehaviour.offsetFromMiddle)
			{
				rightOffset = middle - this.rightMidpointOffset;
			}
			else if (this.rightMidpointBehaviour == MidpointBehaviour.auto)
			{
				rightOffset = Vector3.Distance(this.rightTangent, this.rightMidpoint);
			}

			// Clamp negative values
			if (leftOffset < 0.0f) leftOffset = 0.0f;
			if (rightOffset < 0.0f) rightOffset = 0.0f;

			// Preserve ratio when two midpoints collide
			if (leftOffset + rightOffset > distance)
			{
				float max = leftOffset + rightOffset;
				leftOffset = (leftOffset / max) * distance;
				rightOffset = (rightOffset / max) * distance;
			}

			// ...
			this.leftMidpoint = this.leftTangent + (direction * leftOffset);
			this.rightMidpoint = this.rightTangent - (direction * rightOffset);
		}

		/// <summary>
		/// Returns point on Biarc for a given distance.
		/// </summary>
		/// <param name="distanceOnBiarc">Distance on biarc</param>
		/// <returns><see cref="Vector3" /> Point on Biarc</returns>
		public Vector3 GetPoint (float distanceOnBiarc)
		{
			if (distanceOnBiarc <= this.leftArc.totalLength)
			{
				return this.leftArc.GetPoint(distanceOnBiarc);
			}
			else if (distanceOnBiarc < this.leftArc.totalLength + this.midpointsLength)
			{
				float t = (distanceOnBiarc - this.leftArc.totalLength) / this.midpointsLength;
				return Vector3.Lerp(this.leftArc.destination, this.rightArc.origin, t);
			}
			else
			{
				return this.rightArc.GetPoint(distanceOnBiarc - this.leftArc.totalLength - this.midpointsLength);
			}
		}

		/// <summary>
		/// Returns point on biarc, that is closest to a given point on plane.
		/// </summary>
		/// <param name="pointOnPlane">Point on plane</param>
		/// <param name="pointOnBiarc">Point on Biarc</param>
		/// <param name="distanceOnBiarc">Distance on Biarc</param>
		public void GetNearestPoint (Vector3 pointOnPlane, out Vector3 pointOnBiarc, out float distanceOnBiarc)
		{
			Vector3 leftPointOnArc = this.leftArc.GetNearestPointOnPlane(pointOnPlane);
			Vector3 rightPointOnArc = this.rightArc.GetNearestPointOnPlane(pointOnPlane);

			Vector3 pointOnMidline = Utility.GetClosestPointOnFiniteLine(pointOnPlane, this.leftMidpoint, this.rightMidpoint);

			float left = Vector3.Distance(pointOnPlane, leftPointOnArc);
			float right = Vector3.Distance(pointOnPlane, rightPointOnArc);
			float middle = Vector3.Distance(pointOnPlane, pointOnMidline);

			if (left < right)
			{
				if (left < middle)
				{
					pointOnBiarc = leftPointOnArc;
					distanceOnBiarc = this.leftArc.GetDistanceAtPoint(leftPointOnArc);
				}
				else
				{
					pointOnBiarc = pointOnMidline;
					distanceOnBiarc = this.leftArc.totalLength + Vector3.Distance(this.leftMidpoint, pointOnMidline);
				}
			}
			else
			{
				if (right < middle)
				{
					pointOnBiarc = rightPointOnArc;
					distanceOnBiarc = this.leftArc.totalLength + this.midpointsLength + this.rightArc.GetDistanceAtPoint(rightPointOnArc);
				}
				else
				{
					pointOnBiarc = pointOnMidline;
					distanceOnBiarc = this.leftArc.totalLength + Vector3.Distance(this.leftMidpoint, pointOnMidline);
				}
			}
		}

		/// <summary>
		/// Splits Biarc into two at specified distance on Biarc.
		/// </summary>
		/// <param name="pointOnBiarc">Point on Biarc</param>
		/// <param name="distanceOnBiarc">Distance on Biarc</param>
		/// <param name="leftBiarc">Left Biarc</param>
		/// <param name="rightBiarc">Right Biarc</param>
		public void Split (Vector3 pointOnBiarc, float distanceOnBiarc, out Biarc leftBiarc, out Biarc rightBiarc)
		{
			if (distanceOnBiarc < this.leftArc.totalLength)
			{
				this.leftArc.Split(distanceOnBiarc, out Arc beforePointOnBiarc, out Arc afterPointOnBiarc);
				beforePointOnBiarc.Split(beforePointOnBiarc.totalLength / 2.0f, out Arc firstLeft, out Arc firstRight);
				
				leftBiarc = new Biarc();
				leftBiarc.Initialize(
					firstLeft.origin,
					firstLeft.tangent,
					firstLeft.destination,
					firstRight.origin,
					firstRight.tangent,
					firstRight.destination
				);

				rightBiarc = new Biarc();
				rightBiarc.Initialize(
					afterPointOnBiarc.origin,
					afterPointOnBiarc.tangent,
					afterPointOnBiarc.destination,
					this.rightArc.origin,
					this.rightArc.tangent,
					this.rightArc.destination
				);
			}
			else if (distanceOnBiarc >= this.leftArc.totalLength + this.midpointsLength)
			{
				float distanceOnArc = (distanceOnBiarc - this.leftArc.totalLength - this.midpointsLength);

				this.rightArc.Split(distanceOnArc, out Arc beforePointOnBiarc, out Arc afterPointOnBiarc);
				afterPointOnBiarc.Split(afterPointOnBiarc.totalLength / 2.0f, out Arc firstLeft, out Arc firstRight);

				leftBiarc = new Biarc();
				leftBiarc.Initialize(
					this.leftArc.origin,
					this.leftArc.tangent,
					this.leftArc.destination,
					this.rightArc.origin,
					beforePointOnBiarc.tangent,
					pointOnBiarc
				);

				rightBiarc = new Biarc();
				rightBiarc.Initialize(
					firstLeft.origin,
					firstLeft.tangent,
					firstLeft.destination,
					firstRight.origin,
					firstRight.tangent,
					firstRight.destination
				);
			}
			else // if (bOnMidline == true)
			{
				leftBiarc = new Biarc();
				leftBiarc.Initialize(
					this.leftArc.origin,
					this.leftArc.tangent,
					this.leftArc.destination,
					Vector3.Lerp(pointOnBiarc, this.leftArc.destination, 0.5f),
					Vector3.Lerp(pointOnBiarc, this.leftArc.destination, 0.25f),
					pointOnBiarc
				);

				rightBiarc = new Biarc();
				rightBiarc.Initialize(
					pointOnBiarc,
					Vector3.Lerp(pointOnBiarc, this.rightArc.origin, 0.25f),
					Vector3.Lerp(pointOnBiarc, this.rightArc.origin, 0.5f),
					this.rightArc.origin,
					this.rightArc.tangent,
					this.rightArc.destination
				);
			}
		}

		/// <summary>
		/// Calculates tangent points at specified distance on biarc. Note, that calculated tangents are positions and not directions.
		/// </summary>
		/// <param name="pointOnBiarc">Point on Biarc</param>
		/// <param name="distanceOnBiarc">Distance on Biarc</param>
		/// <param name="leftTangent">Left tangent</param>
		/// <param name="rightTangent">Right tangent</param>
		public void GetTangent (Vector3 pointOnBiarc, float distanceOnBiarc, out Vector3 leftTangent, out Vector3 rightTangent)
		{
			if (distanceOnBiarc <= this.leftArc.totalLength)
			{
				this.leftArc.GetTangent(pointOnBiarc, distanceOnBiarc, out leftTangent, out rightTangent);
			}
			else if (distanceOnBiarc < this.leftArc.totalLength + this.midpointsLength)
			{
				leftTangent = this.leftTangent;
				rightTangent = this.rightTangent;
			}
			else
			{
				float distanceOnArc = distanceOnBiarc - (this.leftArc.totalLength + this.midpointsLength);
				this.rightArc.GetTangent(pointOnBiarc, distanceOnArc, out leftTangent, out rightTangent);
			}
		}

		/// <summary>
		/// Returns direction along the path at given point and distance on biarc.
		/// </summary>
		/// <param name="pointOnBiarc">Point on Biarc</param>
		/// <param name="distanceOnBiarc">Distance on Biarc</param>
		/// <returns><see cref="Vector3" /> Direction on Biarc</returns>
		public Vector3 GetDirection (Vector3 pointOnBiarc, float distanceOnBiarc)
		{
			if (distanceOnBiarc <= this.leftArc.totalLength)
			{
				return this.leftArc.GetDirection(pointOnBiarc, distanceOnBiarc);
			}
			else if (distanceOnBiarc < this.leftArc.totalLength + this.midpointsLength)
			{
				return (this.rightTangent - this.leftTangent).normalized;
			}
			else
			{
				float distanceOnArc = distanceOnBiarc - (this.leftArc.totalLength + this.midpointsLength);
				return this.rightArc.GetDirection(pointOnBiarc, distanceOnArc);
			}
		}

		/// <summary>
		/// Returns direction, perpendicular to normal plane of Biarc at point
		/// and distance on Biarc. Depending on where point is (left or right
		/// arc), it will use different normal plane and thus will have
		/// different perpendicular.
		/// </summary>
		/// <param name="pointOnBiarc">Point on Biarc</param>
		/// <param name="distanceOnBiarc">Distance on Biarc</param>
		/// <returns><see cref="Vector3" /> Perpendicular to normal plane of one of the Arc, depending on distanceOnBiarc</returns>
		public Vector3 GetPerpendicular (Vector3 pointOnBiarc, float distanceOnBiarc)
		{
			this.GetTangent(pointOnBiarc, distanceOnBiarc, out Vector3 leftTangent, out Vector3 rightTangent);

			Vector3 direction = (rightTangent - leftTangent).normalized;
			if (distanceOnBiarc <= this.leftArc.totalLength + this.midpointsLength)
			{
				return Vector3.Cross(direction, this.leftArc.plane.normal).normalized;
			}
			else
			{
				return Vector3.Cross(direction, this.rightArc.plane.normal).normalized;
			}
		}

		/// <summary>
		/// Returns distance for provided AnchorPosition
		/// </summary>
		/// <param name="position">Anchor position</param>
		/// <param name="manualDistanceOnBiarc">Fallback/AnchorPosition.manual distance on Biarc</param>
		/// <returns><see cref="float" /> Distance on Biarc</returns>
		public float GetDistance (AnchorPosition position, float manualDistanceOnBiarc)
		{
			if (position == AnchorPosition.origin)
			{
				return 0.0f;
			}
			else if (position == AnchorPosition.firstLeftExtent)
			{
				return this.leftArc.leftExtentLength;
			}
			else if (position == AnchorPosition.leftTangent)
			{
				return this.leftArc.leftExtentLength + (this.leftArc.arcLength / 2.0f);
			}
			else if (position == AnchorPosition.firstRightExtent)
			{
				return this.leftArc.totalLength - this.leftArc.rightExtentLength;
			}
			else if (position == AnchorPosition.leftMidpoint)
			{
				return this.leftArc.totalLength;
			}
			else if (position == AnchorPosition.middleOfBiarc)
			{
				return this.leftArc.totalLength + this.midpointsLength / 2.0f;
			}
			else if (position == AnchorPosition.rightMidpoint)
			{
				return this.leftArc.totalLength + this.midpointsLength;
			}
			else if (position == AnchorPosition.secondLeftExtent)
			{
				return this.leftArc.totalLength + this.midpointsLength + this.rightArc.leftExtentLength;
			}
			else if (position == AnchorPosition.rightTangent)
			{
				return this.totalLength - this.rightArc.rightExtentLength - (this.rightArc.arcLength / 2.0f);
			}
			else if (position == AnchorPosition.secondRightExtent)
			{
				return this.totalLength - this.rightArc.rightExtentLength;
			}
			else if (position == AnchorPosition.destination)
			{
				return this.totalLength;
			}
			else// (position == AnchorPosition.manual)
			{
				return manualDistanceOnBiarc;
			}
		}

		/// <summary>
		/// Returns point on Biarc for provided AnchorPosition
		/// </summary>
		/// <param name="position">Anchor position</param>
		/// <param name="manualDistanceOnBiarc">Fallback/AnchorPosition.manual distance on Biarc</param>
		/// <returns><see cref="Vector3" /> Point on Biarc</returns>
		public Vector3 GetPoint (AnchorPosition position, float manualDistanceOnBiarc)
		{
			if (position == AnchorPosition.origin)
			{
				return this.origin;
			}
			else if (position == AnchorPosition.firstLeftExtent)
			{
				return this.leftArc.leftExtent;
			}
			else if (position == AnchorPosition.leftTangent)
			{
				return this.leftArc.GetPoint(this.leftArc.leftExtentLength + this.leftArc.arcLength / 2.0f);
			}
			else if (position == AnchorPosition.firstRightExtent)
			{
				return this.leftArc.rightExtent;
			}
			else if (position == AnchorPosition.leftMidpoint)
			{
				return this.leftMidpoint;
			}
			else if (position == AnchorPosition.middleOfBiarc)
			{
				return Vector3.Lerp(this.leftMidpoint, this.rightMidpoint, 0.5f);
			}
			else if (position == AnchorPosition.rightMidpoint)
			{
				return this.rightMidpoint;
			}
			else if (position == AnchorPosition.secondLeftExtent)
			{
				return this.rightArc.leftExtent;
			}
			else if (position == AnchorPosition.rightTangent)
			{
				return this.rightArc.GetPoint(this.rightArc.leftExtentLength + this.rightArc.arcLength / 2.0f);
			}
			else if (position == AnchorPosition.secondRightExtent)
			{
				return this.rightArc.rightExtent;
			}
			else if (position == AnchorPosition.destination)
			{
				return this.destination;
			}
			else
			{
				return this.GetPoint(manualDistanceOnBiarc);
			}
		}



	}



}