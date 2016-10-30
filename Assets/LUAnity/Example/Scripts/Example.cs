using LUAnity;
using LUAnityExample;
using System.IO;
using UnityEngine;

public class Example : MonoBehaviour
{
	GameObject _uiGO;

	void Awake()
	{
		_InitializeLua();

		Object o = Resources.Load( "Prefabs/ExampleUI" );
		_uiGO = Instantiate( o ) as GameObject;
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

	void OnDestroy()
	{
		GameObject.Destroy( _uiGO );
		_uiGO = null;
		
		L2U.lua.Dispose();
		L2U.lua = null;
	}
}
