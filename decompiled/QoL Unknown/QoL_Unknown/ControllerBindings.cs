using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace QoL_Unknown;

public static class ControllerBindings
{
	private static ManualLogSource Logger;

	private static Dictionary<string, string> bindings = new Dictionary<string, string>
	{
		{ "A", "jump" },
		{ "B", "itemuse" },
		{ "X", "autopick" },
		{ "Y", "throw" },
		{ "DpadUp", "toggleinventory" },
		{ "DpadDown", "woundview" },
		{ "DpadLeft", "craft" },
		{ "DpadRight", "switchhands" },
		{ "Start", "pause" },
		{ "Back", "console" },
		{ "RT", "attack" },
		{ "LT", "iteminteract" },
		{ "RB", "slowmouse" },
		{ "LB", "shift" },
		{ "L3", "ragdoll" }
	};

	private static readonly HashSet<string> validButtons = new HashSet<string>
	{
		"A", "B", "X", "Y", "DpadUp", "DpadDown", "DpadLeft", "DpadRight", "Start", "Back",
		"RT", "LT", "RB", "LB", "L3"
	};

	private static readonly HashSet<string> validActions = new HashSet<string>
	{
		"jump", "itemuse", "throw", "attack", "iteminteract", "toggleinventory", "woundview", "craft", "switchhands", "pause",
		"console", "autopick", "slowmouse", "shift", "ragdoll", "none"
	};

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		LoadBindings();
	}

	public static string GetBinding(string button)
	{
		if (bindings.TryGetValue(button, out var value))
		{
			return value;
		}
		return "none";
	}

	public static bool SetBinding(string button, string action)
	{
		if (!validButtons.Contains(button))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[ControllerBindings] Invalid button: " + button));
			}
			return false;
		}
		if (!validActions.Contains(action))
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogWarning((object)("[ControllerBindings] Invalid action: " + action));
			}
			return false;
		}
		bindings[button] = action;
		SaveBindings();
		return true;
	}

	public static void ResetBindings()
	{
		bindings = new Dictionary<string, string>
		{
			{ "A", "jump" },
			{ "B", "itemuse" },
			{ "X", "autopick" },
			{ "Y", "throw" },
			{ "DpadUp", "toggleinventory" },
			{ "DpadDown", "woundview" },
			{ "DpadLeft", "craft" },
			{ "DpadRight", "switchhands" },
			{ "Start", "pause" },
			{ "Back", "console" },
			{ "RT", "attack" },
			{ "LT", "iteminteract" },
			{ "RB", "slowmouse" },
			{ "LB", "shift" },
			{ "L3", "ragdoll" }
		};
		SaveBindings();
	}

	public static string ListBindings()
	{
		List<string> values = new List<string>
		{
			"Face Buttons:",
			"  A      - " + GetBinding("A"),
			"  B      - " + GetBinding("B"),
			"  X      - " + GetBinding("X"),
			"  Y      - " + GetBinding("Y"),
			"",
			"D-Pad:",
			"  Up     - " + GetBinding("DpadUp"),
			"  Down   - " + GetBinding("DpadDown"),
			"  Left   - " + GetBinding("DpadLeft"),
			"  Right  - " + GetBinding("DpadRight"),
			"",
			"Triggers:",
			"  RT     - " + GetBinding("RT"),
			"  LT     - " + GetBinding("LT"),
			"",
			"Bumpers:",
			"  RB     - " + GetBinding("RB"),
			"  LB     - " + GetBinding("LB"),
			"",
			"Stick Press:",
			"  L3     - " + GetBinding("L3"),
			"",
			"Special:",
			"  Start  - " + GetBinding("Start"),
			"  Back   - " + GetBinding("Back"),
			"",
			"Use: controllerbind <button> <action>"
		};
		return string.Join("\n", values);
	}

	public static string ListActions()
	{
		return "Valid actions: " + string.Join(",\n", validActions);
	}

	private static void SaveBindings()
	{
		try
		{
			foreach (KeyValuePair<string, string> binding in bindings)
			{
				PlayerPrefs.SetString("ControllerBind_" + binding.Key, binding.Value);
			}
			PlayerPrefs.Save();
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)("[ControllerBindings] Failed to save bindings: " + ex.Message));
			}
		}
	}

	private static void LoadBindings()
	{
		try
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(bindings);
			foreach (string validButton in validButtons)
			{
				string text = PlayerPrefs.GetString("ControllerBind_" + validButton, "");
				if (!string.IsNullOrEmpty(text) && validActions.Contains(text))
				{
					dictionary[validButton] = text;
				}
			}
			bindings = dictionary;
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)("[ControllerBindings] Failed to load bindings: " + ex.Message));
			}
		}
	}

	public static bool ShouldTrigger(string button, string action)
	{
		return GetBinding(button) == action;
	}

	internal static bool ShouldTrigger(ControllerButton button, string action)
	{
		return ShouldTrigger(ControllerButtonNames.ToBindingKey(button), action);
	}
}
