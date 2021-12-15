namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// Used for specifying type of rendering that should be used in PathEditor.
	/// </summary>
	public enum PathColor
	{
		[InspectorNameAttribute("Solid (single color)")] solid,
		[InspectorNameAttribute("Gradient (color from Samples)")] gradient,
	}



}