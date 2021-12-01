namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// <see cref="Tangent"/> affects <see cref="Biarc"/> path. Each <see cref="Node"/> has two of them.
	/// </summary>
	[System.Serializable]
	public class Tangent : IControl
	{
		public Vector3 localPosition;

		[System.NonSerialized] public Node node;
		[System.NonSerialized] public Tangent cotangent;



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">Owner</param>
		public Tangent (Node node)
		{
			this.node = node;
		}

		/// <returns><see cref="Vector3" /> global position</returns>
		public Vector3 GetPosition ()
		{
			return this.node.position + this.localPosition;
		}

		/// <summary>
		/// Sets position of tangent using global coordinates and adjusts cotangent based on specified behaviour.
		/// </summary>
		/// <param name="position"><see cref="Vector3" /> global position</param>
		/// <param name="behaviour">Behaviour, that will be applied to cotangent</param>
		public void SetPosition (Vector3 position, CotangentBehaviour behaviour = CotangentBehaviour.keepMagnitudeAdjustDirection)
		{
			this.localPosition = position - this.node.position;

			if (behaviour == CotangentBehaviour.keepMagnitudeAdjustDirection)
			{
				this.cotangent.localPosition = -this.localPosition.normalized * this.cotangent.localPosition.magnitude;
			}
			else if (behaviour == CotangentBehaviour.exactCotangent)
			{
				this.cotangent.localPosition = -this.localPosition;
			}
		}



	}



}