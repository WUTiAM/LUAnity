using LUAnity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AssetLoader
{
	static Dictionary<string, LuaFunction> _sceneLoadedCallbacks = new Dictionary<string, LuaFunction>();

	public static void LoadScene( string sceneName )
	{
		LoadScene( sceneName, null );
	}

	public static void LoadScene( string sceneName, object luaCallbackFunc )
	{
		_sceneLoadedCallbacks.Add( sceneName, luaCallbackFunc as LuaFunction );

		SceneManager.LoadScene( sceneName );
	}

	public static void LoadAdditiveScene( string sceneName )
	{
		SceneManager.LoadScene( sceneName, LoadSceneMode.Additive );
	}

	public static void OnSceneLoaded( Scene scene, LoadSceneMode mode )
	{
		Resources.UnloadUnusedAssets();
		System.GC.Collect();

		if( _sceneLoadedCallbacks.Count > 0 )
		{
			LuaFunction callback = _sceneLoadedCallbacks[scene.name];
			_sceneLoadedCallbacks.Remove( scene.name );

			if( callback != null )
			{
				callback.Call( scene.name );
			}
		}
	}

	public static byte[] LoadLuaScript( string luaScriptPath )
	{
		byte[] s = null;

#if !UNITY_EDITOR
		TextAsset ta = null;

		try
		{
			ta = Resources.Load( "LuaScripts/" + luaScriptPath ) as TextAsset;
		}
		catch
		{
			Debug.LogWarning( "Lua script file does NOT exist! (" + luaScriptPath + ")" );
		}

		if( ta != null )
		{
			s = ta.bytes;
		}
		else
		{
			Debug.LogError( "Load Lua script file failed! (" + luaScriptPath + ")" );
		}
#else

		string path = Path.Combine( Application.dataPath, Path.Combine( "LuaScripts", luaScriptPath + ".lua" ) );
		if( !File.Exists( path ) )
		{
			Debug.LogError( "Lua script file does NOT exist! (" + luaScriptPath + ")" );
			return null;
		}

		s = File.ReadAllBytes( path );
#endif

		return s;
	}

	public static Texture2D LoadTexture( string texturePath )
	{
		Texture2D tex = null;

		if( tex == null )
		{
			try
			{
				tex = Resources.Load( "Textures/" + texturePath ) as Texture2D;
			}
			catch
			{
				Debug.LogError( "Texture does NOT exist! (" + texturePath + ")" );
			}
		}

		return tex;
	}

	public static AudioClip LoadAudio( string audioPath )
	{
		AudioClip audioClip = null;

		if( audioClip == null )
		{
			try
			{
				Object o = Resources.Load( "Audio/" + audioPath );
				if( o == null )
				{
					Debug.LogError( "Failed to load audio: " + audioPath );
					return null;
				}

				audioClip = o as AudioClip;
			}
			catch
			{
				Debug.LogError( "Audio does NOT exist! (" + audioPath + ")" );
			}
		}

		return audioClip;
	}

	public static GameObject LoadPrefab( string prefabPathName )
	{
		return LoadPrefab( prefabPathName, null );
	}

	public static GameObject LoadPrefab( string prefabPathName, GameObject parent )
	{
		GameObject prefab = null;

		Object o = Resources.Load( Path.Combine( "Prefabs", prefabPathName ) );
		if( o == null )
		{
			Debug.LogError( "Failed to load prefab: " + prefabPathName );
			return null;
		}

		prefab = MonoBehaviour.Instantiate( o ) as GameObject;
		if( prefab != null && parent != null )
		{
			prefab.transform.parent = parent.transform;
			prefab.transform.localPosition = Vector3.zero;
			prefab.transform.localEulerAngles = Vector3.zero;
			prefab.transform.localScale = Vector3.one;
		}

		return prefab;
	}
}
