namespace LUAnity
{
	using System;

	// Marks a method, field or property to be hidden from Lua auto-completion
	[AttributeUsage( AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property )]
	public sealed class LuaHideAttribute : Attribute
	{
	}
}
