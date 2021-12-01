namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// <see cref="Midpoint"/> is one of the points on <see cref="Biarc"/> after left <see cref="Arc"/> and before right <see cref="Arc"/>.
	/// Each <see cref="Node"/> has two midpoints, one for each <see cref="Biarc"/>.
	/// </summary>
	[System.Serializable]
	public class Midpoint : IControl
	{
		public Vector3 position;
		[System.NonSerialized] public Node node;
		public MidpointBehaviour behaviour;
		public float offset;



		public Midpoint (Node node)
		{
			this.node = node;
			this.behaviour = MidpointBehaviour.auto;
		}



	}



}