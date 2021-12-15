namespace Circular
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;



	/// <summary>
	/// <see cref="Path"/> is used to store collection of Biarcs and Samples. Instance of <see cref="Path"/> can be used as storage, placed in <see cref="ScriptableObject"/> and thus saved/loaded.
	/// </summary>
	[Serializable]
	public class Path
	{
		public List<Biarc> biarcs;
		public List<Sample> samples;

		[NonSerialized] public float totalLength;
		[NonSerialized] public bool isInitialized;



		/// <summary>
		/// Initializes current instance of <see cref="Path" />.
		/// </summary>
		/// <param name="forceInitialize">When true, it will force initialization even if instance is already initialized.</param>
		public void Initialize (bool forceInitialize = false)
		{
			if (this.isInitialized == true && forceInitialize == false) return;
			this.isInitialized = true;

			this.totalLength = 0.0f;
			for (int n = 0; n < this.biarcs.Count; n++)
			{
				this.biarcs[n].Initialize(true);
				this.totalLength += this.biarcs[n].totalLength;
			}

			for (int n = 0; n < this.samples.Count; n++)
			{
				Sample.ComputeDistance(this.biarcs, this.samples[n]);
			}
		}

		/// <summary>
		/// Calculates point on path at specified distance
		/// </summary>
		/// <param name="distanceOnPath">Distance on path</param>
		/// <returns>Point on path at specified distance.</returns>
		public Vector3 GetPoint (float distanceOnPath)
		{
			float accumulatedLength = 0.0f;

			for (int n = 0; n < this.biarcs.Count; n++)
			{
				Biarc biarc = this.biarcs[n];

				// If given distance on path is between accumulated length but less than (accumulated length + biarc total length)
				if (distanceOnPath >= accumulatedLength && distanceOnPath <= accumulatedLength + biarc.totalLength)
				{
					// Calculate distance on biarc and return point
					float distanceOnBiarc = distanceOnPath - accumulatedLength;
					return biarc.GetPoint(distanceOnBiarc);
				}

				accumulatedLength += biarc.totalLength;
			}

			return this.biarcs[this.biarcs.Count - 1].destination;
		}

		/// <summary>
		/// Calculates rotation at specified distance on path. When includeTilt is set to true, rotation is modified by samples.
		/// </summary>
		/// <param name="distanceOnPath">Distance on path</param>
		/// <param name="includeTilt">When enabled, rotation is modified by samples</param>
		/// <returns>Local rotation with or without tilt from samples, depending on parameters.</returns>
		public Quaternion GetRotation (float distanceOnPath, bool includeTilt = false)
		{
			float accumulatedLength = 0.0f;
			float distanceOnBiarc;

			Vector3 pointOnBiarc;
			Vector3 direction;
			Quaternion rotation;

			Biarc biarc;

			for (int n = 0; n < this.biarcs.Count; n++)
			{
				biarc = this.biarcs[n];

				// If given distance on path is between accumulated length but less than (accumulated length + biarc total length)
				if (distanceOnPath >= accumulatedLength && distanceOnPath <= accumulatedLength + biarc.totalLength)
				{
					// Calculate distance on biarc and get point
					distanceOnBiarc = distanceOnPath - accumulatedLength;
					pointOnBiarc = biarc.GetPoint(distanceOnBiarc);

					// Get direction and calculate rotation
					direction = biarc.GetDirection(pointOnBiarc, distanceOnBiarc);
					rotation = Quaternion.LookRotation(direction, Vector3.up);

					if (includeTilt == true)
					{
						// Find tilt at specified distance on path and apply it to rotation
						this.GetSampleAtDistance(distanceOnPath, out Color _, out float tilt);
						rotation *= Quaternion.Euler(0.0f, 0.0f, tilt);
					}
					return rotation;
				}

				accumulatedLength += biarc.totalLength;
			}

			// Last biarc in list
			biarc = this.biarcs[this.biarcs.Count - 1];

			// Get direction and calculate rotation
			direction = (biarc.destination - biarc.rightTangent).normalized;
			rotation = Quaternion.LookRotation(direction, Vector3.up);

			if (includeTilt == true)
			{
				// Find tilt at specified distance on path and apply it to rotation
				this.GetSampleAtDistance(distanceOnPath, out Color _, out float tilt);
				rotation *= Quaternion.Euler(0.0f, 0.0f, tilt);
			}
			return rotation;
		}

		/// <summary>
		/// Returns interpolated color and tilt at specified distance on path.
		/// </summary>
		/// <param name="distanceOnPath">Target distance on path</param>
		/// <param name="color">Interpolated color</param>
		/// <param name="tilt">Interpolated tilt</param>
		public void GetSampleAtDistance (float distanceOnPath, out Color color, out float tilt)
		{
			this.GetSamplePair(distanceOnPath, out Sample left, out Sample right);

			if (left != null && right != null)
			{
				float alpha = Mathf.InverseLerp(left.distanceOnPath, right.distanceOnPath, distanceOnPath);
				Sample.Interpolate(left, right, out color, out tilt, alpha);
			}
			else if (left != null)
			{
				color = left.color;
				tilt = left.tilt;
			}
			else if (right != null)
			{
				color = right.color;
				tilt = right.tilt;
			}
			else
			{
				color = Color.black;
				tilt = 0.0f;
			}
		}

		/// <summary>
		/// Returns pair of samples that are closest to specified distance on path.
		/// </summary>
		/// <param name="distanceOnPath">Target distance on path</param>
		/// <param name="left">Sample before specified distance on path</param>
		/// <param name="right">Sample after specified distance on path</param>
		public void GetSamplePair (float distanceOnPath, out Sample left, out Sample right)
		{
			left = null;
			right = null;

			float leftPosition = float.MinValue;
			float rightPosition = float.MaxValue;

			for (int n = 0; n < this.samples.Count; n++)
			{
				Sample current = this.samples[n];

				// Current sample stands before distance on path and is closer than current left sample
				if (current.distanceOnPath >= leftPosition && current.distanceOnPath <= distanceOnPath)
				{
					leftPosition = current.distanceOnPath;
					left = current;
				}

				// Current sample stands after distance on path and is closer than current right sample
				if (current.distanceOnPath <= rightPosition && current.distanceOnPath >= distanceOnPath)
				{
					rightPosition = current.distanceOnPath;
					right = current;
				}
			}
		}

		/// <summary>
		/// Creates an instance of <see cref="Path" /> from a template.
		/// </summary>
		/// <returns>Instance of <see cref="Path" /></returns>
		public static Path CreateDefault ()
		{
			Path path = new Path();
			path.biarcs = new List<Biarc>();
			path.samples = new List<Sample>();

			path.biarcs.Add(new Biarc());
			path.biarcs[0].Initialize(
				new Vector3(-10.0f, 0.0f, 0.0f),
				new Vector3(-5.0f, 0.0f, 5.0f),
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(5.0f, 0.0f, -5.0f),
				new Vector3(10.0f, 0.0f, 0.0f)
			);

			Sample blueSample = new Sample(0, AnchorPosition.origin, Color.blue, 0.0f);
			Sample redSample = new Sample(0, AnchorPosition.leftTangent, Color.red, +45.0f);
			Sample yellowSample = new Sample(0, AnchorPosition.rightTangent, Color.yellow, -45.0f);
			Sample magentaSample = new Sample(0, AnchorPosition.destination, Color.magenta, 0.0f);
			
			path.samples.Add(blueSample);
			path.samples.Add(redSample);
			path.samples.Add(yellowSample);
			path.samples.Add(magentaSample);

			return path;
		}



	}



}