namespace Circular
{
	using UnityEngine;
	using UnityEditor;
	using UnityEngine.Rendering;



	[CustomEditor(typeof(PathComponent))]
	public class PathEditor : Editor
	{
		public static EditorSettings settings;

		PathComponent component;
		int passiveLayoutIdentifier;

		static GUIContent[] navigationToolbar = new GUIContent[]
		{
			new GUIContent("Go left", "Select previous control on path"),
			new GUIContent("Go right", "Select next control on path"),
		};

		static readonly string warningUnableToInsertNode = "PathEditor: Unable to insert new Node. EditorState.hasPointOnPlane equals FALSE. Point on plane is a result of intersection between mouse ray and plane constructed from EditorState.transform.";



		void OnEnable ()
		{
			// ...
			this.component = this.target as PathComponent;

			// This variable stores unique identifier, that will be used
			// to prevent Unity from deselecting PathComponent when user clicks somewhere on the scene.
			this.passiveLayoutIdentifier = GUIUtility.GetControlID(FocusType.Passive);

			// Creating default instance of path
			if (this.component.path == null)
			{
				this.component.path = Path.CreateDefault();
			}

			// Creating default instance of EditorState
			if (this.component.editorState == null)
			{
				this.component.editorState = EditorState.CreateDefault();
			}

			// Configuring transform tool
			Tools.hidden = !this.component.editorState.showTransformTool;

			// Loading EditorSettings
			if (PathEditor.settings == null)
			{
				PathEditor.settings = EditorSettings.Load();
			}

			// Notifying EditorState, that PathEditor has been attached
			// This will trigger import of data from Path to EditorState
			this.component.editorState.OnEditorAttached(this.component.transform, this.component.path);

			// Subscribing to Undo/Redo actions
			Undo.undoRedoPerformed += this.HandleUndoRedo;
		}

		void OnDisable ()
		{
			// Reseting transform tool
			Tools.hidden = false;

			// Notifying EditorState, that PathEditor has been detached from PathComponent
			// This will trigger export of data from EditorState to Path
			this.component.editorState.OnEditorDetached(this.component.path);

			// Unsubscribing from Undo/Redo actions
			Undo.undoRedoPerformed -= this.HandleUndoRedo;
		}

		void OnSceneGUI ()
		{
			// Declaring commonly used local variables and their values
			Event e = Event.current;
			EditorState state = this.component.editorState;
			Path path = this.component.path;

			if (state.selectedControl != null)
			{
				this.HandlePosition(e, state, state.selectedControl);
			}

			if (e.isKey)
			{
				this.HandleKeyboard(e, state);
			}

			if (e.isMouse == true)
			{
				this.HandleMouse(e, state);
			}

			if (e.type == EventType.Repaint)
			{
				this.HandleRepaint(e, state);
			}

			if (e.type == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(this.passiveLayoutIdentifier);
			}

			// When Transform of PathComponent has been changed, we are required
			// to update normal plane and provoke PathEditor.HandleUpdate(...)
			if (state.transform.hasChanged == true)
			{
				state.transform.hasChanged = false;
				state.requiresUpdate = true;

				state.plane.Update(
					-state.transform.forward,
					state.transform.right,
					state.transform.forward - state.transform.right
				);
			}

			// Reacting to changes
			if (state.requiresUpdate == true)
			{
				state.requiresUpdate = false;
				state.requiresRepaint = true;
				this.HandleUpdate(state, path);
			}
			
			// Requesting repaint event from UnityEditor
			if (state.requiresRepaint == true)
			{
				state.requiresRepaint = false;
				this.Repaint();
			}
		}

