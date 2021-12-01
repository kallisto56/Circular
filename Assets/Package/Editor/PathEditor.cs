namespace Circular
{
	using UnityEngine;
	using UnityEditor;



	[CustomEditor(typeof(PathComponent))]
	public class PathEditor : Editor
	{
		PathComponent component;
		int passiveLayoutIdentifier;

		static readonly GUIContent[] nodeSelections = new GUIContent[]
		{
			new GUIContent("LM", "Left Midpoint"),
			new GUIContent("LT", "Left Tangent"),
			new GUIContent("RT", "Right Tangent"),
			new GUIContent("RM", "Right Midpoint"),
		};

		static readonly string warningUnableToInsertNode = "PathEditor: Unable to insert new Node. EditorState.hasPointOnPlane equals FALSE. Point on plane is a result of intersection between mouse ray and plane constructed from EditorState.transform.";



		void OnEnable ()
		{
			Tools.hidden = true;
			this.passiveLayoutIdentifier = GUIUtility.GetControlID(FocusType.Passive);
			this.component = this.target as PathComponent;

			if (this.component.path == null)
			{
				this.component.path = Path.CreateDefault();
			}

			if (this.component.editorState == null)
			{
				this.component.editorState = EditorState.CreateDefault();
			}

			this.component.editorState.OnEditorAttached(this.component.transform, this.component.path);
			Undo.undoRedoPerformed += this.HandleUndoRedo;
		}

		void OnDisable ()
		{
			Tools.hidden = false;
			this.component.editorState.OnEditorDetached(this.component.path);
			Undo.undoRedoPerformed -= this.HandleUndoRedo;
		}
		
		void OnSceneGUI ()
		{
			Event e = Event.current;
			EditorState state = this.component.editorState;
			Path path = this.component.path;

			state.polylineBatch.EnsureSize(state.polylineBatchSize);

			this.HandlePosition(e, state);

			if (e.isKey)
			{
				this.HandleKeyboard(e, state);
			}
			else if (e.isMouse == true)
			{
				this.HandleMouse(e, state);
			}
			else if (e.type == EventType.Repaint)
			{
				this.HandleRepaint(e, state);
			}
			else if (e.type == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(this.passiveLayoutIdentifier);
			}

			if (state.transform.hasChanged == true)
			{
				state.transform.hasChanged = false;
				state.requiresUpdate = true;

				state.plane = new Plane(
					-state.transform.forward,
					state.transform.right,
					state.transform.forward - state.transform.right
				);
			}

			if (state.requiresUpdate == true)
			{
				state.requiresUpdate = false;
				this.HandleUpdate(e, state, path);
			}
			
			if (state.requiresRepaint == true)
			{
				state.requiresRepaint = false;
				this.Repaint();
			}
		}

		public override void OnInspectorGUI()
		{
			Event e = Event.current;
			EditorState state = this.component.editorState;

			// EDITOR MODE
			EditorMode mode = (EditorMode)EditorGUILayout.EnumPopup("Editor mode:", state.mode);
			if (mode != state.mode)
			{
				Undo.RecordObject(this.component, "Inspector");
				state.mode = mode;
				state.controlInFocus = null;
				state.selectedControl = null;
				state.requiresRepaint = true;
			}

			// EDITOR SETTINGS
			this.DrawUIFoldout("Editor settings", ref state.uiShowEditorSettings);
			if (state.uiShowEditorSettings == true)
			{
				this.DrawUIHeader("Visibility and interaction:", true);
				bool showTransformTool = EditorGUILayout.Toggle("Show transform tool", state.showTransformTool);
				if (Tools.hidden != !showTransformTool)
				{
					Undo.RecordObject(this.component, "Inspector");
					Tools.hidden = !showTransformTool;
					state.showTransformTool = showTransformTool;
				}

				this.DrawUIToggleField(state, "Show point on biarc:", ref state.showPointOnBiarc);
				this.DrawUIToggleField(state, "Show point on plane:", ref state.showPointOnPlane);
				this.DrawUIFloatField(state, "Path line width:", ref state.pathLineWidth, 0.1f, 6.0f);

				this.DrawUIDelimiter();
				this.DrawUIHeader("Always draw:", false);
				this.DrawUIToggleField(state, "Colored path:", ref state.alwaysDrawColoredPath);
				this.DrawUIToggleField(state, "Samples:", ref state.alwaysDrawSamples);
				this.DrawUIDelimiter();

				this.DrawUIHeader("Normals:", false);
				this.DrawUIToggleField(state, "Draw normals:", ref state.renderNormals);
				this.DrawUIToggleField(state, "Use samples color:", ref state.useSamplesForColoringNormals);
				this.DrawUIColorField(state, "Color:", ref state.uiColorForNormals);
				this.DrawUIFloatField(state, "Count segments per unit length:", ref state.countSegmentsForNormalsPerUnitLength, 0.0f, 6.0f);
				
				{
					this.DrawUIHeader("Snapping & behaviours:", true);
					this.DrawUIFloatField(state, "Grid size:", ref state.snapGridSize);
					this.DrawUIFloatField(state, "Minimal distance to point on biarc:", ref state.minimalCursorToPointOnBiarcDistance);

					CotangentBehaviour cotangentBehaviour = (CotangentBehaviour)EditorGUILayout.EnumPopup("Cotangent behaviour:", state.cotangentBehaviour);
					if (cotangentBehaviour != state.cotangentBehaviour)
					{
						Undo.RecordObject(this.component, "Inspector");
						state.cotangentBehaviour = cotangentBehaviour;
					}
				}
				{
					this.DrawUIHeader("Handle sizes:", true);
				
					bool keepConstantSize = EditorGUILayout.Toggle("Keep constant size:", state.uiKeepConstantSizeOfHandles);
					if (keepConstantSize != state.uiKeepConstantSizeOfHandles)
					{
						Undo.RecordObject(this.component, "Inspector");
						state.uiKeepConstantSizeOfHandles = keepConstantSize;
					}

					this.DrawUIFloatField(state, "Node:", ref state.uiHandleSizeForNode);
					this.DrawUIFloatField(state, "Tangent:", ref state.uiHandleSizeForTangent);
					this.DrawUIFloatField(state, "Midpoint:", ref state.uiHandleSizeForMidpoint);
					this.DrawUIFloatField(state, "Sample:", ref state.uiHandleSizeForSample);
					this.DrawUIFloatField(state, "Point on biarc:", ref state.uiHandleSizeForPointOnBiarc);
				}

				this.DrawUIHeader("Color pallete for Node:", true);
				this.DrawUIColorField(state, "Normal:", ref state.uiColorForNode);
				this.DrawUIColorField(state, "In focus:", ref state.uiColorForNodeInFocus);
				this.DrawUIColorField(state, "Selected:", ref state.uiColorForNodeSelected);

				this.DrawUIHeader("Color pallete for Tangent:", true);
				this.DrawUIColorField(state, "Normal:", ref state.uiColorForTangent);
				this.DrawUIColorField(state, "In focus:", ref state.uiColorForTangentInFocus);
				this.DrawUIColorField(state, "Selected:", ref state.uiColorForTangentSelected);
				this.DrawUIColorField(state, "Dotted line:", ref state.uiColorForTangentLine);

				this.DrawUIHeader("Color pallete for Midpoint:", true);
				this.DrawUIColorField(state, "Normal:", ref state.uiColorForMidpoint);
				this.DrawUIColorField(state, "In focus:", ref state.uiColorForMidpointInFocus);
				this.DrawUIColorField(state, "Selected:", ref state.uiColorForMidpointSelected);

				this.DrawUIHeader("Color pallete for Sample:", true);
				this.DrawUIColorField(state, "Normal:", ref state.uiColorForSample);
				this.DrawUIColorField(state, "In focus:", ref state.uiColorForSampleInFocus);
				this.DrawUIColorField(state, "Selected:", ref state.uiColorForSampleSelected);

				this.DrawUIHeader("Other:", true);
				this.DrawUIColorField(state, "Color of path:", ref state.uiColorForPath);
				this.DrawUIColorField(state, "Point on biarc:", ref state.uiColorForPointOnBiarc);
				this.DrawUIIntegerField(state, "Count segments for arc:", ref state.countSegmentsPerArcForOptimizedPath);
				this.DrawUIIntegerField(state, "Count segments per unit length:", ref state.countSegmentsPerUnitLengthForColoredPath);
			}

			// SELECTED CONTROL
			this.DrawUIDelimiter();
			if (state.selectedControl != null)
			{
				if (state.selectedControl is Node node)
				{
					this.InspectorForNode(state, node);
				}
				else if (state.selectedControl is Tangent tangent)
				{
					this.InspectorForTangent(state, tangent);
				}
				else if (state.selectedControl is Midpoint midpoint)
				{
					this.InspectorForMidpoint(state, midpoint);
				}
				else if (state.selectedControl is Sample sample)
				{
					this.InspectorForSample(e, state, sample);
				}
			}
			else
			{
				EditorGUILayout.Foldout(false, "Selected: None");
			}
		}

		void HandleUpdate (Event e, EditorState state, Path path)
		{
			state.totalLength = 0.0f;

			// Compute biarcs from nodes and adjust midpoints
			for (int n = 0; n < state.nodes.Count - 1; n++)
			{
				Biarc biarc = state.biarcs[n];

				state.totalLength += biarc.totalLength;

				Node leftNode = state.nodes[n];
				Node rightNode = state.nodes[n + 1];

				biarc.leftMidpointBehaviour = leftNode.rightMidpoint.behaviour;
				biarc.rightMidpointBehaviour = rightNode.leftMidpoint.behaviour;

				biarc.leftMidpointOffset = leftNode.rightMidpoint.offset;
				biarc.rightMidpointOffset = rightNode.leftMidpoint.offset;
				
				biarc.Initialize(
					leftNode.position,
					leftNode.rightTangent.GetPosition(),
					leftNode.rightMidpoint.position,
					rightNode.leftMidpoint.position,
					rightNode.leftTangent.GetPosition(),
					rightNode.position
				);

				leftNode.rightMidpoint.position = biarc.leftMidpoint;
				rightNode.leftMidpoint.position = biarc.rightMidpoint;
			}

			// Find index of biarc and compute distance on path for each sample
			for (int n = 0; n < state.samples.Count; n++)
			{
				Sample sample = state.samples[n];
				bool response = EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
				if (response == false)
				{
					Debug.LogWarning($"Unable to find biarc with specified indetifier {sample.biarcId}");
					for (int c = 0; c < state.biarcs.Count; c++)
					{
						Debug.Log($"state.biarcs[{c}].identifier = {state.biarcs[c].identifier}");
					}
				}

				Sample.ComputeDistance(state.biarcs, sample);
			}

			state.samples.Sort();
			state.ApplyChanges(path);
		}

		void HandleUndoRedo ()
		{
			EditorState state = this.component.editorState;
			Path path = this.component.path;
			Event e = Event.current;

			this.HandleUpdate(e, state, path);

			state.selectedControl = null;
			state.controlInFocus = null;
			state.biarcInFocus = null;
			state.hasPointOnBiarc = false;
			state.hasPointOnPlane = false;
		}
		
		void HandleRepaint (Event e, EditorState state)
		{
			if (state.renderNormals == true)
			{
				this.HandleRepaintForNormals(state);
			}

			if (state.mode == EditorMode.vertices)
			{
				if (state.alwaysDrawColoredPath == true)
				{
					this.HandleRepaintForPathColored(state);
				}
				else
				{
					this.HandleRepaintForPath(state);
				}
				this.HandleRepaintForVertices(e, state);

				if (state.showPointOnPlane == true)
				{
					if (state.hasPointOnBiarc == false && state.hasPointOnPlane == true)
					{
						this.HandleRepaintForPointOnPlane(state);
					}
				}
			}
			else if (state.mode == EditorMode.samples)
			{
				this.HandleRepaintForPathColored(state);
				this.HandleRepaintForSamples(e, state);
			}

			if (state.alwaysDrawSamples == true && state.mode != EditorMode.samples)
			{
				this.HandleRepaintForSamples(e, state);
			}

			if (state.hasPointOnBiarc == true && state.showPointOnBiarc == true && state.selectedControl == null)
			{
				this.HandleRepaintForPointOnPath(state);
			}

			if (state.controlInFocus is Node node)
			{
				this.RepaintForNode(state, node, state.nodes.IndexOf(node));
			}
			else if (state.controlInFocus is Tangent tangent)
			{
				this.RepaintForTangent(state, tangent, state.nodes.IndexOf(tangent.node));
			}
			else if (state.controlInFocus is Midpoint midpoint)
			{
				this.RepaintForMidpoint(state, midpoint, state.nodes.IndexOf(midpoint.node));
			}
			else if (state.controlInFocus is Sample sample)
			{
				this.RepaintForSample(state, sample);
			}
		}

		void HandleRepaintForNormals (EditorState state)
		{
			Handles.color = state.uiColorForNormals;

			float distanceOnPath = 0.0f;

			for (int n = 0; n < state.biarcs.Count; n++)
			{
				Biarc biarc = state.biarcs[n];

				float distanceOnBiarc = 0.0f;
				int countSegments = (int)(biarc.totalLength * (float)state.countSegmentsForNormalsPerUnitLength);
				float delta = biarc.totalLength / (float)(countSegments - 1);

				for (int i = 0; i < countSegments; i++)
				{
					state.GetSampleAtDistance(distanceOnPath, out Color color, out float tilt);

					Vector3 pointOnBiarc = biarc.GetPoint(distanceOnBiarc);
					Vector3 direction = biarc.GetDirection(pointOnBiarc, distanceOnBiarc);
					direction = state.transform.TransformDirection(direction);
					Quaternion rotation = Quaternion.LookRotation(direction, state.transform.up) * Quaternion.Euler(0.0f, 0.0f, tilt);
					
					pointOnBiarc = state.transform.TransformPoint(pointOnBiarc);
					rotation = Quaternion.LookRotation(rotation * Vector3.up, direction);
					
					if (state.useSamplesForColoringNormals == true)
					{
						Handles.color = color;
					}
					Handles.ArrowHandleCap(0, pointOnBiarc, rotation, 0.5f, EventType.Repaint);

					distanceOnBiarc += delta;
					distanceOnPath += delta;
				}
			}
		}

		void HandleRepaintForPointOnPath (EditorState state)
		{
			Vector3 position = state.transform.TransformPoint(state.pointOnBiarc);
			float handleSize = state.GetHandleSize(position, state.uiHandleSizeForNode);

			Handles.color = state.uiColorForPointOnBiarc;
			Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
		}

		void HandleRepaintForPath (EditorState state)
		{
			Handles.color = state.uiColorForPath;
			state.polylineBatch.lineWidth = state.pathLineWidth;
			state.polylineBatch.Clear();

			for (int n = 0; n < state.biarcs.Count; n++)
			{
				state.polylineBatch.AddBiarc(state.biarcs[n], state.transform, state.countSegmentsPerArcForOptimizedPath);
			}

			state.polylineBatch.Render(true);
		}

		void HandleRepaintForPathColored (EditorState state)
		{
			float distanceOnPath = 0.0f;
			for (int n = 0; n < state.biarcs.Count; n++)
			{
				Biarc biarc = state.biarcs[n];
				
				int countSegments = (int)(biarc.totalLength * (float)state.countSegmentsPerUnitLengthForColoredPath);
				float delta = biarc.totalLength / (float)(countSegments - 1);
				float distanceOnBiarc = delta;

				Vector3 prev = biarc.GetPoint(0.0f);
				prev = state.transform.TransformPoint(prev);

				for (int i = 0; i < countSegments; i++)
				{
					Vector3 next = biarc.GetPoint(distanceOnBiarc);
					next = state.transform.TransformPoint(next);

					this.GetSampleAtDistance(state, distanceOnPath + distanceOnBiarc, out Color color, out float tilt);
					
					Handles.color = color;
					Handles.DrawAAPolyLine(state.pathLineWidth, prev, next);
					
					prev = next;
					distanceOnBiarc += delta;
				}

				distanceOnPath += biarc.totalLength;
			}
		}



		void HandleRepaintForVertices (Event e, EditorState state)
		{
			for (int n = 0; n < state.nodes.Count; n++)
			{
				Node node = state.nodes[n];

				this.RepaintForNode(state, node, n);
				
				if (n > 0) this.RepaintForTangent(state, node.leftTangent, n);
				if (n + 1 < state.nodes.Count) this.RepaintForTangent(state, node.rightTangent, n);

				if (n > 0)
				{
					this.RepaintForMidpoint(state, node.leftMidpoint, n);
				}

				if (n + 1 < state.nodes.Count)
				{
					this.RepaintForMidpoint(state, node.rightMidpoint, n);
				}
			}
		}
		
		void HandleRepaintForSamples (Event e, EditorState state)
		{
			for (int n = 0; n < state.samples.Count; n++)
			{
				this.RepaintForSample(state, state.samples[n]);
			}
		}

		void HandleKeyboard (Event e, EditorState state)
		{
			// Focus
			if (e.keyCode == KeyCode.F)
			{
				if (state.selectedControl != null)
				{
					this.FocusOnSelectedControl(state);
				}
				else
				{
					Bounds bounds = new Bounds();
					for (int n = 0; n < state.nodes.Count; n++)
					{
						Node node = state.nodes[n];
						bounds.Encapsulate(node.position);
						bounds.Encapsulate(node.position + node.leftTangent.localPosition);
						bounds.Encapsulate(node.position + node.rightTangent.localPosition);
					}

					bounds.center = state.transform.TransformPoint(bounds.center);
					bounds.size = state.transform.TransformVector(bounds.size);

					SceneView.lastActiveSceneView.Frame(bounds, false);
				}
			}

			if (e.type == EventType.KeyUp && e.keyCode.HasFlag(KeyCode.N) && state.selectedControl != null)
			{
				Vector3 position = Vector3.zero;

				if (state.selectedControl is Node node)
				{
					position = node.position;
				}
				else if (state.selectedControl is Tangent tangent)
				{
					position = tangent.GetPosition();
				}
				else if (state.selectedControl is Midpoint midpoint)
				{
					position = midpoint.position;
				}
				else if (state.selectedControl is Sample sample)
				{
					position = this.GetSamplePosition(state, sample);
				}

				position = state.transform.TransformPoint(position);
				state.mouseRay = new Ray(state.mouseRay.origin, (position - state.mouseRay.origin).normalized);
				this.FindControlInFocus(e, state);
				if (state.controlInFocus != null)
				{
					state.selectedControl = state.controlInFocus;
				}
			}
		}
		
		void HandleMouse (Event e, EditorState state)
		{
			state.mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			state.hasPointOnPlane = this.GetPointOnPlane(state, state.plane, state.mouseRay, out state.pointOnPlane);

			this.FindBiarcInFocus(e, state);
			this.FindControlInFocus(e, state);

			if (state.mode == EditorMode.vertices)
			{
				this.HandleMouseForVertices(e, state);
			}
			else
			{
				this.HandleMouseForSamples(e, state);
			}
		}
		
		void HandleMouseForVertices (Event e, EditorState state)
		{
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (e.modifiers == EventModifiers.None)
				{
					state.selectedControl = state.controlInFocus;
					state.requiresRepaint = true;
				}
				else if (e.modifiers == EventModifiers.Shift)
				{
					if (state.hasPointOnBiarc == true)
					{
						this.SplitBiarc(state);
					}
					else
					{
						this.InsertNode(state);
					}
				}
				else if (e.modifiers == EventModifiers.Control && state.controlInFocus is Node node)
				{
					this.RemoveNode(state, node);
				}
			}
		}
		
		void HandleMouseForSamples (Event e, EditorState state)
		{
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (e.modifiers == EventModifiers.None)
				{
					state.selectedControl = state.controlInFocus;
					state.requiresRepaint = true;
				}
				else if (e.modifiers == EventModifiers.Shift)
				{
					if (state.hasPointOnBiarc == true)
					{
						this.InsertSample(e, state);
					}
				}
				else if (e.modifiers == EventModifiers.Control && state.controlInFocus is Sample sample)
				{
					this.RemoveSample(state, sample);
				}
			}
		}
		
		void HandlePosition (Event e, EditorState state)
		{
			if (state.selectedControl is Node node)
			{
				this.PositionHandleForNode(e, state, node);
			}
			else if (state.selectedControl is Tangent tangent)
			{
				this.PositionHandleForTangent(e, state, tangent);
			}
			else if (state.selectedControl is Midpoint midpoint)
			{
				this.PositionHandleForMidpoint(e, state, midpoint);
			}
			else if (state.selectedControl is Sample sample)
			{
				this.PositionHandleForSample(e, state, sample);
			}
		}
		
		void InsertNode (EditorState state)
		{
			if (state.hasPointOnPlane == false)
			{
				Debug.LogWarning(PathEditor.warningUnableToInsertNode);
				return;
			}

			Undo.RecordObject(this.component, "Insert Node");

			if (state.nodes.Count == 0)
			{
				Node node = new Node(Vector3.left, state.pointOnPlane, Vector3.right);

				state.nodes.Add(node);

				state.selectedControl = node;
				state.requiresUpdate = true;

				return;
			}

			// Get last node and tangent
			Node lastNode = state.nodes[state.nodes.Count - 1];
			Tangent lastTangent = lastNode.leftTangent;

			// ...
			Vector3 origin = lastNode.position;
			Vector3 leftTangent = Vector3.zero;
			Vector3 rightTangent = Vector3.zero;
			Vector3 destination = state.pointOnPlane;
			
			// ...
			Vector3 initialLeftTangent = -lastTangent.localPosition.normalized;
			Vector3 initialRightTangent = lastTangent.localPosition.normalized;

			// ...
			Quaternion rotation = Quaternion.LookRotation((lastNode.position - lastTangent.GetPosition()).normalized);

			Vector3 forward = rotation * Vector3.forward;
			Vector3 back = rotation * Vector3.back;
			Vector3 right = rotation * Vector3.right;
			Vector3 left = rotation * Vector3.left;

			Vector3 direction = (destination - origin).normalized;

			{
				float dotForward = Mathf.Clamp(Vector3.Dot(forward, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination) / 4.0f;
				leftTangent += (initialLeftTangent * distance) * dotForward;
				rightTangent += (back * distance) * dotForward;
			}

			{
				float dotBack = Mathf.Clamp(Vector3.Dot(back, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination);
				leftTangent += (initialLeftTangent * distance) * dotBack;
				rightTangent += (forward * distance) * dotBack;
			}

			{
				float dotRight = Mathf.Clamp(Vector3.Dot(right, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination);
				leftTangent += (initialLeftTangent * distance) * dotRight;
				rightTangent += (left * distance) * dotRight;
			}

			{
				float dotLeft = Mathf.Clamp(Vector3.Dot(left, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination);
				leftTangent += (initialLeftTangent * distance) * dotLeft;
				rightTangent += (right * distance) * dotLeft;
			}

			Vector3 middle = Vector3.Lerp(origin + leftTangent, destination + rightTangent, 0.5f);
			
			Node newNode = new Node();
			Biarc biarc = new Biarc();

			state.GenerateIdentifierForBiarc(biarc);
			biarc.Initialize(
				origin,
				origin + leftTangent,
				middle,
				middle,
				destination + rightTangent,
				destination
			);
			
			lastNode.rightTangent.localPosition = leftTangent;
			lastNode.rightMidpoint.position = biarc.leftMidpoint;

			newNode.leftMidpoint.position = biarc.rightMidpoint;
			newNode.leftTangent.localPosition = rightTangent;
			newNode.position = destination;

			state.nodes.Add(newNode);
			state.biarcs.Add(biarc);

			state.selectedControl = newNode;
			state.requiresUpdate = true;
		}
		
		/// <summary>
		/// Removes specified Node and related to it Biarcs.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="node"></param>
		void RemoveNode (EditorState state, Node node)
		{
			Undo.RecordObject(this.component, "Remove Node");

			this.GetBiarcsForNode(
				state, node,
				out Biarc leftBiarc,
				out Biarc rightBiarc,
				out Node leftNode,
				out Node rightNode
			);

			int index = state.nodes.IndexOf(node);
			state.nodes.RemoveAt(index);

			int indexOfBiarc = state.biarcs.IndexOf(leftBiarc);
			if (leftBiarc != null)
			{
				this.RemoveAssociatedSamples(state, leftBiarc);
				state.biarcs.Remove(leftBiarc);
			}

			if (rightBiarc != null)
			{
				this.RemoveAssociatedSamples(state, rightBiarc);
				state.biarcs.Remove(rightBiarc);
			}

			if (leftNode != null && rightNode != null)
			{
				Biarc biarc = new Biarc();
				state.GenerateIdentifierForBiarc(biarc);
				biarc.Initialize(
					leftNode.position,
					leftNode.rightTangent.GetPosition(),
					leftNode.rightMidpoint.position,
					rightNode.leftMidpoint.position,
					rightNode.leftTangent.GetPosition(),
					rightNode.position
				);

				leftNode.rightMidpoint.position = biarc.leftMidpoint;
				rightNode.leftMidpoint.position = biarc.rightMidpoint;

				state.biarcs.Insert(indexOfBiarc, biarc);
			}

			state.requiresUpdate = true;
		}



		void InsertSample (Event e, EditorState state)
		{
			Undo.RecordObject(this.component, "Insert Sample");

			Sample sample = new Sample();

			sample.biarcId = state.biarcInFocus.identifier;
			
			sample.distanceOnBiarc = state.distanceOnBiarc;
			sample.anchor = AnchorPosition.manual;
			Sample.ComputeDistance(state.biarcs, sample);

			sample.color = Color.black;
			sample.tilt = 0.0f;

			state.samples.Add(sample);
			state.samples.Sort();

			sample.color = this.GenerateDistinctColor(state, sample);

			state.selectedControl = sample;
			state.requiresRepaint = true;
		}
		
		void RemoveSample (EditorState state, Sample sample)
		{
			Undo.RecordObject(this.component, "Remove Sample");
			
			state.samples.Remove(sample);
			state.requiresRepaint = true;
		}
		
		void SplitBiarc (EditorState state)
		{
			Undo.RecordObject(this.component, "Split Biarc");

			Biarc removedBiarc = state.biarcInFocus;

			state.biarcInFocus.Split(
				state.pointOnBiarc, 
				state.distanceOnBiarc,
				out Biarc leftBiarc,
				out Biarc rightBiarc
			);

			int index = state.biarcs.IndexOf(state.biarcInFocus);

			state.biarcs.Insert(index, rightBiarc);
			state.biarcs.Insert(index, leftBiarc);

			leftBiarc.identifier = -1;
			rightBiarc.identifier = -1;

			state.GenerateIdentifierForBiarc(leftBiarc);
			state.GenerateIdentifierForBiarc(rightBiarc);

			state.biarcs.Remove(state.biarcInFocus);

			int indexOfLeftBiarc = state.biarcs.IndexOf(leftBiarc);
			int indexOfRightBiarc = state.biarcs.IndexOf(rightBiarc);

			Node node = new Node();
			node.position = leftBiarc.destination;
			node.leftMidpoint.position = leftBiarc.rightMidpoint;
			node.leftTangent.SetPosition(leftBiarc.rightTangent, CotangentBehaviour.manual);
			node.rightTangent.SetPosition(rightBiarc.leftTangent, CotangentBehaviour.manual);
			node.rightMidpoint.position = rightBiarc.leftMidpoint;

			node.leftMidpoint.behaviour = MidpointBehaviour.auto;
			node.rightMidpoint.behaviour = MidpointBehaviour.auto;

			node.leftMidpoint.offset = 0.0f;
			node.rightMidpoint.offset = 0.0f;

			state.nodes.Insert(indexOfRightBiarc, node);
			Node prevNode = state.nodes[indexOfRightBiarc - 1];
			Node nextNode = state.nodes[indexOfRightBiarc + 1];

			prevNode.rightTangent.SetPosition(leftBiarc.leftTangent, CotangentBehaviour.manual);
			prevNode.rightMidpoint.position = leftBiarc.leftMidpoint;
			prevNode.rightMidpoint.behaviour = MidpointBehaviour.auto;
			prevNode.rightMidpoint.offset = 0.0f;
			
			nextNode.leftTangent.SetPosition(rightBiarc.rightTangent, CotangentBehaviour.manual);
			nextNode.leftMidpoint.position = rightBiarc.rightMidpoint;
			nextNode.leftMidpoint.behaviour = MidpointBehaviour.auto;
			nextNode.leftMidpoint.offset = 0.0f;

			for (int n = 0; n < state.samples.Count; n++)
			{
				Sample sample = state.samples[n];
				if (sample.biarcId == removedBiarc.identifier)
				{
					sample.anchor = AnchorPosition.manual;
					sample.distanceOnPath = float.MinValue;

					if (sample.distanceOnBiarc <= leftBiarc.totalLength)
					{
						sample.biarcId = leftBiarc.identifier;
					}
					else
					{
						sample.distanceOnBiarc = sample.distanceOnBiarc - leftBiarc.totalLength;
						sample.biarcId = rightBiarc.identifier;
					}
				}
			}

			state.requiresUpdate = true;
		}
		
		void FocusOnSelectedControl (EditorState state)
		{
			Vector3 position;

			if (state.selectedControl is Node node)
			{
				position = node.position;
			}
			else if (state.selectedControl is Tangent tangent)
			{
				position = tangent.GetPosition();
			}
			else if (state.selectedControl is Midpoint midpoint)
			{
				position = midpoint.position;
			}
			else if (state.selectedControl is Sample sample)
			{
				position = this.GetSamplePosition(state, sample);
			}
			else
			{
				return;
			}

			position = state.transform.TransformPoint(position);
			SceneView.lastActiveSceneView.Frame(new Bounds(position, new Vector3(2f, 2f, 2f)), false);
		}

		Midpoint GetOppositeMidpoint (EditorState state, Midpoint midpoint)
		{
			int indexOfNode = state.nodes.IndexOf(midpoint.node);

			Node rightNode = null;
			Node leftNode = null;

			if (indexOfNode + 1 < state.nodes.Count)
			{
				rightNode = state.nodes[indexOfNode + 1];
			}

			if (indexOfNode - 1 >= 0)
			{
				leftNode = state.nodes[indexOfNode - 1];
			}

			if (midpoint.node.rightMidpoint == midpoint && rightNode != null)
			{
				return rightNode.leftMidpoint;
			}
			
			if (midpoint.node.leftMidpoint == midpoint && leftNode != null)
			{
				return leftNode.rightMidpoint;
			}

			return null;
		}


		void HandleRepaintForPointOnPlane (EditorState state)
		{
			Vector3 position = state.transform.TransformPoint(state.pointOnPlane);
			float handleSize = state.GetHandleSize(position, state.uiHandleSizeForPointOnPlane);
			Handles.color = state.uiColorForPointOnPlane;
			Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
		}



		void InspectorForNode (EditorState state, Node node)
		{
			int nodeIndex = state.nodes.IndexOf(node);
			EditorGUILayout.Foldout(true, $"Selected: Node ({nodeIndex} / {state.nodes.Count - 1})");

			Vector3 localPosition = EditorGUILayout.Vector3Field("Local position:", node.position);
			GUILayout.Space(10.0f);

			Vector3 prevGlobalPosition = state.transform.TransformPoint(node.position);
			Vector3 globalPosition = EditorGUILayout.Vector3Field("Global position:", prevGlobalPosition);
			GUILayout.Space(10.0f);

			if (localPosition != node.position)
			{
				Undo.RecordObject(this.component, "Move Node");
				node.position = localPosition;
				state.requiresRepaint = true;
				state.requiresUpdate = true;
			}

			if (prevGlobalPosition != globalPosition)
			{
				Undo.RecordObject(this.component, "Move Node");
				node.position = state.transform.InverseTransformPoint(globalPosition);
				state.requiresRepaint = true;
				state.requiresUpdate = true;
			}

			this.DrawUIHeader("Related controls:");
			int index = GUILayout.Toolbar(-1, PathEditor.nodeSelections);

			if (index == 0) state.selectedControl = node.leftMidpoint;
			if (index == 1) state.selectedControl = node.leftTangent;
			if (index == 2) state.selectedControl = node.rightTangent;
			if (index == 3) state.selectedControl = node.rightMidpoint;

			if (index != -1)
			{
				state.requiresRepaint = true;
				return;
			}

			if (GUI.tooltip == PathEditor.nodeSelections[0].tooltip) state.controlInFocus = node.leftMidpoint;
			if (GUI.tooltip == PathEditor.nodeSelections[1].tooltip) state.controlInFocus = node.leftTangent;
			if (GUI.tooltip == PathEditor.nodeSelections[2].tooltip) state.controlInFocus = node.rightTangent;
			if (GUI.tooltip == PathEditor.nodeSelections[3].tooltip) state.controlInFocus = node.rightMidpoint;
		}
		
		void PositionHandleForNode (Event e, EditorState state, Node node)
		{
			if (Tools.pivotRotation == PivotRotation.Global)
			{
				Quaternion rotation = Quaternion.identity;
				Vector3 prevPosition = state.transform.TransformPoint(node.position);
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, rotation);

				if (nextPosition != prevPosition)
				{
					if (e.modifiers == EventModifiers.Control)
					{
						nextPosition = Utility.SnapVector(nextPosition, state.transform.position, state.snapGridSize);
					}

					Undo.RecordObject(this.component, "Move Node");
					node.position = state.transform.InverseTransformPoint(nextPosition);
					state.requiresUpdate = true;
				}
			}
			else if (Tools.pivotRotation == PivotRotation.Local)
			{
				Quaternion rotation = state.transform.rotation;
				Vector3 prevPosition = state.transform.TransformPoint(node.position);
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, rotation);

				if (nextPosition != prevPosition)
				{
					if (e.modifiers == EventModifiers.Control)
					{
						nextPosition = Utility.SnapVector(nextPosition, state.transform, state.snapGridSize);
					}

					Undo.RecordObject(this.component, "Move Node");
					node.position = state.transform.InverseTransformPoint(nextPosition);
					state.requiresUpdate = true;
				}
			}
		}
		
		void RepaintForNode (EditorState state, Node node, int nodeIndex)
		{
			float handleSizeExtend = 0.0f;
			if (state.selectedControl == node)
			{
				Handles.color = state.uiColorForNodeSelected;
				handleSizeExtend = state.uiHandleSizeForNode / 2.0f;
			}
			else if (state.controlInFocus == node)
			{
				Handles.color = state.uiColorForNodeInFocus;
				handleSizeExtend = state.uiHandleSizeForNode / 4.0f;
			}
			else
			{
				Handles.color = state.uiColorForNode;
			}

			Vector3 position = state.transform.TransformPoint(node.position);
			float handleSize = state.GetHandleSize(position, state.uiHandleSizeForNode + handleSizeExtend);
			Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
		}
		


		void InspectorForTangent (EditorState state, Tangent tangent)
		{
			string foldoutLabel = tangent.node.leftTangent == tangent ? "Selected: Left Tangent" : "Selected: Right Tangent";
			EditorGUILayout.Foldout(true, foldoutLabel);

			Vector3 prevLocalToNodePosition = tangent.localPosition;
			Vector3 prevLocalPosition = tangent.GetPosition();
			Vector3 prevGlobalPosition = state.transform.TransformPoint(prevLocalPosition);

			Vector3 localToNodePosition = EditorGUILayout.Vector3Field("Local to Node position:", prevLocalToNodePosition);
			Vector3 localPosition = EditorGUILayout.Vector3Field("Local position:", prevLocalPosition);
			Vector3 globalPosition = EditorGUILayout.Vector3Field("Global position:", prevGlobalPosition);
			
			if (localToNodePosition != prevLocalToNodePosition)
			{
				Undo.RecordObject(this.component, "Move Tangent");
				tangent.SetPosition(localToNodePosition + tangent.node.position, state.cotangentBehaviour);
				state.requiresUpdate = true;
			}

			if (localPosition != prevLocalPosition)
			{
				Undo.RecordObject(this.component, "Move Tangent");
				tangent.SetPosition(localPosition, state.cotangentBehaviour);
				state.requiresUpdate = true;
			}

			if (globalPosition != prevGlobalPosition)
			{
				Undo.RecordObject(this.component, "Move Tangent");
				tangent.SetPosition(state.transform.InverseTransformPoint(globalPosition), state.cotangentBehaviour);
				state.requiresUpdate = true;
			}

			this.DrawUIHeader("Related controls:", true);

			if (GUILayout.Button(new GUIContent("Select Cotangent", "Cotangent")) == true)
			{
				state.selectedControl = tangent.cotangent;
				state.requiresRepaint = true;
			}
			else if (GUI.tooltip == "Cotangent")
			{
				state.controlInFocus = tangent.cotangent;
				state.requiresRepaint = true;
			}

			Tangent oppositeTangent = this.GetOppositeTangent(state, tangent);
			if (GUILayout.Button(new GUIContent("Select opposite Tangent", "Opposite Tangent")) == true && oppositeTangent != null)
			{
				state.selectedControl = oppositeTangent;
				state.requiresRepaint = true;
			}
			else if (GUI.tooltip == "Opposite Tangent" && oppositeTangent != null)
			{
				state.controlInFocus = oppositeTangent;
				state.requiresRepaint = true;
			}

			if (GUILayout.Button(new GUIContent("Select Node", "Node")) == true)
			{
				state.selectedControl = tangent.node;
				state.requiresRepaint = true;
			}
			else if (GUI.tooltip == "Node")
			{
				state.controlInFocus = tangent.node;
				state.requiresRepaint = true;
			}
		}
		
		void PositionHandleForTangent (Event e, EditorState state, Tangent tangent)
		{
			if (e.capsLock == true)
			{
				Vector3 tangentPosition = tangent.GetPosition();
				if (state.initialTangentDirectionSet == false)
				{
					Vector3 a = state.transform.TransformPoint(tangent.node.position);
					Vector3 b = state.transform.TransformPoint(tangentPosition);
					state.initialTangentDirection = (a - b).normalized;
					state.initialTangentDirectionSet = true;
				}

				Quaternion rotation = Quaternion.LookRotation(state.initialTangentDirection);
				Vector3 prevPosition = state.transform.TransformPoint(tangentPosition);
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, rotation);

				if (nextPosition != prevPosition)
				{
					if (e.modifiers == EventModifiers.Control)
					{
						nextPosition = Utility.SnapVector(nextPosition, state.transform, state.snapGridSize);
					}

					Undo.RecordObject(this.component, "Move Tangent");
					tangent.SetPosition(state.transform.InverseTransformPoint(nextPosition), state.cotangentBehaviour);
					state.requiresRepaint = true;
					state.requiresUpdate = true;
				}
			}
			else if (Tools.pivotRotation == PivotRotation.Global)
			{
				state.initialTangentDirectionSet = false;
				Quaternion rotation = Quaternion.identity;
				Vector3 prevPosition = state.transform.TransformPoint(tangent.GetPosition());
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, rotation);

				if (nextPosition != prevPosition)
				{
					if (e.modifiers == EventModifiers.Control)
					{
						nextPosition = Utility.SnapVector(nextPosition, state.transform.position, state.snapGridSize);
					}

					Undo.RecordObject(this.component, "Move Node");
					tangent.SetPosition(state.transform.InverseTransformPoint(nextPosition), state.cotangentBehaviour);
					state.requiresRepaint = true;
					state.requiresUpdate = true;
				}
			}
			else
			{
				state.initialTangentDirectionSet = false;
				Vector3 tangentPosition = tangent.GetPosition();
				Vector3 prevPosition = state.transform.TransformPoint(tangentPosition);
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, state.transform.rotation);

				if (nextPosition != prevPosition)
				{
					if (e.modifiers == EventModifiers.Control)
					{
						nextPosition = Utility.SnapVector(nextPosition, state.transform, state.snapGridSize);
					}

					Undo.RecordObject(this.component, "Move Tangent");
					tangent.SetPosition(state.transform.InverseTransformPoint(nextPosition), state.cotangentBehaviour);
					state.requiresRepaint = true;
					state.requiresUpdate = true;
				}
			}
		}
		
		void RepaintForTangent (EditorState state, Tangent tangent, int nodeIndex)
		{
			Vector3 tangentPosition = state.transform.TransformPoint(tangent.GetPosition());
			Vector3 nodePosition = state.transform.TransformPoint(tangent.node.position);
			
			Handles.color = state.uiColorForTangentLine;
			Handles.DrawDottedLine(nodePosition, tangentPosition, 2.0f);

			float handleSizeExtend = 0.0f;
			if (state.selectedControl == tangent)
			{
				Handles.color = state.uiColorForTangentSelected;
				handleSizeExtend = state.uiHandleSizeForTangent / 2.0f;
			}
			else if (state.controlInFocus == tangent)
			{
				Handles.color = state.uiColorForTangentInFocus;
				handleSizeExtend = state.uiHandleSizeForTangent / 4.0f;
			}
			else
			{
				Handles.color = state.uiColorForTangent;
			}

			float handleSize = state.GetHandleSize(tangentPosition, state.uiHandleSizeForTangent + handleSizeExtend);
			Handles.SphereHandleCap(0, tangentPosition, Quaternion.identity, handleSize, EventType.Repaint);
		}
		


		void InspectorForMidpoint (EditorState state, Midpoint midpoint)
		{
			string foldoutLabel = (midpoint.node.leftMidpoint == midpoint)
				? "Selected: Left Midpoint"
				: "Selected: Right Midpoint";

			EditorGUILayout.Foldout(true, foldoutLabel);

			Vector3 prevLocalPosition = midpoint.position;
			Vector3 prevGlobalPosition = state.transform.TransformPoint(midpoint.position);

			Vector3 localPosition = EditorGUILayout.Vector3Field("Local to Node position:", prevLocalPosition);
			Vector3 globalPosition = EditorGUILayout.Vector3Field("Global position:", prevGlobalPosition);

			this.DrawUIHeader("Behaviour and offset:", true);
			MidpointBehaviour nextBehaviour = (MidpointBehaviour)EditorGUILayout.EnumPopup("Behaviour:", midpoint.behaviour);

			if (nextBehaviour != midpoint.behaviour)
			{
				Undo.RecordObject(this.component, "Move Midpoint");
				midpoint.behaviour = nextBehaviour;
				state.requiresUpdate = true;
			}

			float nextOffset = EditorGUILayout.FloatField("Offset:", midpoint.offset);
			if (nextOffset != midpoint.offset)
			{
				Undo.RecordObject(this.component, "Move Midpoint");
				midpoint.offset = nextOffset;
				state.requiresUpdate = true;
			}

			if (localPosition != prevLocalPosition)
			{
				Undo.RecordObject(this.component, "Move Midpoint");
				midpoint.position = localPosition;
				state.requiresUpdate = true;
			}

			if (globalPosition != prevGlobalPosition)
			{
				Undo.RecordObject(this.component, "Move Midpoint");
				midpoint.position = state.transform.InverseTransformPoint(globalPosition);
				state.requiresUpdate = true;
			}

			Midpoint oppositeMidpoint = this.GetOppositeMidpoint(state, midpoint);

			this.DrawUIHeader("Related controls:", true);

			if (GUILayout.Button(new GUIContent("Select opposite Midpoint", "Opposite Midpoint")) == true && oppositeMidpoint != null)
			{
				state.selectedControl = oppositeMidpoint;
			}
			else if (GUI.tooltip == "Opposite Midpoint" && oppositeMidpoint != null)
			{
				state.controlInFocus = oppositeMidpoint;
			}

			if (GUILayout.Button(new GUIContent("Select Node", "Node")) == true)
			{
				state.selectedControl = midpoint.node;
			}
			else if (GUI.tooltip == "Node")
			{
				state.controlInFocus = midpoint.node;
			}
		}
		
		void PositionHandleForMidpoint (Event e, EditorState state, Midpoint midpoint)
		{
			int nodeIndex = state.nodes.IndexOf(midpoint.node);

			Vector3 tangentPosition;
			Vector3 cotangentPosition;

			if (midpoint.node.leftMidpoint == midpoint)
			{
				tangentPosition = midpoint.node.leftTangent.GetPosition();
				cotangentPosition = state.nodes[nodeIndex - 1].rightTangent.GetPosition();
			}
			else
			{
				tangentPosition = midpoint.node.rightTangent.GetPosition();
				cotangentPosition = state.nodes[nodeIndex + 1].leftTangent.GetPosition();
			}

			tangentPosition = state.transform.TransformPoint(tangentPosition);
			cotangentPosition = state.transform.TransformPoint(cotangentPosition);
			Vector3 direction = (tangentPosition - cotangentPosition).normalized;

			Quaternion rotation = Quaternion.LookRotation(direction, state.transform.up);
	
			Vector3 prevPosition = state.transform.TransformPoint(midpoint.position);
			Vector3 nextPosition = Handles.DoPositionHandle(prevPosition, rotation);

			if (nextPosition != prevPosition)
			{
				if (e.modifiers == EventModifiers.Control)
				{
					nextPosition = Utility.SnapVector(nextPosition, state.transform.position, state.snapGridSize);
				}

				Undo.RecordObject(this.component, "Move Midpoint");

				Vector3 midpointTangent, oppositeTangent;
				if (midpoint.node.leftMidpoint == midpoint)
				{
					state.controlInFocus = midpoint.node.leftTangent;
					midpointTangent = state.biarcInFocus.rightTangent;//midpoint.node.rightTangent.GetPosition();
					oppositeTangent = state.biarcInFocus.leftTangent;
				}
				else
				{
					state.controlInFocus = midpoint.node.rightTangent;
					midpointTangent = state.biarcInFocus.leftTangent;//midpoint.node.leftTangent.GetPosition();
					oppositeTangent = state.biarcInFocus.rightTangent;
				}

				nextPosition = state.transform.InverseTransformPoint(nextPosition);
				if (midpoint.behaviour == MidpointBehaviour.offsetFromMiddle)
				{
					Vector3 middle = Vector3.Lerp(midpointTangent, oppositeTangent, 0.5f);
					nextPosition = Utility.GetClosestPointOnFiniteLine(nextPosition, middle, midpointTangent);
					midpoint.offset = Vector3.Distance(middle, nextPosition);
				}
				else if (midpoint.behaviour == MidpointBehaviour.offsetFromTangent)
				{
					nextPosition = Utility.GetClosestPointOnFiniteLine(nextPosition, midpointTangent, oppositeTangent);
					midpoint.offset = Vector3.Distance(nextPosition, midpointTangent);
				}

				midpoint.position = nextPosition;
				state.requiresUpdate = true;
			}
		}
		
		void RepaintForMidpoint (EditorState state, Midpoint midpoint, int nodeIndex)
		{
			float handleSizeExtend = 0.0f;
			if (state.selectedControl == midpoint)
			{
				Handles.color = state.uiColorForMidpointSelected;
				handleSizeExtend = state.uiHandleSizeForMidpoint / 2.0f;
			}
			else if (state.controlInFocus == midpoint)
			{
				Handles.color = state.uiColorForMidpointInFocus;
				handleSizeExtend = state.uiHandleSizeForMidpoint / 4.0f;
			}
			else
			{
				Handles.color = state.uiColorForMidpoint;
			}

			Vector3 position = state.transform.TransformPoint(midpoint.position);
			float handleSize = state.GetHandleSize(position, state.uiHandleSizeForMidpoint + handleSizeExtend);
			Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
		}
		


		void InspectorForSample (Event e, EditorState state, Sample sample)
		{
			int sampleIndex = state.samples.IndexOf(sample);
			EditorGUILayout.Foldout(true, $"Selected: Sample ({sampleIndex} / {state.samples.Count - 1})");

			bool isChanged = false;

			// Sample.position
			AnchorPosition anchorPosition = (AnchorPosition)EditorGUILayout.EnumPopup("Snapping:", sample.anchor);
			if (anchorPosition != sample.anchor)
			{
				isChanged = true;
				Undo.RecordObject(this.component, "Change Sample");
				sample.anchor = anchorPosition;

				if (anchorPosition != AnchorPosition.manual)
				{
					EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
					sample.distanceOnBiarc = biarc.GetDistance(sample.anchor, sample.distanceOnBiarc);
				}
			
				Sample.ComputeDistance(state.biarcs, sample);
			}

			// Sample.distanceOnBiarc
			if (sample.anchor == AnchorPosition.manual)
			{
				float distanceOnBiarc = EditorGUILayout.FloatField("Distance on biarc:", sample.distanceOnBiarc);
				if (sample.distanceOnBiarc != distanceOnBiarc)
				{
					Undo.RecordObject(this.component, "Change Sample");
					isChanged = true;
					EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
					sample.distanceOnBiarc = Mathf.Clamp(distanceOnBiarc, 0.0f, biarc.totalLength);
					Sample.ComputeDistance(state.biarcs, sample);
				}
			}
			else
			{
				EditorGUILayout.FloatField("Distance on biarc: (locked)", sample.distanceOnBiarc);
			}
			
			// Sample.distanceOnPath
			float distanceOnPath = EditorGUILayout.FloatField("Distance on path:", sample.distanceOnPath);
			if (distanceOnPath != sample.distanceOnPath)
			{
				isChanged = true;
				Undo.RecordObject(this.component, "Change Sample");

				sample.distanceOnPath = Mathf.Clamp(distanceOnPath, 0.0f, state.totalLength);
				this.FindBiarcAtDistanceOnPath(state, sample.distanceOnPath, out Biarc biarc, out sample.biarcId, out sample.distanceOnBiarc);
			}

			GUILayout.Space(10.0f);

			// Sample.color
			Color color = EditorGUILayout.ColorField("Color:", sample.color);
			if (sample.color != color)
			{
				isChanged = true;
				Undo.RecordObject(this.component, "Change Sample");
				sample.color = color;
			}

			GUILayout.Space(5.0f);
			
			if (GUILayout.Button("Generate random color") == true)
			{
				Undo.RecordObject(this.component, "Change Sample");
				sample.color = this.GenerateDistinctColor(state, sample);
				isChanged = true;
			}

			GUILayout.Space(10.0f);

			// Sample.tilt
			float tilt = EditorGUILayout.FloatField("Tilt:", sample.tilt);
			if (sample.tilt != tilt)
			{
				isChanged = true;
				Undo.RecordObject(this.component, "Change Sample");
				sample.tilt = tilt;
			}

			// ...
			if (isChanged == true)
			{
				state.requiresUpdate = true;
			}
		}

		void PositionHandleForSample (Event e, EditorState state, Sample sample)
		{
			Vector3 prevPosition = this.GetSamplePosition(state, sample, out Biarc biarc, out float distanceOnBiarc);
			Vector3 transformedPrevPosition = state.transform.TransformPoint(prevPosition);
			
			Quaternion rotation = this.GetSampleRotation(state, sample, biarc, prevPosition, distanceOnBiarc);
			Vector3 nextPosition = Handles.PositionHandle(transformedPrevPosition, rotation);

			if (transformedPrevPosition != nextPosition)
			{
				this.FindBiarcInFocus(e, state);

				if (state.hasPointOnBiarc == true && state.biarcInFocus != null)
				{
					Undo.RecordObject(this.component, "Change Sample");

					sample.anchor = AnchorPosition.manual;
					sample.biarcId = state.biarcInFocus.identifier;
					sample.distanceOnBiarc = state.distanceOnBiarc;
					Sample.ComputeDistance(state.biarcs, sample);

					state.requiresUpdate = true;
				}
			}
		}

		void RepaintForSample (EditorState state, Sample sample)
		{
			Vector3 pointOnPath = this.GetSamplePosition(state, sample, out Biarc biarc, out float distanceOnBiarc);
			Vector3 transformedPointOnPath = state.transform.TransformPoint(pointOnPath);

			float handleSize = state.GetHandleSize(pointOnPath, state.uiHandleSizeForSample);
			float handleSizeExtend = 0.0f;

			Handles.color = sample.color;
			Handles.SphereHandleCap(0, transformedPointOnPath, Quaternion.identity, handleSize * 2.0f, EventType.Repaint);

			if (state.selectedControl == sample)
			{
				Handles.color = state.uiColorForSampleSelected;
				handleSizeExtend = state.uiHandleSizeForSample;
			}
			else if (state.controlInFocus == sample)
			{
				Handles.color = state.uiColorForSampleInFocus;
				handleSizeExtend = state.uiHandleSizeForSample / 2.0f;
			}
			else
			{
				Handles.color = state.uiColorForSample;
			}

			biarc.GetTangent(pointOnPath, distanceOnBiarc, out Vector3 leftTangent, out Vector3 rightTangent);

			Quaternion rotation = this.GetSampleRotation(state, sample, biarc, pointOnPath, distanceOnBiarc);

			//rotation *= Quaternion.Euler(Vector3.forward * sample.tilt);
			rotation *= Quaternion.Euler(Vector3.left * 90.0f);

			Handles.color = sample.color;
			Handles.ArrowHandleCap(0, transformedPointOnPath, rotation, 0.5f, EventType.Repaint);

			Handles.color = Color.black;
			Handles.SphereHandleCap(0, transformedPointOnPath, Quaternion.identity, handleSize + handleSizeExtend, EventType.Repaint);
		}



		bool GetPointOnPlane (EditorState state, Plane plane, Ray mouseRay, out Vector3 pointOnPlane)
		{
			if (plane.Raycast(mouseRay, out float distance) == true)
			{
				pointOnPlane = mouseRay.origin + mouseRay.direction * distance;
				pointOnPlane = state.transform.InverseTransformPoint(pointOnPlane);
				return true;
			}

			pointOnPlane = Vector3.zero;
			return false;
		}

		void FindBiarcInFocus (Event e, EditorState state, int countSegments = 32)
		{
			state.biarcInFocus = null;
			state.pointOnBiarc = Vector3.zero;
			state.distanceOnBiarc = 0.0f;

			float distanceOnScreen = float.MaxValue;

			Vector3[] vertices3d = new Vector3[countSegments];
			Vector2[] vertices2d = new Vector2[countSegments];

			for (int n = 0; n < state.biarcs.Count; n++)
			{
				Biarc biarc = state.biarcs[n];

				state.temporaryBiarc.Initialize(
					state.transform.TransformPoint(biarc.origin),
					state.transform.TransformPoint(biarc.leftTangent),
					state.transform.TransformPoint(biarc.leftMidpoint),
					state.transform.TransformPoint(biarc.rightMidpoint),
					state.transform.TransformPoint(biarc.rightTangent),
					state.transform.TransformPoint(biarc.destination)
				);

				Utility.GetNearestPointOnBiarc(
					state.temporaryBiarc,
					e.mousePosition,
					out float currentDistanceOnBiarc,
					out Vector3 currentPointOnBiarc,
					ref vertices3d,
					ref vertices2d
				);
				
				Vector2 pointOnBiarc2d = HandleUtility.WorldToGUIPoint(currentPointOnBiarc);
				float currentDistanceOnScreen = Vector2.Distance(pointOnBiarc2d, e.mousePosition);

				if (currentDistanceOnScreen < distanceOnScreen)
				{
					// Converting distance from world to local space
					state.distanceOnBiarc = (currentDistanceOnBiarc / state.temporaryBiarc.totalLength) * biarc.totalLength;
					state.pointOnBiarc = biarc.GetPoint(state.distanceOnBiarc);

					distanceOnScreen = currentDistanceOnScreen;
					state.biarcInFocus = biarc;
				}
			}

			state.hasPointOnBiarc = distanceOnScreen <= state.minimalCursorToPointOnBiarcDistance;
		}

		void FindControlInFocus (Event e, EditorState state)
		{
			float currentDistance = float.MaxValue;
			IControl targetInFocus = null;
			
			if (state.mode == EditorMode.vertices)
			{
				for (int n = 0; n < state.nodes.Count; n++)
				{
					// Node itself
					RaycastControl(state, state.nodes[n], ref targetInFocus, ref currentDistance);
					
					// Tangents
					if (n > 0) RaycastControl(state, state.nodes[n].leftTangent, ref targetInFocus, ref currentDistance);
					if (n + 1 < state.nodes.Count) RaycastControl(state, state.nodes[n].rightTangent, ref targetInFocus, ref currentDistance);

					// Midpoints
					if (n > 0)
					{
						RaycastControl(state, state.nodes[n].leftMidpoint, ref targetInFocus, ref currentDistance);
					}
					if (n + 1 < state.nodes.Count)
					{
						RaycastControl(state, state.nodes[n].rightMidpoint, ref targetInFocus, ref currentDistance);
					}
				}
			}
			else if (state.mode == EditorMode.samples)
			{
				for (int n = 0; n < state.samples.Count; n++)
				{
					RaycastControl(state, state.samples[n], ref targetInFocus, ref currentDistance);
				}
			}

			state.controlInFocus = targetInFocus;
		}

		void RaycastControl (EditorState state, IControl target, ref IControl targetInFocus, ref float currentDistance)
		{
			Vector3 position;
			float handleSize;

			if (target is Node node)
			{
				position = node.position;
				position = state.transform.TransformPoint(position);
				handleSize = state.GetHandleSize(position, state.uiHandleSizeForNode);
			}
			else if (target is Tangent tangent)
			{
				position = tangent.GetPosition();
				position = state.transform.TransformPoint(position);
				handleSize = state.GetHandleSize(position, state.uiHandleSizeForTangent);
			}
			else if (target is Midpoint midpoint)
			{
				position = midpoint.position;
				position = state.transform.TransformPoint(position);
				handleSize = state.GetHandleSize(position, state.uiHandleSizeForMidpoint);
			}
			else if (target is Sample sample)
			{
				EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
				position = biarc.GetPoint(sample.anchor, sample.distanceOnBiarc);
				position = state.transform.TransformPoint(position);
				handleSize = state.GetHandleSize(position, state.uiHandleSizeForSample);
			}
			else
			{
				Debug.Log("PathEditor.RaycastControl: Unable to determine type of IControl target.");
				Debug.Log(target);
				return;
			}


			if (Utility.RaycastSphere(state.mouseRay, position, handleSize) && state.selectedControl != target)
			{
				float distance = Vector3.Distance(position, state.mouseRay.origin);
				if (distance < currentDistance)
				{
					currentDistance = distance;
					targetInFocus = target;
				}
			}
		}

		Vector3 GetSamplePosition (EditorState state, Sample sample)
		{
			EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
			float distanceOnBiarc = (sample.distanceOnBiarc / biarc.totalLength) * biarc.totalLength;
			return biarc.GetPoint(distanceOnBiarc);
		}

		Vector3 GetSamplePosition (EditorState state, Sample sample, out Biarc biarc, out float distanceOnBiarc)
		{
			EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out biarc);
			distanceOnBiarc = (sample.distanceOnBiarc / biarc.totalLength) * biarc.totalLength;
			return biarc.GetPoint(distanceOnBiarc);
		}

		Quaternion GetSampleRotation (EditorState state, Sample sample, Biarc biarc, Vector3 pointOnBiarc, float distanceOnBiarc)
		{
			biarc.GetTangent(
				pointOnBiarc, 
				distanceOnBiarc,
				out Vector3 leftTangent,
				out Vector3 rightTangent
			);

			Vector3 direction = (rightTangent - leftTangent).normalized;
			direction = state.transform.TransformDirection(direction);
			if (direction == Vector3.zero)
			{
				direction = state.transform.up;
			}
			Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
			rotation *= Quaternion.Euler(Vector3.forward * sample.tilt);

			return rotation;
		}

		void GetSampleAtDistance (EditorState state, float distanceOnPath, out Color color, out float tilt)
		{
			this.GetSamplePair(state, distanceOnPath, out Sample left, out Sample right);

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

		void GetSamplePair (EditorState state, float distanceOnPath, out Sample left, out Sample right)
		{
			left = null;
			right = null;

			float leftPosition = float.MinValue;
			float rightPosition = float.MaxValue;

			for (int n = 0; n < state.samples.Count; n++)
			{
				Sample current = state.samples[n];

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

		void GetBiarcsForNode (EditorState state, Node node, out Biarc leftBiarc, out Biarc rightBiarc, out Node leftNode, out Node rightNode)
		{
			leftBiarc = null;
			rightBiarc = null;
			
			leftNode = null;
			rightNode = null;

			int indexOfNode = state.nodes.IndexOf(node);

			if (indexOfNode + 1 < state.nodes.Count)
			{
				rightBiarc = state.biarcs[indexOfNode];
				rightNode = state.nodes[indexOfNode + 1];
			}

			if (indexOfNode - 1 >= 0)
			{
				leftBiarc = state.biarcs[indexOfNode - 1];
				leftNode = state.nodes[indexOfNode - 1];
			}
		}

		/// <summary>
		/// Removes any sample, that points towards specified biarc.
		/// </summary>
		/// <param name="state">Current instance of EditorState.</param>
		/// <param name="biarc">Biarc that is pending removal.</param>
		void RemoveAssociatedSamples (EditorState state, Biarc biarc)
		{
			for (int n = state.samples.Count - 1; n >= 0; n--)
			{
				if (state.samples[n].biarcId == biarc.identifier)
				{
					state.samples.RemoveAt(n);
				}
			}
		}

		void FindBiarcAtDistanceOnPath (EditorState state, float distanceOnPath, out Biarc biarc, out int pointer, out float distanceOnBiarc)
		{
			float accumulatedLength = 0.0f;

			for (int n = 0; n < state.biarcs.Count; n++)
			{
				biarc = state.biarcs[n];
				if (distanceOnPath >= accumulatedLength && distanceOnPath <= accumulatedLength + biarc.totalLength)
				{
					distanceOnBiarc = distanceOnPath - accumulatedLength;
					pointer = biarc.identifier;
					return;
				}

				accumulatedLength += biarc.totalLength;
			}

			biarc = state.biarcs[state.biarcs.Count - 1];
			pointer = biarc.identifier;
			distanceOnBiarc = biarc.totalLength;
		}

		Color GenerateDistinctColor (EditorState state, Sample sample)
		{
			Color output = Color.white;
			Color previous = sample.color;
			Color current = sample.color;
			Color next = sample.color;
			
			int index = state.samples.IndexOf(sample);
			if (index > 0) previous = state.samples[index - 1].color;
			if (index + 1 < state.samples.Count) next = state.samples[index + 1].color;

			while (true)
			{
				switch (UnityEngine.Random.Range(0, 6))
				{
					case 0: output = Color.red; break;
					case 1: output = new Color(1.0f, 0.5f, 0.0f); break;
					case 2: output = Color.yellow; break;
					case 3: output = Color.green; break;
					case 4: output = Color.blue; break;
					case 5: output = Color.magenta; break;
					default: output = Color.white; break;
				}

				if (output != previous && output != current && output != next)
				{
					return output;
				}
			}
		}

		Tangent GetOppositeTangent (EditorState state, Tangent tangent)
		{
			int indexOfNode = state.nodes.IndexOf(tangent.node);

			Node rightNode = null;
			Node leftNode = null;

			if (indexOfNode + 1 < state.nodes.Count)
			{
				rightNode = state.nodes[indexOfNode + 1];
			}

			if (indexOfNode - 1 >= 0)
			{
				leftNode = state.nodes[indexOfNode - 1];
			}

			if (tangent.node.rightTangent == tangent && rightNode != null)
			{
				return rightNode.leftTangent;
			}
			
			if (tangent.node.leftTangent == tangent && leftNode != null)
			{
				return leftNode.rightTangent;
			}

			return null;
		}
		
		void FindBiarcByNode (EditorState state, Node node, out Biarc biarc)
		{
			int index = state.nodes.IndexOf(node) / 2;
			if (index == state.biarcs.Count)
			{
				biarc = state.biarcs[index - 1];
			}
			else
			{
				biarc = state.biarcs[index];
			}
		}


		/// <summary>
		/// Draw header
		/// </summary>
		/// <param name="label">Label</param>
		/// <param name="addSpaceBefore">When true, adds space before header.</param>
		void DrawUIHeader (string label, bool addSpaceBefore = false)
		{
			if (addSpaceBefore == true) GUILayout.Space(10.0f);
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
		}

		/// <summary>
		/// Draw foldout
		/// </summary>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		void DrawUIFoldout (string label, ref bool fieldValue)
		{
			bool nextValue = EditorGUILayout.Foldout(fieldValue, label);
			if (nextValue != fieldValue)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
			}
		}
		
		/// <summary>
		/// Draw float field with clamped range
		/// </summary>
		/// <param name="state">Current editor state</param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Field value</param>
		/// <param name="min">Minimum</param>
		/// <param name="max">Maximum</param>
		void DrawUIFloatField (EditorState state, string label, ref float fieldValue, float min = float.MinValue, float max = float.MaxValue)
		{
			float nextValue = EditorGUILayout.FloatField(label, fieldValue);
			nextValue = Mathf.Clamp(nextValue, min, max);

			if (Mathf.Approximately(fieldValue, nextValue) == false)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
				state.requiresRepaint = true;
			}
		}

		/// <summary>
		/// Draw integer field
		/// </summary>
		/// <param name="state">Current editor state</param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		void DrawUIIntegerField (EditorState state, string label, ref int fieldValue)
		{
			int nextValue = EditorGUILayout.IntField(label, fieldValue);
			if (nextValue != fieldValue)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
				state.requiresRepaint = true;
			}
		}

		/// <summary>
		/// Draw integer field
		/// </summary>
		/// <param name="state">Current editor state</param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Integer value</param>
		void DrawUIIntegerField (EditorState state, string label, int fieldValue)
		{
			int nextValue = EditorGUILayout.IntField(label, fieldValue);
			if (nextValue != fieldValue)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
				state.requiresRepaint = true;
			}
		}

		/// <summary>
		/// Draw toggle (boolean) field
		/// </summary>
		/// <param name="state">Current editor state</param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		void DrawUIToggleField (EditorState state, string label, ref bool fieldValue)
		{
			bool nextValue = EditorGUILayout.Toggle(label, fieldValue);
			if (nextValue != fieldValue)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
				state.requiresRepaint = true;
			}
		}

		/// <summary>
		/// Draw color field
		/// </summary>
		/// <param name="state">Current editor state</param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		void DrawUIColorField (EditorState state, string label, ref Color fieldValue)
		{
			Color nextValue = EditorGUILayout.ColorField(label, fieldValue);
			if (nextValue != fieldValue)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
				state.requiresRepaint = true;
			}
		}

		/// <summary>
		/// Draws delimiter
		/// </summary>
		/// <param name="thickness">Thickness of delimiter</param>
		/// <param name="padding">Vertical padding</param>
		void DrawUIDelimiter (int thickness = 1, int padding = 15)
		{
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
			
			r.height = thickness;
			r.y += padding / 2;
			r.x -= 18;
			r.width += 22;

			Color whiteTheme = new Color(0.7294118f, 0.7294118f, 0.7294118f);
			Color darkTheme = new Color(0.1882353f, 0.1882353f, 0.1882353f);
			EditorGUI.DrawRect(r, EditorGUIUtility.isProSkin ? darkTheme : whiteTheme);
		}



	}



}