using LUAnity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class LuaSystem
{
	static Lua _lua;

	public static void Initialize()
	{
		if( _lua != null )
		{
			_lua.Dispose();
		}

		_lua = new Lua();
		LuaRegistrationHelper.TaggedStaticMethods( _lua, typeof( LuaSystem ) );

#if _DEBUG_
		_lua["_DEBUG_"] = true;
#endif
#if ( UNITY_IOS || UNITY_ANDROID ) && !UNITY_EDITOR
		_lua["_MOBILE_PLATFORM_"] = true;
#endif

		RequireLua( "initialize" );
	}

	[LuaGlobalAttribute()]
	public static object RequireLua( string luaScriptPath )
	{
		byte[] luaCode = null;
		object[] results = null;

#if UNITY_EDITOR
		string path = Path.Combine( Application.dataPath, Path.Combine( "LuaScripts", luaScriptPath + ".lua" ) );
		if( File.Exists( path ) )
		{
			luaCode = File.ReadAllBytes( path );	
		}
		else
		{
		    Debug.LogError( "Lua script file does NOT exist! (" + luaScriptPath + ")" );
		}
#else
		try
		{
			TextAsset ta = Resources.Load( "LuaScripts/" + luaScriptPath ) as TextAsset;
			if( ta != null )
			{
				luaCode = ta.bytes;
			}
			else
			{
				Debug.LogError( "Load Lua script file failed! (" + luaScriptPath + ")" );
			}
		}
		catch
		{
			Debug.LogWarning( "Lua script file does NOT exist! (" + luaScriptPath + ")" );
		}
#endif

		if( luaCode != null )
		{
			try
			{
				results = _lua.DoString( luaCode, luaScriptPath );
			}
			catch( Exception e )
			{
				Debug.LogError( string.Format( "Failed to do RequireLua(\"{0}\"): {1} {2}", luaScriptPath, e.Source, e.Message ) );
			}
		}

		return ( results != null && results.Length > 0 ) ? results[0] : null;
	}

	[LuaGlobalAttribute()]
	public static void LuaPrint( string message )
	{
		Debug.Log( "[DEBUG:Lua] " + message );
	}

	[LuaGlobalAttribute()]
	public static void LuaLog( string message )
	{
		Debug.Log( "[LOG:Lua] " + message );
	}

	[LuaGlobalAttribute()]
	public static void LuaError( string message )
	{
		Debug.LogError( "[ERROR:Lua] " + message );
	}

	// Anti-Strip_Byte_Code here!
	static void __AntiStripByteCode__()
	{
		GameObject go = new GameObject();
		go = go.gameObject;
		go.SendMessage( "" );

		Animation anim = go.GetComponent<Animation>();
		anim.Blend( "" );
		anim.GetClip( "" );
		anim.IsPlaying( "" );
		anim.CrossFade( "" );
		anim.CrossFadeQueued( "" );

		AudioClip clip = new AudioClip();
		float audioClipLength = clip.length;

		Camera camera = new Camera();
		Vector3 vector3 = go.transform.localEulerAngles;
		camera.transform.localEulerAngles = vector3;

		Shader shader = Shader.Find( "" );

		Material m = new Material( shader );
		m.GetFloat( "" );
		m.SetFloat( "", audioClipLength );

		RenderSettings.fog = RenderSettings.fog;
		RenderSettings.fogColor = RenderSettings.fogColor;
		RenderSettings.fogDensity = RenderSettings.fogDensity;
		RenderSettings.fogStartDistance = RenderSettings.fogStartDistance;
		RenderSettings.fogEndDistance = RenderSettings.fogEndDistance;
	}
}
