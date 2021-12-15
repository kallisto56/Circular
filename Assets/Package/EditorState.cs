namespace Circular
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;



	/// <summary>
	/// Container for storing current state of PathEditor
	/// </summary>
	[Serializable]
	public class EditorState
	{
		// Mode in which user currently operates
		public EditorMode mode = EditorMode.vertices;

		// Behaviour, that will be applied to cotangent of modified tangent
		public CotangentBehaviour cotangentBehaviour = CotangentBehaviour.keepMagnitudeAdjustDirection;

		// ...
		public bool showTransformTool = false;
		public float pathTotalLength;

		// Initialization and invalidation
		[NonSerialized] public bool isInitialized = false;
		[NonSerialized] public bool requiresUpdate = true;
		[NonSerialized] public bool requiresRepaint = true;

		// Transform of PathComponent
		public Transform transform;

		// Collections, constructed from on Path
		public List<Biarc> biarcs = new List<Biarc>();
		public List<Sample> samples = new List<Sample>();
		public List<Node> nodes = new List<Node>();

		// Foldouts
		public bool inspectorShowGlobalSettings = false;
		public bool inspectorShowLocalSettings = false;

		// Current controls
		public IControl controlInFocus;
		public IControl selectedControl;

		// For control in focus
		public bool emphasizeControlInFocus = false;

		// Biarc in focus and its properties
		public bool hasPointOnBiarc = false;
		public Biarc biarcInFocus = null;
		public Vector3 pointOnBiarc = Vector3.zero;
		public float distanceOnBiarc = 0.0f;
		public float distanceOnPath = 0.0f;

		// Point on plane
		public bool hasPointOnPlane = false;
		public Vector3 pointOnPlane = Vector3.zero;

		// Used for rendering path in batches
		public PolylineBatch polylineBatch = new PolylineBatch();

		// ...
		public Ray mouseRay;
		public Plane plane;

		// ...
		public bool isSnappingEnabled = false;


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

		/// <summary>
		/// Searches for biarc with specified identifier in provided list.
		/// </summary>
		/// <param name="biarcs">List of biarcs to search in</param>
		/// <param name="identifier">Wanted identifier</param>
		/// <param name="biarc">Biarc with wanted identifier</param>
		/// <returns>Returns true, when biarc has been found, otherwise returns false.</returns>
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

		/// <summary>
		/// Applies changes in current instance of EditorState to a specified instance of Path.
		/// </summary>
		/// <param name="path">Target instance of <see cref="Path" /></param>
		public void ApplyChanges (Path path)
		{
			path.biarcs.Clear();
			path.samples.Clear();
			path.totalLength = this.pathTotalLength;

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

				outputSample.distanceOnBiarc = inputSample.distanceOnBiarc;
				outputSample.distanceOnPath = inputSample.distanceOnPath;

				path.samples.Add(outputSample);
			}
		}

		/// <summary>
		/// Returns new instance of <see cref="EditorState" />.
		/// </summary>
		/// <returns>Instance of <see cref="EditorState" />.</returns>
		public static EditorState CreateDefault ()
		{
			return new EditorState();
		}

		/// <summary>
		/// Generates unique (in scope of current instance of EditorState) identifier for target biarc
		/// </summary>
		/// <param name="biarc">Target <see cref="Biarc" /></param>
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

		/// <summary>
		/// Returns true, if biarc with specified identifier is present in current instance of EditorState.
		/// </summary>
		/// <param name="identifier">Target identifier</param>
		/// <returns>True, when identifier is present, otherwise false.</returns>
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

		/// <summary>
		/// Returns <see cref="IControl" />, previous to specified control on the path.
		/// </summary>
		/// <param name="node">Owner of `currentControl`</param>
		/// <param name="currentControl">Target control</param>
		/// <returns><see cref="IControl" />, previous to specified control on the path.</returns>
		public IControl GetPrevious (Node node, IControl currentControl)
		{
			int nodeIndex = this.nodes.IndexOf(node);

			if (currentControl == node.rightMidpoint)
			{
				return node.rightTangent;
			}
			else if (currentControl == node.rightTangent)
			{
				return node;
			}
			else if (currentControl == node)
			{
				return (nodeIndex > 0) ? node.leftTangent : currentControl;
			}
			else if (currentControl == node.leftTangent)
			{
				return node.leftMidpoint;
			}
			else// if (currentControl == node.leftMidpoint)
			{
				return this.nodes[nodeIndex - 1].rightMidpoint;
			}
		}

		/// <summary>
		/// Returns <see cref="IControl" />, that stands after specified control on the path.
		/// </summary>
		/// <param name="node">Owner of `currentControl`</param>
		/// <param name="currentControl">Target control</param>
		/// <returns>Next <see cref="IControl" /> on path after specified control.</returns>
		public IControl GetNext (Node node, IControl currentControl)
		{
			int nodeIndex = this.nodes.IndexOf(node);

			if (currentControl == node.leftMidpoint)
			{
				return node.leftTangent;
			}
			else if (currentControl == node.leftTangent)
			{
				return node;
			}
			else if (currentControl == node)
			{
				return (nodeIndex + 1 < this.nodes.Count) ? node.rightTangent : currentControl;
			}
			else if (currentControl == node.rightTangent)
			{
				return node.rightMidpoint;
			}
			else// if (currentControl == node.rightMidpoint)
			{
				return this.nodes[nodeIndex + 1].leftMidpoint;
			}

		}

		/// <summary>
		/// Returns <see cref="Tangent" />, opposite to specified <see cref="Midpoint" />.
		/// </summary>
		/// <param name="midpoint">Target <see cref="Midpoint" /></param>
		/// <returns><see cref="Tangent" />, opposite to specified <see cref="Midpoint" /></returns>
		public Tangent GetOppositeTangent (Midpoint midpoint)
		{
			int nodeIndex = this.nodes.IndexOf(midpoint.node);
			if (midpoint.node.leftMidpoint == midpoint)
			{
				return this.nodes[nodeIndex - 1].rightTangent;
			}
			else
			{
				return this.nodes[nodeIndex + 1].leftTangent;
			}
		}

		/// <summary>
		/// Returns <see cref="Tangent" />, related to specified <see cref="Midpoint" />
		/// </summary>
		/// <param name="midpoint">Target <see cref="Midpoint" /></param>
		/// <returns><see cref="Tangent" />, related to specified <see cref="Midpoint" /></returns>
		public Tangent GetTangent (Midpoint midpoint)
		{
			return midpoint.node.leftMidpoint == midpoint
				? midpoint.node.leftTangent
				: midpoint.node.rightTangent;
		}

		/// <summary>
		/// Returns <see cref="Midpoint" />, opposite to specified <see cref="Midpoint" />
		/// </summary>
		/// <param name="midpoint">Target <see cref="Midpoint" /></param>
		/// <returns><see cref="Midpoint" />, opposite to specified <see cref="Midpoint" /></returns>
		public Midpoint GetOpposite (Midpoint midpoint)
		{
			if (midpoint.node.leftMidpoint == midpoint)
			{
				return this.GetPrevious(midpoint.node, midpoint) as Midpoint;
			}
			else
			{
				return this.GetNext(midpoint.node, midpoint) as Midpoint;
			}
		}

		/// <summary>
		/// Returns <see cref="Tangent" />, opposite to specified <see cref="Tangent" />
		/// </summary>
		/// <param name="tangent">Target <see cref="Tangent" /></param>
		/// <returns><see cref="Tangent" />, opposite to specified <see cref="Tangent" /></returns>
		public Tangent GetOpposite (Tangent tangent)
		{
			int nodeIndex = this.nodes.IndexOf(tangent.node);

			if (tangent.node.leftTangent == tangent)
			{
				return this.nodes[nodeIndex - 1].rightTangent;
			}
			else
			{
				return this.nodes[nodeIndex + 1].leftTangent;
			}
		}


	}



}