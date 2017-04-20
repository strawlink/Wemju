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
		static IngameConsole()
		{
//			RegisterMethod(Method.CFG_LOAD, StartLoadConfig);
//			RegisterMethod(Method.CFG_SAVE, StartSaveConfig);
//			RegisterMethod(Method.CFG_PATH, EchoConfigPath);
//			RegisterMethodWithParams(Method.SET, SetVariable);
//			RegisterMethodWithParams(Method.GET, GetVariable);

//			RegisterMethodWithParams(Method.BIND, BindKey);

			LoadAllMethods();
			StartLoadConfig();
			//LoadConfig();
		}

		private static void LoadAllMethods()
		{
			var type = typeof(IngameConsole);
			var assembly = Assembly.GetAssembly(type);
			var allTypes = assembly.GetTypes();
			var allMethods = allTypes.SelectMany(x => x.GetMethods()).Where(y => y.GetCustomAttributes(typeof(ConsoleMethod), false).Length > 0);

			foreach (var info in allMethods)
			{
				var attr = info.GetCustomAttributes(typeof(ConsoleMethod), false);

				//info.GetParameters()
				//_methodCollection.Add(, Delegate.CreateDelegate(typeof(Action<string[]>),info));
				var cName = ((ConsoleMethod) attr[0]).command;
				_newMethodCollection.Add(cName, info);
				//Debug.Log("Found method with name: " + cName);
			}

			//Debug.Log("AllMethod count: " + allMethods.Count());
		}

		private static Dictionary<string, MethodInfo> _newMethodCollection = new Dictionary<string, MethodInfo>();


		private const char SPLIT_INPUT_CHARACTER = ' ';
		public static void InvokeNewStyle(string input)
		{
			ConsoleHelper.AddLogToHistory(">" + input);
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
					if (!info.IsStatic)
					{
						Debug.LogError("Unable to invoke non-static method");
						return;
						//target= info.
					}

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

		public class Variable<T>
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
		}

		//private const string PATH_TO_CONFIG = "C:\\Data\\config.cfg";

		private static string PATH_TO_CONFIG
		{
			get { return Path.Combine(Environment.CurrentDirectory, "wemjuconfig.cfg"); }
		}

		private static bool _isDirty = false;

		private static Dictionary<string, Variable> _variableCollection = new Dictionary<string, Variable>
		{
			{Command.SETTINGS_NAME, new Variable("defaultName1")},
			{Command.SETTINGS_MAXFPS, new Variable(123)},
			{Command.SETTINGS_SENSITIVITY, new Variable(0.25f)},
		};

		public static Variable GetVar(string key)
		{
			Variable obj;
			if (_variableCollection.TryGetValue(key, out obj))
			{
				return obj;
			}

			return null;
		}
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

		public static void SetVar<T>(string name, T val)
		{
			_variableCollection[name].value = val;

			_isDirty = true;

			SaveConfig();
		}

//		public static void RegisterVar<T>(string name, T defaultValue, Action<object> onChange = null)
//		{
//			_variableCollection[name] = new Variable(defaultValue, onChange);
//		}

		public static IEnumerable<string> GetAllMethods()
		{
			return _newMethodCollection.Keys;
		}

		//private static Dictionary<string, Action<string[]>> _methodCollection = new Dictionary<string, Action<string[]>>();

