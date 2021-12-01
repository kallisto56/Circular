namespace Circular
{
	using UnityEngine;
	using UnityEditor;



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
			//if (UnityEditor.Selection.activeGameObject == this.gameObject) return;
			if (this.path == null) return;
			this.path.Initialize(false);

			Handles.color = Color.black;

			this.polylineBatch.EnsureSize(256);
			this.polylineBatch.lineWidth = 2.0f;
			int countSegmentsPerArc = 32;
			
			for (int n = 0; n < this.path.biarcs.Count; n++)
			{
				this.polylineBatch.AddBiarc(this.path.biarcs[n], this.transform, countSegmentsPerArc);
			}

			this.polylineBatch.Render(true);
		}



	}



}