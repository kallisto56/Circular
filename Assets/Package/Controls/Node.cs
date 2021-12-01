namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// <see cref="Node"/> is either origin or destination on <see cref="Biarc"/>.
	/// </summary>
	[System.Serializable]
	public class Node : IControl
	{
		public Vector3 position;

		public Tangent leftTangent;
		public Tangent rightTangent;
		
		public Midpoint leftMidpoint;
		public Midpoint rightMidpoint;



		public Node ()
		{
			this.leftTangent = new Tangent(this);
			this.rightTangent = new Tangent(this);

			this.leftMidpoint = new Midpoint(this);
			this.rightMidpoint = new Midpoint(this);

			this.leftTangent.cotangent = this.rightTangent;
			this.rightTangent.cotangent = this.leftTangent;
		}



		public Node (Vector3 leftTangent, Vector3 position, Vector3 rightTangent) : this()
		{
			this.position = position;
			this.leftTangent.localPosition = leftTangent;
			this.rightTangent.localPosition = rightTangent;
		}



	}



}