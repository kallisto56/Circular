namespace Circular
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;



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
		}

		public void GetPointAndRotation (float distanceOnPath, Vector3 upwards, out Vector3 pointOnBiarc, out Quaternion rotation)
		{
			float accumulatedLength = 0.0f;
			float distanceOnBiarc;

			Vector3 leftTangent;
			Vector3 rightTangent;

			Biarc biarc;

			for (int n = 0; n < this.biarcs.Count; n++)
			{
				biarc = this.biarcs[n];
				if (distanceOnPath >= accumulatedLength && distanceOnPath <= accumulatedLength + biarc.totalLength)
				{
					distanceOnBiarc = distanceOnPath - accumulatedLength;
					pointOnBiarc = biarc.GetPoint(distanceOnBiarc);
					biarc.GetTangent(pointOnBiarc, distanceOnBiarc, out leftTangent, out rightTangent);
					rotation = Quaternion.LookRotation((rightTangent - leftTangent).normalized, upwards);
					return;
				}

				accumulatedLength += biarc.totalLength;
			}

			biarc = this.biarcs[this.biarcs.Count - 1];
			pointOnBiarc = biarc.destination;
			rotation = Quaternion.LookRotation((biarc.destination - biarc.rightTangent).normalized, upwards);
		}

		public Vector3 GetPoint (float distanceOnPath)
		{
			float accumulatedLength = 0.0f;

			for (int n = 0; n < this.biarcs.Count; n++)
			{
				Biarc biarc = this.biarcs[n];
				if (distanceOnPath >= accumulatedLength && distanceOnPath <= accumulatedLength + biarc.totalLength)
				{
					float distanceOnBiarc = distanceOnPath - accumulatedLength;
					return biarc.GetPoint(distanceOnBiarc);
				}

				accumulatedLength += biarc.totalLength;
			}

			return this.biarcs[this.biarcs.Count - 1].destination;
		}

		public Quaternion GetRotation (float distanceOnPath, Vector3 upwards)
		{
			float accumulatedLength = 0.0f;
			float distanceOnBiarc;

			Vector3 pointOnBiarc;
			Vector3 leftTangent;
			Vector3 rightTangent;

			Biarc biarc;

			for (int n = 0; n < this.biarcs.Count; n++)
			{
				biarc = this.biarcs[n];
				if (distanceOnPath >= accumulatedLength && distanceOnPath <= accumulatedLength + biarc.totalLength)
				{
					distanceOnBiarc = distanceOnPath - accumulatedLength;
					pointOnBiarc = biarc.GetPoint(distanceOnBiarc);
					biarc.GetTangent(pointOnBiarc, distanceOnBiarc, out leftTangent, out rightTangent);
					return Quaternion.LookRotation((rightTangent - leftTangent).normalized, upwards);
				}

				accumulatedLength += biarc.totalLength;
			}

			biarc = this.biarcs[this.biarcs.Count - 1];
			return Quaternion.LookRotation((biarc.destination - biarc.rightTangent).normalized, upwards);
		}

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

		public void GetSamplePair (float distanceOnPath, out Sample left, out Sample right)
		{
			left = null;
			right = null;

			float leftPosition = float.MinValue;
			float rightPosition = float.MaxValue;

			for (int n = 0; n < this.samples.Count; n++)
			{
				Sample current = this.samples[n];

				if (current.distanceOnPath >= leftPosition && current.distanceOnPath <= distanceOnPath)
				{
					leftPosition = current.distanceOnPath;
					left = current;
				}

				if (current.distanceOnPath <= rightPosition && current.distanceOnPath >= distanceOnPath)
				{
					rightPosition = current.distanceOnPath;
					right = current;
				}
			}
		}

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

			Sample blueSample = new Sample(0, AnchorPosition.origin, Color.blue, -10.0f);
			Sample redSample = new Sample(0, AnchorPosition.leftTangent, Color.red, +10.0f);
			Sample yellowSample = new Sample(0, AnchorPosition.rightTangent, Color.yellow, -10.0f);
			Sample magentaSample = new Sample(0, AnchorPosition.destination, Color.magenta, +10.0f);
			
			path.samples.Add(blueSample);
			path.samples.Add(redSample);
			path.samples.Add(yellowSample);
			path.samples.Add(magentaSample);

			return path;
		}



	}



}