using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class EditorMenuItems : MonoBehaviour
{
	const string EDITOR_MENU_ROOT_NAME = "[ LUAnity Project ]";

	//
	// Basic utilities
	//

	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Open [ The 0th Scene ]", false, 1000 )]
	static void OpenMainScene()
	{
		if( EditorBuildSettings.scenes.Length > 0 )
		{
			EditorSceneManager.OpenScene( EditorBuildSettings.scenes[0].path );
		}
		else
		{
			Debug.LogError( "No scenes in build! Please add at least one scene in \"Build Settings | Scenes in Build\"" );
		}
	}
		
	//
	// Debug switcher
	//

	const string DEBUG_MODE_SYMBOL = "_DEBUG_";
	static bool _debugModeEnabled = false;

	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Debug Mode/ON", false, 9100 )]
	public static void EnableDebugMode()
	{
		_debugModeEnabled = true;

		AddScriptingSymbol( DEBUG_MODE_SYMBOL );
	}
	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Debug Mode/ON", true )]
	static bool ValidateEnableDebugMode()
	{
		_debugModeEnabled = _IsScriptingSymbolEnabled( DEBUG_MODE_SYMBOL );
		return !_debugModeEnabled;
	}

	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Debug Mode/OFF", false, 9101 )]
	public static void DisableDebugMode()
	{
		_debugModeEnabled = false;

		RemoveScriptingSymbol( DEBUG_MODE_SYMBOL );
	}
	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Debug Mode/OFF", true )]
	static bool ValidateDisableDebugMode()
	{
		_debugModeEnabled = _IsScriptingSymbolEnabled( DEBUG_MODE_SYMBOL );
		return _debugModeEnabled;
	}

	// Symbol utilities

	public static void AddScriptingSymbol( string symbol )
	{
		if( !_IsScriptingSymbolEnabled( symbol ) )
		{
			string symbolsString = PlayerSettings.GetScriptingDefineSymbolsForGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
			symbolsString += ";" + symbol;

			PlayerSettings.SetScriptingDefineSymbolsForGroup( EditorUserBuildSettings.selectedBuildTargetGroup, symbolsString );
		}
	}

	public static void RemoveScriptingSymbol( string symbol )
	{
		string symbolsString = PlayerSettings.GetScriptingDefineSymbolsForGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
		string[] symbols = symbolsString.Split( ';' );

		symbolsString = "";

		foreach( string s in symbols )
		{
			if( s != symbol )
			{
				if( symbolsString.Length > 0 )
				{
					symbolsString += ';';
				}
				symbolsString += s;
			}
		}
		PlayerSettings.SetScriptingDefineSymbolsForGroup( EditorUserBuildSettings.selectedBuildTargetGroup, symbolsString );
	}

	static bool _IsScriptingSymbolEnabled( string symbol )
	{
		string symbolsString = PlayerSettings.GetScriptingDefineSymbolsForGroup( EditorUserBuildSettings.selectedBuildTargetGroup );

		return symbolsString.Contains( symbol );
	}

	//
	// Unity project windows
	//

	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Show [ Player Settings ] window", false, 10010 )]
	static void ShowPlayerSettingsInspector()
	{
		EditorApplication.ExecuteMenuItem( "Edit/Project Settings/Player" );
	}

	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Show [ Quality Settings ] window", false, 10020 )]
	static void ShowQualitySettingsInspector()
	{
		EditorApplication.ExecuteMenuItem( "Edit/Project Settings/Quality" );
	}

	[MenuItem( EDITOR_MENU_ROOT_NAME + "/Show [ Graphics Settings ] window", false, 10030 )]
	static void ShowGraphicsSettingsInspector()
	{
		EditorApplication.ExecuteMenuItem( "Edit/Project Settings/Graphics" );
	}
}
