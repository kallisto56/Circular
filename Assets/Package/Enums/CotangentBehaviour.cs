namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// <see cref="CotangentBehaviour"/> used by <see cref="Tangent"/> to determine how it should process cotangent.
	/// </summary>
	public enum CotangentBehaviour
	{
		[InspectorNameAttribute("01. Keep magnitude, but adjust direction")] keepMagnitudeAdjustDirection,
		[InspectorNameAttribute("02. Exact cotangent")] exactCotangent,
		[InspectorNameAttribute("03. Manual")] manual
	}



}