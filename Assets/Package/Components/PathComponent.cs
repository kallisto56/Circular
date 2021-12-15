namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// <see cref="PathComponent"/> used by <see cref="PathEditor"/> to store and operate on list of Biarcs and Samples.
	/// </summary>
	public class PathComponent : MonoBehaviour
	{
		public Path path;
		public EditorState editorState;

		PolylineBatch polylineBatch = new PolylineBatch();



		void Reset ()
		{
			this.path = Path.CreateDefault();
			this.editorState = EditorState.CreateDefault();
			this.editorState.OnEditorAttached(this.transform, this.path);
		}



		void OnDrawGizmos ()
		{
			// When this is the currently selected game object,
			// we are assuming PathEditor will handle repaint for us.
			if (UnityEditor.Selection.activeGameObject == this.gameObject) return;

			// ...
			if (this.path == null) return;

			// Remember to intialize path
			this.path.Initialize(false);

			// Below we are using PolylineBatch for drawing path,
			// but you can use whatever you want.

			// Ensure polyline batch array size
			this.polylineBatch.EnsureSize(512);

			// Set rendering properties
			this.polylineBatch.lineWidth = 2.0f;
			this.polylineBatch.normalColor = Color.black;
			this.polylineBatch.occludedColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);

			// ...
			int countSegmentsPerArc = 32;

			for (int n = 0; n < this.path.biarcs.Count; n++)
			{
				this.polylineBatch.AddBiarc(this.path.biarcs[n], this.transform, countSegmentsPerArc);
			}

			// If there are any lines left, render them
			this.polylineBatch.Render(true);
		}



	}



}