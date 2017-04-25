using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Extensions;
using UnityEngine;

namespace Wemju.Console
{
	public static class IngameConsole
	{
		public static void Initialize()
		{
			var cfg = Resources.Load<TextAsset>(DEFAULT_CONFIG_FILENAME);
			if (cfg == null)
			{
				Debug.LogWarning("Unable to find default config");
			}
			else
			{
				_defaultConfig = cfg.text.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
			}
			LoadAllMethods();
			StartLoadConfig();
		}

		static IngameConsole()
		{

		}

		private static void LoadAllMethods()
		{
			_newMethodCollection.Clear();
			_variableCollection.Clear();

			var type = typeof(IngameConsole);
			var assembly = Assembly.GetAssembly(type);
			var allTypes = assembly.GetTypes();
			var allMethods = allTypes.SelectMany(x => x.GetMethods()).Where(y => y.GetCustomAttributes(typeof(ConsoleMethod), false).Length > 0);
			var allCvars = allTypes.SelectMany(x => x.GetFields()).Where(y => y.GetCustomAttributes(typeof(Cvar), false).Length > 0);
			var allCvarOnChange = allTypes.SelectMany(x => x.GetMethods()).Where(y => y.GetCustomAttributes(typeof(CvarOnChange), false).Length > 0);

			foreach (var info in allMethods)
			{
				if (!info.IsStatic)
				{
					Debug.LogError("Unable to access non-static method \"" + info.Name + "\"");
					continue;
				}

				var attr = info.GetCustomAttributes(typeof(ConsoleMethod), false)[0];
				var cName = ((ConsoleMethod) attr).command;
				_newMethodCollection.Add(cName, info);
			}

			foreach (var info in allCvars)
			{
				if (!info.IsStatic)
				{
					Debug.LogError("Unable to access non-static field \"" + info.Name + "\"");
					continue;
				}

				var attr = info.GetCustomAttributes(typeof(Cvar), false)[0];
				var cvarAttr = ((Cvar) attr);

				// TODO: This style doesn't really make sense, we want to modify the direct variable instead of wrapping it
				Variable newVar = new Variable(cvarAttr.defaultValue);
				_variableCollection.Add(cvarAttr.command, newVar);
			}

			foreach (var info in allCvarOnChange)
			{
				if (!info.IsStatic)
				{
					Debug.LogError("Unable to access non-static method \"" + info.Name + "\"");
					continue;
				}

				var attr = info.GetCustomAttributes(typeof(CvarOnChange), false)[0];
				var cName = ((CvarOnChange) attr).command;

				Variable newVar;
				if (_variableCollection.TryGetValue(cName, out newVar))
				{
					var info1 = info;
					newVar.OnChange += (val) => info1.Invoke(null, new[] {val});
				}
				else
				{
					Debug.LogError("Unable to add OnChange listener; variable \"" + cName + "\" not found");
				}
			}
		}

		private static Dictionary<string, MethodInfo> _newMethodCollection = new Dictionary<string, MethodInfo>();

