namespace Circular
{
	using UnityEngine;



	// Source: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Geometry/Plane.cs
	[System.Serializable]
	public class Plane
	{
		public bool isFlipped = false;
		public Vector3 normal;
		public float distance;



		public Plane(Vector3 a, Vector3 b, Vector3 c)
		{
			normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
			distance = -Vector3.Dot(normal, a);
		}

		public void Update (Vector3 a, Vector3 b, Vector3 c)
		{
			this.normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
			this.distance = -Vector3.Dot(normal, a);
		}

		public bool Raycast(Ray ray, out float enter)
		{
			float vdot = Vector3.Dot(ray.direction, normal);
			float ndot = -Vector3.Dot(ray.origin, normal) - distance;

			if (Mathf.Approximately(vdot, 0.0f) == true)
			{
				enter = 0.0F;
				return false;
			}

			enter = ndot / vdot;

			return enter > 0.0F;
		}

		public void Flip ()
		{
			this.isFlipped = !this.isFlipped;
			this.normal = -this.normal;
			this.distance = -this.distance;
		}



	}



}