namespace Circular
{



	/// <summary>
	/// <see cref="MidpointBehaviour"/> used by <see cref="Midpoint"/> to determine how it should position itself between tangents.
	/// </summary>
	public enum MidpointBehaviour
	{
		stayAtMiddle,
		offsetFromTangent,
		offsetFromMiddle,
		auto,
	}



}