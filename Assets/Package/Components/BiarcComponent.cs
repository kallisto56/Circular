namespace Circular
{
	using UnityEngine;
	using UnityEditor;



	/// <summary>
	/// <see cref="BiarcComponent"/> demonstrates how one could create and draw an <see cref="Biarc"/>.
	/// </summary>
	public class BiarcComponent : MonoBehaviour
	{
		public Transform origin;
		public Transform leftTangent;
		public Transform leftMidpoint;
		public Transform rightMidpoint;
		public Transform rightTangent;
		public Transform destination;

		[Range(16, 192)]
		public int countSegments = 64;

		public Color color = Color.black;

		public Biarc biarc = new Biarc();
		Vector3[] vertices;



		void OnDrawGizmos ()
		{
			if (this.origin == null) return;
			if (this.leftTangent == null) return;
			if (this.leftMidpoint == null) return;
			if (this.rightMidpoint == null) return;
			if (this.rightTangent == null) return;
			if (this.destination == null) return;

			// In reality, we could check if points has changed and only then
			// initialize Biarc and vertices, but for purposes of demonstration
			// this will do.
			this.biarc.Initialize(
				this.origin.position,
				this.leftTangent.position,
				this.leftMidpoint.position,
				this.rightMidpoint.position,
				this.rightTangent.position,
				this.destination.position
			);

			// After biarc initialized - adjust midpoints.
			// This way, we will be able to change position of
			// midpoints constrained to line between tangents.
			this.leftMidpoint.position = this.biarc.leftMidpoint;
			this.rightMidpoint.position = this.biarc.rightMidpoint;

			// NOTE: Midpoints have behaviour and offset, for staying at certain
			// position on biarc, you can play with it in inspector.
			
			// Ensure array is initialized and it's length equals to specified
			// count of segments we will be drawing.
			if (this.vertices == null || this.vertices.Length != this.countSegments)
			{
				this.vertices = new Vector3[this.countSegments];
			}

			// Calculate step size using biarc total length
			float delta = this.biarc.totalLength / (float)(this.countSegments - 1);
			float distance = 0.0f;

			// ...
			for (int n = 0; n < this.countSegments; n++)
			{
				this.vertices[n] = this.biarc.GetPoint(distance);
				distance += delta;
			}

			// Drawing vertices
			Handles.color = this.color;
			Handles.DrawAAPolyLine(2, this.vertices);

			// Drawing tangent lines
			Handles.DrawDottedLine(this.biarc.origin, this.biarc.leftTangent, 2.0f);
			Handles.DrawDottedLine(this.biarc.destination, this.biarc.rightTangent, 2.0f);
			
			// Drawing origin point
			Handles.color = Color.red;
			Handles.SphereHandleCap(0, this.biarc.origin, Quaternion.identity, 0.35f, EventType.Repaint);
			
			// Drawing tangent points
			Handles.color = Color.green;
			Handles.SphereHandleCap(0, this.biarc.leftTangent, Quaternion.identity, 0.25f, EventType.Repaint);
			Handles.SphereHandleCap(0, this.biarc.rightTangent, Quaternion.identity, 0.25f, EventType.Repaint);
			
			// Drawing midpoints
			Handles.color = Color.yellow;
			Handles.SphereHandleCap(0, this.biarc.leftMidpoint, Quaternion.identity, 0.25f, EventType.Repaint);
			Handles.SphereHandleCap(0, this.biarc.rightMidpoint, Quaternion.identity, 0.25f, EventType.Repaint);
			
			// Drawing destination point
			Handles.color = Color.blue;
			Handles.SphereHandleCap(0, this.biarc.destination, Quaternion.identity, 0.35f, EventType.Repaint);
		}



	}



}