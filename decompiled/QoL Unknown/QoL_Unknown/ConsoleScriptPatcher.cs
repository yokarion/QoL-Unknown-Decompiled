using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace QoL_Unknown;

internal class ConsoleScriptPatcher
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Action _003C_003E9__8_0;

		public static Predicate<Command> _003C_003E9__8_1;

		public static Action _003C_003E9__8_2;

		public static Action _003C_003E9__8_3;

		public static Action _003C_003E9__8_4;

		public static Action _003C_003E9__8_5;

		internal void _003CRegisterAllCommands_Postfix_003Eb__8_0(string[] args)
		{
			if ((Object)(object)WorldGeneration.world == (Object)null)
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>No active world! :p</color>");
				return;
			}
			float totalLootRarity = WorldGeneration.world.totalLootRarity;
			float totalTrapRarity = WorldGeneration.world.totalTrapRarity;
			float lootRarityMultiplier = WorldGeneration.world.lootRarityMultiplier;
			float trapRarityMultiplier = WorldGeneration.world.trapRarityMultiplier;
			ConsoleHelper.LogToConsole(ConsoleScript.instance, $"<color=green>Loot: {totalLootRarity * 100f:0.#}% | Traps: {totalTrapRarity * 100f:0.#}%</color> <color=grey>(layer: {lootRarityMultiplier * 100f:0.#}%/{trapRarityMultiplier * 100f:0.#}%)</color>");
		}

		internal bool _003CRegisterAllCommands_Postfix_003Eb__8_1(Command cmd)
		{
			return cmd?.name.Equals("fucklore", StringComparison.OrdinalIgnoreCase) ?? false;
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__8_2(string[] args)
		{
			bool flag = PreRunScriptPatcher.ToggleAlwaysSkipLore();
			ConsoleHelper.LogToConsole(ConsoleScript.instance, flag ? "Always skipping the lore now..." : "Always showing the lore now.");
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__8_3(string[] args)
		{
			bool flag = QoLModSettings.ToggleExperimentalFeatures();
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "Experimental features " + (flag ? "enabled" : "disabled") + ". Reopen the settings menu to refresh experimental options.");
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__8_4(string[] args)
		{
			if (args.Length == 0)
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, ControllerBindings.ListBindings());
			}
			else if (args.Length == 1)
			{
				if (args[0].Equals("reset", StringComparison.OrdinalIgnoreCase))
				{
					ControllerBindings.ResetBindings();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Controller bindings reset to defaults</color>");
				}
				else if (args[0].Equals("actions", StringComparison.OrdinalIgnoreCase))
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, ControllerBindings.ListActions());
				}
				else if (args[0].Equals("info", StringComparison.OrdinalIgnoreCase))
				{
					if ((Object)(object)ControllerInputManager.Instance == (Object)null)
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Controller manager not initialized</color>");
					}
					else
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>" + ControllerInputManager.Instance.GetDebugInfo() + "</color>");
					}
				}
				else
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Usage: controllerbind <button> <action> OR controllerbind reset/actions/info</color>");
				}
			}
			else if (args.Length == 2)
			{
				string text = args[0];
				string text2 = args[1];
				if (ControllerBindings.SetBinding(text, text2))
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Bound " + text + " to " + text2 + "</color>");
				}
				else
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=red>Failed to bind " + text + " to " + text2 + "</color>");
					ConsoleHelper.LogToConsole(ConsoleScript.instance, ControllerBindings.ListActions());
				}
			}
			else
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Too many arguments</color>");
			}
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__8_5(string[] args)
		{
			if (args.Length < 2)
			{
				string text = PlayerPrefs.GetString("RunAtStart", "");
				ConsoleHelper.LogToConsole(ConsoleScript.instance, string.IsNullOrEmpty(text) ? "No start commands set." : ("Current start commands: " + text));
				return;
			}
			string text2 = string.Join(" ", args.Skip(1));
			if (text2.Equals("clear", StringComparison.OrdinalIgnoreCase))
			{
				PlayerPrefs.DeleteKey("RunAtStart");
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "Cleared start commands!");
			}
			else
			{
				PlayerPrefs.SetString("RunAtStart", text2);
				PlayerPrefs.Save();
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "Saved start commands: " + text2);
			}
		}
	}

	private static int selectedAutofillIndex = 0;

	private static int lastSuggestionCount = 0;

	private static string lastInputBuffer = "";

	private const int MaxVisibleSuggestions = 20;

	internal static int SelectedAutofillIndex => selectedAutofillIndex;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Harmony val = new Harmony("org.bepinex.plugins.qol.consolepatch");
		val.PatchAll(typeof(ConsoleScriptPatcher));
		logger.LogInfo((object)"ConsoleScriptPatcher initialized");
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterSpawnEntities")]
	[HarmonyPrefix]
	private static bool RegisterSpawnEntities_Prefix()
	{
		Command val = ConsoleScript.SearchExact("spawn");
		if (val != null && val.argAutofill != null && val.argAutofill.ContainsKey(0))
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterAllCommands_Postfix()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected O, but got Unknown
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Expected O, but got Unknown
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Expected O, but got Unknown
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Expected O, but got Unknown
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Expected O, but got Unknown
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Expected O, but got Unknown
		if (ConsoleScript.SearchExact("worldrates") == null)
		{
			List<Command> commands = ConsoleScript.Commands;
			object obj = _003C_003Ec._003C_003E9__8_0;
			if (obj == null)
			{
				Action val = delegate
				{
					if ((Object)(object)WorldGeneration.world == (Object)null)
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>No active world! :p</color>");
					}
					else
					{
						float totalLootRarity = WorldGeneration.world.totalLootRarity;
						float totalTrapRarity = WorldGeneration.world.totalTrapRarity;
						float lootRarityMultiplier = WorldGeneration.world.lootRarityMultiplier;
						float trapRarityMultiplier = WorldGeneration.world.trapRarityMultiplier;
						ConsoleHelper.LogToConsole(ConsoleScript.instance, $"<color=green>Loot: {totalLootRarity * 100f:0.#}% | Traps: {totalTrapRarity * 100f:0.#}%</color> <color=grey>(layer: {lootRarityMultiplier * 100f:0.#}%/{trapRarityMultiplier * 100f:0.#}%)</color>");
					}
				};
				_003C_003Ec._003C_003E9__8_0 = val;
				obj = (object)val;
			}
			commands.Add(new Command("worldrates", "Shows current loot% and traps% multipliers.", (Action)obj, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
		}
		if (ConsoleScript.SearchExact("fucklore") != null)
		{
			ConsoleScript.Commands.RemoveAll((Command cmd) => cmd?.name.Equals("fucklore", StringComparison.OrdinalIgnoreCase) ?? false);
		}
		if (ConsoleScript.SearchExact("fucklore") == null)
		{
			List<Command> commands2 = ConsoleScript.Commands;
			object obj2 = _003C_003Ec._003C_003E9__8_2;
			if (obj2 == null)
			{
				Action val2 = delegate
				{
					bool flag = PreRunScriptPatcher.ToggleAlwaysSkipLore();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, flag ? "Always skipping the lore now..." : "Always showing the lore now.");
				};
				_003C_003Ec._003C_003E9__8_2 = val2;
				obj2 = (object)val2;
			}
			commands2.Add(new Command("fucklore", "Toggles always skipping the fullscreen intro lore.", (Action)obj2, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
		}
		if (ConsoleScript.SearchExact("toggleExperimentalFeatures") == null)
		{
			List<Command> commands3 = ConsoleScript.Commands;
			object obj3 = _003C_003Ec._003C_003E9__8_3;
			if (obj3 == null)
			{
				Action val3 = delegate
				{
					bool flag = QoLModSettings.ToggleExperimentalFeatures();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Experimental features " + (flag ? "enabled" : "disabled") + ". Reopen the settings menu to refresh experimental options.");
				};
				_003C_003Ec._003C_003E9__8_3 = val3;
				obj3 = (object)val3;
			}
			commands3.Add(new Command("toggleExperimentalFeatures", "Toggles experimental QoL features.", (Action)obj3, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
		}
		if (ConsoleScript.SearchExact("controllerbind") != null)
		{
			return;
		}
		List<string> value = new List<string>
		{
			"A", "B", "X", "Y", "DpadUp", "DpadDown", "DpadLeft", "DpadRight", "Start", "Back",
			"RT", "LT", "reset", "actions", "info"
		};
		List<string> value2 = new List<string>
		{
			"jump", "itemuse", "throw", "attack", "iteminteract", "toggleinventory", "woundview", "craft", "switchhands", "pause",
			"console", "autopick", "none"
		};
		Dictionary<int, List<string>> dictionary = new Dictionary<int, List<string>>
		{
			{ 0, value },
			{ 1, value2 }
		};
		List<Command> commands4 = ConsoleScript.Commands;
		object obj4 = _003C_003Ec._003C_003E9__8_4;
		if (obj4 == null)
		{
			Action val4 = delegate(string[] args)
			{
				if (args.Length == 0)
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, ControllerBindings.ListBindings());
				}
				else if (args.Length == 1)
				{
					if (args[0].Equals("reset", StringComparison.OrdinalIgnoreCase))
					{
						ControllerBindings.ResetBindings();
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Controller bindings reset to defaults</color>");
					}
					else if (args[0].Equals("actions", StringComparison.OrdinalIgnoreCase))
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, ControllerBindings.ListActions());
					}
					else if (args[0].Equals("info", StringComparison.OrdinalIgnoreCase))
					{
						if ((Object)(object)ControllerInputManager.Instance == (Object)null)
						{
							ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Controller manager not initialized</color>");
						}
						else
						{
							ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>" + ControllerInputManager.Instance.GetDebugInfo() + "</color>");
						}
					}
					else
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Usage: controllerbind <button> <action> OR controllerbind reset/actions/info</color>");
					}
				}
				else if (args.Length == 2)
				{
					string text = args[0];
					string text2 = args[1];
					if (ControllerBindings.SetBinding(text, text2))
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Bound " + text + " to " + text2 + "</color>");
					}
					else
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=red>Failed to bind " + text + " to " + text2 + "</color>");
						ConsoleHelper.LogToConsole(ConsoleScript.instance, ControllerBindings.ListActions());
					}
				}
				else
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Too many arguments</color>");
				}
			};
			_003C_003Ec._003C_003E9__8_4 = val4;
			obj4 = (object)val4;
		}
		commands4.Add(new Command("controllerbind", "Set or view controller button bindings", (Action)obj4, dictionary, new(string, string)[2]
		{
			("button", "Button name (A, B, X, Y, DpadUp, etc.)"),
			("action", "Action to bind (jump, attack, craft, etc.)")
		}));
		if (ConsoleScript.SearchExact("runatstart") != null)
		{
			return;
		}
		List<Command> commands5 = ConsoleScript.Commands;
		object obj5 = _003C_003Ec._003C_003E9__8_5;
		if (obj5 == null)
		{
			Action val5 = delegate(string[] args)
			{
				if (args.Length < 2)
				{
					string text = PlayerPrefs.GetString("RunAtStart", "");
					ConsoleHelper.LogToConsole(ConsoleScript.instance, string.IsNullOrEmpty(text) ? "No start commands set." : ("Current start commands: " + text));
				}
				else
				{
					string text2 = string.Join(" ", args.Skip(1));
					if (text2.Equals("clear", StringComparison.OrdinalIgnoreCase))
					{
						PlayerPrefs.DeleteKey("RunAtStart");
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "Cleared start commands!");
					}
					else
					{
						PlayerPrefs.SetString("RunAtStart", text2);
						PlayerPrefs.Save();
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "Saved start commands: " + text2);
					}
				}
			};
			_003C_003Ec._003C_003E9__8_5 = val5;
			obj5 = (object)val5;
		}
		commands5.Add(new Command("runatstart", "Sets commands to execute on a fresh run (split by ;). Usage: runatstart [commands] OR runatstart clear", (Action)obj5, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
	}

	[HarmonyPatch(typeof(ConsoleScript), "TryExecuteCommand")]
	[HarmonyPrefix]
	private static bool TryExecuteCommand_Prefix(ref string[] args, ConsoleScript __instance)
	{
		if (TryForwardClientHealCommand(ref args))
		{
			return false;
		}
		if (TryForwardClientSpawnCommand(ref args))
		{
			return false;
		}
		if (args != null && args.Length == 1 && args[0].Equals("explode", StringComparison.OrdinalIgnoreCase))
		{
			args = new string[2] { "explode", "cursor" };
			return true;
		}
		return true;
	}

	private static bool TryForwardClientHealCommand(ref string[] args)
	{
		if (args == null || args.Length == 0 || !args[0].Equals("heal", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!KrokoshaMpCompat.IsNetworkRunning || !KrokoshaMpCompat.IsClient || KrokoshaMpCompat.IsServer)
		{
			return false;
		}
		if (!KrokMpCommandUtil.AreClientCheatCommandsAllowed())
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Host has sv_cheats or client commands disabled.</color>");
			args = Array.Empty<string>();
			return true;
		}
		if (!KrokMpCommandUtil.SendClientHealRequest())
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Failed to send heal request to host.</color>");
		}
		else
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=grey>Forwarded heal request to host.</color>");
		}
		args = Array.Empty<string>();
		return true;
	}

	private static bool TryForwardClientSpawnCommand(ref string[] args)
	{
		if (args == null || args.Length < 2 || !args[0].Equals("spawn", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!KrokoshaMpCompat.IsNetworkRunning || !KrokoshaMpCompat.IsClient || KrokoshaMpCompat.IsServer)
		{
			return false;
		}
		if (!TryBuildClientSpawnCommand(args, out var commandString, out var errorMessage))
		{
			if (!string.IsNullOrEmpty(errorMessage))
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>" + errorMessage + "</color>");
			}
			args = Array.Empty<string>();
			return true;
		}
		if (!KrokMpCommandUtil.SendClientSpawnRequest(commandString))
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Failed to send spawn request to host.</color>");
		}
		else
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=grey>Forwarded spawn request to host.</color>");
		}
		args = Array.Empty<string>();
		return true;
	}

	private static bool TryBuildClientSpawnCommand(string[] args, out string commandString, out string errorMessage)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		commandString = null;
		errorMessage = null;
		string item = args[1];
		string positionArg = ((args.Length > 2) ? args[2] : "cursor");
		if (!TryResolveClientSpawnPosition(positionArg, out var position, out errorMessage))
		{
			return false;
		}
		List<string> list = new List<string>
		{
			"spawn",
			item,
			FormatVector2(position)
		};
		if (args.Length > 3)
		{
			list.Add(args[3]);
		}
		if (args.Length > 4)
		{
			list.Add(args[4]);
		}
		commandString = string.Join(" ", list);
		return true;
	}

	private static bool TryResolveClientSpawnPosition(string positionArg, out Vector2 position, out string errorMessage)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		position = Vector2.zero;
		errorMessage = null;
		switch (positionArg.ToLowerInvariant())
		{
		case "cursor":
			if ((Object)(object)Camera.main == (Object)null)
			{
				errorMessage = "Cannot resolve cursor position right now.";
				return false;
			}
			position = Vector2.op_Implicit(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			return true;
		case "player":
			if ((Object)(object)PlayerCamera.main?.body == (Object)null)
			{
				errorMessage = "Cannot resolve player position right now.";
				return false;
			}
			position = Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position);
			return true;
		case "random":
			errorMessage = "Client-side MP spawn forwarding does not support random positions.";
			return false;
		default:
		{
			string[] array = positionArg.Split(new char[1] { ',' });
			if (array.Length != 2 || !float.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
			{
				errorMessage = "\"" + positionArg + "\" is not a valid position for spawn forwarding.";
				return false;
			}
			position = new Vector2(result, result2);
			return true;
		}
		}
	}

	private static string FormatVector2(Vector2 position)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return string.Format(CultureInfo.InvariantCulture, "{0},{1}", position.x, position.y);
	}

	[HarmonyPatch(typeof(ConsoleScript), "TryExecuteCommand")]
	[HarmonyPostfix]
	private static void TryExecuteCommand_Postfix(string[] args)
	{
		if (args == null || args.Length == 0 || !args[0].Equals("heal", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Body val = PlayerCamera.main?.body;
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		MindwipeScript[] components = ((Component)val).GetComponents<MindwipeScript>();
		for (int i = 0; i < components.Length; i++)
		{
			Object.Destroy((Object)(object)components[i]);
		}
		val.mindWipe = null;
		Limb[] limbs = val.limbs;
		foreach (Limb val2 in limbs)
		{
			if (!((Object)(object)val2 == (Object)null))
			{
				val2.dismembered = false;
				if (!((Component)val2).gameObject.activeSelf)
				{
					((Component)val2).gameObject.SetActive(true);
				}
			}
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "Update")]
	[HarmonyPrefix]
	private static bool Update_Prefix(ConsoleScript __instance)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (!__instance.active)
		{
			return true;
		}
		if (Input.GetKeyDown((KeyCode)306) && Input.GetKeyDown((KeyCode)308))
		{
			Vector3 mousePosition = Input.mousePosition;
			ConsoleHelper.LogToConsole(ConsoleScript.instance, $"X: {mousePosition.x} px from the left, Y : {mousePosition.y} px from the top");
		}
		if (__instance.input.text != lastInputBuffer)
		{
			selectedAutofillIndex = 0;
			lastInputBuffer = __instance.input.text;
		}
		if (!Input.GetKey((KeyCode)304) && !Input.GetKey((KeyCode)303) && __instance.descriptionBack.activeSelf && lastSuggestionCount > 0)
		{
			if (Input.GetKeyDown((KeyCode)273))
			{
				selectedAutofillIndex--;
				if (selectedAutofillIndex < 0)
				{
					selectedAutofillIndex = lastSuggestionCount - 1;
				}
				__instance.HandleDescriptionText(__instance.input.text.Split(new char[1] { ' ' }));
				return false;
			}
			if (Input.GetKeyDown((KeyCode)274))
			{
				selectedAutofillIndex++;
				if (selectedAutofillIndex >= lastSuggestionCount)
				{
					selectedAutofillIndex = 0;
				}
				__instance.HandleDescriptionText(__instance.input.text.Split(new char[1] { ' ' }));
				return false;
			}
		}
		return true;
	}

	[HarmonyPatch(typeof(ConsoleScript), "TryFinishCommandPart")]
	[HarmonyPrefix]
	private static bool TryFinishCommandPart_Prefix(ConsoleScript __instance, string[] args)
	{
		if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
		{
			return true;
		}
		if (args.Length == 1)
		{
			List<Command> list = ConsoleScript.Search(args[0]);
			if (list.Count > 0)
			{
				int index = Mathf.Clamp(selectedAutofillIndex, 0, list.Count - 1);
				__instance.input.text = list[index].name + " ";
				__instance.SetCaretToEnd();
				return false;
			}
		}
		else
		{
			Command val = ConsoleScript.SearchExact(args[0]);
			if (val != null && val.argAutofill != null && val.argAutofill.TryGetValue(args.Length - 2, out var value))
			{
				List<string> list2 = ConsoleScript.SearchArgumentAutofill(args[^1], value);
				if (list2.Count > 0)
				{
					int index2 = Mathf.Clamp(selectedAutofillIndex, 0, list2.Count - 1);
					__instance.input.text = string.Join(" ", args.Take(args.Length - 1).ToArray()) + " " + list2[index2] + " ";
					__instance.SetCaretToEnd();
					return false;
				}
			}
		}
		return true;
	}

	[HarmonyPatch(typeof(ConsoleScript), "HandleDescriptionText")]
	[HarmonyPrefix]
	private static bool HandleDescriptionText_Prefix(ConsoleScript __instance, string[] args)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		((TMP_Text)__instance.descriptionText).text = "";
		lastSuggestionCount = 0;
		if (__instance.input.caretPosition >= 0 && __instance.input.caretPosition < __instance.input.textComponent.textInfo.characterCount)
		{
			Vector3 bottomLeft = __instance.input.textComponent.textInfo.characterInfo[__instance.input.caretPosition].bottomLeft;
			Vector3 val = ((Component)__instance.input).transform.TransformPoint(bottomLeft);
			((TMP_Text)__instance.descriptionText).transform.position = val + Vector3.down * 35f;
		}
		if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
		{
			return false;
		}
		string text = "";
		if (args.Length == 1)
		{
			List<Command> list = ConsoleScript.Search(args[0]);
			lastSuggestionCount = list.Count;
			if (list.Count == 0)
			{
				((TMP_Text)__instance.descriptionText).text = "<color=orange>Invalid command \"" + args[0] + "\"";
				return false;
			}
			selectedAutofillIndex = Mathf.Clamp(selectedAutofillIndex, 0, list.Count - 1);
			int num = 0;
			if (list.Count > 20)
			{
				num = selectedAutofillIndex - 10;
				if (num < 0)
				{
					num = 0;
				}
				int num2 = list.Count - 20;
				if (num > num2)
				{
					num = num2;
				}
			}
			int num3 = Mathf.Min(list.Count, num + 20);
			if (num > 0)
			{
				text = text + "<color=grey>... (" + num + " more)</color>\n";
			}
			for (int i = num; i < num3; i++)
			{
				bool flag = i == selectedAutofillIndex;
				if (flag)
				{
					text += "<color=yellow>";
				}
				text = text + list[i].GetShortDescription(flag) + "\n";
				if (flag)
				{
					text += "</color>";
				}
			}
			if (num3 < list.Count)
			{
				text = text + "<color=grey>... (" + (list.Count - num3) + " more)</color>\n";
			}
		}
		else
		{
			Command val2 = ConsoleScript.SearchExact(args[0]);
			if (val2 == null)
			{
				((TMP_Text)__instance.descriptionText).text = "<color=orange>Invalid command \"" + args[0] + "\"";
				return false;
			}
			text = val2.name + " ";
			int num4 = args.Length - 2;
			for (int j = 0; j < val2.argDescription.Length; j++)
			{
				text = ((j != num4) ? (text + "[" + val2.argDescription[j].Item1 + "] ") : (text + "<color=yellow>[" + val2.argDescription[j].Item1 + " - " + val2.argDescription[j].Item2 + "] </color>"));
			}
			if (val2.argAutofill != null && val2.argAutofill.TryGetValue(num4, out var value))
			{
				List<string> list2 = ConsoleScript.SearchArgumentAutofill(args[^1], value);
				lastSuggestionCount = list2.Count;
				text += "\n";
				if (list2.Count > 0)
				{
					selectedAutofillIndex = Mathf.Clamp(selectedAutofillIndex, 0, list2.Count - 1);
				}
				int num5 = 0;
				if (list2.Count > 20)
				{
					num5 = selectedAutofillIndex - 10;
					if (num5 < 0)
					{
						num5 = 0;
					}
					int num6 = list2.Count - 20;
					if (num5 > num6)
					{
						num5 = num6;
					}
				}
				int num7 = Mathf.Min(list2.Count, num5 + 20);
				if (num5 > 0)
				{
					text = text + "<color=grey>... (" + num5 + " more)</color>\n";
				}
				for (int k = num5; k < num7; k++)
				{
					bool flag2 = k == selectedAutofillIndex;
					if (flag2)
					{
						text += "<color=yellow>";
					}
					text = text + list2[k] + "\n";
					if (flag2)
					{
						text += "</color>";
					}
				}
				if (num7 < list2.Count)
				{
					text = text + "<color=grey>... (" + (list2.Count - num7) + " more)</color>\n";
				}
			}
		}
		((TMP_Text)__instance.descriptionText).text = text;
		return false;
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	private static void FinishWorldGeneration_RunStartCommands()
	{
		if (!SaveSystem.loadedRun && (Object)(object)ConsoleScript.instance != (Object)null && PlayerPrefs.HasKey("RunAtStart"))
		{
			string text = PlayerPrefs.GetString("RunAtStart");
			if (!string.IsNullOrEmpty(text))
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "Running your startup commands!...");
				ConsoleHelper.RunCommandString(ConsoleScript.instance, text);
			}
		}
	}
}
