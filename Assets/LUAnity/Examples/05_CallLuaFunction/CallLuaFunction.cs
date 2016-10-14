using LUAnity;
using System.Text;
using UnityEngine;

public class CallLuaFunction : MonoBehaviour
{
	private string script = @"
		function luaFunc(message)
			print(message)
			return 42
		end
	";

	void Start()
	{
		Lua l = new Lua();

		// First run the script so the function is created
		l.DoString( Encoding.UTF8.GetBytes( script ) );

		// Get the function object
		LuaFunction f = l.GetFunction( "luaFunc" );
		// Call it, takes a variable number of object parameters and attempts to interpet them appropriately
		object[] r = f.Call( "I called a Lua function!" );
		// Lua functions can have variable returns, so we again store those as a C# object array, and in this case print the first one
		print( r[0] );
	}
}
