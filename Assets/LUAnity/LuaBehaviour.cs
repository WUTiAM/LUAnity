using LUAnity;
using System;
using UnityEngine;

public class LuaBehaviour : MonoBehaviour
{
	public string LuaScriptPathName;

	protected LuaTable _selfTable;

	LuaFunction _awakeFunc;
	LuaFunction _startFunc;
	LuaFunction _updateFunc;
	LuaFunction _lateUpdateFunc;
	LuaFunction _onEnableFunc;
	LuaFunction _onDisableFunc;
	LuaFunction _onDestroyFunc;
	LuaFunction _setGameObjectFunc;

	void _Initialize()
	{
		object result = LuaSystem.RequireLua( LuaScriptPathName );
		Debug.Assert( result != null, string.Format( "Lua controller file must return something! ({0})", LuaScriptPathName ) );
		LuaFunction rf = result as LuaFunction;
		Debug.Assert( rf != null, string.Format( "Lua controller file must return a function! ({0})", LuaScriptPathName ) );

		object[] results;
#if UNITY_EDITOR
		try
		{
#endif
			results = rf.Call();
#if UNITY_EDITOR
		}
		catch( Exception e )
		{
			Debug.LogError( string.Format( "{0} {1}", e.Source, e.Message ) );

			return;
		}
#endif
		Debug.Assert( results.Length > 0, string.Format( "Lua controller class must return something! ({0})", LuaScriptPathName ) );

		_selfTable = results[0] as LuaTable;
		Debug.Assert( _selfTable != null, string.Format( "Lua controller class must return a table! ({0})", LuaScriptPathName ) );

		_setGameObjectFunc = _selfTable["setGameObject"] as LuaFunction;
		Debug.Assert( _setGameObjectFunc != null );
		_setGameObjectFunc.Call( gameObject );

		_awakeFunc = _selfTable["awake"] as LuaFunction;
		_startFunc = _selfTable["start"] as LuaFunction;
		_updateFunc = _selfTable["update"] as LuaFunction;
		_lateUpdateFunc = _selfTable["lateUpdate"] as LuaFunction;
		_onEnableFunc = _selfTable["onEnable"] as LuaFunction;
		_onDisableFunc = _selfTable["onDisable"] as LuaFunction;
		_onDestroyFunc = _selfTable["__onDestroy"] as LuaFunction;
	}

	void Awake()
	{
		if( !string.IsNullOrEmpty( LuaScriptPathName ) )
		{
			_Initialize();
		}

		if( _awakeFunc != null )
		{
#if UNITY_EDITOR
			try
			{
#endif
				_awakeFunc.Call();
#if UNITY_EDITOR
			}
			catch( Exception e )
			{
				Debug.LogError( string.Format( "{0} {1}", e.Source, e.Message ) );
			}
#endif
		}
	}

	void Start()
	{
		if( _startFunc != null )
		{
#if UNITY_EDITOR
			try
			{
#endif
				_startFunc.Call();
#if UNITY_EDITOR
			}
			catch( Exception e )
			{
				Debug.LogError( string.Format( "{0} {1}", e.Source, e.Message ) );
			}
#endif
		}
	}
	
	void Update()
	{
		if( _updateFunc != null )
		{
#if UNITY_EDITOR
			try
			{
#endif
				_updateFunc.Call();
#if UNITY_EDITOR
			}
			catch( Exception e )
			{
				Debug.LogError( string.Format( "{0} {1}", e.Source, e.Message ) );
			}
#endif
		}

		_PostUpdate();
	}

	protected virtual void _PostUpdate()
	{ 
	}

	void LateUpdate()
	{
		if( _lateUpdateFunc != null )
		{
#if UNITY_EDITOR
			try
			{
#endif
				_lateUpdateFunc.Call();
#if UNITY_EDITOR
			}
			catch( Exception e )
			{
				Debug.LogError( string.Format( "{0} {1}", e.Source, e.Message ) );
			}
#endif
		}
	}

	void OnEnable()
	{
		if (_onEnableFunc != null)
		{
#if UNITY_EDITOR
			try
			{
#endif
				_onEnableFunc.Call();
#if UNITY_EDITOR
			}
			catch( Exception e )
			{
				Debug.LogError(string.Format("{0} {1}", e.Source, e.Message));
			}
#endif
		}
	}

	void OnDisable()
	{
		if (_onDisableFunc != null)
		{
#if UNITY_EDITOR
			try
			{
#endif
				_onDisableFunc.Call();
#if UNITY_EDITOR
			}
			catch( Exception e )
			{
				Debug.LogError(string.Format("{0} {1}", e.Source, e.Message));
			}
#endif
		}
	}
	
	void OnDestroy()
	{
		if( _onDestroyFunc != null )
		{
#if UNITY_EDITOR
			try
			{
#endif
				_onDestroyFunc.Call();
#if UNITY_EDITOR
			}
			catch( Exception e )
			{
				Debug.LogError( string.Format( "{0} {1}", e.Source, e.Message ) );
			}
#endif
		}
	}
}
