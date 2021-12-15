namespace Circular
{
	using UnityEngine;
	using UnityEditor;



	/// <summary>
	/// <see cref="ArcComponent"/> demonstrates how one could create and draw an <see cref="Arc"/>.
	/// </summary>
	public class ArcComponent : MonoBehaviour
	{
		public Transform origin;
		public Transform tangent;
		public Transform destination;

		[Range(16, 192)]
		public int countSegments = 64;

		public Color color = Color.black;

		Arc arc = new Arc();
		Vector3[] vertices;



		void OnDrawGizmos ()
		{
			if (this.origin == null) return;
			if (this.tangent == null) return;
			if (this.destination == null) return;

			// In reality, we could check if points has changed and only then
			// initialize Arc and vertices, but for purposes of demonstration
			// this will do.
			this.arc.Initialize(this.origin.position, this.tangent.position, this.destination.position);
			
			// Ensure array is initialized and its length equals to specified
			// count of segments we will be drawing.
			if (this.vertices == null || this.vertices.Length != this.countSegments)
			{
				this.vertices = new Vector3[this.countSegments];
			}

			// Calculate step size using arc total length
			float delta = this.arc.totalLength / (float)(this.countSegments - 1);
			float distance = 0.0f;

			// ...
			for (int n = 0; n < this.countSegments; n++)
			{
				this.vertices[n] = this.arc.GetPoint(distance);
				distance += delta;
			}

			// Drawing vertices
			Handles.color = this.color;
			Handles.DrawAAPolyLine(2, this.vertices);

			// Drawing tangent lines
			Handles.DrawDottedLine(this.arc.origin, this.arc.tangent, 2.0f);
			Handles.DrawDottedLine(this.arc.destination, this.arc.tangent, 2.0f);
			
			// Drawing origin point
			Handles.color = Color.red;
			Handles.SphereHandleCap(0, this.arc.origin, Quaternion.identity, 0.35f, EventType.Repaint);
			
			// Drawing tangent point
			Handles.color = Color.green;
			Handles.SphereHandleCap(0, this.arc.tangent, Quaternion.identity, 0.25f, EventType.Repaint);
			
			// Drawing destination point
			Handles.color = Color.blue;
			Handles.SphereHandleCap(0, this.arc.destination, Quaternion.identity, 0.35f, EventType.Repaint);
		}



	}



}