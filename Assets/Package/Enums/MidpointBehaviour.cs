namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// <see cref="MidpointBehaviour"/> used by <see cref="Midpoint"/> to determine how it should position itself between tangents.
	/// </summary>
	public enum MidpointBehaviour
	{
		[InspectorNameAttribute("01. Stay at the middle")] stayAtMiddle,
		[InspectorNameAttribute("02. Offset from tangent")] offsetFromTangent,
		[InspectorNameAttribute("03. Offset from middle")] offsetFromMiddle,
		[InspectorNameAttribute("04. Auto")] auto,
	}



}