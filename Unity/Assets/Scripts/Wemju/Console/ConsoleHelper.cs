using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wemju.Console;

public class ConsoleHelper : MonoBehaviour
{
	private static ConsoleHelper _instance;

	public static ConsoleHelper Instance
	{
		get
		{
			if (_instance == null)
			{
				GameObject go = new GameObject("_ConsoleHelper");
				_instance = go.AddComponent<ConsoleHelper>();
				DontDestroyOnLoad(go);
			}

			return _instance;
		}
	}

	public bool isActive
	{
		get { return gameObject.activeSelf; }
	}


	public void ToggleConsole()
	{
		gameObject.SetActive(!gameObject.activeSelf);
	}

	private string _consoleTextInput = string.Empty;


	private const string CONTROL_NAME = "ConsoleField";

	private GUIStyle _consoleStyle;

	private List<string> _logHistory = new List<string>();

	public void AddLogToHistory(string log)
	{
		_logHistory.Add(log);
	}

	public void OnGUI()
	{
//		GUILayout.Box("Ayy");
		if (_consoleStyle == null)
		{
			_consoleStyle = new GUIStyle("Box")
			{
				alignment = TextAnchor.MiddleLeft,
			};
		}

//		using (new GUILayout.HorizontalScope())
//		{
			using (new GUILayout.VerticalScope())
			{
				DrawConsoleHistory();
				DrawInteractiveConsole();
			}

//		}

//		if (Input.GetKeyDown(KeyCode.Return))
//		{
//		}
		//GUI.FocusControl();
	}

	private Vector2 _historyScrollPosition = new Vector2();

	private void DrawConsoleHistory()
	{
		using (var scope = new GUILayout.ScrollViewScope(_historyScrollPosition, _consoleStyle))
		{
			_historyScrollPosition = scope.scrollPosition;

			GUILayout.FlexibleSpace();
			foreach (var log in _logHistory)
			{
				GUILayout.Label(log);
			}
		}
	}

	private void DrawInteractiveConsole()
	{
		GUI.SetNextControlName(CONTROL_NAME);
		var e = Event.current;
		if (e.isKey && e.rawType == EventType.KeyUp)
		{
			switch (e.keyCode)
			{
				case KeyCode.Return:
					Console.ExecuteCommand(_consoleTextInput);
					_consoleTextInput = string.Empty;
					e.Use();
					break;
				case KeyCode.Escape:
					if (string.IsNullOrEmpty(_consoleTextInput))
					{
						ToggleConsole();
					}
					else
					{
						_consoleTextInput = string.Empty;
					}
					e.Use();
					break;
			}
		}

		_consoleTextInput = GUILayout.TextField(_consoleTextInput, _consoleStyle, GUILayout.Width(Screen.width));
		GUI.FocusControl(CONTROL_NAME);
	}
}