		private const char SPLIT_INPUT_CHARACTER = ' ';
		public static void InvokeNewStyle(string input)
		{
			//ConsoleHelper.AddLogToHistory(">" + input);
			var commands = new List<string>(input.Split(SPLIT_INPUT_CHARACTER));

			if (commands.Count == 0)
				return;

			var key = commands[0];

			MethodInfo info;
			if (_newMethodCollection.TryGetValue(key, out info))
			{
				//ConsoleHelper.AddLogToHistory("Found method " + key);

				try
				{
					object target = null;

					// Remove the call to the method
					commands.RemoveAt(0);

					var parameters = info.GetParameters();
					var parsedParams = new object[parameters.Length];
					bool failedParse = false;

					if (parameters.Length > 0)
					{
						string[] actualCommands = new string[parameters.Length];

						int commandCount = 0;

						bool isInStart = false;
						bool isInEnd = false;

						for (int i = 0; i < commands.Count; i++)
						{
//							while (true)
//							{
							if (commandCount >= actualCommands.Length)
							{
								//ConsoleHelper.AddLogToHistory("Invalid parameters");
								//Debug.LogError("Error1");
								break;
							}


							var trimmed = commands[i].Trim();
							if (string.IsNullOrEmpty(trimmed) && !isInStart)
							{
								//commandCount++;
								continue;
							}

							if (!string.IsNullOrEmpty(actualCommands[commandCount]))
							{
								trimmed = SPLIT_INPUT_CHARACTER + trimmed; // TODO: Don't do this if it's a quotation mark
							}

							actualCommands[commandCount] += trimmed;

							isInStart = !string.IsNullOrEmpty(actualCommands[commandCount]) && actualCommands[commandCount].StartsWith("\"");
							isInEnd = actualCommands[commandCount].EndsWith("\"");

							if (isInStart == isInEnd)
							{
//								if (isInStart)
//								{
//									actualCommands[commandCount] = actualCommands[commandCount]
//										.Substring(1, actualCommands[commandCount].Length - 2);
//								}
								commandCount++;
							}
//							if ( && !actualCommands[commandCount].EndsWith("}"))
//							{
//								//commandCount++;
//							}
//							else
//							{
//								break;
//							}
//							}
						}

//						do
//						{
//							if ()
//							{
//								failedParse = true;
//								break;
//							}
//						} while (actualCommands[commandCount].StartsWith("{") && !actualCommands[commandCount].EndsWith("}"));

						for (int i = 0; i < parameters.Length; i++)
						{
							var paramType = parameters[i].ParameterType;

							object val;
							if (!ConvertFromString(actualCommands[i], paramType, out val))
							{
								failedParse = true;
								break;
							}

							parsedParams[i] = val;
							//commandCount++;
						}
					}

					if (!failedParse)
					{
						if (parsedParams.Length != parameters.Length)
						{
							ConsoleHelper.AddLogToHistory("Parameter count not matching");
						}
						else
						{
							info.Invoke(null, parsedParams);
						}
					}
					else
					{

						string paramTypes = string.Empty;
						foreach (var parameterInfo in parameters)
						{
							paramTypes += "<" + parameterInfo.ParameterType.ToString().Replace("System.", "") + "> ";
						}

						ConsoleHelper.AddLogToHistory(info.Name + " " + paramTypes);
					}
				}
				catch (Exception e)
				{
					Debug.LogError("Exception while invoking: " + e);
				}
			}
		}

		private static bool ConvertFromString(string inputVal, Type paramType, out object val)
		{
			//object val;
			//string inputVal = commands[i];

			int intVal;
			float floatVal;
			bool boolVal;

			if (paramType == typeof(bool) && bool.TryParse(inputVal, out boolVal))
			{
				val = boolVal;
			}
			else if (paramType == typeof(bool) && int.TryParse(inputVal, out intVal) && (intVal == 0 || intVal == 1))
			{
				val = intVal == 1;
			}
			else if (paramType == typeof(int) && int.TryParse(inputVal, out intVal))
			{
				val = intVal;
			}
			else if (paramType == typeof(float) && float.TryParse(inputVal, out floatVal))
			{
				val = floatVal;
			}
			else if (paramType == typeof(string))
			{
				val = inputVal;
			}
			else
			{
				val = null;
				Debug.Log("Failed to parse input " + inputVal + " to type " + paramType);
				return false;
			}

			return true;
		}

		public class Variable
		{
			public Variable(object defaultValue, Action<object> onChange = null)
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

		/*public class Variable<T>
		{
			public Variable(T defaultValue)
			{
				this.defaultValue = defaultValue;
				valueType = defaultValue.GetType();
				_value = defaultValue;
				//OnChange = onChange;
			}

			public readonly T defaultValue;
			public readonly Type valueType;
			private T _value;
			public event Action<T> OnChange;

			public T value
			{
				get { return _value; }
				set
				{
					_value = value;
					OnChange.SafeInvoke(_value);
				}
			}

			public void Set(T val)
			{
				this.value = val;
			}

			public T Get()
			{
				return value;
			}
		}*/

		private static string GetPathToConfig
		{
			get { return Path.Combine(Environment.CurrentDirectory, "wemjuconfig.cfg"); }
		}

		private static bool _isDirty = false;

