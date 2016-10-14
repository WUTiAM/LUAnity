namespace LUAnity
{
	using System;

	// Marks a method for global usage in Lua scripts
	[AttributeUsage( AttributeTargets.Method )]
	public sealed class LuaGlobalAttribute : Attribute
	{
		// An alternative name to use for calling the function in Lua - leave empty for CLR name
		public string Name { get; set; }

		// A description of the function
		public string Description { get; set; }
	}
}
