using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using Wemju.Console;

public class ConsoleHelper : MonoBehaviour
{
	public static event Action<bool> ConsoleEnabledChanged;

	private static ConsoleHelper _instance;

	public static ConsoleHelper Instance
	{
		get
		{
			if (_instance == null)
			{
				var go = Resources.Load<GameObject>("ConsoleCanvas");
				go = Instantiate(go);
				_instance = go.GetComponent<ConsoleHelper>();
				_instance.Setup();
				//var helper = new GameObject("_ConsoleHelperSpawner").AddComponent<ConsoleHelper>();
				//_instance = helper.InstantiatePrefab();
				//Destroy(helper.gameObject);
			}

			return _instance;
		}
	}

	public void Setup()
	{
		ChangeState(VisibilityState.Hidden, true);
		SetRedirectDebugLogs(true);

		//IngameConsole.RegisterMethod(Method.TOGGLE_CONSOLE, ShowSingle);
		//IngameConsole.RegisterMethod(Method.TOGGLE_CONSOLE_EXPANDED, ShowExpanded);
		//IngameConsole.RegisterMethod(Method.CONSOLE_LOG_LEVEL, SetRedirectDebugLogs);
		//IngameConsole.RegisterMethodWithParams(Method.CONSOLE_LOG_LEVEL, SetConsoleLogLevel2);
		//IngameConsole.RegisterMethod(Method.DEBUG_TEST_LOG, TestLogMessage);
//		IngameConsole.RegisterMethodWithParams(Method.ECHO, Echo);
	}

	[ConsoleMethod(Method.DEBUG_TEST_LOG)]
	public static void TestLogMessage()
	{
		Debug.Log("Test1");
		Debug.LogWarning("TestWarning");
		Debug.LogError("TestError");
		Debug.LogException(new Exception("TestException"));
		Debug.LogAssertion("TestAssertion");
	}

	[ConsoleMethod(Method.ECHO)]
	public static void Echo(string input)
	{
		Debug.Log(input);
	}

	[ConsoleMethod(Method.CONSOLE_LOG_LEVEL)]
	private static void SetRedirectDebugLogs(bool state)
	{
		if (!state)
		{
			if (_didSubscribeLogMessageReceived)
			{
				Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
				_didSubscribeLogMessageReceived = false;
			}
		}
		else if(state)
		{
			if (!_didSubscribeLogMessageReceived)
			{
				Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
				_didSubscribeLogMessageReceived = true;
			}
		}

		AddLogToHistory("Redirect debug logs: " + state);
	}

	private static bool _didSubscribeLogMessageReceived = false;


	private void OnDestroy()
	{
		if (_didSubscribeLogMessageReceived)
		{
			Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
			_didSubscribeLogMessageReceived = false;
		}
	}

	private static void OnLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
	{
		var color = GetColorForLogType(type);
		var message = type.ToString().Substring(0, 3).ToUpper() + ": " + condition;

		if (!string.IsNullOrEmpty(color))
		{
			message = color + message + "</color>";
		}

		AddLogToHistory(message);
	}

	private static string GetColorForLogType(LogType type)
	{
		switch (type)
		{
			case LogType.Error:
			case LogType.Exception:
			case LogType.Assert:
				return "<color=red>";
			case LogType.Warning:
				return "<color=yellow>";
			default:
				return "<color=silver>";
		}
	}

	void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			_instance.Setup();
		}
		else if (_instance != this)
		{
			Debug.Log("Multiple ConsoleHelpers detected, destroying self");
			Destroy(gameObject);
		}
	}

	[SerializeField] private GameObject _helperPrefab = null;

	private ConsoleHelper InstantiatePrefab()
	{
		var go = Instantiate(_helperPrefab, transform);
		DontDestroyOnLoad(go);
		return go.GetComponent<ConsoleHelper>();
	}

	private void UpdateActiveState()
	{
		//gameObject.SetActive(_currentState != VisibilityState.Hidden);
		ConsoleEnabledChanged.SafeInvoke(_currentState != VisibilityState.Hidden);
	}


	[ConsoleMethod(Method.TOGGLE_CONSOLE_EXPANDED)]
	public static void ShowExpanded()
	{
		Instance.ChangeState(VisibilityState.Expanded);
	}

