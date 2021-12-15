namespace Circular
{
	using System;
	using UnityEngine;
	using UnityEditor;



	/// <summary>
	/// Used for storing global settings for <see cref="PathEditor" />.
	/// </summary>
	public class EditorSettings : ScriptableObject
	{
		public Rendering rendering;
		public Interaction interaction;
		public PointOnBiarc pointOnBiarc;
		public PointOnPlane pointOnPlane;
		public SolidPath solidPath;
		public GradientPath gradientPath;
		public Normals normals;
		public HandleSizes handleSizes;
		public Nodes nodes;
		public Tangents tangents;
		public Midpoints midpoints;
		public Samples samples;
		public Inspector inspector;
		

		[Serializable]
		public class Rendering
		{
			public bool isFoldoutOpen = false;
			public Occlusion occlusion = Occlusion.dimmed;

			public PathColor pathColorForVertices = PathColor.solid;
			public PathColor pathColorForSamples = PathColor.gradient;
			
			public float pathLineWidth = 3.0f;
			public int polylineBatchSize = 512;
		}

		[Serializable]
		public class Interaction
		{
			public bool isFoldoutOpen = false;
			public float snapGridSize = 0.5f;
			public int countSegmentsForScreenSpace = 32;
			public Color colorOfEmphasizedPoint = Color.black;
			public float focusBoundsForPathIncrease = 1.1f;
			public float focusBoundsForControlIncrease = 5.0f;
		}

		[Serializable]
		public class PointOnBiarc
		{
			public bool isFoldoutOpen = false;
			public bool isVisible = false;
			public float handleSize = 0.1f;
			public float minimumDistance = 45.0f;

			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);
		}

		[Serializable]
		public class PointOnPlane
		{
			public bool isFoldoutOpen = false;
			public bool isVisible = false;

			public float handleSize = 0.08f;

			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);

			public Color normalLineColor = new Color(0.0f, 0.0f, 0.0f, 0.75f);
			public Color occludedLineColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);
		}

		[Serializable]
		public class SolidPath
		{
			public bool isFoldoutOpen = false;
			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);
			public int countSegments = 32;
		}

		[Serializable]
		public class GradientPath
		{
			public bool isFoldoutOpen = false;
			public float occludedColorGain = 0.35f;
			public int countSegments = 32;
		}

		[Serializable]
		public class Normals
		{
			public bool isFoldoutOpen = false;
			public bool isVisible = false;
			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);
			public int countSegments = 32;

			public bool onlyBiarcInFocus = true;
			public bool useSamples = false;
			
			public float magnitude = 1.0f;
		}

		[Serializable]
		public class HandleSizes
		{
			public bool isFoldoutOpen = false;
			public bool keepConstant = true;

			public float controlInFocus = 1.2f;
			public float selectedControl = 1.4f;
		}

		[Serializable]
		public class Nodes
		{
			public bool isFoldoutOpen = false;
			public float handleSize = 0.08f;
			public float arrowHandleSize = 0.36f;

			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);

			public Color inFocusColor = Color.red;
			public Color selectedColor = Color.red;

			public Color leftArrowColor = Color.yellow;
			public Color rightArrowColor = Color.red;
		}

		[Serializable]
		public class Tangents
		{
			public bool isFoldoutOpen = false;
			public float handleSize = 0.08f;
			public float arrowHandleSize = 0.36f;

			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);

			public Color inFocusColor = Color.green;
			public Color selectedColor = Color.green;

			public Color lineColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);
			public Color occludedLineColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);

			public Color oppositeTangentColor = new Color(0.0f, 0.0f, 0.0f, 0.85f);
		}

		[Serializable]
		public class Midpoints
		{
			public bool isFoldoutOpen = false;
			public float handleSize = 0.08f;
			public float arrowHandleSize = 0.36f;
			
			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);

			public Color inFocusColor = Color.cyan;
			public Color selectedColor = Color.cyan;
		}

		[Serializable]
		public class Samples
		{
			public bool isFoldoutOpen = false;
			public float handleSize = 0.08f;
			public float arrowHandleSize = 0.36f;
			public float tiltHandleSize = 1.0f;
			public float tiltSnappingInterval = 5.0f;

			public Color normalColor = Color.black;
			public Color occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);

			public Color inFocusColor = Color.white;
			public Color selectedColor = Color.white;

			public float anchorSnappingMinimum = 0.25f;
		}

		[Serializable]
		public class Inspector
		{
			public bool isFoldoutOpen = false;
			public Color delimiterWhiteTheme = new Color(0.7294118f, 0.7294118f, 0.7294118f);
			public Color delimiterDarkTheme = new Color(0.1882353f, 0.1882353f, 0.1882353f);
		}


		/// <summary>
		/// Creates instance of EditorSettings with default values or loads existing one.
		/// </summary>
		public static EditorSettings Load ()
		{
			string[] instances = AssetDatabase.FindAssets("EditorSettings");

			if (instances.Length == 0)
			{
				EditorSettings editorSettings = ScriptableObject.CreateInstance<EditorSettings>();
				editorSettings.rendering = new Rendering();
				editorSettings.interaction = new Interaction();
				editorSettings.pointOnBiarc = new PointOnBiarc();
				editorSettings.pointOnPlane = new PointOnPlane();
				editorSettings.solidPath = new SolidPath();
				editorSettings.gradientPath = new GradientPath();
				editorSettings.normals = new Normals();
				editorSettings.handleSizes = new HandleSizes();
				editorSettings.nodes = new Nodes();
				editorSettings.tangents = new Tangents();
				editorSettings.midpoints = new Midpoints();
				editorSettings.samples = new Samples();
				editorSettings.inspector = new Inspector();

				return editorSettings;
			}
			else
			{
				string path = AssetDatabase.GUIDToAssetPath(instances[0]);
				return AssetDatabase.LoadAssetAtPath<EditorSettings>(path);
			}
		}



	}



}