//		public static void RegisterMethodWithParams(string name, Action<string[]> method)
//		{
//			_methodCollection[name] = method;
//		}
//		public static void RegisterMethod(string name, Action method)
//		{
//			_methodCollection[name] = (input) => method();
//		}

		[ConsoleMethod(Method.CFG_PATH)]
		public static void EchoConfigPath()
		{
			ConsoleHelper.AddLogToHistory(PATH_TO_CONFIG);
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
		public static void SaveConfig()
		{
			Debug.Log("SaveConfig");
			if (!_isDirty)
			{
				Debug.Log("Nothing was changed");
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

			Debug.Log("Saved");
		}

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
			Debug.Log("LoadConfig");
			if (File.Exists(PATH_TO_CONFIG))
			{
				var lines = File.ReadAllLines(PATH_TO_CONFIG);
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
					{KeyCode.BackQuote, Method.TOGGLE_CONSOLE_EXPANDED},
				};
			}

			_isDirty = true;
			StartSaveConfig();
		}

		public static void ExecuteCommand(string input)
		{
//			//Debug.Log("ExecuteCommand: " + input);
//			ConsoleHelper.AddLogToHistory(">" + input);
//			var commands = new List<string>(input.Split(' '));
//
//			if (commands.Count == 0)
//				return;
//
//			var key = commands[0];
//
//			Action<string[]> method;
//			if (_methodCollection.TryGetValue(key, out method))
//			{
//				var args = commands.Count > 1 ? commands.GetRange(1, commands.Count - 1).ToArray() : null;
//				method.SafeInvoke(args);
//			}
//			else
//			{
//				Debug.Log("Invalid input: " + key);
//			}




			/*
			var inputVal = commands.Length >= 2 ? commands[1] : null;

			if (commands.Length == 1)
			{
				Action<string[]> method;
				if (_methodCollection.TryGetValue(key, out method))
				{
					method.SafeInvoke(null);
				}
				else
				{
					Debug.Log("Invalid input: " + key);
				}
			}
			else if (commands.Length == 2)
			{
				Variable variable;
				Action<string[]> method;
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
				else if(_methodCollection.TryGetValue(key, out method))
				{
					string[] inputArr = commands.ToList().GetRange(1, commands.Length - 1).ToArray();
					method.SafeInvoke(inputArr);
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
			}*/
		}


		[ConsoleMethod(Method.BIND)]
		public static void BindKey(string key, string command)
		{
			try
			{
				var keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), key);
				if (keyCode != KeyCode.None && Enum.IsDefined(typeof(KeyCode), keyCode))
				{
					//Debug.Log("KeyCode is: " + keyCode);
					_keyBindings[keyCode] = command;
					_isDirty = true;
					//Debug.Log("Bound keycode: " + keyCode + " to command: " + command);

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

		public static void CheckInputPressed()
		{
			lock (_keyBindings)
			{
				foreach (var binding in _keyBindings)
				{
					if (Core.GetKeyDown(binding.Key))
					{
						InvokeNewStyle(binding.Value);
//						MethodInfo method;
//						if (_newMethodCollection.TryGetValue(binding.Value, out method))
//						{
//							method.Invoke(null, null);
//						}
					}
				}
			}
		}

		[ConsoleMethod(Method.GET)]
		public static void GetVariable(params string[] input)
		{
			if (input == null || input.Length != 1)
			{
				Debug.Log("Invalid input");
				return;
			}

			var variable = input[0];


//			Variable var;
//			if (_variableCollection.TryGetValue(variable, out var))
//			{
//				Debug.Log("Found variable: " + variable + " : " + var.value);
//			}
//			else
//			{
//				Debug.Log("Unable to find variable " + variable);
//			}
//			BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.NonPublic;
//			var info = typeof(Settings).GetField(variable, bindFlags);
//			if (info != null)
//			{
//				Debug.Log("Found variable: " + variable + " : " + info);
//				//var ctrl = info.GetValue(_) as Controller;
//			}
//			else
//			{
//				Debug.Log("Failed to find variable: " + variable);
//			}
		}

		[CVar("test_one", 1)]
		private static int _testOne = 20;

		[ConsoleMethod(Method.SET)]
		private static void SetVariable(string[] input)
		{
			if (input == null || input.Length != 2)
			{
				Debug.Log("Invalid input");
				return;
			}

			var variable = input[0];
			var value = input[1];

			Variable var;
			if (_variableCollection.TryGetValue(variable, out var))
			{
				var.value = value;
				Debug.Log("Set variable to " + value);
			}
			else
			{
				Debug.Log("Unable to find variable " + variable);
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

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class CVar : Attribute
	{
		public CVar(string command, object defaultValue)
		{

		}
		//...
	}



	public static class Command
	{
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