		public override void OnInspectorGUI()
		{
			// Declaring commonly used local variables and their values
			Event e = Event.current;
			EditorState state = this.component.editorState;
			
			// EditorSettings
			if (this.DrawUIFoldout("Editor settings (global)", ref state.inspectorShowGlobalSettings) == true)
			{
				EditorGUI.indentLevel++;
				
				// EditorSettings.Rendering
				if (this.DrawUIFoldout("Rendering:", ref settings.rendering.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					
					this.DrawUIEnumField<Occlusion>(state, "Occlusion:", ref settings.rendering.occlusion);
					
					this.DrawUIEnumField<PathColor>(state, "Path color for vertices:", ref settings.rendering.pathColorForVertices);
					this.DrawUIEnumField<PathColor>(state, "Path color for samples:", ref settings.rendering.pathColorForSamples);
					
					EditorGUILayout.Space();

					this.DrawUIFloatField(state, "Path line width:", ref settings.rendering.pathLineWidth, 0.01f, 10.0f);
					this.DrawUIIntegerField(state, "Polyline batch size:", ref settings.rendering.polylineBatchSize, 16, 1024);
					this.DrawUIDelimiter();

					EditorGUI.indentLevel--;
				}

				// EditorSettings.Interaction
				if (this.DrawUIFoldout("Interaction:", ref settings.interaction.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIFloatField(state, "Snapping grid size:", ref settings.interaction.snapGridSize, 0.05f, 5.0f);
					this.DrawUIIntegerField(state, "Count segments for screen space:", ref settings.interaction.countSegmentsForScreenSpace, 8, 128);
					EditorGUILayout.HelpBox("When searching for point on path, this value will be used to create approximation of biarc. Each biarc will be converted into polyline. Polyline points will be transformed to screen space in order to determine closest point to mouse position.", MessageType.None);

					this.DrawUIColorField(state, "Color of emphasized point:", ref settings.interaction.colorOfEmphasizedPoint);
					this.DrawUIFloatField(state, "Increase focus bounds of path:", ref settings.interaction.focusBoundsForPathIncrease, 0.1f);
					this.DrawUIFloatField(state, "Increase focus bounds of control:", ref settings.interaction.focusBoundsForControlIncrease, 0.1f);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.PointOnBiarc
				if (this.DrawUIFoldout("Point on biarc:", ref settings.pointOnBiarc.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIToggleField(state, "Visible:", ref settings.pointOnBiarc.isVisible);
					this.DrawUIFloatField(state, "Handle size:", ref settings.pointOnBiarc.handleSize, 0.01f, 1.0f);
					
					EditorGUILayout.Space();
					this.DrawUIFloatField(state, "Minimum distance:", ref settings.pointOnBiarc.minimumDistance, 0.0f, 100.0f);
					EditorGUILayout.HelpBox("Minimum distance from point on biarc to mouse position for it to be considered that we have a point on path.", MessageType.None);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color:", ref settings.pointOnBiarc.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.pointOnBiarc.occludedColor);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.PointOnPlane
				if (this.DrawUIFoldout("Point on plane:", ref settings.pointOnPlane.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIToggleField(state, "Visible:", ref settings.pointOnPlane.isVisible);
					EditorGUILayout.Space();

					this.DrawUIFloatField(state, "Handle size:", ref settings.pointOnPlane.handleSize, 0.01f, 1.0f);
					EditorGUILayout.Space();

					this.DrawUIColorField(state, "Color:", ref settings.pointOnPlane.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.pointOnPlane.occludedColor);
					EditorGUILayout.Space();

					this.DrawUIColorField(state, "Line color:", ref settings.pointOnPlane.normalLineColor);
					this.DrawUIColorField(state, "Line color (occluded):", ref settings.pointOnPlane.occludedLineColor);
					EditorGUILayout.HelpBox("Line between point on biarc and point on plane, that will be drawned, if both points are enabled.", MessageType.None);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.SolidPath
				if (this.DrawUIFoldout("Solid path:", ref settings.solidPath.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIColorField(state, "Color:", ref settings.solidPath.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.solidPath.occludedColor);
					EditorGUILayout.Space();
					this.DrawUIIntegerField(state, "Count segments per arc:", ref settings.solidPath.countSegments);
					EditorGUILayout.HelpBox("Solid path is rendered in a way, that it uses least amount of vertices to define a path, with exception to arcs.", MessageType.None);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.GradientPath
				if (this.DrawUIFoldout("Gradient path:", ref settings.gradientPath.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIFloatField(state, "Occluded color gain:", ref settings.gradientPath.occludedColorGain, 0.0f, 1.0f);
					EditorGUILayout.HelpBox("How much color will be used to draw parts of path that are occluded by something.", MessageType.None);
					EditorGUILayout.Space();
					this.DrawUIIntegerField(state, "Count segments per unit length:", ref settings.gradientPath.countSegments, 1, 16);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.Normals
				if (this.DrawUIFoldout("Normals:", ref settings.normals.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIToggleField(state, "Visible:", ref settings.normals.isVisible);
			
					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color:", ref settings.normals.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.normals.occludedColor);
			
					EditorGUILayout.Space();
					this.DrawUIIntegerField(state, "Count segments per biarc:", ref settings.normals.countSegments);

					EditorGUILayout.Space();
					this.DrawUIToggleField(state, "Show only on biarc in focus:", ref settings.normals.onlyBiarcInFocus);
					this.DrawUIToggleField(state, "Use color from samples:", ref settings.normals.useSamples);
					
					EditorGUILayout.Space();
					this.DrawUIFloatField(state, "Magnitude of normals:", ref settings.normals.magnitude);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.HandleSizes
				if (this.DrawUIFoldout("Handle sizes:", ref settings.handleSizes.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIToggleField(state, "Keep constant size:", ref settings.handleSizes.keepConstant);

					EditorGUILayout.Space();
					this.DrawUIFloatField(state, "Base size for control in focus:", ref settings.handleSizes.controlInFocus, 0.01f, 5.0f);
					this.DrawUIFloatField(state, "Base size for selected control:", ref settings.handleSizes.selectedControl, 0.01f, 5.0f);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.Nodes
				if (this.DrawUIFoldout("Nodes:", ref settings.nodes.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIFloatField(state, "Handle size:", ref settings.nodes.handleSize, 0.01f, 1.0f);
					this.DrawUIFloatField(state, "Arrow handle size:", ref settings.nodes.arrowHandleSize, 0.01f, 1.0f);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color:", ref settings.nodes.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.nodes.occludedColor);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color (in focus):", ref settings.nodes.inFocusColor);
					this.DrawUIColorField(state, "Color (selected):", ref settings.nodes.selectedColor);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color for left arrow:", ref settings.nodes.leftArrowColor);
					this.DrawUIColorField(state, "Color for right arrow:", ref settings.nodes.rightArrowColor);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.Tangents
				if (this.DrawUIFoldout("Tangents:", ref settings.tangents.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIFloatField(state, "Handle size:", ref settings.tangents.handleSize, 0.01f, 1.0f);
					this.DrawUIFloatField(state, "Arrow handle size:", ref settings.tangents.arrowHandleSize, 0.01f, 1.0f);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color:", ref settings.tangents.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.tangents.occludedColor);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color (in focus):", ref settings.tangents.inFocusColor);
					this.DrawUIColorField(state, "Color (selected):", ref settings.tangents.selectedColor);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color for line:", ref settings.tangents.lineColor);
					this.DrawUIColorField(state, "Color for line (occluded):", ref settings.tangents.occludedLineColor);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color for opposite tangent:", ref settings.tangents.oppositeTangentColor);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.Midpoints
				if (this.DrawUIFoldout("Midpoints:", ref settings.midpoints.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIFloatField(state, "Handle size:", ref settings.midpoints.handleSize, 0.01f, 1.0f);
					this.DrawUIFloatField(state, "Arrow handle size:", ref settings.midpoints.arrowHandleSize, 0.01f, 1.0f);
					
					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color:", ref settings.midpoints.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.midpoints.occludedColor);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color (in focus):", ref settings.midpoints.inFocusColor);
					this.DrawUIColorField(state, "Color (selected):", ref settings.midpoints.selectedColor);
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.Samples
				if (this.DrawUIFoldout("Samples:", ref settings.samples.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIFloatField(state, "Handle size:", ref settings.samples.handleSize, 0.01f, 1.0f);
					this.DrawUIFloatField(state, "Arrow handle size:", ref settings.samples.arrowHandleSize, 0.01f, 1.0f);
					this.DrawUIFloatField(state, "Tilt handle size:", ref settings.samples.tiltHandleSize, 0.1f);
					this.DrawUIFloatField(state, "Tilt snapping interval:", ref settings.samples.tiltSnappingInterval, 0.1f);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color:", ref settings.samples.normalColor);
					this.DrawUIColorField(state, "Color (occluded):", ref settings.samples.occludedColor);

					EditorGUILayout.Space();
					this.DrawUIColorField(state, "Color (in focus):", ref settings.samples.inFocusColor);
					this.DrawUIColorField(state, "Color (selected):", ref settings.samples.selectedColor);

					EditorGUILayout.Space();
					this.DrawUIFloatField(state, "Snapping distance:", ref settings.samples.anchorSnappingMinimum);
					
					this.DrawUIDelimiter();
					EditorGUI.indentLevel--;
				}

				// EditorSettings.Inspector
				if (this.DrawUIFoldout("Inspector:", ref settings.inspector.isFoldoutOpen) == true)
				{
					EditorGUI.indentLevel++;
					this.DrawUIColorField(state, "Delimiter white theme:", ref settings.inspector.delimiterWhiteTheme);
					this.DrawUIColorField(state, "Delimiter dark theme:", ref settings.inspector.delimiterDarkTheme);
					EditorGUI.indentLevel--;
				}

				EditorGUI.indentLevel--;
			}
			
			this.DrawUIDelimiter();
			
			// EditorState
			if (this.DrawUIFoldout("Editor settings (instance)", ref state.inspectorShowLocalSettings) == true)
			{
				EditorGUI.indentLevel++;
				// Editor mode
				if (this.DrawUIEnumField<EditorMode>(state, "Editor mode:", ref state.mode) == true)
				{
					state.controlInFocus = null;
					state.selectedControl = null;
					state.requiresRepaint = true;
				}

				// Cotangent behaviour
				this.DrawUIEnumField<CotangentBehaviour>(state, "Cotangent behaviour:", ref state.cotangentBehaviour);

				// Transform tool
				if (this.DrawUIToggleField(state, "Show transform tool:", ref state.showTransformTool) == true)
				{
					Tools.hidden = !state.showTransformTool;
				}
				EditorGUI.indentLevel--;
			}

			this.DrawUIDelimiter();

			// IControl
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
				EditorGUI.indentLevel--;
			}
			else
			{
				EditorGUILayout.Foldout(false, "Selected: None");
			}
		}

		/// <summary>
		/// Updates current instance of EditorState.
		/// Called from OnSceneGUI, when EditorState.requiresUpdate is set to true.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="path">Instance of <see cref="Path" /></param>
		void HandleUpdate (EditorState state, Path path)
		{
			// Resting total length of path
			state.pathTotalLength = 0.0f;

			// Compute biarcs from nodes and adjust midpoints
			for (int n = 0; n < state.nodes.Count - 1; n++)
			{
				Biarc biarc = state.biarcs[n];

				state.pathTotalLength += biarc.totalLength;

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
				EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
				Sample.ComputeDistance(state.biarcs, sample);
			}

			// Sort samples based on .distanceOnPath (see Sample.CompareTo) and apply changes to Path
			state.samples.Sort();
			state.ApplyChanges(path);
		}

		/// <summary>
		/// Event handler, that is triggered by UnityEditor after an undo or redo action was executed.
		/// </summary>
		void HandleUndoRedo ()
		{
			EditorState state = this.component.editorState;
			this.HandleUpdate(state, this.component.path);

			state.selectedControl = null;
			state.controlInFocus = null;
			state.biarcInFocus = null;
			state.hasPointOnBiarc = false;
			state.hasPointOnPlane = false;
		}

		/// <summary>
		/// Handles repaint of scene.
		/// Invoked by OnSceneGUI when EventType.Repaint fired.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleRepaint (Event e, EditorState state)
		{
			// Ensure, that size of polylineBatch is the same as specified in settings
			state.polylineBatch.EnsureSize(settings.rendering.polylineBatchSize);
			state.polylineBatch.occlusion = settings.rendering.occlusion;

			// Rendering normals
			if (settings.normals.isVisible == true)
			{
				this.HandleRepaintForNormals(state);
			}

			// Retrieving PathColor based on EditorMode
			PathColor pathColor = state.mode == EditorMode.vertices
				? settings.rendering.pathColorForVertices
				: settings.rendering.pathColorForSamples;
			
			// Rendering path in specified mode
			if (pathColor == PathColor.solid)
			{
				this.HandleRepaintForSolidPath(state);
			}
			else// if (pathColor == PathColor.gradient)
			{
				this.HandleRepaintForGradientPath(state);
			}

			// Rendering for specific EditorMode
			if (state.mode == EditorMode.vertices)
			{
				this.HandleRepaintForVertices(state);
			}
			else// if (state.mode == EditorMode.samples)
			{
				this.HandleRepaintForSamples(state);
			}

			// Rendering point on biarc/plane
			this.HandleRepaintForInputPoints(state);

			// Rendering selected control
			if (state.selectedControl is Node selectedNode)
			{
				this.RepaintForNode(state, selectedNode, ControlState.selected);
			}
			else if (state.selectedControl is Tangent selectedTangent)
			{
				this.RepaintForTangent(state, selectedTangent, ControlState.selected);
			}
			else if (state.selectedControl is Midpoint selectedMidpoint)
			{
				this.RepaintForMidpoint(state, selectedMidpoint, ControlState.selected);
			}
			else if (state.selectedControl is Sample selectedSample)
			{
				this.RepaintForSample(state, selectedSample, ControlState.selected);
			}

			// Rendering control in focus
			if (state.controlInFocus is Node nodeInFocus)
			{
				this.RepaintForNode(state, nodeInFocus, ControlState.focused);
			}
			else if (state.controlInFocus is Tangent tangentInFocus)
			{
				this.RepaintForTangent(state, tangentInFocus, ControlState.focused);
			}
			else if (state.controlInFocus is Midpoint midpointInFocus)
			{
				this.RepaintForMidpoint(state, midpointInFocus, ControlState.focused);
			}
			else if (state.controlInFocus is Sample sampleInFocus)
			{
				this.RepaintForSample(state, sampleInFocus, ControlState.focused);
			}
		}

		/// <summary>
		/// Handles repaint of normals
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleRepaintForNormals (EditorState state)
		{
			float distanceOnPath = 0.0f;

			// Drawing normals only on biarc in focus
			if (settings.normals.onlyBiarcInFocus == true)
			{
				// If there is not biarc in focus, we have nothing to do
				if (state.biarcInFocus == null) return;

				// Looping over biarcs to calculate distance on path
				for (int n = 0; n < state.biarcs.Count; n++)
				{
					Biarc biarc = state.biarcs[n];
					if (biarc == state.biarcInFocus)
					{
						// We've reached biarc in focus
						this.DrawNormalsOnBiarc(state, biarc, ref distanceOnPath);
						return;
					}
					else
					{
						// Accumulate distance on path
						distanceOnPath += biarc.totalLength;
					}
				}
			}

			// Drawing normals on each biarc
			for (int n = 0; n < state.biarcs.Count; n++)
			{
				this.DrawNormalsOnBiarc(state, state.biarcs[n], ref distanceOnPath);
			}
		}

		/// <summary>
		/// Handles repaint of path for case: PathColor.solid
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleRepaintForSolidPath (EditorState state)
		{
			// ...
			state.polylineBatch.Clear();

			// ...
			state.polylineBatch.lineWidth = settings.rendering.pathLineWidth;
			state.polylineBatch.normalColor = settings.solidPath.normalColor;
			state.polylineBatch.occludedColor = settings.solidPath.occludedColor;

			// Draw each biarc using polyline
			for (int n = 0; n < state.biarcs.Count; n++)
			{
				state.polylineBatch.AddBiarc(state.biarcs[n], state.transform, settings.solidPath.countSegments);
			}

			// For case, when polyline batch limit has not been exceeded.
			state.polylineBatch.Render(true);
		}

		/// <summary>
		/// Handles repaint of path for case: PathColor.gradient
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleRepaintForGradientPath (EditorState state)
		{
			// Distance on path used for computing sample's data
			float distanceOnPath = 0.0f;

			// ...
			for (int n = 0; n < state.biarcs.Count; n++)
			{
				Biarc biarc = state.biarcs[n];
				
				// Calculating count of segments per biarc
				// Compared to PathColor.solid we're not using PolylineBatch because samples can be anywhere,
				// we cannot make straight lines from origin to start of arc for example.
				int countSegments = (int)(biarc.totalLength * (float)settings.gradientPath.countSegments);
				float delta = biarc.totalLength / (float)(countSegments - 1);
				float distanceOnBiarc = delta;

				// First point
				Vector3 prev = biarc.GetPoint(0.0f);
				prev = state.transform.TransformPoint(prev);

				// ...
				for (int i = 0; i < countSegments; i++)
				{
					// ...
					Vector3 next = biarc.GetPoint(distanceOnBiarc);
					next = state.transform.TransformPoint(next);

					// Retrieving sample's data at specified distance and drawing a line
					state.GetSampleAtDistance(distanceOnPath + distanceOnBiarc, out Color color, out float tilt);
					this.DrawLine(settings.rendering.pathLineWidth, color, color * settings.gradientPath.occludedColorGain, prev, next);
					
					// Advance
					prev = next;
					distanceOnBiarc += delta;
				}

				// Advance
				distanceOnPath += biarc.totalLength;
			}
		}

		/// <summary>
		/// Handles repaint of two points, point on biarc in focus and point on plane.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleRepaintForInputPoints (EditorState state)
		{
			if (state.selectedControl != null) return;

			// ...
			Vector3 pointOnBiarc = Vector3.zero;
			Vector3 pointOnPlane = Vector3.zero;


			// Drawing point on biarc
			if (settings.pointOnBiarc.isVisible == true && state.hasPointOnBiarc == true)
			{
				pointOnBiarc = state.transform.TransformPoint(state.pointOnBiarc);
				float handleSize = this.GetHandleSize(pointOnBiarc, settings.nodes.handleSize);
				this.DrawSphere(pointOnBiarc, handleSize, settings.pointOnBiarc.normalColor, settings.pointOnBiarc.occludedColor);
			}

			// Drawing point on plane
			if (state.hasPointOnPlane == true && settings.pointOnPlane.isVisible == true)
			{
				pointOnPlane = state.transform.TransformPoint(state.pointOnPlane);
				float handleSize = this.GetHandleSize(pointOnPlane, settings.pointOnPlane.handleSize);
				this.DrawSphere(pointOnPlane, handleSize, settings.pointOnPlane.normalColor, settings.pointOnPlane.occludedColor);
			}

			// Drawing line between point on biarc and point on plane, when both enabled
			if (settings.pointOnBiarc.isVisible == true && settings.pointOnPlane.isVisible == true)
			{
				// Checking that we have both points
				if (state.hasPointOnBiarc == true && state.hasPointOnPlane == true)
				{
					this.DrawLine(
						settings.rendering.pathLineWidth,
						settings.pointOnPlane.normalLineColor,
						settings.pointOnPlane.occludedLineColor,
						pointOnBiarc,
						pointOnPlane
					);
				}
			}
		}

		/// <summary>
		/// Handles repaint of nodes, tangents and midpoints.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleRepaintForVertices (EditorState state)
		{
			for (int n = 0; n < state.biarcs.Count; n++)
			{
				this.HandleRepaintForBiarc(state, state.biarcs[n]);
			}
		}

		/// <summary>
		/// Handles repaint of samples.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleRepaintForSamples (EditorState state)
		{
			if (state.isSnappingEnabled == true && state.biarcInFocus != null && state.selectedControl is Sample sample)
			{
				float handleSize = settings.nodes.handleSize;
				Color normalColor = settings.nodes.normalColor;
				Color obscuredColor = settings.nodes.occludedColor;

				var values = System.Enum.GetValues(typeof(AnchorPosition));
				foreach (var value in values)
				{
					float distanceOnBiarc = state.biarcInFocus.GetDistance((AnchorPosition)value, 0.0f);
					Vector3 pointOnBiarc = state.biarcInFocus.GetPoint(distanceOnBiarc);
					Vector3 position = state.transform.TransformPoint(pointOnBiarc);
					handleSize = this.GetHandleSize(position, settings.nodes.handleSize);

					float distanceOnPath = (state.distanceOnPath - state.distanceOnBiarc) + distanceOnBiarc;
					state.GetSampleAtDistance(distanceOnPath, out Color color, out float tilt);

					this.DrawSphere(position, handleSize, color, obscuredColor);
				}

				this.RepaintForSample(state, sample, ControlState.selected);
			}
			else
			{
				for (int n = 0; n < state.samples.Count; n++)
				{
					this.RepaintForSample(state, state.samples[n], ControlState.normal);
				}
			}
		}

		/// <summary>
		/// Handles keyboard events.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleKeyboard (Event e, EditorState state)
		{
			// Grid snapping
			state.isSnappingEnabled = (e.control || e.command) && state.selectedControl != null;
		}

		/// <summary>
		/// Handles mouse events.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleMouse (Event e, EditorState state)
		{
			state.mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			state.hasPointOnPlane = this.GetPointOnPlane(state, state.plane, state.mouseRay, out state.pointOnPlane);

			this.FindBiarcInFocus(e, state, settings.interaction.countSegmentsForScreenSpace, settings.pointOnBiarc.minimumDistance);
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

		/// <summary>
		/// Handles mouse events, when EditorState.mode equals to EditorMode.vertices
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleMouseForVertices (Event e, EditorState state)
		{
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				// Make control in focus selected
				if (e.modifiers == EventModifiers.None)
				{
					state.selectedControl = state.controlInFocus;
					state.requiresRepaint = true;
				}

				// Split biarc in focus
				else if (e.shift && state.hasPointOnBiarc == true)
				{
					this.SplitBiarc(state);
				}

				// Insert node
				else if ((e.control || e.command) && state.controlInFocus == null)
				{
					this.InsertNode(state);
				}

				// Remove node in focus
				else if ((e.control || e.command) && state.controlInFocus is Node node)
				{
					this.RemoveNode(state, node);
				}
			}
		}

		/// <summary>
		/// Handles mouse events, when EditorState.mode equals to EditorMode.samples
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void HandleMouseForSamples (Event e, EditorState state)
		{
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				// Make control in focus selected
				if (e.modifiers == EventModifiers.None)
				{
					state.selectedControl = state.controlInFocus;
					state.requiresRepaint = true;
				}

				// Insert sample
				else if ((e.control || e.command) && state.controlInFocus == null)
				{
					if (state.hasPointOnBiarc == true)
					{
						this.InsertSample(e, state);
					}
				}

				// Remove sample
				else if ((e.control || e.command) && state.controlInFocus is Sample sample)
				{
					this.RemoveSample(state, sample);
				}
			}
		}

		/// <summary>
		/// Operates with position handles for target control.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="target">Target <see cref="IControl" /></param>
		void HandlePosition (Event e, EditorState state, IControl target)
		{
			if (target is Node node)
			{
				this.PositionHandleForNode(e, state, node);
			}
			else if (target is Tangent tangent)
			{
				this.PositionHandleForTangent(e, state, tangent);
			}
			else if (target is Midpoint midpoint)
			{
				this.PositionHandleForMidpoint(e, state, midpoint);
			}
			else if (target is Sample sample)
			{
				this.PositionHandleForSample(e, state, sample);
			}
		}

		/// <summary>
		/// Inserts new <see cref="Node" /> using parameters of specified <see cref="EditorState" />
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void InsertNode (EditorState state)
		{
			// When intersection test between mouse ray and plane has failed
			if (state.hasPointOnPlane == false)
			{
				Debug.LogWarning(PathEditor.warningUnableToInsertNode);
				return;
			}

			// ..
			Undo.RecordObject(this.component, "Insert Node");
			state.requiresUpdate = true;

			// Case, when there are no nodes yet
			if (state.nodes.Count == 0)
			{
				// Create new node, add it to the list and selected it.
				Node node = new Node(-state.transform.right, state.pointOnPlane, state.transform.right);
				state.nodes.Add(node);
				state.selectedControl = node;
				return;
			}

			// Code bellow calculates position of the node's left tangent,
			// which is a blend between four possible positions.
			// The closer a certain direction points towards a new node,
			// the more weight it has. 

			// Get last node and tangent
			Node lastNode = state.nodes[state.nodes.Count - 1];
			Tangent lastTangent = lastNode.leftTangent;

			// Calculating rotation in direction from last left tangent to its node
			Quaternion rotation = Quaternion.LookRotation((lastNode.position - lastTangent.GetPosition()).normalized);
			
			// Directions
			Vector3 forward = rotation * Vector3.forward;
			Vector3 back = rotation * Vector3.back;
			Vector3 right = rotation * Vector3.right;
			Vector3 left = rotation * Vector3.left;

			// Origin and destination of new biarc
			Vector3 origin = lastNode.position;
			Vector3 destination = state.pointOnPlane;

			// Tangents will be a blend between four directions.
			Vector3 leftTangent = Vector3.zero;
			Vector3 rightTangent = Vector3.zero;
			
			// ...
			Vector3 initialLeftTangent = -lastTangent.localPosition.normalized;
			Vector3 initialRightTangent = lastTangent.localPosition.normalized;

			Vector3 direction = (destination - origin).normalized;

			// Forward
			{
				float dotProduct = Mathf.Clamp(Vector3.Dot(forward, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination) / 4.0f;
				leftTangent += (initialLeftTangent * distance) * dotProduct;
				rightTangent += (back * distance) * dotProduct;
			}

			// Back
			{
				float dotProduct = Mathf.Clamp(Vector3.Dot(back, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination);
				leftTangent += (initialLeftTangent * distance) * dotProduct;
				rightTangent += (forward * distance) * dotProduct;
			}

			// Right
			{
				float dotProduct = Mathf.Clamp(Vector3.Dot(right, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination);
				leftTangent += (initialLeftTangent * distance) * dotProduct;
				rightTangent += (left * distance) * dotProduct;
			}

			// Left
			{
				float dotProduct = Mathf.Clamp(Vector3.Dot(left, direction), 0.0f, 1.0f) / 4.0f;
				float distance = Vector3.Distance(origin, destination);
				leftTangent += (initialLeftTangent * distance) * dotProduct;
				rightTangent += (right * distance) * dotProduct;
			}

			// Middle between tangents will be a position for both midpoints of new biarc
			Vector3 middle = Vector3.Lerp(origin + leftTangent, destination + rightTangent, 0.5f);
			
			// ...
			Node newNode = new Node();
			Biarc biarc = new Biarc();

			// Generating unique identifier for biarc
			state.GenerateIdentifierForBiarc(biarc);

			// ...
			biarc.Initialize(origin, origin + leftTangent, middle, middle, destination + rightTangent, destination);
			
			// Adjusting positions for tangent and midpoint of existing node
			lastNode.rightTangent.localPosition = leftTangent;
			lastNode.rightMidpoint.position = biarc.leftMidpoint;

			// Adjusting positions for tangent and midpoint of new node
			newNode.leftMidpoint.position = biarc.rightMidpoint;
			newNode.leftTangent.localPosition = rightTangent;

			// ...
			newNode.position = destination;

			// Adding node and biarc to the list
			state.nodes.Add(newNode);
			state.biarcs.Add(biarc);

			// Selecting a newly create node
			state.selectedControl = newNode;
		}

		/// <summary>
		/// Removes specified Node and related to it Biarcs.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="node"></param>
		void RemoveNode (EditorState state, Node node)
		{
			// ...
			Undo.RecordObject(this.component, "Remove Node");
			state.requiresUpdate = true;

			// First, find adjacent nodes and biarcs
			this.GetBiarcsForNode(
				state, node,
				out Biarc leftBiarc,
				out Biarc rightBiarc,
				out Node leftNode,
				out Node rightNode
			);

			// ...
			state.nodes.Remove(node);

			// Remove biarc on the left side of the node
			int indexOfBiarc = state.biarcs.IndexOf(leftBiarc);
			if (leftBiarc != null)
			{
				this.RemoveAssociatedSamples(state, leftBiarc);
				state.biarcs.Remove(leftBiarc);
			}

			// Remove biarc on the right side of the node
			if (rightBiarc != null)
			{
				this.RemoveAssociatedSamples(state, rightBiarc);
				state.biarcs.Remove(rightBiarc);
			}

			// When target node has two neighboring nodes, we are required
			// to generate new biarc for adjacent nodes.
			if (leftNode != null && rightNode != null)
			{
				// ...
				Biarc biarc = new Biarc();

				// ...
				state.GenerateIdentifierForBiarc(biarc);

				// ...
				biarc.Initialize(
					leftNode.position,
					leftNode.rightTangent.GetPosition(),
					leftNode.rightMidpoint.position,
					rightNode.leftMidpoint.position,
					rightNode.leftTangent.GetPosition(),
					rightNode.position
				);

				// Adjust midpoints
				leftNode.rightMidpoint.position = biarc.leftMidpoint;
				rightNode.leftMidpoint.position = biarc.rightMidpoint;

				// Insert new biarc at specific position
				state.biarcs.Insert(indexOfBiarc, biarc);
			}
		}

		/// <summary>
		/// Inserts sample to specified instance of EditorState
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void InsertSample (Event e, EditorState state)
		{
			// ...
			Undo.RecordObject(this.component, "Insert Sample");
			state.requiresRepaint = true;

			Sample sample = new Sample();

			// Identifier of biarc, that this sample will be pinned to
			sample.biarcId = state.biarcInFocus.identifier;
			
			// New Sample always have manual anchor position
			sample.anchor = AnchorPosition.manual;

			// Distance on biarc and on path
			sample.distanceOnBiarc = state.distanceOnBiarc;
			Sample.ComputeDistance(state.biarcs, sample);

			// Setting default values
			sample.color = Color.black;
			sample.tilt = 0.0f;

			// Adding new sample to list and sorting it
			state.samples.Add(sample);
			state.samples.Sort();

			// Generating distinct color for new sample
			sample.color = this.GenerateDistinctColor(state, sample);

			// Selecting new sample for user to adjust its properties
			state.selectedControl = sample;
		}

		/// <summary>
		/// Removes specified Sample
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="sample">Target <see cref="Sample" /></param>		
		void RemoveSample (EditorState state, Sample sample)
		{
			// ...
			Undo.RecordObject(this.component, "Remove Sample");
			state.requiresRepaint = true;
			
			// ...
			state.samples.Remove(sample);
		}

		/// <summary>
		/// Splits Biarc into two at specified distance on Biarc.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void SplitBiarc (EditorState state)
		{
			// ...
			Undo.RecordObject(this.component, "Split Biarc");
			state.requiresUpdate = true;

			// ...
			Biarc removedBiarc = state.biarcInFocus;
			int index = state.biarcs.IndexOf(state.biarcInFocus);

			// First, we need to split biarc itself
			state.biarcInFocus.Split(
				state.pointOnBiarc, 
				state.distanceOnBiarc,
				out Biarc leftBiarc,
				out Biarc rightBiarc
			);

			// Inserting new biarcs at position where soon to be removed biarc is
			state.biarcs.Insert(index, rightBiarc);
			state.biarcs.Insert(index, leftBiarc);

			// Generating unique identifier for each new biarc
			state.GenerateIdentifierForBiarc(leftBiarc);
			state.GenerateIdentifierForBiarc(rightBiarc);

			// Removing biarc that we've splitted, because we have two now
			state.biarcs.Remove(state.biarcInFocus);

			// Retrieving indices of new biarcs
			int indexOfRightBiarc = state.biarcs.IndexOf(rightBiarc);

			// Creating new node
			Node node = new Node();
			node.position = leftBiarc.destination;

			// Adjusting tangents and midpoints
			node.leftMidpoint.position = leftBiarc.rightMidpoint;
			node.leftTangent.SetPosition(leftBiarc.rightTangent, CotangentBehaviour.manual);
			node.rightTangent.SetPosition(rightBiarc.leftTangent, CotangentBehaviour.manual);
			node.rightMidpoint.position = rightBiarc.leftMidpoint;

			// Default behaviour for midpoints
			node.leftMidpoint.behaviour = MidpointBehaviour.auto;
			node.rightMidpoint.behaviour = MidpointBehaviour.auto;

			// Because behaviour of midpoints is set to .auto, offset will be zero
			node.leftMidpoint.offset = 0.0f;
			node.rightMidpoint.offset = 0.0f;

			// Inserting new node
			state.nodes.Insert(indexOfRightBiarc, node);

			// Retrieving nodes between new node to adjust their tangents and midpoints
			Node prevNode = state.nodes[indexOfRightBiarc - 1];
			Node nextNode = state.nodes[indexOfRightBiarc + 1];

			// ...
			prevNode.rightTangent.SetPosition(leftBiarc.leftTangent, CotangentBehaviour.manual);
			prevNode.rightMidpoint.position = leftBiarc.leftMidpoint;

			// Reseting midpoint behaviour and offset
			prevNode.rightMidpoint.behaviour = MidpointBehaviour.auto;
			prevNode.rightMidpoint.offset = 0.0f;
			
			// ...
			nextNode.leftTangent.SetPosition(rightBiarc.rightTangent, CotangentBehaviour.manual);
			nextNode.leftMidpoint.position = rightBiarc.rightMidpoint;

			// Reseting midpoint behaviour and offset
			nextNode.leftMidpoint.behaviour = MidpointBehaviour.auto;
			nextNode.leftMidpoint.offset = 0.0f;

			// Adjusting each sample, that was pointing at removed biarc
			for (int n = 0; n < state.samples.Count; n++)
			{
				Sample sample = state.samples[n];

				// ...
				if (sample.biarcId == removedBiarc.identifier)
				{
					// Resetting anchor position to .manual
					// Theoretically, we could try to find anchor but, meh.
					sample.anchor = AnchorPosition.manual;

					// Distance on path will be recalculated anyway in HandleUpdate
					sample.distanceOnPath = 0.0f;

					// Sample that was pointing on removed biarc is either on left, or on right biarc.
					// Which one, can be determined by comparing .distanceOnBiarc to totalLength of new left biarc.
					if (sample.distanceOnBiarc <= leftBiarc.totalLength)
					{
						sample.biarcId = leftBiarc.identifier;
					}
					else
					{
						// This is the case, when sample will be positioned at right biarc,
						// in this case we are required to calculate new distanceOnBiarc
						// for it minus total length of left biarc.
						sample.distanceOnBiarc = sample.distanceOnBiarc - leftBiarc.totalLength;
						sample.biarcId = rightBiarc.identifier;
					}
				}
			}

		}

		/// <summary>
		/// Returns transformed bounds of target control.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="control">Target <see cref="IControl" /></param>
		/// <returns>Transformed bounds of target control</returns>
		Bounds GetControlBounds (EditorState state, IControl control)
		{
			// ...
			Bounds bounds = new Bounds();
			bounds.size = Vector3.one * settings.interaction.focusBoundsForControlIncrease;

			// Retrieving position of control
			if (control is Node node)
			{
				bounds.center = node.position;
			}
			else if (control is Tangent tangent)
			{
				bounds.center = tangent.GetPosition();
			}
			else if (control is Midpoint midpoint)
			{
				bounds.center = midpoint.position;
			}
			else// if (control is Sample sample)
			{
				Sample sample = control as Sample;
				EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
				bounds.center = biarc.GetPoint(sample.anchor, sample.distanceOnBiarc);
				bounds.size = Vector3.one;
			}

			// Transforming point to world space
			bounds.center = state.transform.TransformPoint(bounds.center);
			return bounds;
		}

		/// <summary>
		/// Calculates bounds of entire path.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <returns>Bounds of entire path.</returns>
		Bounds GetPathBounds (EditorState state)
		{
			// ...
			Bounds bounds = new Bounds();

			// Iterating over nodes
			for (int n = 0; n < state.nodes.Count; n++)
			{
				Node node = state.nodes[n];

				// Node position
				bounds.Encapsulate(node.position);

				// Left tangent, excluding first node
				if (n > 0)
				{
					bounds.Encapsulate(node.position + node.leftTangent.localPosition);	
				}

				// Right tangent, excluding last node
				if (n + 1 < state.nodes.Count)
				{
					bounds.Encapsulate(node.position + node.rightTangent.localPosition);
				}
			}

			// Transforming bounds to world space
			bounds.center = state.transform.TransformPoint(bounds.center);
			bounds.size = state.transform.TransformVector(bounds.size * settings.interaction.focusBoundsForPathIncrease);

			// ...
			return bounds;
		}

		/// <summary>
		/// Handles repaint for inspector, when currently selected control is Node
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="node">Target <see cref="Node" /></param>
		void InspectorForNode (EditorState state, Node node)
		{
			// ...
			this.DrawNavigationToolbarForVertices(state, node, node);

			// Drawing foldout for currently selected control
			int nodeIndex = state.nodes.IndexOf(node);
			EditorGUILayout.Foldout(true, $"Selected: Node ({nodeIndex} / {state.nodes.Count - 1})");
			EditorGUI.indentLevel++;

			if (Tools.pivotRotation == PivotRotation.Global)
			{
				// Transforming point to world space
				Vector3 prevValue = state.transform.TransformPoint(node.position);
				Vector3 nextValue = EditorGUILayout.Vector3Field("Position: (Global)", prevValue);
				EditorGUILayout.Space();

				// Value has been changed
				if (prevValue != nextValue)
				{
					// ...
					Undo.RecordObject(this.component, "Move Node");
					state.requiresUpdate = true;

					// ...
					node.position = state.transform.InverseTransformPoint(nextValue);
				}
			}
			else if (Tools.pivotRotation == PivotRotation.Local)
			{
				// Local space, no need to transform point
				Vector3 nextValue = EditorGUILayout.Vector3Field("Position: (Local)", node.position);
				EditorGUILayout.Space();

				// Value has been changed
				if (nextValue != node.position)
				{
					// ...
					Undo.RecordObject(this.component, "Move Node");
					state.requiresUpdate = true;

					// ...
					node.position = nextValue;
				}
			}
		}

		/// <summary>
		/// Provides position handle for target Node.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="node">Target <see cref="Node" /></param>
		void PositionHandleForNode (Event e, EditorState state, Node node)
		{
			if (Tools.pivotRotation == PivotRotation.Global)
			{
				// ...
				Vector3 prevPosition = state.transform.TransformPoint(node.position);
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, Quaternion.identity);

				// Value has been changed
				if (nextPosition != prevPosition)
				{
					// ...
					Undo.RecordObject(this.component, "Move Node");
					state.requiresUpdate = true;

					// ...
					if (state.isSnappingEnabled == true)
					{
						nextPosition = Utility.SnapVector(nextPosition, settings.interaction.snapGridSize);
					}

					// ...
					node.position = state.transform.InverseTransformPoint(nextPosition);
				}
			}
			else if (Tools.pivotRotation == PivotRotation.Local)
			{
				// For local space we are still transforming point, but instead of using
				// Quaternion.identity, we are using rotation from PathComponent.transform
				Vector3 prevPosition = state.transform.TransformPoint(node.position);
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, state.transform.rotation);

				// Value has been changed
				if (nextPosition != prevPosition)
				{
					// ...
					Undo.RecordObject(this.component, "Move Node");
					state.requiresUpdate = true;

					// ...
					if (state.isSnappingEnabled == true)
					{
						nextPosition = Utility.SnapVector(nextPosition, state.transform, settings.interaction.snapGridSize);
					}

					// ...
					node.position = state.transform.InverseTransformPoint(nextPosition);
				}
			}
		}

		/// <summary>
		/// Handles repaint of target Node, that is either .selected or .inFocus
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="node">Target <see cref="Node" /></param>
		/// <param name="controlState">State of target Node</param>		
		void RepaintForNode (EditorState state, Node node, ControlState controlState)
		{
			// Determine color and initial size of handle based on ControlState
			Color color;
			float handleSize = settings.nodes.handleSize;
			if (controlState == ControlState.selected)
			{
				// When node is selected
				handleSize *= settings.handleSizes.selectedControl;
				color = settings.nodes.selectedColor;
			}
			else// if (controlState == ControlState.focused)
			{
				// When node hovered by mouse
				handleSize *= settings.handleSizes.controlInFocus;
				color = settings.nodes.inFocusColor;
			}

			// Transform point and handle size for arrow
			Vector3 position = state.transform.TransformPoint(node.position);
			float arrowHandleSize = this.GetHandleSize(position, settings.nodes.arrowHandleSize);

			// Right arrow
			{
				Vector3 rightTangent = state.transform.TransformPoint(node.rightTangent.GetPosition());
				Vector3 direction = (rightTangent - position).normalized;
				if (direction != Vector3.zero)
				{
					Quaternion rotation = Quaternion.LookRotation(direction);
					this.DrawArrow(position, rotation, arrowHandleSize, settings.nodes.rightArrowColor, settings.nodes.occludedColor);
				}
			}

			// Left arrow
			{
				Vector3 leftTangent = state.transform.TransformPoint(node.leftTangent.GetPosition());
				Vector3 direction = (leftTangent - position).normalized;
				if (direction != Vector3.zero)
				{
					Quaternion rotation = Quaternion.LookRotation(direction);
					this.DrawArrow(position, rotation, arrowHandleSize, settings.nodes.leftArrowColor, settings.nodes.occludedColor);
				}
			}

			// ...
			handleSize = this.GetHandleSize(position, handleSize);
			this.DrawSphere(position, handleSize, color, settings.nodes.occludedColor);

			// Emphasize current control
			if (state.emphasizeControlInFocus == true && controlState == ControlState.focused)
			{
				this.EmphasizePoint(state, position, handleSize);
			}
		}

		/// <summary>
		/// Handles repaint for inspector, when currently selected control is Tangent
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="tangent">Target <see cref="Tangent" /></param>
		void InspectorForTangent (EditorState state, Tangent tangent)
		{
			// ...
			this.DrawNavigationToolbarForVertices(state, tangent.node, tangent);

			// Drawing foldout for currently selected control
			string foldoutLabel = tangent.node.leftTangent == tangent
				? "Selected: Left Tangent"
				: "Selected: Right Tangent";

			EditorGUILayout.Foldout(true, foldoutLabel);
			EditorGUI.indentLevel++;

			// if (Tools.pivotRotation == PivotRotation.Global)
			{
				// Position of tangent in world space
				Vector3 prevValue = state.transform.TransformPoint(tangent.GetPosition());
				Vector3 nextValue = EditorGUILayout.Vector3Field("Position: (Global)", prevValue);
				EditorGUILayout.Space();

				// Value has been changed
				if (prevValue != nextValue)
				{
					// ...
					Undo.RecordObject(this.component, "Move Tangent");
					state.requiresUpdate = true;

					// ...
					nextValue = state.transform.InverseTransformPoint(nextValue);
					tangent.SetPosition(nextValue, state.cotangentBehaviour);
					return;
				}
			}
			// else if (Tools.pivotRotation == PivotRotation.Local)
			{
				// Position of tangent in local space
				Vector3 prevValue = tangent.GetPosition();
				Vector3 nextValue = EditorGUILayout.Vector3Field("Position: (Local)", prevValue);
				EditorGUILayout.Space();

				// Value has been changed
				if (prevValue != nextValue)
				{
					// ...
					Undo.RecordObject(this.component, "Move Tangent");
					state.requiresUpdate = true;

					tangent.SetPosition(nextValue, state.cotangentBehaviour);
					return;
				}
			}

			{
				// Position of tangent local to node it belongs to
				Vector3 nextValue = EditorGUILayout.Vector3Field("Position: (local to Node)", tangent.localPosition);
				EditorGUILayout.Space();

				// Value has been changed
				if (nextValue != tangent.localPosition)
				{
					// ...
					Undo.RecordObject(this.component, "Move Tangent");
					state.requiresUpdate = true;

					tangent.SetPosition(tangent.node.position + nextValue, state.cotangentBehaviour);
					return;
				}
			}
		}

		/// <summary>
		/// Provides position handle for target Tangent.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="tangent">Target <see cref="Tangent" /></param>
		void PositionHandleForTangent (Event e, EditorState state, Tangent tangent)
		{
			// Working in magnitude adjusting mode while holding shift.
			if (e.modifiers.HasFlag(EventModifiers.Shift) == true)
			{
				// ...
				Vector3 tangentPosition = tangent.GetPosition();
				Vector3 prevPosition = state.transform.TransformPoint(tangentPosition);

				// ...
				Vector3 nodePosition = state.transform.TransformPoint(tangent.node.position);
				Vector3 direction = (nodePosition - prevPosition).normalized;

				// Calculating rotation for handle
				Quaternion rotation = Quaternion.LookRotation(direction);

				// ...
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, rotation);

				// Constraining position to a line based on direction from tangent to node.
				nextPosition = Utility.GetClosestPointOnInfiniteLine(nextPosition, nodePosition, direction);

				// Value has been changed
				if (nextPosition != prevPosition)
				{
					// ...
					Undo.RecordObject(this.component, "Move Tangent");
					state.requiresUpdate = true;

					// ...
					nextPosition = state.transform.InverseTransformPoint(nextPosition);
					tangent.SetPosition(nextPosition, state.cotangentBehaviour);
				}
			}
			else if (Tools.pivotRotation == PivotRotation.Global)
			{
				// Transforming point to world space
				Vector3 prevPosition = state.transform.TransformPoint(tangent.GetPosition());

				// We are using Quaternion.identity here, because we are in a world space.
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, Quaternion.identity);

				// Value has been changed
				if (nextPosition != prevPosition)
				{
					// ...
					Undo.RecordObject(this.component, "Move Tangent");
					state.requiresUpdate = true;

					// ...
					if (state.isSnappingEnabled == true)
					{
						nextPosition = Utility.SnapVector(nextPosition, settings.interaction.snapGridSize);
					}

					nextPosition = state.transform.InverseTransformPoint(nextPosition);
					tangent.SetPosition(nextPosition, state.cotangentBehaviour);
				}
			}
			else// if (Tools.pivotRotation == PivotRotation.Local)
			{
				// For local space we are still transforming point, but instead of using
				// Quaternion.identity, we are using rotation from PathComponent.transform
				Vector3 tangentPosition = tangent.GetPosition();
				Vector3 prevPosition = state.transform.TransformPoint(tangentPosition);
				Vector3 nextPosition = Handles.PositionHandle(prevPosition, state.transform.rotation);

				// Value has been changed
				if (nextPosition != prevPosition)
				{
					// ...
					Undo.RecordObject(this.component, "Move Tangent");
					state.requiresUpdate = true;

					// ...
					if (state.isSnappingEnabled == true)
					{
						nextPosition = Utility.SnapVector(nextPosition, state.transform, tangent.node.position, settings.interaction.snapGridSize);
					}

					// ...
					nextPosition = state.transform.InverseTransformPoint(nextPosition);
					tangent.SetPosition(nextPosition, state.cotangentBehaviour);
				}
			}
		}

		/// <summary>
		/// Handles repaint of target Tangent, that is either .selected or .inFocus
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="tangent">Target <see cref="Tangent" /></param>
		/// <param name="controlState">State of target Tangent</param>	
		void RepaintForTangent (EditorState state, Tangent tangent, ControlState controlState)
		{
			// Determine color and initial size of handle based on ControlState
			Color color;
			float handleSize = settings.tangents.handleSize;
			if (controlState == ControlState.selected)
			{
				// When tangent is selected
				color = settings.tangents.selectedColor;
				handleSize *= settings.handleSizes.selectedControl;
			}
			else// if (controlState == ControlState.focused)
			{
				// When tangent hovered by mouse
				color = settings.tangents.inFocusColor;
				handleSize *= settings.handleSizes.controlInFocus;
			}

			// ...
			Vector3 tangentPosition = state.transform.TransformPoint(tangent.GetPosition());

			// Draw arrow pointing from tangent towards node it belongs to
			{
				Vector3 nodePosition = state.transform.TransformPoint(tangent.node.position);
				Vector3 direction = (nodePosition - tangentPosition).normalized;
				if (direction != Vector3.zero)
				{
					Quaternion rotation = Quaternion.LookRotation(direction);
					float arrowHandleSize = this.GetHandleSize(tangentPosition, settings.tangents.arrowHandleSize);
					this.DrawArrow(tangentPosition, rotation, arrowHandleSize, color, settings.tangents.occludedColor);
				}
			}

			// Draw arrow pointing from tangent towards opposite tangent
			{
				Vector3 opossiteTangent = state.transform.TransformPoint(state.GetOpposite(tangent).GetPosition());
				Vector3 direction = (opossiteTangent - tangentPosition).normalized;
				if (direction != Vector3.zero)
				{
					Quaternion rotation = Quaternion.LookRotation(direction);
					float arrowHandleSize = this.GetHandleSize(tangentPosition, settings.tangents.arrowHandleSize);
					this.DrawArrow(tangentPosition, rotation, arrowHandleSize, settings.tangents.oppositeTangentColor, settings.tangents.occludedColor);
				}
			}

			// Draw sphere where tangent positioned at
			handleSize = this.GetHandleSize(tangentPosition, handleSize);
			this.DrawSphere(tangentPosition, handleSize * 2, settings.tangents.occludedColor, settings.tangents.occludedColor);
			this.DrawSphere(tangentPosition, handleSize, color, settings.tangents.occludedColor);

			// Emphasize current control
			if (state.emphasizeControlInFocus == true && controlState == ControlState.focused)
			{
				this.EmphasizePoint(state, tangentPosition, handleSize);
			}
		}

		/// <summary>
		/// Handles repaint of inspector, when currently selected control is Midpoint
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="midpoint">Target <see cref="Midpoint" /></param>
		void InspectorForMidpoint (EditorState state, Midpoint midpoint)
		{
			// ...
			this.DrawNavigationToolbarForVertices(state, midpoint.node, midpoint);

			// Drawing foldout for currently selected control
			string foldoutLabel = midpoint.node.leftMidpoint == midpoint
				? "Selected: Left Midpoint"
				: "Selected: Right Midpoint";

			EditorGUILayout.Foldout(true, foldoutLabel);
			EditorGUI.indentLevel++;
			
			// Behaviour
			MidpointBehaviour nextBehaviour = (MidpointBehaviour)EditorGUILayout.EnumPopup("Behaviour:", midpoint.behaviour);
			if (nextBehaviour != midpoint.behaviour)
			{
				// ...
				Undo.RecordObject(this.component, "Move Midpoint");
				state.requiresUpdate = true;

				if (nextBehaviour == MidpointBehaviour.offsetFromTangent)
				{
					Vector3 midpointTangent = state.GetTangent(midpoint).GetPosition();

					// Calculating offset: distance from midpoint to tangent it belongs to
					midpoint.offset = Vector3.Distance(midpoint.position, midpointTangent);
					midpoint.behaviour = nextBehaviour;
				}
				else if (nextBehaviour == MidpointBehaviour.offsetFromMiddle)
				{
					Vector3 midpointTangent = state.GetTangent(midpoint).GetPosition();
					Vector3 oppositeTangent = state.GetOppositeTangent(midpoint).GetPosition();
					Vector3 middle = Vector3.Lerp(midpointTangent, oppositeTangent, 0.5f);

					// Clamp position between midpoint tangent and middle of tangents
					midpoint.position = Utility.GetClosestPointOnFiniteLine(midpoint.position, midpointTangent, middle);

					// Calculating offset: distance from midpoint to middle point
					midpoint.offset = Vector3.Distance(midpoint.position, middle);
					midpoint.behaviour = nextBehaviour;
				}
				else if (nextBehaviour == MidpointBehaviour.stayAtMiddle)
				{
					Vector3 midpointTangent = state.GetTangent(midpoint).GetPosition();
					Vector3 oppositeTangent = state.GetOppositeTangent(midpoint).GetPosition();

					// In this case, we are adjusting position of midpoint so that it stays at middle between tangents
					midpoint.position = Vector3.Lerp(midpointTangent, oppositeTangent, 0.5f);
					midpoint.behaviour = nextBehaviour;
					midpoint.offset = 0.0f;
				}
				else// if (nextBehaviour == MidpointBehaviour.auto)
				{
					midpoint.behaviour = MidpointBehaviour.auto;
					midpoint.offset = 0.0f;
				}
			}

			// Offset
			float nextOffset = EditorGUILayout.FloatField("Offset:", midpoint.offset);
			EditorGUILayout.Space();
			if (nextOffset != midpoint.offset)
			{
				// ...
				Undo.RecordObject(this.component, "Move Midpoint");
				state.requiresUpdate = true;

				if (midpoint.behaviour == MidpointBehaviour.offsetFromTangent)
				{
					// ...
					Vector3 midpointTangent = state.GetTangent(midpoint).GetPosition();
					Vector3 oppositeTangent = state.GetOppositeTangent(midpoint).GetPosition();
					Vector3 oppositeMidpoint = state.GetOpposite(midpoint).position;

					// ...
					Vector3 direction = (oppositeTangent - midpointTangent).normalized;
					Vector3 point = midpointTangent + (direction * nextOffset);

					// Midpoint will stay on the line between its tangent and opposite midpoint
					// Offset is the distance from midpoint to tangent it belongs to
					midpoint.position = Utility.GetClosestPointOnFiniteLine(point, midpointTangent, oppositeMidpoint);
					midpoint.offset = Vector3.Distance(midpoint.position, midpointTangent);
				}
				else if (midpoint.behaviour == MidpointBehaviour.offsetFromMiddle)
				{
					// ...
					Vector3 midpointTangent = state.GetTangent(midpoint).GetPosition();
					Vector3 oppositeTangent = state.GetOppositeTangent(midpoint).GetPosition();
					Vector3 oppositeMidpoint = state.GetOpposite(midpoint).position;
					Vector3 middle = Vector3.Lerp(midpointTangent, oppositeTangent, 0.5f);
					Vector3 lockedPoint = middle;

					// Case, when opposite midpoint stands closer to current midpoint tangent, than middle point between tangents
					if (Vector3.Distance(middle, midpointTangent) <= Vector3.Distance(middle, midpointTangent))
					{
						lockedPoint = oppositeMidpoint;
					}
					
					// ...
					Vector3 direction = (midpointTangent - oppositeTangent).normalized;
					Vector3 point = middle + (direction * nextOffset);

					// Midpoint will stay on the line between 'lockedPoint' and tangent it belongs to
					// Offset is the distance from midpoint to middle point between tangents
					midpoint.position = Utility.GetClosestPointOnFiniteLine(point, lockedPoint, midpointTangent);
					midpoint.offset = Vector3.Distance(middle, midpoint.position);
				}
				else if (midpoint.behaviour == MidpointBehaviour.stayAtMiddle || midpoint.behaviour == MidpointBehaviour.auto)
				{
					// When behaviour is set to auto, or was .stayAtMidle, but user changed value
					// we are understand it as a reset to .auto state.
					midpoint.behaviour = MidpointBehaviour.auto;
					midpoint.offset = 0.0f;
				}
			}
		}

		/// <summary>
		/// Provides position handle for target Midpoint.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="midpoint">Target <see cref="Midpoint" /></param>
		void PositionHandleForMidpoint (Event e, EditorState state, Midpoint midpoint)
		{
			// ...
			Vector3 prevPosition = state.transform.TransformPoint(midpoint.position);
			Vector3 nextPosition;

			// Position handle
			{
				// For calculating Quaternion, we are using direction from midpoint's tangent to opposite tangent.
				Vector3 midpointTangent = state.transform.TransformPoint(state.GetTangent(midpoint).GetPosition());
				Vector3 oppositeTangent = state.transform.TransformPoint(state.GetOppositeTangent(midpoint).GetPosition());

				// ...
				Vector3 direction = (midpointTangent - oppositeTangent).normalized;
				Quaternion rotation = Quaternion.LookRotation(direction);
				
				// ...
				float handleSize = this.GetHandleSize(prevPosition, settings.midpoints.handleSize * 2.0f);
				nextPosition = Handles.Slider(prevPosition, direction, handleSize, Handles.CircleHandleCap, 0.25f);
			}

			// Value has been changed
			if (prevPosition != nextPosition)
			{
				// ...
				Undo.RecordObject(this.component, "Move Midpoint");
				state.requiresUpdate = true;

				// Transforming nextPosition back to local space
				nextPosition = state.transform.InverseTransformPoint(nextPosition);

				// Snapping position to grid
				if (state.isSnappingEnabled == true)
				{
					nextPosition = Utility.SnapVector(nextPosition, state.transform, midpoint.node.position, settings.interaction.snapGridSize);
				}

				// Retrieving position of opposite midpoint
				Vector3 oppositeMidpoint = state.GetOpposite(midpoint).position;

				// Retrieving position of tangents
				Vector3 midpointTangent = state.GetTangent(midpoint).GetPosition();
				Vector3 oppositeTangent = state.GetOppositeTangent(midpoint).GetPosition();

				// Middle between tangents
				Vector3 middle = Vector3.Lerp(midpointTangent, oppositeTangent, 0.5f);

				if (midpoint.behaviour == MidpointBehaviour.offsetFromTangent)
				{
					// Midpoint position is locked between its tangent and opposite midpoint
					midpoint.position = Utility.GetClosestPointOnFiniteLine(nextPosition, midpointTangent, oppositeMidpoint);

					// Offset is the distance from midpoint to tangent it belongs to
					midpoint.offset = Vector3.Distance(midpoint.position, midpointTangent);
				}
				else if (midpoint.behaviour == MidpointBehaviour.offsetFromMiddle)
				{
					Vector3 point;
					// Depending on circumstances, middle point might be further, than opposite midpoint.
					// If so, current midpoint position will be locked between its tangent and opposite midpoint.
					// Otherwise, midpoint position will be locked between its tangent and middle point.
					if (Vector3.Distance(midpointTangent, middle) <= Vector3.Distance(midpointTangent, oppositeMidpoint))
					{
						point = middle;
					}
					else
					{
						point = oppositeMidpoint;
					}

					// Midpoint position is locked between its tangent and special point calculated above
					midpoint.position = Utility.GetClosestPointOnFiniteLine(nextPosition, midpointTangent, point);

					// Offset is the distance from midpoint to middle between tangents (even if 'point' is opposite midpoint)
					midpoint.offset = Vector3.Distance(midpoint.position, middle);
				}
				else// if (midpoint.behaviour == MidpointBehaviour.offsetFromMiddle || midpoint.behaviour == MidpointBehaviour.auto)
				{
					// If midpoint.behaviour equals to .stayAtMiddle, we will reset it to .auto
					// Because midpoint with such behaviour cannot be adjusted manually,
					// and we interpret user's interaction with handle as wish to switch to .auto behaviour
					midpoint.behaviour = MidpointBehaviour.auto;

					// Midpoint position is locked between its tangent and opposite midpoint
					midpoint.position = Utility.GetClosestPointOnFiniteLine(nextPosition, midpointTangent, oppositeMidpoint);
					midpoint.offset = 0.0f;
				}
			}
		}

		/// <summary>
		/// Handles repaint of inspector, when currently selected control is Midpoint
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="midpoint">Target <see cref="Midpoint" /></param>
		/// <param name="controlState">State of target Midpoint</param>	
		void RepaintForMidpoint (EditorState state, Midpoint midpoint, ControlState controlState)
		{
			// Determine color and initial size of handle based on ControlState
			Color color;
			float handleSize = settings.midpoints.handleSize;
			if (controlState == ControlState.selected)
			{
				// When midpoint is selected
				color = settings.midpoints.selectedColor;
				handleSize *= settings.handleSizes.selectedControl;
			}
			else// if (controlState == ControlState.focused)
			{
				// When midpoint hovered by mouse
				color = settings.midpoints.inFocusColor;
				handleSize *= settings.handleSizes.controlInFocus;
			}

			// Draw sphere where midpoint positioned at
			Vector3 midpointPosition = state.transform.TransformPoint(midpoint.position);
			handleSize = this.GetHandleSize(midpointPosition, handleSize);
			this.DrawSphere(midpointPosition, handleSize * 2, settings.midpoints.occludedColor, settings.midpoints.occludedColor);
			this.DrawSphere(midpointPosition, handleSize, color, settings.midpoints.occludedColor);

			// Draw arrow pointing from midpoint towards tangent it belongs to
			Vector3 tangentPosition = state.transform.TransformPoint(state.GetTangent(midpoint).GetPosition());
			Vector3 direction = (tangentPosition - midpointPosition).normalized;
			if (direction != Vector3.zero)
			{
				Quaternion rotation = Quaternion.LookRotation(direction);
				float arrowHandleSize = this.GetHandleSize(midpointPosition, settings.midpoints.arrowHandleSize);
				this.DrawArrow(midpointPosition, rotation, arrowHandleSize, color, settings.midpoints.occludedColor);
			}

			// Draw perpendicular line at point between tangents
			if (midpoint.behaviour == MidpointBehaviour.offsetFromMiddle && controlState == ControlState.selected)
			{
				Tangent midpointTangent = state.GetTangent(midpoint);
				Tangent oppositeTangent = state.GetOppositeTangent(midpoint);

				Vector3 a = state.transform.TransformPoint(midpointTangent.GetPosition());
				Vector3 b = state.transform.TransformPoint(oppositeTangent.GetPosition());

				Vector3 middle = Vector3.Lerp(a, b, 0.5f);
				Vector3 cross = Vector3.Cross(b - a, state.transform.right).normalized;
				this.DrawLine(settings.rendering.pathLineWidth, settings.tangents.lineColor, settings.tangents.occludedColor, middle - cross, middle + cross);
			}

			// Emphasize current control
			if (state.emphasizeControlInFocus == true && controlState == ControlState.focused)
			{
				this.EmphasizePoint(state, midpointPosition, handleSize);
			}
		}

		/// <summary>
		/// Handles repaint of inspector, when currently selected control is Sample
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="sample">Target <see cref="Sample" /></param>
		void InspectorForSample (Event e, EditorState state, Sample sample)
		{
			// ...
			this.DrawNavigationToolbarForSamples(state, sample);

			// Drawing foldout for currently selected control
			int sampleIndex = state.samples.IndexOf(sample);
			EditorGUILayout.Foldout(true, $"Selected: Sample ({sampleIndex} / {state.samples.Count - 1})");
			EditorGUI.indentLevel++;

			// Anchor
			AnchorPosition anchorPosition = (AnchorPosition)EditorGUILayout.EnumPopup("Snapping:", sample.anchor);
			if (anchorPosition != sample.anchor)
			{
				// ...
				Undo.RecordObject(this.component, "Change Sample");
				state.requiresUpdate = true;

				// ...
				sample.anchor = anchorPosition;
			
				// Calculating distance
				Sample.ComputeDistance(state.biarcs, sample);
			}

			// Sample.distanceOnBiarc
			float distanceOnBiarc = EditorGUILayout.FloatField("Distance on biarc:", sample.distanceOnBiarc);
			if (sample.distanceOnBiarc != distanceOnBiarc)
			{
				// ...
				Undo.RecordObject(this.component, "Change Sample");
				state.requiresUpdate = true;

				// Clamping distance on biarc between zero and total length of biarc
				EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
				sample.distanceOnBiarc = Mathf.Clamp(distanceOnBiarc, 0.0f, biarc.totalLength);
				sample.anchor = AnchorPosition.manual;
				Sample.ComputeDistance(state.biarcs, sample);
			}

			// Sample.distanceOnPath
			float distanceOnPath = EditorGUILayout.FloatField("Distance on path:", sample.distanceOnPath);
			if (distanceOnPath != sample.distanceOnPath)
			{
				// ...
				Undo.RecordObject(this.component, "Change Sample");
				state.requiresUpdate = true;

				// Clamping distance on path between zero and total length of path
				sample.distanceOnPath = Mathf.Clamp(distanceOnPath, 0.0f, state.pathTotalLength);
				sample.anchor = AnchorPosition.manual;

				// Searching for biarc at specified distance on path and using saving result directly to sample
				this.FindBiarcAtDistanceOnPath(state, sample.distanceOnPath, out Biarc biarc, out sample.distanceOnBiarc);
				sample.biarcId = biarc.identifier;
			}

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			// Sample.color
			Color color = EditorGUILayout.ColorField("Color:", sample.color);
			if (sample.color != color)
			{
				// ...
				Undo.RecordObject(this.component, "Change Sample");
				state.requiresRepaint = true;

				// ...
				sample.color = color;
			}

			// Sample.color
			if (GUILayout.Button("Random", GUILayout.ExpandWidth(false)) == true)
			{
				// ...
				Undo.RecordObject(this.component, "Change Sample");
				state.requiresRepaint = true;

				// Generating distinct color that does not equal
				// to color of most nearest samples to target.
				sample.color = this.GenerateDistinctColor(state, sample);
			}

			EditorGUILayout.EndHorizontal();

			// Sample.tilt
			float tilt = EditorGUILayout.FloatField("Tilt:", sample.tilt);
			if (sample.tilt != tilt)
			{
				// ...
				Undo.RecordObject(this.component, "Change Sample");
				state.requiresRepaint = true;

				// ...
				sample.tilt = tilt;
			}
		}

		/// <summary>
		/// Provides position handle for target Sample.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="sample">Target <see cref="Sample" /></param>
		void PositionHandleForSample (Event e, EditorState state, Sample sample)
		{
			// ...
			EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);

			// Retrieving position of sample and transforming it to world space
			Vector3 pointOnBiarc = biarc.GetPoint(sample.anchor, sample.distanceOnBiarc);
			Vector3 pointOnPath = state.transform.TransformPoint(pointOnBiarc);

			// Handle for moving sample along the path
			{
				// ...
				float handleSize = this.GetHandleSize(pointOnPath, settings.samples.handleSize * 2.0f);

				// Calculating rotation in such a way, that handle will always face camera
				Vector3 direction = (state.mouseRay.origin - pointOnPath).normalized;
				Vector3 upwards = SceneView.lastActiveSceneView.camera.transform.up;
				Quaternion rotation = Quaternion.LookRotation(direction, upwards);

				// ...
				Vector3 response = Handles.FreeMoveHandle(pointOnPath, rotation, handleSize, Vector3.zero, Handles.CircleHandleCap);

				// Value has been changed
				if (pointOnPath != response)
				{
					// Invoke method to find point on path
					this.FindBiarcInFocus(e, state, settings.interaction.countSegmentsForScreenSpace, float.MaxValue);

					if (state.hasPointOnBiarc == true)
					{
						// ...
						Undo.RecordObject(this.component, "Change Sample");
						state.requiresUpdate = true;

						if (state.isSnappingEnabled == true)
						{
							// Snapping to anchor points on biarc in focus
							sample.anchor = this.SnapSample(state, sample);
							sample.distanceOnBiarc = state.biarcInFocus.GetDistance(sample.anchor, state.distanceOnBiarc);
						}
						else
						{
							// Manual position of sample on path
							sample.anchor = AnchorPosition.manual;
							sample.distanceOnBiarc = state.distanceOnBiarc;
						}

						// ...
						sample.biarcId = state.biarcInFocus.identifier;
						Sample.ComputeDistance(state.biarcs, sample);
					}
				}
			}

			// Handle for adjusting tilt
			{
				// ...
				float handleSize = this.GetHandleSize(pointOnPath, settings.samples.tiltHandleSize);

				// Calculating direction along the path
				Vector3 direction = biarc.GetDirection(pointOnBiarc, sample.distanceOnBiarc);
				Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

				// Applying tilt and transforming quaternion to world space
				rotation *= Quaternion.Euler(0.0f, 0.0f, sample.tilt);
				rotation = Utility.TransformRotation(state.transform, rotation);

				// ...
				Vector3 forward = rotation * Vector3.forward;

				// ...
				Quaternion response = Handles.Disc(0, rotation, pointOnPath, forward, handleSize, false, settings.samples.tiltSnappingInterval);

				// Value has been changed
				if (rotation != response)
				{
					// ...
					Undo.RecordObject(this.component, "Change Sample");
					state.requiresRepaint = true;

					// ...
					Vector3 up = rotation * Vector3.up;
					Vector3 nextUp = response * Vector3.up;
					Vector3 axis = rotation * Vector3.forward;

					// Adding difference between angles to current value
					sample.tilt += Vector3.SignedAngle(up, nextUp, axis);

					if (e.control || e.command)
					{
						// Applying snapping
						sample.tilt = Mathf.Round(sample.tilt / settings.samples.tiltSnappingInterval) * settings.samples.tiltSnappingInterval;
					}
				}
			}


		}

		/// <summary>
		/// Handles repaint of inspector, when currently selected control is Sample
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="sample">Target <see cref="Sample" /></param>
		/// <param name="controlState">State of target Sample</param>
		void RepaintForSample (EditorState state, Sample sample, ControlState controlState)
		{
			// Searching for biarc for target sample
			EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);

			// Retrieving sample's position on path and transforming it to world space
			Vector3 pointOnPath = biarc.GetPoint(sample.anchor, sample.distanceOnBiarc);
			Vector3 transformedPointOnPath = state.transform.TransformPoint(pointOnPath);

			// ...
			float handleSize = settings.samples.handleSize;
			Color color;

			if (controlState == ControlState.selected)
			{
				// When sample is selected
				color = settings.samples.selectedColor;
				handleSize *= settings.handleSizes.selectedControl;
			}
			else if (controlState == ControlState.focused)
			{
				// When sample hovered by mouse
				color = settings.samples.inFocusColor;
				handleSize *= settings.handleSizes.controlInFocus;
			}
			else// if (controlState == ControlState.normal)
			{
				// When sample neither in focus nor selected
				color = settings.samples.normalColor;
			}

			// Retrieving direction to calculate rotation
			Vector3 direction = biarc.GetDirection(pointOnPath, sample.distanceOnBiarc);
			Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

			// Applying tilt and transforming quaternion to world space
			rotation *= Quaternion.Euler(0.0f, 0.0f, sample.tilt);
			rotation = Utility.TransformRotation(state.transform, rotation);

			// Retrieved rotation is a direction along the path, what we want is to swap Vector3.forward with Vector3.up
			rotation = Quaternion.LookRotation(rotation * Vector3.up, rotation * Vector3.forward);

			// Calculating handle size for point on path.
			handleSize = this.GetHandleSize(transformedPointOnPath, handleSize);

			// Dimmed sphere
			this.DrawSphere(transformedPointOnPath, handleSize * 2.0f, settings.samples.occludedColor, settings.samples.occludedColor);

			// Arrow, that shows tilt of sample
			float arrowSize = this.GetHandleSize(transformedPointOnPath, settings.samples.arrowHandleSize);
			this.DrawArrow(transformedPointOnPath, rotation, arrowSize, sample.color, settings.samples.occludedColor);

			// Sphere color of sample
			this.DrawSphere(transformedPointOnPath, handleSize, sample.color, settings.samples.occludedColor);
			
			// Emphasize current control
			if (state.emphasizeControlInFocus == true && controlState == ControlState.focused)
			{
				this.EmphasizePoint(state, transformedPointOnPath, handleSize);
			}
		}

		/// <summary>
		/// Calculates point of intersection between given <see cref="Plane" /> and <see cref="Ray" />.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="plane">Target <see cref="Plane" /></param>
		/// <param name="mouseRay">Target <see cref="Ray" /></param>
		/// <param name="pointOnPlane">Point of intersection</param>
		/// <returns>Returns true, when <see cref="Ray" /> intersects <see cref="Plane" />, otherwise returns false.</returns>
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

		/// <summary>
		/// Iterates over list of biarcs in target EditorState in order to determine, which biarc's path is closest to current mouse position.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="countSegments">How many segments will be used to calculate approximate path of each biarc.</param>
		/// <param name="minimumDistance">Minimum distance between mouse position and distance on biarc for it to be considered that we have a point on biarc.</param>
		void FindBiarcInFocus (Event e, EditorState state, int countSegments, float minimumDistance)
		{
			// Reset
			state.biarcInFocus = null;
			state.pointOnBiarc = Vector3.zero;
			state.distanceOnBiarc = 0.0f;
			state.distanceOnPath = 0.0f;

			// ...
			float currentDistanceOnScreen = float.MaxValue;

			// Creating two arrays with specified length
			Vector3[] vertices3d = new Vector3[countSegments];
			Vector2[] vertices2d = new Vector2[countSegments];

			// ...
			for (int n = 0; n < state.biarcs.Count; n++)
			{
				Biarc biarc = state.biarcs[n];

				// Finding nearest point on currently processed biarc
				Utility.GetNearestPointOnBiarc(
					biarc,
					state.transform,
					e.mousePosition,
					out float currentDistanceOnBiarc,
					out Vector3 currentPointOnBiarc,
					out float distanceOnBiarc2d,
					ref vertices3d,
					ref vertices2d
				);
				
				// If this point is closer to mouse position than previous point, we go further
				if (distanceOnBiarc2d < currentDistanceOnScreen)
				{
					state.distanceOnBiarc = currentDistanceOnBiarc;
					state.pointOnBiarc = currentPointOnBiarc;

					currentDistanceOnScreen = distanceOnBiarc2d;
					state.biarcInFocus = biarc;
				}
			}

			// Distance on path
			for (int n = 0; n < state.biarcs.Count; n++)
			{
				Biarc biarc = state.biarcs[n];
				if (biarc == state.biarcInFocus)
				{
					state.distanceOnPath += state.distanceOnBiarc;
					break;
				}
				state.distanceOnPath += biarc.totalLength;
			}

			// We have a point on biarc, if distance between point on biarc
			// and mouse position is less or equals to minimum allowed.
			state.hasPointOnBiarc = currentDistanceOnScreen <= minimumDistance;
		}

		/// <summary>
		/// Searches for control, that is hovered by mouse cursor.
		/// </summary>
		/// <param name="e"><see cref="UnityEngine.Event" /></param>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		void FindControlInFocus (Event e, EditorState state)
		{
			float currentDistance = float.MaxValue;
			IControl targetInFocus = null;

			if (state.mode == EditorMode.vertices)
			{
				// First: Searching for nearest control to mouse position
				for (int n = 0; n < state.nodes.Count; n++)
				{
					Node node = state.nodes[n];

					// Node itself
					RaycastControl(state, e.mousePosition, node, ref targetInFocus, ref currentDistance);
					
					// Tangents
					if (n > 0) RaycastControl(state, e.mousePosition, node.leftTangent, ref targetInFocus, ref currentDistance);
					if (n + 1 < state.nodes.Count) RaycastControl(state, e.mousePosition, node.rightTangent, ref targetInFocus, ref currentDistance);

					// Midpoints
					if (n > 0)
					{
						RaycastControl(state, e.mousePosition, node.leftMidpoint, ref targetInFocus, ref currentDistance);
					}
					if (n + 1 < state.nodes.Count)
					{
						RaycastControl(state, e.mousePosition, node.rightMidpoint, ref targetInFocus, ref currentDistance);
					}
				}
			}
			else// if (state.mode == EditorMode.samples)
			{
				// First: Searching for nearest control to mouse position
				for (int n = 0; n < state.samples.Count; n++)
				{
					RaycastControl(state, e.mousePosition, state.samples[n], ref targetInFocus, ref currentDistance);
				}
			}

			// Second: Determining, that 2d distance between nearest
			// control and mouse position is less or equals to radius of handle.
			if (targetInFocus != null)
			{
				// Get position and handle size
				this.GetControlHandle(state, targetInFocus, out Vector3 position, out float handleSize);

				// Perpendicular with length of handle size
				Vector3 perpendicular = SceneView.lastActiveSceneView.camera.transform.right * handleSize;

				// Second point, that lies on surface of sphere (since we're using spheres for handles)
				Vector3 sphereSurface = position + perpendicular;

				// Position on screen for both points
				Vector2 screenPosition = HandleUtility.WorldToGUIPoint(position);
				Vector2 screenSphereSurface = HandleUtility.WorldToGUIPoint(sphereSurface);

				// Determining radius of sphere on screen
				float radius = Vector2.Distance(screenPosition, screenSphereSurface);
				
				// If distance to nearest control is greater than radius,
				// it means that mouse cursor is not hovering over control.
				if (currentDistance > radius)
				{
					targetInFocus = null;
				}
			}

			// ...
			state.emphasizeControlInFocus = false;
			state.controlInFocus = targetInFocus;
		}

		/// <summary>
		/// When distance between target control and mouse position is closer than provided current distance, method will set targetInFocus to be current target.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="mousePosition">Mouse position in 2d space</param>
		/// <param name="target">Target <see cref="IControl" /></param>
		/// <param name="targetInFocus">Current nearest <see cref="IControl" /></param>
		/// <param name="currentDistance">Distance between `targetInFocus` and mouse position.</param>
		void RaycastControl (EditorState state, Vector2 mousePosition, IControl target, ref IControl targetInFocus, ref float currentDistance)
		{
			// Get position and handle size
			this.GetControlHandle(state, target, out Vector3 position, out float handleSize);

			// Transform to screen coordinates and calculate distance
			Vector3 pointOnScreen = HandleUtility.WorldToGUIPoint(position);
			float distance = Vector2.Distance(mousePosition, pointOnScreen);

			// Decide
			if (distance <= currentDistance)
			{
				targetInFocus = target;
				currentDistance = distance;
			}
		}

		/// <summary>
		/// Calculates world space position and size of handle for target <see cref="IControl" />.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="target">Target <see cref="IControl" /></param>
		/// <param name="position">Position of target <see cref="IControl" /> in world space</param>
		/// <param name="handleSize">Size of handle for <see cref="IControl" /></param>
		void GetControlHandle (EditorState state, IControl target, out Vector3 position, out float handleSize)
		{
			if (target is Node node)
			{
				position = node.position;
				handleSize = settings.nodes.handleSize;
			}
			else if (target is Tangent tangent)
			{
				position = tangent.GetPosition();
				handleSize = settings.tangents.handleSize;
			}
			else if (target is Midpoint midpoint)
			{
				position = midpoint.position;
				handleSize = settings.midpoints.handleSize;
			}
			else if (target is Sample sample)
			{
				EditorState.GetBiarcByIdentifier(state.biarcs, sample.biarcId, out Biarc biarc);
				position = biarc.GetPoint(sample.anchor, sample.distanceOnBiarc);
				handleSize = settings.samples.handleSize;
			}
			else
			{
				throw new System.NotImplementedException();
			}

			// Transform point to world space & calculate size of handle
			position = state.transform.TransformPoint(position);
			handleSize = this.GetHandleSize(position, handleSize);
		}

		/// <summary>
		/// Returns biarcs and nodes, related to target <see cref="Node" /> on path.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="node">Target <see cref="Node" /></param>
		/// <param name="leftBiarc"><see cref="Biarc" /> on the left side of target <see cref="Node" /></param>
		/// <param name="rightBiarc"><see cref="Biarc" /> on the right side of target <see cref="Node" /></param>
		/// <param name="leftNode"><see cref="Node" />, that stands before target <see cref="Node" /></param>
		/// <param name="rightNode"><see cref="Node" />, that stands after target <see cref="Node" /></param>
		void GetBiarcsForNode (EditorState state, Node node, out Biarc leftBiarc, out Biarc rightBiarc, out Node leftNode, out Node rightNode)
		{
			leftBiarc = null;
			rightBiarc = null;
			
			leftNode = null;
			rightNode = null;

			// ...
			int indexOfNode = state.nodes.IndexOf(node);

			// Target node has adjacent node on the right
			if (indexOfNode + 1 < state.nodes.Count)
			{
				rightBiarc = state.biarcs[indexOfNode];
				rightNode = state.nodes[indexOfNode + 1];
			}

			// Target node has adjacent node on the left
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

		/// <summary>
		/// Returns <see cref="Biarc" />, and distance on it, based on provided distance on path.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="distanceOnPath">Distance on path</param>
		/// <param name="biarc">Output <see cref="Biarc" /></param>
		/// <param name="distanceOnBiarc">Distance on <see cref="Biarc" /></param>
		void FindBiarcAtDistanceOnPath (EditorState state, float distanceOnPath, out Biarc biarc, out float distanceOnBiarc)
		{
			float accumulatedLength = 0.0f;

			for (int n = 0; n < state.biarcs.Count; n++)
			{
				biarc = state.biarcs[n];

				// If given distance on path is between accumulated length but less than (accumulated length + biarc total length)
				if (distanceOnPath >= accumulatedLength && distanceOnPath <= accumulatedLength + biarc.totalLength)
				{
					distanceOnBiarc = distanceOnPath - accumulatedLength;
					return;
				}

				accumulatedLength += biarc.totalLength;
			}

			// ...
			biarc = state.biarcs[state.biarcs.Count - 1];
			distanceOnBiarc = biarc.totalLength;
		}

		/// <summary>
		/// Generates color for target <see cref="Sample" />, that differs from samples, nearest to target.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="sample">Target <see cref="Sample" /></param>
		/// <returns>Distinct color, that differs from samples, nearest to target.</returns>
		Color GenerateDistinctColor (EditorState state, Sample sample)
		{
			Color output = sample.color;
			Color previous = sample.color;
			Color current = sample.color;
			Color next = sample.color;
			
			// ...
			int index = state.samples.IndexOf(sample);

			// Try to get previous and next samples
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

		/// <summary>
		/// Returns <see cref="AnchorPosition" />, nearest to where target <see cref="Sample" /> positioned at.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="sample">Target <see cref="Sample" /></param>
		/// <returns><see cref="AnchorPosition" />, nearest to where target <see cref="Sample" /> positioned at.</returns>
		AnchorPosition SnapSample (EditorState state, Sample sample)
		{
			float minimum = settings.samples.anchorSnappingMinimum;
			float distance;
			float currentDistance = float.MaxValue;
			AnchorPosition anchor = AnchorPosition.manual;

			// .origin
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.origin, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.origin;
			}

			// .firstLeftExtent
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.firstLeftExtent, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.firstLeftExtent;
			}

			// .leftTangent
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.leftTangent, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.leftTangent;
			}

			// .firstRightExtent
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.firstRightExtent, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.firstRightExtent;
			}

			// .leftMidpoint
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.leftMidpoint, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.leftMidpoint;
			}

			// .middleOfBiarc
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.middleOfBiarc, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.middleOfBiarc;
			}

			// .rightMidpoint
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.rightMidpoint, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.rightMidpoint;
			}

			// .secondLeftExtent
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.secondLeftExtent, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.secondLeftExtent;
			}

			// .rightTangent
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.rightTangent, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.rightTangent;
			}

			// .secondRightExtent
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.secondRightExtent, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.secondRightExtent;
			}

			// .destination
			distance = Mathf.Abs(state.biarcInFocus.GetDistance(AnchorPosition.destination, 0.0f) - state.distanceOnBiarc);
			if (distance < currentDistance && distance < minimum)
			{
				currentDistance = distance;
				anchor = AnchorPosition.destination;
			}

			return anchor;
		}

		/// <summary>
		/// Returns size of the handle for given position
		/// </summary>
		/// <param name="position">Position of point in world space</param>
		/// <param name="handleSize">Initial size of the handle</param>
		/// <returns></returns>
		float GetHandleSize (Vector3 position, float handleSize)
		{
			// Depending on settings, we are either:
			// - Keeping size of handle to be constant on screen, no matter how far it is
			// - Going with provided size, which means that the further point from camera is, the smaller it will be on screen.
			return settings.handleSizes.keepConstant
				? (HandleUtility.GetHandleSize(position) * handleSize)
				: handleSize;
		}

		/// <summary>
		/// Draw header
		/// </summary>
		/// <param name="label">Label</param>
		/// <param name="addSpaceBefore">When true, adds space before header.</param>
		void DrawUIHeader (string label, bool addSpaceBefore = false)
		{
			if (addSpaceBefore == true) EditorGUILayout.Space();
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
		}

		/// <summary>
		/// Draw foldout
		/// </summary>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		/// <returns>Returns true, if foldout is open, otherwise returns false.</returns>
		bool DrawUIFoldout (string label, ref bool fieldValue)
		{
			bool nextValue = EditorGUILayout.Foldout(fieldValue, label);
			if (nextValue != fieldValue)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
			}

			return nextValue;
		}

		/// <summary>
		/// Draw float field with clamped range
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
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
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		/// <param name="minimum">Minimum allowed value</param>
		/// <param name="maximum">Maximum allowed value</param>
		void DrawUIIntegerField (EditorState state, string label, ref int fieldValue, int minimum = int.MinValue, int maximum = int.MaxValue)
		{
			int nextValue = EditorGUILayout.IntField(label, fieldValue);
			
			if (nextValue < minimum) nextValue = minimum;
			if (nextValue > maximum) nextValue = maximum;

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
		/// <param name="state">Instance of <see cref="EditorState" /></param>
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
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		/// <returns>Returns true, if value has been changed, otherwise returns false.</returns>
		bool DrawUIToggleField (EditorState state, string label, ref bool fieldValue)
		{
			bool nextValue = EditorGUILayout.Toggle(label, fieldValue);
			if (nextValue != fieldValue)
			{
				Undo.RecordObject(this.component, "Inspector");
				fieldValue = nextValue;
				state.requiresRepaint = true;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Draw color field
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
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
		/// Draw field for enumerator
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="label">Label</param>
		/// <param name="fieldValue">Reference to value</param>
		/// <returns></returns>
		public bool DrawUIEnumField<T> (EditorState state, string label, ref T value) where T : System.Enum, System.IComparable, System.IConvertible
		{
			T nextValue = (T)EditorGUILayout.EnumPopup(label, value);
			if (nextValue.Equals(value) == false)
			{
				// ...
				Undo.RecordObject(this.component, "Inspector");
				state.requiresRepaint = true;

				// ...
				value = nextValue;
				return true;
			}

			return false;
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
			r.width = EditorGUIUtility.currentViewWidth;

			Color color = EditorGUIUtility.isProSkin
				? settings.inspector.delimiterDarkTheme
				: settings.inspector.delimiterWhiteTheme;

			EditorGUI.DrawRect(r, color);
		}

		/// <summary>
		/// Draws toolbar for navigating between vertices on path
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="currentNode">Node of currently selected control</param>
		/// <param name="currentControl">Currently selected control</param>
		void DrawNavigationToolbarForVertices (EditorState state, Node currentNode, IControl currentControl)
		{
			int index = GUILayout.Toolbar(-1, PathEditor.navigationToolbar);
			this.DrawUIDelimiter();

			// Select previous or next control on path
			if (index == 0)
			{
				state.selectedControl = state.GetPrevious(currentNode, currentControl);
			}
			else if (index == 1)
			{
				state.selectedControl = state.GetNext(currentNode, currentControl);
			}

			// When user pushed the button
			if (index != -1)
			{
				state.requiresRepaint = true;
				return;
			}

			// Depending on which button user hovers a mouse,
			// set control in focus to be previous or next control on path.
			if (GUI.tooltip == PathEditor.navigationToolbar[0].tooltip)
			{
				state.controlInFocus = state.GetPrevious(currentNode, currentControl);
				state.emphasizeControlInFocus = true;
			}
			else if (GUI.tooltip == PathEditor.navigationToolbar[1].tooltip)
			{
				state.controlInFocus = state.GetNext(currentNode, currentControl);
				state.emphasizeControlInFocus = true;
			}
		}

		/// <summary>
		/// Draws toolbar for navigating between samples on path
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="currentSample">Currently selected <see cref="Sample" /></param>
		void DrawNavigationToolbarForSamples (EditorState state, Sample currentSample)
		{
			int index = GUILayout.Toolbar(-1, PathEditor.navigationToolbar);
			this.DrawUIDelimiter();

			int sampleIndex = state.samples.IndexOf(currentSample);

			// For both samples, we are either retrieving previous/next sample,
			// or setting it to current sample. This way, user won't go out
			// of range and we do not have to check it.
			Sample previousSample = sampleIndex > 0 ? state.samples[sampleIndex - 1] : currentSample;
			Sample nextSample = sampleIndex + 1 < state.samples.Count ? state.samples[sampleIndex + 1] : currentSample;

			// Select previous or next control on path
			if (index == 0)
			{
				state.selectedControl = previousSample;
			}
			else if (index == 1)
			{
				state.selectedControl = nextSample;
			}

			// When user pushed the button
			if (index != -1)
			{
				state.requiresRepaint = true;
				return;
			}

			// Depending on which button user hovers a mouse,
			// set control in focus to be previous or next control on path.
			if (GUI.tooltip == PathEditor.navigationToolbar[0].tooltip)
			{
				state.controlInFocus = previousSample;
				state.emphasizeControlInFocus = true;
			}
			else if (GUI.tooltip == PathEditor.navigationToolbar[1].tooltip)
			{
				state.controlInFocus = nextSample;
				state.emphasizeControlInFocus = true;
			}
		}

		/// <summary>
		/// Draws sphere with specified properties.
		/// </summary>
		/// <param name="position">Center of sphere</param>
		/// <param name="handleSize">Radius</param>
		/// <param name="normalColor">Color, that will be used to draw non-occluded sphere</param>
		/// <param name="obscuredColor">Color, that will be used to draw occluded sphere.</param>
		void DrawSphere (Vector3 position, float handleSize, Color normalColor, Color obscuredColor)
		{
			if (settings.rendering.occlusion == Occlusion.disabled)
			{
				// Rendering without occlusion
				Handles.color = normalColor;
				Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
			}
			else if (settings.rendering.occlusion == Occlusion.visibleOnly)
			{
				// Rendering only what's visible
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = normalColor;
				Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);

				// Reset
				Handles.zTest = CompareFunction.Disabled;
			}
			else if (settings.rendering.occlusion == Occlusion.dimmed)
			{
				// First: Rendering only what's visible
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = normalColor;
				Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
				
				// Second: Rendering occluded
				Handles.zTest = CompareFunction.Greater;
				Handles.color = obscuredColor;
				Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);

				// Reset
				Handles.zTest = CompareFunction.Disabled;
			}
		}

		/// <summary>
		/// Draws line with specified properties
		/// </summary>
		/// <param name="lineWidth">Thickness of the line</param>
		/// <param name="normalColor">Color, that will be used to draw non-occluded line</param>
		/// <param name="obscuredColor">Color, that will be used to draw occluded line.</param>
		/// <param name="points">Array of points</param>
		void DrawLine (float lineWidth, Color normalColor, Color obscuredColor, params Vector3[] points)
		{
			if (settings.rendering.occlusion == Occlusion.disabled)
			{
				// Rendering everything
				Handles.color = normalColor;
				Handles.DrawAAPolyLine(lineWidth, points);
			}
			else if (settings.rendering.occlusion == Occlusion.visibleOnly)
			{
				// Rendering only what's visible
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = normalColor;
				Handles.DrawAAPolyLine(lineWidth, points);

				// Reset
				Handles.zTest = CompareFunction.Disabled;
			}
			else if (settings.rendering.occlusion == Occlusion.dimmed)
			{
				// First: Rendering only what's visible
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = normalColor;
				Handles.DrawAAPolyLine(lineWidth, points);
				
				// Second: Rendering occluded
				Handles.zTest = CompareFunction.Greater;
				Handles.color = obscuredColor;
				Handles.DrawAAPolyLine(lineWidth, points);

				// Reset
				Handles.zTest = CompareFunction.Disabled;
			}
		}

		/// <summary>
		/// Draws arrow with specified properties
		/// </summary>
		/// <param name="position">Origin of arrow</param>
		/// <param name="rotation">Rotation of arrow</param>
		/// <param name="handleSize">Size of arrow</param>
		/// <param name="normalColor">Color, that will be used to draw non-occluded arrow</param>
		/// <param name="obscuredColor">Color, that will be used to draw occluded arrow.</param>
		void DrawArrow (Vector3 position, Quaternion rotation, float handleSize, Color normalColor, Color obscuredColor)
		{
			if (settings.rendering.occlusion == Occlusion.disabled)
			{
				// Rendering everything
				Handles.color = normalColor;
				Handles.ArrowHandleCap(0, position, rotation, handleSize, EventType.Repaint);
			}
			else if (settings.rendering.occlusion == Occlusion.visibleOnly)
			{
				// Rendering only what's visible
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = normalColor;
				Handles.ArrowHandleCap(0, position, rotation, handleSize, EventType.Repaint);

				// Reset
				Handles.zTest = CompareFunction.Disabled;
			}
			else if (settings.rendering.occlusion == Occlusion.dimmed)
			{
				// First: Rendering only what's visible
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = normalColor;
				Handles.ArrowHandleCap(0, position, rotation, handleSize, EventType.Repaint);
				
				// Second: Rendering occluded
				Handles.zTest = CompareFunction.Greater;
				Handles.color = obscuredColor;
				Handles.ArrowHandleCap(0, position, rotation, handleSize, EventType.Repaint);

				// Reset
				Handles.zTest = CompareFunction.Disabled;
			}
		}

		/// <summary>
		/// Draws normals on target biarc based on samples.
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="biarc">Target <see cref="Biarc" /></param>
		/// <param name="distanceOnPath">Target distance on path</param>
		void DrawNormalsOnBiarc (EditorState state, Biarc biarc, ref float distanceOnPath)
		{
			// ...
			Color normalColor;

			// Calculating count of segments based on count of vertices per unit length
			int countSegments = (int)(biarc.totalLength * (float)settings.normals.countSegments);

			for (int n = 0; n < countSegments; n++)
			{
				// If i understood it correctly, calculating delta in some circumstances produces
				// floating-point precision errors, which is not critical for this kind of situation.
				// However, in this case, we calculate current alpha based on index in the loop.
				float distanceOnBiarc = (float)n * (biarc.totalLength / (float)countSegments);

				// Get color and tilt
				state.GetSampleAtDistance(distanceOnPath + distanceOnBiarc, out Color color, out float tilt);

				// Get point and direction at distance on biarc
				Vector3 pointOnBiarc = biarc.GetPoint(distanceOnBiarc);
				Vector3 direction = biarc.GetDirection(pointOnBiarc, distanceOnBiarc);
				
				// Transform point on biarc to world space
				Vector3 pointOnPath = state.transform.TransformPoint(pointOnBiarc);

				// Calculate rotation
				Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
				rotation *= Quaternion.Euler(0.0f, 0.0f, tilt);

				// Calculate Vector3.up and transform it
				Vector3 up = state.transform.TransformDirection(rotation * Vector3.up * settings.normals.magnitude);

				// Depending on settings, use color from sample or color specified in settings
				if (settings.normals.useSamples == true)
				{
					normalColor = color;
				}
				else
				{
					normalColor = settings.normals.normalColor;
				}

				// Finally, draw a line
				this.DrawLine(settings.rendering.pathLineWidth, normalColor, settings.normals.occludedColor, pointOnPath, pointOnPath + up);
			}

			// Advance
			distanceOnPath += biarc.totalLength;
		}

		/// <summary>
		/// Handles repaint of target biarc
		/// </summary>
		/// <param name="state">Instance of <see cref="EditorState" /></param>
		/// <param name="biarc">Target <see cref="Biarc" /></param>
		void HandleRepaintForBiarc (EditorState state, Biarc biarc)
		{
			// Transforming points of biarc
			Vector3 origin = state.transform.TransformPoint(biarc.origin);
			Vector3 leftTangent = state.transform.TransformPoint(biarc.leftTangent);
			Vector3 leftMidpoint = state.transform.TransformPoint(biarc.leftMidpoint);
			Vector3 rightMidpoint = state.transform.TransformPoint(biarc.rightMidpoint);
			Vector3 rightTangent = state.transform.TransformPoint(biarc.rightTangent);
			Vector3 destination = state.transform.TransformPoint(biarc.destination);
			
			// Drawing lines between extents and tangents
			{
				Vector3 leftExtent = state.transform.TransformPoint(biarc.leftArc.leftExtent);
				Vector3 rightExtent = state.transform.TransformPoint(biarc.rightArc.rightExtent);

				this.DrawLine(settings.rendering.pathLineWidth, settings.tangents.lineColor, settings.tangents.occludedLineColor, leftExtent, leftTangent);
				this.DrawLine(settings.rendering.pathLineWidth, settings.tangents.lineColor, settings.tangents.occludedLineColor, rightExtent, rightTangent);
			}

			// Origin (Node)
			{
				float handleSize = this.GetHandleSize(origin, settings.nodes.handleSize);
				this.DrawSphere(origin, handleSize * 2, settings.nodes.occludedColor, settings.nodes.occludedColor);
				this.DrawSphere(origin, handleSize, settings.nodes.normalColor, settings.nodes.occludedColor);
			}

			// Left tangent
			{
				float handleSize = this.GetHandleSize(leftTangent, settings.tangents.handleSize);
				this.DrawSphere(leftTangent, handleSize, settings.tangents.normalColor, settings.tangents.occludedColor);
			}

			// Left midpoint
			{
				float handleSize = this.GetHandleSize(leftMidpoint, settings.midpoints.handleSize);
				this.DrawSphere(leftMidpoint, handleSize, settings.midpoints.normalColor, settings.midpoints.occludedColor);
			}

			// Right midpoint
			{
				float handleSize = this.GetHandleSize(rightMidpoint, settings.midpoints.handleSize);
				this.DrawSphere(rightMidpoint, handleSize, settings.midpoints.normalColor, settings.midpoints.occludedColor);
			}

			// Right tangent
			{
				float handleSize = this.GetHandleSize(rightTangent, settings.tangents.handleSize);
				this.DrawSphere(rightTangent, handleSize, settings.tangents.normalColor, settings.tangents.occludedColor);
			}

			// Destination (Node)
			{
				float handleSize = this.GetHandleSize(destination, settings.nodes.handleSize);
				this.DrawSphere(destination, handleSize * 2, settings.nodes.occludedColor, settings.nodes.occludedColor);
				this.DrawSphere(destination, handleSize, settings.nodes.normalColor, settings.nodes.occludedColor);
			}
		}

		/// <summary>
		/// Emphasizes target position.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="position"></param>
		/// <param name="handleSize"></param>
		void EmphasizePoint (EditorState state, Vector3 position, float handleSize)
		{
			// Facing camera
			Vector3 direction = (state.mouseRay.origin - position).normalized;

			// ...
			Handles.color = settings.interaction.colorOfEmphasizedPoint;
			Handles.DrawWireDisc(position, direction, handleSize);
		}

		/// <summary>
		/// Returns true, when current instance of <see cref="PathComponent" /> has custom bounds.
		/// </summary>
		/// <returns>True, when current instance of <see cref="PathComponent" /> has custom bounds, otherwise returns false.</returns>
		public bool HasFrameBounds ()
		{
			// As long as we have at least one node, we have custom bounds
			return this.component.editorState.nodes.Count != 0;
		}

		/// <summary>
		/// Gets custom bounds for the target of this editor.
		/// </summary>
		/// <returns>Custom <see cref="Bounds" /> for target of this editor.</returns>
		public Bounds OnGetFrameBounds ()
		{
			EditorState state = this.component.editorState;
			
			if (state.selectedControl != null)
			{
				// Focus on currently selected control
				return this.GetControlBounds(state, state.selectedControl);
			}
			else
			{
				// Focus on whole path
				return this.GetPathBounds(state);
			}
		}


	}



}