namespace Circular
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;



	[Serializable]
	public class EditorState
	{
		[NonSerialized] public bool isInitialized = false;

		public EditorMode mode = EditorMode.vertices;
		public Transform transform;

		public List<Biarc> biarcs = new List<Biarc>();
		public List<Sample> samples = new List<Sample>();
		public List<Node> nodes = new List<Node>();
		public float totalLength;

		// Invalidation
		public bool requiresUpdate = true;
		public bool requiresRepaint = true;

		// Foldouts
		public bool uiShowEditorSettings = false;

		public bool showTransformTool = false;
		public bool showPointOnBiarc = false;
		public bool showPointOnPlane = false;
		public bool alwaysDrawColoredPath = false;
		public bool alwaysDrawSamples = false;

		// EditorMode.vertices
		public int countSegmentsPerArcForOptimizedPath = 32;
		// EditorMode.samples
		public int countSegmentsPerUnitLengthForColoredPath = 4;
		
		public float pathLineWidth = 3.0f;
		public float snapGridSize = 0.5f;

		// Normals
		public bool renderNormals = false;
		public bool useSamplesForColoringNormals = false;
		public float countSegmentsForNormalsPerUnitLength = 2;

		// Tangent behaviour
		public CotangentBehaviour cotangentBehaviour = CotangentBehaviour.keepMagnitudeAdjustDirection;
		public Vector3 initialTangentDirection;
		public bool initialTangentDirectionSet = false;

		public int polylineBatchSize = 512;

		public Ray mouseRay;
		public Plane plane;

		// Current controls
		public IControl controlInFocus;
		public IControl selectedControl;

		// Biarc in focus and it's properties
		public bool hasPointOnBiarc;
		public Biarc biarcInFocus;
		public Vector3 pointOnBiarc;
		public float distanceOnBiarc;
		public float minimalCursorToPointOnBiarcDistance = 45.0f;

		public bool hasPointOnPlane;
		public Vector3 pointOnPlane;

		// Size of handles
		public bool uiKeepConstantSizeOfHandles = true;
		public float uiHandleSizeForNode = 0.08f;
		public float uiHandleSizeForTangent = 0.08f;
		public float uiHandleSizeForMidpoint = 0.08f;
		public float uiHandleSizeForSample = 0.08f;
		public float uiHandleSizeForPointOnBiarc = 0.1f;

		// Colors for Node
		public Color uiColorForNode = Color.black;
		public Color uiColorForNodeInFocus = Color.red;
		public Color uiColorForNodeSelected = Color.red;

		// Colors for Tangent
		public Color uiColorForTangent = Color.black;
		public Color uiColorForTangentInFocus = Color.green;
		public Color uiColorForTangentSelected = Color.green;
		public Color uiColorForTangentLine = new Color(0.0f, 0.0f, 0.0f, 0.8f);

		// Colors for Midpoint
		public Color uiColorForMidpoint = Color.black;
		public Color uiColorForMidpointInFocus = Color.cyan;
		public Color uiColorForMidpointSelected = Color.cyan;

		// Colors for Sample
		public Color uiColorForSample = Color.black;
		public Color uiColorForSampleInFocus = Color.white;
		public Color uiColorForSampleSelected = Color.white;

		// ...
		public Color uiColorForPointOnBiarc = Color.black;
		public Color uiColorForPath = Color.black;
		public Color uiColorForNormals = Color.black;
		public Color uiColorForPointOnPlane = Color.black;

		// ...
		public float uiHandleSizeForPointOnPlane = 0.08f;

		// ...
		public PolylineBatch polylineBatch = new PolylineBatch();
		public Biarc temporaryBiarc = new Biarc();



		/// <summary>
		/// Invoked, when instance of PathEditor is attached to PathComponent
		/// </summary>
		/// <param name="transform">Transform of PathComponent</param>
		/// <param name="path">Path of PathComponent</param>
		public void OnEditorAttached (Transform transform, Path path)
		{
			this.requiresUpdate = true;
			if (this.isInitialized == true) return;
			this.isInitialized = true;

			this.transform = transform;
			this.plane = new Plane(
				-this.transform.forward,
				this.transform.right,
				this.transform.forward - this.transform.right
			);

			this.biarcs.Clear();
			this.samples.Clear();
			this.nodes.Clear();

			// Importing biarcs
			for (int n = 0; n < path.biarcs.Count; n++)
			{
				Biarc inputBiarc = path.biarcs[n];
				Biarc outputBiarc = new Biarc();

				outputBiarc.identifier = inputBiarc.identifier;

				outputBiarc.leftMidpointBehaviour = inputBiarc.leftMidpointBehaviour;
				outputBiarc.rightMidpointBehaviour = inputBiarc.rightMidpointBehaviour;

				outputBiarc.leftMidpointOffset = inputBiarc.leftMidpointOffset;
				outputBiarc.rightMidpointOffset = inputBiarc.rightMidpointOffset;
				
				outputBiarc.Initialize(
					inputBiarc.origin,
					inputBiarc.leftTangent,
					inputBiarc.leftMidpoint,
					inputBiarc.rightMidpoint,
					inputBiarc.rightTangent,
					inputBiarc.destination
				);

				this.biarcs.Add(outputBiarc);
			}

			// Importing samples
			for (int n = 0; n < path.samples.Count; n++)
			{
				Sample inputSample = path.samples[n];
				Sample outputSample = new Sample();

				EditorState.GetBiarcByIdentifier(path.biarcs, inputSample.biarcId, out Biarc inputBiarc);
				EditorState.GetBiarcByIdentifier(this.biarcs, inputSample.biarcId, out Biarc outputBiarc);

				outputSample.biarcId = inputSample.biarcId;
				outputSample.color = inputSample.color;
				outputSample.tilt = inputSample.tilt;
				outputSample.anchor = inputSample.anchor;

				outputSample.distanceOnBiarc = inputSample.distanceOnBiarc;
				outputSample.distanceOnPath = inputSample.distanceOnPath;

				this.samples.Add(outputSample);
			}

			// Generating nodes, tangents and midpoints
			Node leftNode = null;
			for (int n = 0; n < this.biarcs.Count; n++)
			{
				Biarc biarc = this.biarcs[n];

				if (leftNode == null)
				{
					leftNode = new Node();
					leftNode.leftTangent.localPosition = biarc.leftArc.origin - biarc.leftArc.tangent;
					this.nodes.Add(leftNode);
				}

				Node rightNode = new Node();

				leftNode.position = biarc.leftArc.origin;
				leftNode.rightTangent.localPosition = biarc.leftArc.tangent - biarc.leftArc.origin;

				leftNode.rightMidpoint.behaviour = biarc.leftMidpointBehaviour;
				rightNode.leftMidpoint.behaviour = biarc.rightMidpointBehaviour;

				leftNode.rightMidpoint.offset = biarc.leftMidpointOffset;
				rightNode.leftMidpoint.offset = biarc.rightMidpointOffset;

				leftNode.rightMidpoint.position = biarc.leftArc.destination;
				rightNode.leftMidpoint.position = biarc.rightArc.origin;

				rightNode.leftTangent.localPosition = biarc.rightArc.tangent - biarc.rightArc.destination;
				rightNode.position = biarc.rightArc.destination;

				rightNode.rightTangent.localPosition = -rightNode.leftTangent.localPosition;
				this.nodes.Add(rightNode);

				leftNode = rightNode;
			}
		}

		/// <summary>
		/// Invoked, when instance of PathEditor is detached from PathComponent
		/// </summary>
		/// <param name="path">Path of PathComponent</param>
		public void OnEditorDetached (Path path)
		{
			this.ApplyChanges(path);
		}

		public static bool GetBiarcByIdentifier (List<Biarc> biarcs, int identifier, out Biarc biarc)
		{
			for (int n = 0; n < biarcs.Count; n++)
			{
				if (biarcs[n].identifier == identifier)
				{
					biarc = biarcs[n];
					return true;
				}
			}

			biarc = null;
			return false;
		}

		public void ApplyChanges (Path path)
		{
			path.biarcs.Clear();
			path.samples.Clear();
			path.totalLength = this.totalLength;

			for (int n = 0; n < this.biarcs.Count; n++)
			{
				Biarc inputBiarc = this.biarcs[n];
				Biarc outputBiarc = new Biarc();

				outputBiarc.identifier = inputBiarc.identifier;

				outputBiarc.leftMidpointBehaviour = inputBiarc.leftMidpointBehaviour;
				outputBiarc.rightMidpointBehaviour = inputBiarc.rightMidpointBehaviour;
				
				outputBiarc.leftMidpointOffset = inputBiarc.leftMidpointOffset;
				outputBiarc.rightMidpointOffset = inputBiarc.rightMidpointOffset;

				outputBiarc.Initialize(
					inputBiarc.origin,
					inputBiarc.leftTangent,
					inputBiarc.leftMidpoint,
					inputBiarc.rightMidpoint,
					inputBiarc.rightTangent,
					inputBiarc.destination
				);

				path.biarcs.Add(outputBiarc);
			}

			for (int n = 0; n < this.samples.Count; n++)
			{
				Sample inputSample = this.samples[n];
				Sample outputSample = new Sample();

				EditorState.GetBiarcByIdentifier(this.biarcs, inputSample.biarcId, out Biarc inputBiarc);
				EditorState.GetBiarcByIdentifier(path.biarcs, inputSample.biarcId, out Biarc outputBiarc);

				outputSample.biarcId = inputSample.biarcId;
				outputSample.color = inputSample.color;
				outputSample.tilt = inputSample.tilt;
				outputSample.anchor = inputSample.anchor;

				// Because EditorState is using transformed biarcs
				// we are required to recalculate distanceOnBiarc.
				outputSample.distanceOnBiarc = inputSample.distanceOnBiarc;
				outputSample.distanceOnPath = inputSample.distanceOnPath;

				path.samples.Add(outputSample);
			}
		}

		public float GetHandleSize (Vector3 position, float handleSize)
		{
			return this.uiKeepConstantSizeOfHandles
				? (HandleUtility.GetHandleSize(position) * handleSize)
				: handleSize;
		}


		public static EditorState CreateDefault ()
		{
			return new EditorState();
		}


		public void GenerateIdentifierForBiarc (Biarc biarc)
		{
			biarc.identifier = -1;
			int identifier = 0;
			while (true)
			{
				if (this.ContainsBiarcIdentifier(identifier) == false)
				{
					biarc.identifier = identifier;
					break;
				}

				identifier++;
			}
		}

		public bool ContainsBiarcIdentifier (int identifier)
		{
			for (int n = 0; n < this.biarcs.Count; n++)
			{
				if (this.biarcs[n].identifier == identifier)
				{
					return true;
				}
			}

			return false;
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



	}



}