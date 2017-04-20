using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Core
{
	static Core()
	{
		ConsoleHelper.ConsoleEnabledChanged += OnConsoleEnabledChanged;
	}

	private static void OnConsoleEnabledChanged(bool state)
	{
		_isConsoleActive = state;
	}

	private static bool _isConsoleActive = false;


	public static bool GetKey(string name)
	{
		return !_isConsoleActive && Input.GetKey(name);
	}

	public static bool GetKey(KeyCode key)
	{
		return !_isConsoleActive && Input.GetKey(key);
	}

	public static bool GetKeyDown(string name)
	{
		return !_isConsoleActive && Input.GetKeyDown(name);
	}

	public static bool GetKeyDown(KeyCode key)
	{
		return !_isConsoleActive && Input.GetKeyDown(key);
	}

	public static bool GetKeyUp(string name)
	{
		return !_isConsoleActive && Input.GetKey(name);
	}

	public static bool GetKeyUp(KeyCode key)
	{
		return !_isConsoleActive && Input.GetKey(key);
	}
}