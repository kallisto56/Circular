namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// <see cref="AnchorPosition"/> is an enum that helps user to define specific position on <see cref="Biarc"/>, that <see cref="Sample"/> will try to lock onto, when user changes <see cref="Biarc"/> path.
	/// For example: if <see cref="Sample"/> has <see cref="AnchorPosition.leftTangent"/>, it will try to stay at the middle of the first <see cref="Arc"/> of <see cref="Biarc"/>.
	/// </summary>
	public enum AnchorPosition
	{
		[InspectorNameAttribute("01. Origin")] origin,
		[InspectorNameAttribute("02. First left extent")] firstLeftExtent,
		[InspectorNameAttribute("03. Left tangent")] leftTangent,
		[InspectorNameAttribute("04. First right extent")] firstRightExtent,
		[InspectorNameAttribute("05. Left midpoint")] leftMidpoint,
		[InspectorNameAttribute("06. Middle between midpoints")] middleOfBiarc,
		[InspectorNameAttribute("07. Right midpoint")] rightMidpoint,
		[InspectorNameAttribute("08. Second left extent")] secondLeftExtent,
		[InspectorNameAttribute("09. Right tangent")] rightTangent,
		[InspectorNameAttribute("10. Second right extent")] secondRightExtent,
		[InspectorNameAttribute("11. Destination")] destination,
		[InspectorNameAttribute("12. Manual")] manual
	}



}