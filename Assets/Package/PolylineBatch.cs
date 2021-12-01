namespace Circular
{
	using UnityEngine;
	using UnityEditor;



	/// <summary>
	/// Used for drawing one-color lines in batches.
	/// </summary>
	public class PolylineBatch
	{
		public Vector3[] vertices;
		public float lineWidth = 2.0f;
		public int index = 0;

		public Vector3 last;



		enum DistanceOnBiarc
		{
			origin,
			firstLeftExtent,
			leftArc,
			firstRightExtent,
			leftMidpoint,
			rightMidpoint,
			secondLeftExtent,
			rightArc,
			secondRightExtent,
			destination,
			stop
		}



		/// <summary>
		/// Ensures, that local array is initialized and is of a specified size.
		/// </summary>
		/// <param name="batchSize">Size of the array/batch</param>
		public void EnsureSize (int batchSize)
		{
			if (this.vertices == null || this.vertices.Length != batchSize)
			{
				this.vertices = new Vector3[batchSize];
			}
		}

		/// <summary>
		/// Adds vertex to batch. Triggers rendering, when batch is full.
		/// </summary>
		/// <param name="point">Position</param>
		public void AddPoint (Vector3 point)
		{
			if (this.index != 0 && this.last == point) return;
			this.vertices[this.index] = point;
			this.last = point;
			this.index++;

			if (this.index == this.vertices.Length) this.Render();
		}

		/// <summary>
		/// Adds <see cref="Biarc" /> to batch as a set of vertices. Triggers rendering, when batch is full.
		/// </summary>
		/// <param name="biarc">Target biarc</param>
		/// <param name="transform">Transform, that will be used to transform vertices from local space to world space.</param>
		/// <param name="countSegmentsPerArc">Resolution per arc</param>
		public void AddBiarc (Biarc biarc, Transform transform, int countSegmentsPerArc)
		{
			float distanceOnBiarc = 0.0f;
			DistanceOnBiarc position = DistanceOnBiarc.origin;

			while (true)
			{
				Vector3 point = biarc.GetPoint(distanceOnBiarc);
				point = transform.TransformPoint(point);

				this.AddPoint(point);
				this.GetAlpha(biarc, ref position, ref distanceOnBiarc, countSegmentsPerArc);

				if (position == DistanceOnBiarc.stop)break;
			}
		}

		/// <summary>
		/// Calculates next point on Biarc or sends DistanceOnBiarc.stop, when end is reached.
		/// </summary>
		/// <param name="biarc">Target biarc</param>
		/// <param name="position">Current step</param>
		/// <param name="distanceOnBiarc">Current distance on biarc</param>
		/// <param name="countSegmentsPerArc">Resolution per arc</param>
		void GetAlpha (Biarc biarc, ref DistanceOnBiarc position, ref float distanceOnBiarc, int countSegmentsPerArc)
		{
			if (position == DistanceOnBiarc.origin)
			{
				position = DistanceOnBiarc.firstLeftExtent;
				distanceOnBiarc = biarc.leftArc.leftExtentLength;
			}
			else if (position == DistanceOnBiarc.firstLeftExtent)
			{
				position = DistanceOnBiarc.leftArc;
				distanceOnBiarc = biarc.leftArc.leftExtentLength;
			}
			else if (position == DistanceOnBiarc.leftArc)
			{
				if (biarc.leftArc.isValid == false)
				{
					distanceOnBiarc = biarc.leftArc.leftExtentLength + biarc.leftArc.arcLength;
					position = DistanceOnBiarc.firstRightExtent;
					return;
				}
				float delta = biarc.leftArc.arcLength / (float)(countSegmentsPerArc);
				distanceOnBiarc += delta;

				if (distanceOnBiarc > biarc.leftArc.leftExtentLength + biarc.leftArc.arcLength)
				{
					distanceOnBiarc = biarc.leftArc.leftExtentLength + biarc.leftArc.arcLength;
					position = DistanceOnBiarc.firstRightExtent;
				}
			}
			else if (position == DistanceOnBiarc.firstRightExtent)
			{
				position = DistanceOnBiarc.leftMidpoint;
				distanceOnBiarc = biarc.leftArc.totalLength;
			}
			else if (position == DistanceOnBiarc.leftMidpoint)
			{
				position = DistanceOnBiarc.rightMidpoint;
				distanceOnBiarc = biarc.leftArc.totalLength + biarc.midpointsLength;
			}
			else if (position == DistanceOnBiarc.rightMidpoint)
			{
				position = DistanceOnBiarc.secondLeftExtent;
				distanceOnBiarc = biarc.leftArc.totalLength + biarc.midpointsLength;
			}
			else if (position == DistanceOnBiarc.secondLeftExtent)
			{
				position = DistanceOnBiarc.rightArc;
				distanceOnBiarc = biarc.leftArc.totalLength + biarc.midpointsLength + biarc.rightArc.leftExtentLength;
			}
			else if (position == DistanceOnBiarc.rightArc)
			{
				if (biarc.rightArc.isValid == false)
				{
					distanceOnBiarc = biarc.totalLength - biarc.rightArc.rightExtentLength;
					position = DistanceOnBiarc.secondRightExtent;
					return;
				}

				float delta = biarc.rightArc.arcLength / (float)(countSegmentsPerArc);
				distanceOnBiarc += delta;
				float total = biarc.totalLength - biarc.rightArc.rightExtentLength;
				if (distanceOnBiarc > total)
				{
					distanceOnBiarc = total;
					position = DistanceOnBiarc.secondRightExtent;
				}
			}
			else if (position == DistanceOnBiarc.secondRightExtent)
			{
				position = DistanceOnBiarc.destination;
				distanceOnBiarc = biarc.totalLength;
			}
			else if (position == DistanceOnBiarc.destination)
			{
				position = DistanceOnBiarc.stop;
			}
			
		}

		/// <summary>
		/// Dispatches batch for rendering.
		/// </summary>
		/// <param name="clear">Set true, when next batch is not has no connection to currently rendered batch.</param>
		public void Render (bool clear = false)
		{
			if (this.index == 0) return;
			Handles.DrawAAPolyLine(this.lineWidth, this.index, this.vertices);

			if (clear == true)
			{
				this.index = 0;
			}
			else
			{
				this.vertices[0] = this.last;
				this.index = 1;
			}
		}

		/// <summary>
		/// Resets batch
		/// </summary>
		public void Clear ()
		{
			this.index = 0;
		}



	}



}