		private static Dictionary<string, Variable> _variableCollection = new Dictionary<string, Variable>();

//		public static Variable GetVar(string key)
//		{
//			Variable obj;
//			if (_variableCollection.TryGetValue(key, out obj))
//			{
//				return obj;
//			}
//
//			return null;
//		}
//		public static T GetVar<T>(string key, T defaultValue = default(T))
//		{
//			Variable obj;
//			if (_variableCollection.TryGetValue(key, out obj))
//			{
//				return (T) obj.value;
//			}
//
//			return defaultValue;
//		}
//
//		public static T GetDefaultVar<T>(string key)
//		{
//			Variable obj;
//			if (_variableCollection.TryGetValue(key, out obj))
//			{
//				return (T) obj.defaultValue;
//			}
//
//			return default(T);
//		}

//		public static void SetVar<T>(string name, T val)
//		{
//			_variableCollection[name].value = val;
//
//			_isDirty = true;
//
//			SaveConfig();
//		}

//		public static void RegisterVar<T>(string name, T defaultValue, Action<object> onChange = null)
//		{
//			_variableCollection[name] = new Variable(defaultValue, onChange);
//		}

		public static IEnumerable<string> GetAllMethods()
		{
			return _newMethodCollection.Keys;
		}
		public static IEnumerable<string> GetAllVariables()
		{
			return _variableCollection.Keys;
		}

		[Cvar(Command.CFG_AUTO_SAVE, false)]
		public static bool _cfgAutoSave = false;

		[CvarOnChange(Command.CFG_AUTO_SAVE)]
		public static void CfgAutoSaveOnChange(bool state)
		{
			_cfgAutoSave = state;
		}

		[Cvar("debug_all_input", false)]
		public static bool _debugAllInput = false;

		[CvarOnChange("debug_all_input")]
		public static void DebugAllInputOnChange(bool state)
		{
			_debugAllInput = state;
		}

		[ConsoleMethod(Method.BINDLIST)]
		public static void BindList()
		{
			foreach (var binding in _keyBindings)
			{
				ConsoleHelper.AddLogToHistory(binding.Key + ": " + binding.Value);
			}
		}

		[ConsoleMethod(Method.UNBIND)]
		public static void Unbind(string key)
		{
			var keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), key);
			if (keyCode != KeyCode.None && Enum.IsDefined(typeof(KeyCode), keyCode))
			{
				if (_keyBindings.Remove(keyCode))
				{
					ConsoleHelper.AddLogToHistory("Unbound \"" + keyCode.ToString() + "\"");

					if (_cfgAutoSave)
					{
						StartSaveConfig();
					}
				}
				else
				{
					ConsoleHelper.AddLogToHistory("Key \"" + keyCode.ToString() + "\" was not bound");
				}
			}
			else
			{
				ConsoleHelper.AddLogToHistory("Invalid keyCode \"" + key + "\"");
			}
		}

		[ConsoleMethod(Method.CFG_PATH)]
		public static void EchoConfigPath()
		{
			ConsoleHelper.AddLogToHistory(GetPathToConfig);
		}

		[ConsoleMethod(Method.CFG_SAVE)]
		public static void StartSaveConfig()
		{
			var thread = new Thread(SaveConfig)
			{
				IsBackground = true
			};
			thread.Start();
		}
		private static void SaveConfig()
		{
			if (!_isDirty)
			{
				ConsoleHelper.AddLogToHistory("Attempted to save, but no changes were detected");
				return;
			}

			lock (_keyBindings)
			{
				WriteCurrentConfigToPath(GetPathToConfig);
			}
			_isDirty = false;

			if (!_cfgAutoSave)
			{
				ConsoleHelper.AddLogToHistory("Saved config");
			}
		}

		private static void WriteCurrentConfigToPath(string path)
		{
			using (var writer = new StreamWriter(path))
			{
				foreach (var binding in _keyBindings)
				{
					writer.WriteLine(Method.BIND + " " + binding.Key.ToString() + " \"" + binding.Value + "\"");
				}

				foreach (var variable in _variableCollection)
				{
					writer.WriteLine(Method.SET + " " + variable.Key + " " + variable.Value.value);
				}
			}
		}

		private static bool _isLoadingConfig = false;

		[ConsoleMethod(Method.CFG_LOAD)]
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
			_isLoadingConfig = true;
			Debug.Log("LoadConfig");
			if (File.Exists(GetPathToConfig))
			{
				var lines = File.ReadAllLines(GetPathToConfig);
				lock (_keyBindings)
				{
					_keyBindings.Clear();

					foreach (var line in lines)
					{
						InvokeNewStyle(line);
					}
				}
			}
			else
			{
				SetDefaultConfig();
			}

