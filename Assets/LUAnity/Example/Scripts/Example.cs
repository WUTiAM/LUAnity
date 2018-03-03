using System;
using LUAnity;
using LUAnityExample;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public class Example : MonoBehaviour
{
	GameObject _uiGO;

	void Awake()
	{
		_InitializeLua();

		Object o = Resources.Load( "Prefabs/ExampleUI" );
		_uiGO = Instantiate( o ) as GameObject;


		int i = GetObject<int>( 1 );
	}

	void _InitializeLua()
	{
		if( L2U.lua != null )
		{
			L2U.lua.Dispose();
		}

		L2U.lua = new Lua();

		L2U.lua["_DEBUG_"] = true;
#if ( UNITY_IOS || UNITY_ANDROID ) && !UNITY_EDITOR
		L2U.lua["_MOBILE_DEVICE_"] = true;
#endif
		LuaRegistrationHelper.TaggedStaticMethods( L2U.lua, typeof( Example ) );

		L2U.RequireLua( "initialization" );
	}
	
	[LuaGlobalAttribute()]
	static int GetMagicNumber(  )
	{
		return 0;
	}

	void OnDestroy()
	{
		GameObject.Destroy( _uiGO );
		_uiGO = null;

		L2U.lua.Dispose();
		L2U.lua = null;
	}



	T GetObject<T>( int index )
	{
		T o;
		ToObject( index, out o );

		return o;
	}

	void ToObject( int index, out Type o )
	{
		o = null;
	}

	void ToObject( int index, out bool o )
	{
		o = false;
	}

	void ToObject( int index, out int o )
	{
		o = 0;
	}
}
