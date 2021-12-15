namespace Game
{
	using UnityEngine;
	using UnityEditor;

	using Circular;



	public class DemoNormals : MonoBehaviour
	{
		public Path path;
		public int countSegments = 48;
		public float lineWidth = 2.5f;


		void OnDrawGizmos()
		{
			// Remember to intialize instance of Path
			this.path = Path.CreateDefault();
			this.path.Initialize();

			// ...
			DrawPath(this.path, this.transform, this.lineWidth, this.countSegments);
			DrawNormals(this.path, this.transform, this.lineWidth, this.countSegments);
		}

		static void DrawPath (Path path, Transform transform, float lineWidth, int countSegments)
		{
			// ...
			Handles.color = Color.black;

			// Not a good idea to allocate an array every frame
			Vector3[] vertices = new Vector3[countSegments];

			// ...
			float distanceOnPath = 0.0f;
			float delta = path.totalLength / ((float)countSegments - 1);

			for (int n = 0; n < countSegments; n++)
			{
				// Get point on path and transform it to world space
				Vector3 pointOnPath = path.GetPoint(distanceOnPath);
				vertices[n] = transform.TransformPoint(pointOnPath);

				// Advance
				distanceOnPath += delta;
			}

			// Draw all vertices at once
			Handles.DrawAAPolyLine(lineWidth, vertices);
		}

		static void DrawNormals (Path path, Transform transform, float lineWidth, int countSegments)
		{
			// ...
			Handles.color = Color.red;

			// ...
			float distanceOnPath = 0.0f;
			float delta = path.totalLength / ((float)countSegments - 1);

			for (int n = 0; n < countSegments; n++)
			{
				// Get point on path and transform it to world space
				Vector3 pointOnPath = path.GetPoint(distanceOnPath);
				pointOnPath = transform.TransformPoint(pointOnPath);

				// Get rotation and transform it to world space
				Vector3 up = path.GetRotation(distanceOnPath, true) * Vector3.up;
				up = transform.TransformVector(up);

				// ...
				Handles.DrawAAPolyLine(lineWidth, pointOnPath, pointOnPath + up);

				// Advance
				distanceOnPath += delta;
			}
		}


	}



}