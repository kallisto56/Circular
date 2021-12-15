namespace Circular
{
	using UnityEngine;



	/// <summary>
	/// Used for specifying how polylines and primitives should be rendered in PathEditor.
	/// </summary>
	public enum Occlusion
	{
		[InspectorNameAttribute("Disabled")] disabled,
		[InspectorNameAttribute("Render only visible")] visibleOnly,
		[InspectorNameAttribute("Render occluded parts as dimmed")] dimmed
	}



}