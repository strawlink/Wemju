using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Extensions;
using UnityEngine;

namespace Wemju.Console
{
	public static class Console
	{
		static Console()
		{
			RegisterMethod(Method.CFG_LOAD, StartLoadConfig);
			RegisterMethod(Method.CFG_SAVE, StartSaveConfig);

			RegisterMethod(Method.TOGGLE_CONSOLE, ConsoleHelper.Instance.ToggleConsole);
			//LoadConfig();
		}

		private class Variable
		{
			public Variable(object defaultValue, Action<object> onChange)
			{
				this.defaultValue = defaultValue;
				valueType = defaultValue.GetType();
				_value = defaultValue;
				OnChange = onChange;
			}

			public readonly object defaultValue;
			public readonly Type valueType;
			private object _value;
			public event Action<object> OnChange;

			public object value
			{
				get { return _value; }
				set
				{
					_value = value;
					OnChange.SafeInvoke(_value);
				}
			}
		}

		private const string PATH_TO_CONFIG = "D:\\config.cfg";

		private static bool _isDirty = false;

		private static Dictionary<string, Variable> _variableCollection = new Dictionary<string, Variable>();


		public static T GetVar<T>(string name, T defaultValue = default(T))
		{
			Variable obj;
			if (_variableCollection.TryGetValue(name, out obj))
			{
				return (T) obj.value;
			}

			return defaultValue;
		}

		public static T GetDefaultVar<T>(string name)
		{
			Variable obj;
			if (_variableCollection.TryGetValue(name, out obj))
			{
				return (T) obj.defaultValue;
			}

			return default(T);
		}

		public static void SetVar<T>(string name, T val)
		{
			_variableCollection[name].value = val;

			_isDirty = true;

			SaveConfig();
		}

		public static void RegisterVar<T>(string name, T defaultValue, Action<object> onChange = null)
		{
			_variableCollection[name] = new Variable(defaultValue, onChange);
		}


		private static Dictionary<string, Action> _methodCollection = new Dictionary<string, Action>();

		public static void RegisterMethod(string name, Action method)
		{
			_methodCollection[name] = method;
		}

		public static void ToggleShowHide()
		{
		}

		public static void StartSaveConfig()
		{
			var thread = new Thread(SaveConfig)
			{
				IsBackground = true
			};
			thread.Start();
		}
		public static void SaveConfig()
		{
			Debug.Log("SaveConfig");
			if (!_isDirty)
			{
				return;
			}

			lock (_keyBindings)
			{
				var lines = new string[_keyBindings.Count];

				int i = 0;
				foreach (var binding in _keyBindings)
				{
					lines[i] = Method.BIND + " " + binding.Key.ToString() + " " + binding.Value;
					i++;
				}

				File.WriteAllLines(PATH_TO_CONFIG, lines);
				_isDirty = false;
			}
		}

		public static void StartLoadConfig()
		{
			var thread = new Thread(LoadConfig)
			{
				IsBackground = true
			};
			thread.Start();
		}

		private static void LoadConfig()
		{
			Debug.Log("LoadConfig");
			if (File.Exists(PATH_TO_CONFIG))
			{
				var lines = File.ReadAllLines(PATH_TO_CONFIG);
				lock (_keyBindings)
				{
					_keyBindings.Clear();

					foreach (var line in lines)
					{
						ExecuteCommand(line);
					}
				}
			}
			else
			{
				SetDefaultBindings();
			}
		}

		private static void SetDefaultBindings()
		{
			Debug.Log("SetDefaultBindings");
			lock (_keyBindings)
			{
				_keyBindings = new Dictionary<KeyCode, string>
				{
					{KeyCode.Mouse0, Method.ACTION_SHOOT},
				};
			}
		}

		public static void ExecuteCommand(string input)
		{
			Debug.Log("ExecuteCommand: " + input);
			ConsoleHelper.Instance.AddLogToHistory(input);
			var commands = input.Split(' ');

			if (commands.Length < 2)
				return;

			var key = commands[0];
			var inputVal = commands[1];

			if (commands.Length == 2)
			{
				Variable variable;
				if (_variableCollection.TryGetValue(key, out variable))
				{
					var varType = variable.valueType;

					int intVal;
					float floatVal;
					bool boolVal;

					if (varType == typeof(int) && int.TryParse(inputVal, out intVal))
					{
						variable.value = intVal;
					}
					else if (varType == typeof(float) && float.TryParse(inputVal, out floatVal))
					{
						variable.value = floatVal;
					}
					else if (varType == typeof(bool) && bool.TryParse(inputVal, out boolVal))
					{
						variable.value = boolVal;
					}
					else if (varType == typeof(string))
					{
						variable.value = inputVal;
					}
					else
					{
						Debug.Log("Invalid input: " + inputVal);
					}
				}
				else
				{
				}
//				else
//				{
//					Debug.Log("Unknown variable");
//				}
			}
			else if (commands.Length >= 3)
			{
				var method = commands[2];

				if (key == Method.BIND)
				{
					try
					{
						var keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), inputVal);
						if (keyCode != KeyCode.None && Enum.IsDefined(typeof(KeyCode), keyCode))
						{
							Debug.Log("KeyCode is: " + keyCode);
							if (_methodCollection.ContainsKey(method))
							{
								_keyBindings[keyCode] = method;
								_isDirty = true;
								Debug.Log("Bound keycode: " + keyCode + " to method: " + method);
							}
							else
							{
								Debug.Log("Invalid method: " + method);
							}
						}
						else
						{
							Debug.Log("Invalid key: " + inputVal);
						}
					}
					catch
					{
						Debug.Log("Invalid key: " + inputVal);
					}
				}
			}


		}

		private static Dictionary<KeyCode, string> _keyBindings = new Dictionary<KeyCode, string>();

		public static void CheckInputPressed()
		{
			lock (_keyBindings)
			{
				foreach (var binding in _keyBindings)
				{
					if (Input.GetKeyDown(binding.Key))
					{
						Action method;
						if (_methodCollection.TryGetValue(binding.Value, out method))
						{
							method.SafeInvoke();
						}
					}
				}
			}
		}
	}


	public static class Command
	{
		public const string SETTINGS_NAME = "set_name";
		public const string SETTINGS_MAXFPS = "set_maxfps";
		public const string SETTINGS_SENSITIVITY = "set_sensitivity";
	}

	public static class Method
	{
		public const string TOGGLE_CONSOLE = "+toggle_console";
		public const string CFG_LOAD = "+cfg_load";
		public const string CFG_SAVE = "+cfg_save";
		public const string BIND = "+bind";
		public const string UNBIND = "+unbind";
		public const string ACTION_SHOOT = "+shoot";
		public const string MOVE_FORWARD = "+forward";
		public const string MOVE_BACKWARD = "+forward";
		public const string MOVE_STRAFE_LEFT = "+strafe_left";
		public const string MOVE_STRAFE_RIGHT = "+strafe_right";

		public const string SERVER_CONNECT = "+connect";
		public const string SERVER_DISCONNECT = "+disconnect";
		public const string SERVER_RECONNECT = "+reconnect";
	}
}