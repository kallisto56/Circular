namespace Circular
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;



	/// <summary>
	/// <see cref="Sample"/> is point on <see cref="Biarc"/>, that contains interpolation values, that will be used by <see cref="Path"/> to adjust path color and tilt.
	/// </summary>
	[Serializable]
	public class Sample : IControl, IComparable<Sample>
	{
		public int biarcId;
		public float distanceOnBiarc;
		[NonSerialized] public float distanceOnPath;
		public AnchorPosition anchor;

		public Color color;
		public float tilt;


		public Sample ()
		{
		
		}


		public Sample (int biarcId, AnchorPosition anchor, Color color, float tilt = 0.0f)
		{
			this.biarcId = biarcId;
			this.anchor = anchor;
			this.color = color;
			this.tilt = tilt;
		}



		/// <summary>
		/// Interpolates properties of two samples
		/// </summary>
		/// <param name="left">Start sample</param>
		/// <param name="right">End sample</param>
		/// <param name="color">Interpolated color</param>
		/// <param name="tilt">Interpolated tilt</param>
		/// <param name="alpha">Value between 0.0f and 1.0f</param>
		public static void Interpolate (Sample left, Sample right, out Color color, out float tilt, float alpha)
		{
			color = new Color(
				EaseInOutQuad(left.color.r, right.color.r, alpha),
				EaseInOutQuad(left.color.g, right.color.g, alpha),
				EaseInOutQuad(left.color.b, right.color.b, alpha)
			);

			tilt = EaseInOutQuad(left.tilt, right.tilt, alpha);
		}
		
		/// <summary>
		/// Quadratic interpolation between two values
		/// </summary>
		/// <param name="start">Start value</param>
		/// <param name="end">End value</param>
		/// <param name="value">Value between 0.0f and 1.0f</param>
		/// <returns></returns>
		public static float EaseInOutQuad(float start, float end, float value)
		{
			// source: https://gist.github.com/cjddmut/d789b9eb78216998e95c
			value /= .5f;
			end -= start;
			if (value < 1) return end * 0.5f * value * value + start;
			value--;
			return -end * 0.5f * (value * (value - 2) - 1) + start;
		}

		/// <summary>
		/// Computes distance on biarc and distance on path for target sample.
		/// </summary>
		/// <param name="biarcs">List of biarcs, on which method will operate on.</param>
		/// <param name="sample">Target <see cref="Sample" /></param>
		public static void ComputeDistance (List<Biarc> biarcs, Sample sample)
		{
			float distanceOnPath = 0.0f;

			for (int n = 0; n < biarcs.Count; n++)
			{
				if (sample.biarcId == biarcs[n].identifier)
				{
					if (sample.anchor != AnchorPosition.manual)
					{
						sample.distanceOnBiarc = biarcs[n].GetDistance(sample.anchor, sample.distanceOnBiarc);
					}
					sample.distanceOnPath = distanceOnPath + sample.distanceOnBiarc;
					return;
				}

				distanceOnPath += biarcs[n].totalLength;
			}

			
			if (sample.anchor != AnchorPosition.manual)
			{
				sample.distanceOnBiarc = biarcs[biarcs.Count - 1].GetDistance(sample.anchor, sample.distanceOnBiarc);
			}
			sample.distanceOnPath = distanceOnPath;
		}

		public int CompareTo (Sample other)
		{
			return (this.distanceOnPath < other.distanceOnPath) ? -1 : 1;
		}



	}



}