			_isLoadingConfig = false;
		}

#if UNITY_EDITOR
		[ConsoleMethod("+cfg_write_default")]
		public static void WriteDefaultConfig()
		{
			WriteCurrentConfigToPath(GetDefaultConfigPath());
		}

		private static string GetDefaultConfigPath()
		{
			return string.Format("{0}{1}Assets{1}Wemju{1}Console{1}Internal{1}Resources{1}{2}",
				Environment.CurrentDirectory, Path.DirectorySeparatorChar, DEFAULT_CONFIG_FILENAME + DEFAULT_CONFIG_EXTENSION);
		}
#endif

		private const string DEFAULT_CONFIG_FILENAME = "defaultConfig";
		private const string DEFAULT_CONFIG_EXTENSION = ".txt";

		private static string[] _defaultConfig = null;

		[ConsoleMethod("+cfg_reset_default")]
		public static void SetDefaultConfig()
		{
			Debug.Log("SetDefaultConfig");

			var defaultConfig = _defaultConfig;

			//var path =  GetDefaultConfigPath();
//			if (File.Exists(path))
//			{
//
//				defaultConfig = File.ReadAllLines(GetDefaultConfigPath());
//			}
//			else
//			{
//			}


//			var defaultConfig = new string[]
//			{
//			};

			_keyBindings.Clear();

			foreach (var s in defaultConfig)
			{
				InvokeNewStyle(s);
			}
//			lock (_keyBindings)
//			{
//				_keyBindings = new Dictionary<KeyCode, string>
//				{
//					{KeyCode.Mouse0, Method.ACTION_SHOOT},
//					{KeyCode.F1, Method.TOGGLE_CONSOLE_EXPANDED},
//				};
//			}

			_isDirty = true;
			StartSaveConfig();
		}

		[ConsoleMethod(Method.BIND)]
		public static void BindKey(string key, string command)
		{
			try
			{
				var keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), key);
				if (keyCode != KeyCode.None && Enum.IsDefined(typeof(KeyCode), keyCode))
				{
					command = command.Trim('\"');
					_keyBindings[keyCode] = command;
					_isDirty = true;

					if (_cfgAutoSave && !_isLoadingConfig)
					{
						StartSaveConfig();
					}

//					if (_newMethodCollection.ContainsKey(method))
//					{
//						_keyBindings[keyCode] = method;
//						_isDirty = true;
//						Debug.Log("Bound keycode: " + keyCode + " to method: " + method);
//					}
//					else
//					{
//						Debug.Log("Invalid method: " + method);
//					}
				}
				else
				{
					Debug.Log("Invalid key: " + key);
				}
			}
			catch
			{
				Debug.Log("Invalid key: " + key);
			}
		}

		private static Dictionary<KeyCode, string> _keyBindings = new Dictionary<KeyCode, string>();

		public enum KeyPressState
		{
			None,
			Down,
			Up,
			Held
		}

		public static KeyPressState keyState { get; private set; }

		public static void CheckInputPressed()
		{
			lock (_keyBindings)
			{
				foreach (var binding in _keyBindings)
				{
					bool getKeyDown = Core.GetKeyDown(binding.Key);
					bool getKeyUp = !getKeyDown && Core.GetKeyUp(binding.Key);
					bool getKey = !getKeyDown && !getKeyUp && Core.GetKey(binding.Key);

					if(getKeyDown) {keyState = KeyPressState.Down;}
					else if(getKeyUp) {keyState = KeyPressState.Up;}
					else if(getKey) {keyState = KeyPressState.Held;}
					else {keyState = KeyPressState.None;}

					if (keyState != KeyPressState.None)
					{
						InvokeNewStyle(binding.Value);
					}
				}

				DebugAllKeys();
			}
		}

		private static KeyCode[] _allKeyCodes = null;
		private static void DebugAllKeys()
		{
			if (_debugAllInput)
			{
				if (_allKeyCodes == null)
				{
					_allKeyCodes = Enum.GetValues(typeof(KeyCode)) as KeyCode[];
				}

				foreach (var code in _allKeyCodes)
				{
					if (Input.GetKeyDown(code))
					{
						ConsoleHelper.AddLogToHistory("GetKeyDown: " + code);
					}
					/*if (Input.GetKey(code))
					{
						ConsoleHelper.AddLogToHistory("GetKey: " + code);
					}*/
					if (Input.GetKeyUp(code))
					{
						ConsoleHelper.AddLogToHistory("GetKeyUp: " + code);
					}
				}
			}
		}

		[ConsoleMethod(Method.GET)]
		public static void GetVariable(string key)
		{
			Variable var;
			if (_variableCollection.TryGetValue(key, out var))
			{
				ConsoleHelper.AddLogToHistory(key + " : " + var.value + " (default: " + var.defaultValue + ")");
			}
			else
			{
				ConsoleHelper.AddLogToHistory("\"" + key + "\" not found." );
			}
		}

		/*[Cvar("test_one", 1)]
		public static int _testOne = 20;

		[Cvar("test_two", 2)]
		public static int _testTwo = 22;

		[Cvar("test_three", 3)]
		public static int _testThree = 23; // TODO: This value is not used, figure out a way to get the default value from here instead of in the attribute

		[CvarOnChange("test_three")]
		public static void TestThreeOnChange(int test)
		{
			Debug.LogWarning("Test Three: " + test);
		}*/

		[ConsoleMethod(Method.SET)]
		public static void SetVariable(string key, string value)
		{
			Variable var;
			if (_variableCollection.TryGetValue(key, out var))
			{
				if (string.IsNullOrEmpty(value))
				{
					GetVariable(key);
				}
				else
				{
					object parsedVal;
					if (ConvertFromString(value, var.valueType, out parsedVal))
					{
						var.value = parsedVal;
						ConsoleHelper.AddLogToHistory("Set \"" + key + "\" to " + value);
						_isDirty = true;

						if (_cfgAutoSave && !_isLoadingConfig)
						{
							StartSaveConfig();
						}
					}
				}
			}
			else
			{
				ConsoleHelper.AddLogToHistory("\"" + key + "\" not found." );
			}
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class ConsoleMethod : Attribute
	{
		public string command { get; private set; }
		public ConsoleMethod(string command)
		{
			this.command = command;
		}
	}
	[AttributeUsage(AttributeTargets.Method)]
	public class CvarOnChange : Attribute
	{
		public string command { get; private set; }
		public CvarOnChange(string command)
		{
			this.command = command;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class Cvar : Attribute
	{
		public string command { get; private set; }
		public object defaultValue { get; private set; }
		//public Type type { get; private set; }

		public Cvar(string command, object defaultValue)
		{
			this.command = command;
			this.defaultValue = defaultValue;
			//type = defaultValue.GetType();
		}
	}

	public static class Command
	{
		public const string CFG_AUTO_SAVE = "cfg_auto_save";

		public const string SETTINGS_NAME = "set_name";
		public const string SETTINGS_MAXFPS = "set_maxfps";
		public const string SETTINGS_SENSITIVITY = "set_sensitivity";
	}

//	public class Settings
//	{
//		public IngameConsole.Variable<string> name = new IngameConsole.Variable<string>("testname1");
//		public IngameConsole.Variable<int> maxfps = new IngameConsole.Variable<int>(125);
//	}

	public static class Method
	{
		public const string DEBUG_TEST_LOG = "+debug_test_log";
		public const string ECHO = "+echo";

		public const string SET = "+set";
		public const string GET = "+get";

		public const string TOGGLE_CONSOLE = "+toggle_console";
		public const string TOGGLE_CONSOLE_EXPANDED = "+toggle_console_expanded";
		public const string CONSOLE_LOG_LEVEL = "+redirect_debug_logs";
		public const string CFG_LOAD = "+cfg_load";
		public const string CFG_SAVE = "+cfg_save";
		public const string CFG_PATH = "+cfg_path";
		public const string BIND = "+bind";
		public const string UNBIND = "+unbind";
		public const string BINDLIST = "+bindlist";
		public const string ACTION_SHOOT = "+shoot";
		public const string MOVE_FORWARD = "+forward";
		public const string MOVE_BACKWARD = "+backward";
		public const string MOVE_STRAFE_LEFT = "+strafe_left";
		public const string MOVE_STRAFE_RIGHT = "+strafe_right";

		public const string SERVER_CONNECT = "+connect";
		public const string SERVER_DISCONNECT = "+disconnect";
		public const string SERVER_RECONNECT = "+reconnect";
	}
}