//	public void ShowSingle()
//	{
//		ChangeState(VisibilityState.Single);
//	}

	private enum VisibilityState
	{
		Hidden,
		Single,
		Expanded
	}

	private VisibilityState _currentState = VisibilityState.Hidden;

	private void ChangeState(VisibilityState state, bool forceChange = false)
	{
//		if (_currentState == VisibilityState.Hidden && _currentState == state && !forceChange)
//		{
//			return false;
//		}

		//Debug.Log("Change state from: " + _currentState + " to: " + state);

		if (_currentState == VisibilityState.Single && state == VisibilityState.Expanded)
		{
		}
		else if (_currentState == state)
		{
			state = VisibilityState.Hidden;
		}

//		if (_currentState != VisibilityState.Single && state == VisibilityState.Expanded)
//		{
//			state = VisibilityState.Hidden;
//		}

		_singleViewContainer.SetActive(state == VisibilityState.Single);
		_expandedViewContainer.SetActive(state == VisibilityState.Expanded);

		_currentState = state;

		SelectInputField();

		UpdateActiveState();
	}

	[SerializeField] private GameObject _singleViewContainer = null;
	[SerializeField] private GameObject _expandedViewContainer = null;

	[SerializeField] private InputField _expandedInputField = null;
	[SerializeField] private InputField _singleInputField = null;
	[SerializeField] private Text _expandedLogHistory = null;

	public void OnEndEdit(string val)
	{
		_lastTabFinalLength = -1;
		if (!string.IsNullOrEmpty(val))
		{
			_singleInputField.text = string.Empty;
			_expandedInputField.text = string.Empty;

			IngameConsole.InvokeNewStyle(val);
			_commandLogs.Add(val);
			_lastCommandIndex = _commandLogs.Count;
		}

		SelectInputField();
	}

	private void SelectInputField()
	{
		var field = GetCurrentInputField();
		if (field != null)
		{
			field.Select();
			field.ActivateInputField();
		}
	}


	//private int _currentSuggestion = -1;

	private int _lastTabFinalLength = -1;

	private void Update()
	{
		if (_currentState == VisibilityState.Hidden)
		{
			return;
		}

		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if (_suggestions.Count > 0)
			{
				int index = -1;
				char c = _suggestions[0][0];

				for (int i = 0; i < _suggestions[0].Length; i++)
				{
					c = _suggestions[0][i];
					for (int j = 0; j < _suggestions.Count; j++)
					{
						if (_suggestions[j].Length < i || _suggestions[j][i] != c)
						{
							index = i;
							break;
						}
					}

					if (index != -1)
					{
						break;
					}
				}

				string finalInput = index != -1 ? _suggestions[0].Substring(0, index) : _suggestions[0];
				if (_suggestions.Count > 1 && _lastTabFinalLength == finalInput.Length)
				{
					AddLogToHistory(">" + finalInput);
					foreach (var suggestion in _suggestions)
					{
						AddLogToHistory(suggestion);
					}
				}
				else
				{
					_lastTabFinalLength = finalInput.Length;
					SetCommand(finalInput);
				}
			}
		}

		if (_updateLogWindow && _currentState == VisibilityState.Expanded)
		{
			_updateLogWindow = false;
			_expandedLogHistory.text = string.Join("\r\n", _logHistory.ToArray());
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{

			var field = GetCurrentInputField();
			if (field != null)
			{
				if (string.IsNullOrEmpty(field.text))
				{
					ChangeState(VisibilityState.Hidden);
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			if (_lastCommandIndex > 0)
			{
				_lastCommandIndex--;
				SetCommand();
			}
		}
		else if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			if (_lastCommandIndex < _commandLogs.Count)
			{
				_lastCommandIndex++;
			}

			if (_lastCommandIndex < _commandLogs.Count)
			{
				SetCommand();
			}
			else
			{
				SetCommand(string.Empty);
			}
		}
	}

	private void SetCommand(string overrideCommand = null)
	{
		string command = overrideCommand ?? (_commandLogs.Count == 0 ? string.Empty : _commandLogs[_lastCommandIndex]);


		var field = GetCurrentInputField();

		if (field != null)
		{
			field.text = command;
			field.selectionAnchorPosition = command.Length;
			field.selectionFocusPosition = command.Length;
		}

	}

	private InputField GetCurrentInputField()
	{
		switch (_currentState)
		{
			case VisibilityState.Single:
				return _singleInputField;
			case VisibilityState.Expanded:
				return _expandedInputField;
			default:
				return null;
		}
	}

	private List<string> _commandLogs = new List<string>();
	private int _lastCommandIndex = 0;

//	[SerializeField] private InputField _singleInput;


//	private string _consoleTextInput = string.Empty;


//	private const string CONTROL_NAME = "ConsoleField";

//	private GUIStyle _consoleStyle;

	private static List<string> _logHistory = new List<string>();
	private static bool _updateLogWindow = false;

	public static void AddLogToHistory(string log)
	{
		_logHistory.Add(log);
		_updateLogWindow = true;
	}

	public void CheckSuggestions(string input)
	{
		//_currentSuggestion = -1;
		if (input.StartsWith(Method.SET) || input.StartsWith(Method.GET))
		{
			var prefix = input.Split(' ')[0];
			var withoutPrefix = input.Remove(0, prefix.Length).TrimStart();
			_suggestions = FindVariableSuggestions(withoutPrefix).ToList();
			for (int i = 0; i < _suggestions.Count; i++)
			{
				_suggestions[i] = prefix + " " + _suggestions[i];
			}
		}
		else
		{
			_suggestions = FindSuggestions(input).ToList();
		}

		_suggestionLabel.text = string.Join("\r\n", _suggestions.ToArray());
	}

	private List<string> _suggestions = new List<string>();

	[SerializeField] private Text _suggestionLabel = null;

	private IEnumerable<string> FindSuggestions(string prefix)
	{
		return IngameConsole.GetAllMethods().ToList().FindAll(x => x.StartsWith(prefix));
	}

	private IEnumerable<string> FindVariableSuggestions(string prefix)
	{
		return IngameConsole.GetAllVariables().ToList().FindAll(x => x.StartsWith(prefix));
	}

/*
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
					IngameConsole.ExecuteCommand(_consoleTextInput);
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
	}*/
}