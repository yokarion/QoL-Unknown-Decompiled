using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class ConsoleScriptPatcher
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static UnityAction<Scene, LoadSceneMode> _003C_003E9__14_0;

		public static Action _003C_003E9__16_0;

		public static Predicate<Command> _003C_003E9__16_1;

		public static Action _003C_003E9__16_2;

		public static Action _003C_003E9__16_3;

		public static Action _003C_003E9__16_4;

		public static Predicate<Command> _003C_003E9__16_5;

		public static Action _003C_003E9__16_6;

		public static Action _003C_003E9__16_7;

		public static Action _003C_003E9__16_8;

		internal void _003CInitialize_003Eb__14_0(Scene scene, LoadSceneMode loadMode)
		{
			locateAutofillDirty = true;
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__16_0(string[] args)
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

		internal bool _003CRegisterAllCommands_Postfix_003Eb__16_1(Command cmd)
		{
			return cmd?.name.Equals("fucklore", StringComparison.OrdinalIgnoreCase) ?? false;
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__16_2(string[] args)
		{
			bool flag = PreRunScriptPatcher.ToggleAlwaysSkipLore();
			ConsoleHelper.LogToConsole(ConsoleScript.instance, flag ? "Always skipping the lore now..." : "Always showing the lore now.");
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__16_3(string[] args)
		{
			bool flag = QoLModSettings.ToggleExperimentalFeatures();
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "Experimental features " + (flag ? "enabled" : "disabled") + ". Reopen the settings menu to refresh experimental options.");
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__16_4(string[] args)
		{
			if (args == null || args.Length < 2)
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Usage: nexus &lt;name or guid&gt;</color>");
				return;
			}
			string text = string.Join(" ", args.Skip(1)).Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Usage: nexus &lt;name or guid&gt;</color>");
				return;
			}
			if (!ModManagementSettings.TryGetNexusPageUrl(text, out var nexusPageUrl))
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>No installed mod matches \"" + text + "\". Try the mod name or its BepInEx plugin GUID.</color>");
				return;
			}
			Application.OpenURL(nexusPageUrl);
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Opened Nexus page: " + nexusPageUrl + "</color>");
		}

		internal bool _003CRegisterAllCommands_Postfix_003Eb__16_5(Command cmd)
		{
			return cmd?.name.Equals("bind", StringComparison.OrdinalIgnoreCase) ?? false;
		}

		internal unsafe void _003CRegisterAllCommands_Postfix_003Eb__16_6(string[] args)
		{
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
			if (args.Length <= 1)
			{
				throw new Exception(string.Format("Expected at least {0} argument{1}, but got {2}.", 1, "s", args.Length - 1));
			}
			switch (args[1])
			{
			case "add":
			{
				if (args.Length <= 3)
				{
					throw new Exception(string.Format("Expected at least {0} argument{1}, but got {2}.", 3, "s", args.Length - 1));
				}
				if (!Enum.TryParse<KeyCode>(args[2], out KeyCode result2))
				{
					throw new Exception("\"" + args[2] + "\" is not a valid keycode!");
				}
				KeyValuePair<KeyCode, string> keyValuePair = new KeyValuePair<KeyCode, string>(result2, args[3]);
				ConsoleScript.instance.customBinds.Add(keyValuePair.Key, keyValuePair.Value);
				ConsoleScript.instance.settings.customBinds.Add(((object)keyValuePair.Key/*cast due to constrained. prefix*/).ToString(), keyValuePair.Value);
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "Bound \"" + args[2] + "\" to \"" + args[3] + "\".");
				WriteConsoleSettings();
				break;
			}
			case "remove":
			{
				if (args.Length <= 2)
				{
					throw new Exception(string.Format("Expected at least {0} argument{1}, but got {2}.", 2, "s", args.Length - 1));
				}
				if (!Enum.TryParse<KeyCode>(args[2], out KeyCode result))
				{
					throw new Exception("\"" + args[2] + "\" is not a valid keycode!");
				}
				if (ConsoleScript.instance.customBinds.Remove(result))
				{
					ConsoleScript.instance.settings.customBinds.Remove(((object)(*(KeyCode*)(&result))/*cast due to constrained. prefix*/).ToString());
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Removed bind for \"" + args[2] + "\".");
					WriteConsoleSettings();
				}
				else
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>No bind found for \"" + args[2] + "\".</color>");
				}
				break;
			}
			case "clear":
				ConsoleScript.instance.customBinds.Clear();
				ConsoleScript.instance.settings.customBinds.Clear();
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "Cleared all custom binds.");
				WriteConsoleSettings();
				break;
			case "list":
			{
				if (ConsoleScript.instance.customBinds.Count == 0)
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "No command binds active.");
					break;
				}
				List<string> list = new List<string>();
				foreach (KeyValuePair<KeyCode, string> customBind in ConsoleScript.instance.customBinds)
				{
					list.Add($"\"{customBind.Key}\" bound to \"{customBind.Value}\"");
				}
				ConsoleHelper.LogToConsole(ConsoleScript.instance, string.Join("\n", list));
				break;
			}
			default:
				throw new Exception("\"" + args[1] + "\" is not a valid bind action!");
			}
		}

		internal void _003CRegisterAllCommands_Postfix_003Eb__16_7(string[] args)
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

		internal void _003CRegisterAllCommands_Postfix_003Eb__16_8(string[] args)
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

	private const float LocateAutofillRefreshSeconds = 1f;

	private static readonly HashSet<string> locateAutofillSeen = new HashSet<string>();

	private static readonly List<Transform> locateTransformStack = new List<Transform>();

	private static float locateAutofillLastRefresh = float.MinValue;

	private static bool locateAutofillDirty = true;

	private const string LayerAutofillSeparator = " - ";

	private const string LayerIndexCommandName = "skiplayer";

	private static readonly HashSet<string> LocateBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"algaeparticle", "anim", "arrowguide", "audio", "back", "blocker1", "blocker2", "chunk", "chunkback", "chutegrabpoint",
		"circle", "copygrid", "dirtwaterparticle", "doorspawn", "maindoor", "experiment", "fog", "floor", "fireparticle", "gameobject",
		"grid", "laser", "lineofsight", "magmaparticle", "tutorialspawn", "processvolume", "testtilemap", "securityguardbody", "securityguardarms", "rockbackground",
		"sandback", "tileback", "square", "head", "uptorso", "downtorso", "uparmf", "downarmf", "handf", "uparmb",
		"downarmb", "handb", "thighf", "crusf", "footf", "thighb", "crusb", "footb", "arml", "armr",
		"footl", "footr", "thighl", "thighr", "leg1", "torso", "ambientlight", "blindlight", "imbouttablow", "liquidfill",
		"rain", "rotpoint", "scarfline", "world", "eventsystem", "lighting", "sprite", "shuttlestarter", "musicmanager", "light",
		"criticalheartbeat", "criticalheartbeatunconcious"
	};

	internal static int SelectedAutofillIndex => selectedAutofillIndex;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Harmony val = new Harmony("org.bepinex.plugins.qol.consolepatch");
		val.PatchAll(typeof(ConsoleScriptPatcher));
		SceneManager.sceneLoaded += delegate
		{
			locateAutofillDirty = true;
		};
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
	private unsafe static void RegisterAllCommands_Postfix()
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
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Expected O, but got Unknown
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Expected O, but got Unknown
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Expected O, but got Unknown
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Expected O, but got Unknown
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Expected O, but got Unknown
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Expected O, but got Unknown
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Expected O, but got Unknown
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Expected O, but got Unknown
		if (ConsoleScript.SearchExact("worldrates") == null)
		{
			List<Command> commands = ConsoleScript.Commands;
			object obj = _003C_003Ec._003C_003E9__16_0;
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
				_003C_003Ec._003C_003E9__16_0 = val;
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
			object obj2 = _003C_003Ec._003C_003E9__16_2;
			if (obj2 == null)
			{
				Action val2 = delegate
				{
					bool flag = PreRunScriptPatcher.ToggleAlwaysSkipLore();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, flag ? "Always skipping the lore now..." : "Always showing the lore now.");
				};
				_003C_003Ec._003C_003E9__16_2 = val2;
				obj2 = (object)val2;
			}
			commands2.Add(new Command("fucklore", "Toggles always skipping the fullscreen intro lore.", (Action)obj2, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
		}
		if (ConsoleScript.SearchExact("toggleExperimentalFeatures") == null)
		{
			List<Command> commands3 = ConsoleScript.Commands;
			object obj3 = _003C_003Ec._003C_003E9__16_3;
			if (obj3 == null)
			{
				Action val3 = delegate
				{
					bool flag = QoLModSettings.ToggleExperimentalFeatures();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Experimental features " + (flag ? "enabled" : "disabled") + ". Reopen the settings menu to refresh experimental options.");
				};
				_003C_003Ec._003C_003E9__16_3 = val3;
				obj3 = (object)val3;
			}
			commands3.Add(new Command("toggleExperimentalFeatures", "Toggles experimental QoL features.", (Action)obj3, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
		}
		if (ConsoleScript.SearchExact("nexus") == null)
		{
			List<Command> commands4 = ConsoleScript.Commands;
			object obj4 = _003C_003Ec._003C_003E9__16_4;
			if (obj4 == null)
			{
				Action val4 = delegate(string[] args)
				{
					if (args == null || args.Length < 2)
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Usage: nexus &lt;name or guid&gt;</color>");
					}
					else
					{
						string text = string.Join(" ", args.Skip(1)).Trim();
						string nexusPageUrl;
						if (string.IsNullOrWhiteSpace(text))
						{
							ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Usage: nexus &lt;name or guid&gt;</color>");
						}
						else if (!ModManagementSettings.TryGetNexusPageUrl(text, out nexusPageUrl))
						{
							ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>No installed mod matches \"" + text + "\". Try the mod name or its BepInEx plugin GUID.</color>");
						}
						else
						{
							Application.OpenURL(nexusPageUrl);
							ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Opened Nexus page: " + nexusPageUrl + "</color>");
						}
					}
				};
				_003C_003Ec._003C_003E9__16_4 = val4;
				obj4 = (object)val4;
			}
			commands4.Add(new Command("nexus", "Open the Nexus Mods page for an installed mod. Usage: nexus <name or guid>", (Action)obj4, BuildNexusCommandAutofill(), new(string, string)[1] { ("name or guid", "Installed mod name or BepInEx plugin GUID") }));
		}
		ConsoleScript.Commands.RemoveAll((Command cmd) => cmd?.name.Equals("bind", StringComparison.OrdinalIgnoreCase) ?? false);
		List<Command> commands5 = ConsoleScript.Commands;
		object obj5 = _003C_003Ec._003C_003E9__16_6;
		if (obj5 == null)
		{
			Action val5 = delegate(string[] args)
			{
				//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
				//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
				//IL_0115: Unknown result type (might be due to invalid IL or missing references)
				//IL_011a: Unknown result type (might be due to invalid IL or missing references)
				//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
				//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
				if (args.Length <= 1)
				{
					throw new Exception(string.Format("Expected at least {0} argument{1}, but got {2}.", 1, "s", args.Length - 1));
				}
				switch (args[1])
				{
				case "add":
				{
					if (args.Length <= 3)
					{
						throw new Exception(string.Format("Expected at least {0} argument{1}, but got {2}.", 3, "s", args.Length - 1));
					}
					if (!Enum.TryParse<KeyCode>(args[2], out KeyCode result2))
					{
						throw new Exception("\"" + args[2] + "\" is not a valid keycode!");
					}
					KeyValuePair<KeyCode, string> keyValuePair = new KeyValuePair<KeyCode, string>(result2, args[3]);
					ConsoleScript.instance.customBinds.Add(keyValuePair.Key, keyValuePair.Value);
					ConsoleScript.instance.settings.customBinds.Add(((object)keyValuePair.Key/*cast due to constrained. prefix*/).ToString(), keyValuePair.Value);
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Bound \"" + args[2] + "\" to \"" + args[3] + "\".");
					WriteConsoleSettings();
					break;
				}
				case "remove":
				{
					if (args.Length <= 2)
					{
						throw new Exception(string.Format("Expected at least {0} argument{1}, but got {2}.", 2, "s", args.Length - 1));
					}
					if (!Enum.TryParse<KeyCode>(args[2], out KeyCode result))
					{
						throw new Exception("\"" + args[2] + "\" is not a valid keycode!");
					}
					if (ConsoleScript.instance.customBinds.Remove(result))
					{
						ConsoleScript.instance.settings.customBinds.Remove(((object)(*(KeyCode*)(&result))/*cast due to constrained. prefix*/).ToString());
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "Removed bind for \"" + args[2] + "\".");
						WriteConsoleSettings();
					}
					else
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>No bind found for \"" + args[2] + "\".</color>");
					}
					break;
				}
				case "clear":
					ConsoleScript.instance.customBinds.Clear();
					ConsoleScript.instance.settings.customBinds.Clear();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Cleared all custom binds.");
					WriteConsoleSettings();
					break;
				case "list":
					if (ConsoleScript.instance.customBinds.Count == 0)
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "No command binds active.");
					}
					else
					{
						List<string> list = new List<string>();
						foreach (KeyValuePair<KeyCode, string> customBind in ConsoleScript.instance.customBinds)
						{
							list.Add($"\"{customBind.Key}\" bound to \"{customBind.Value}\"");
						}
						ConsoleHelper.LogToConsole(ConsoleScript.instance, string.Join("\n", list));
					}
					break;
				default:
					throw new Exception("\"" + args[1] + "\" is not a valid bind action!");
				}
			};
			_003C_003Ec._003C_003E9__16_6 = val5;
			obj5 = (object)val5;
		}
		commands5.Add(new Command("bind", "Manages custom command binds, by either adding/removing/listing them.", (Action)obj5, new Dictionary<int, List<string>>
		{
			{
				0,
				new List<string> { "add", "remove", "clear", "list" }
			},
			{
				1,
				Enum.GetNames(typeof(KeyCode)).ToList()
			}
		}, new(string, string)[3]
		{
			("string action", "add to add new binds, remove to remove a bind, clear to clear all binds and list to list all binds"),
			("keycode", "(if adding/removing) Keyboard button to bind"),
			("string action", "(if adding) Commands to run, separated by ; and spaces replaced by _")
		}));
		RefreshSkiPlayerCommandAutofill();
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
		List<Command> commands6 = ConsoleScript.Commands;
		object obj6 = _003C_003Ec._003C_003E9__16_7;
		if (obj6 == null)
		{
			Action val6 = delegate(string[] args)
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
			_003C_003Ec._003C_003E9__16_7 = val6;
			obj6 = (object)val6;
		}
		commands6.Add(new Command("controllerbind", "Set or view controller button bindings", (Action)obj6, dictionary, new(string, string)[2]
		{
			("button", "Button name (A, B, X, Y, DpadUp, etc.)"),
			("action", "Action to bind (jump, attack, craft, etc.)")
		}));
		if (ConsoleScript.SearchExact("runatstart") != null)
		{
			return;
		}
		List<Command> commands7 = ConsoleScript.Commands;
		object obj7 = _003C_003Ec._003C_003E9__16_8;
		if (obj7 == null)
		{
			Action val7 = delegate(string[] args)
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
			_003C_003Ec._003C_003E9__16_8 = val7;
			obj7 = (object)val7;
		}
		commands7.Add(new Command("runatstart", "Sets commands to execute on a fresh run (split by ;). Usage: runatstart [commands] OR runatstart clear", (Action)obj7, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
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

	internal static void RefreshNexusCommandAutofill()
	{
		Command val = ConsoleScript.SearchExact("nexus");
		if (val != null)
		{
			val.argAutofill = BuildNexusCommandAutofill();
		}
	}

	private static Dictionary<int, List<string>> BuildNexusCommandAutofill()
	{
		return new Dictionary<int, List<string>> { 
		{
			0,
			ModManagementSettings.GetNexusAutofillEntries()
		} };
	}

	internal static void RefreshSkiPlayerCommandAutofill()
	{
		Command val = ConsoleScript.SearchExact("skiplayer");
		WorldGeneration world = WorldGeneration.world;
		if (val != null && !((Object)(object)world == (Object)null) && world.biomeTitles != null)
		{
			if (val.argAutofill == null)
			{
				val.argAutofill = new Dictionary<int, List<string>>();
			}
			val.argAutofill[0] = BuildSkiPlayerCommandAutofill(world.biomeTitles, world.amountOfLayers);
		}
	}

	private static List<string> BuildSkiPlayerCommandAutofill(string[] biomeTitles, int amountOfLayers)
	{
		int num = Mathf.Min(biomeTitles.Length, Mathf.Max(amountOfLayers, 1));
		List<string> list = new List<string>(num);
		for (int i = 0; i < num; i++)
		{
			if (!string.IsNullOrWhiteSpace(biomeTitles[i]))
			{
				list.Add(i + " - " + biomeTitles[i].Replace('\n', ' '));
			}
		}
		return list;
	}

	private static bool TryForwardClientHealCommand(ref string[] args)
	{
		if (args == null || args.Length == 0 || !args[0].Equals("heal", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!MultiplayerCompat.IsNetworkRunning || !MultiplayerCompat.IsClient || MultiplayerCompat.IsServer)
		{
			return false;
		}
		if (!MultiplayerCommandUtil.AreClientCheatCommandsAllowed())
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Host has sv_cheats or client commands disabled.</color>");
			args = Array.Empty<string>();
			return true;
		}
		if (!MultiplayerCommandUtil.SendClientHealRequest())
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
		if (!MultiplayerCompat.IsNetworkRunning || !MultiplayerCompat.IsClient || MultiplayerCompat.IsServer)
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
		if (!MultiplayerCommandUtil.SendClientSpawnRequest(commandString))
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
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		commandString = null;
		errorMessage = null;
		if (args.Length > 5)
		{
			errorMessage = "Spawn forwarding accepts at most entity, position, condition, and amount.";
			return false;
		}
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
					__instance.input.text = string.Join(" ", args.Take(args.Length - 1).ToArray()) + " " + GetAutofillCompletion(val, list2[index2]) + " ";
					__instance.SetCaretToEnd();
					return false;
				}
			}
		}
		return true;
	}

	private static string GetAutofillCompletion(Command command, string entry)
	{
		if (!command.name.Equals("skiplayer", StringComparison.OrdinalIgnoreCase))
		{
			return entry;
		}
		int num = entry.IndexOf(" - ", StringComparison.Ordinal);
		return (num > 0) ? entry.Substring(0, num) : entry;
	}

	[HarmonyPatch(typeof(ConsoleScript), "HandleDescriptionText")]
	[HarmonyPrefix]
	private static bool HandleDescriptionText_Prefix(ConsoleScript __instance, string[] args)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		((TMP_Text)__instance.descriptionText).text = "";
		lastSuggestionCount = 0;
		RefreshLocateAutofill(args);
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

	private static void RefreshLocateAutofill(string[] args)
	{
		if (args == null || args.Length != 2 || !args[0].Equals("locate", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Command val = ConsoleScript.SearchExact("locate");
		if (val != null)
		{
			if (val.argAutofill == null)
			{
				val.argAutofill = new Dictionary<int, List<string>>();
			}
			if (!val.argAutofill.TryGetValue(0, out var value))
			{
				value = new List<string>();
				val.argAutofill[0] = value;
			}
			if (locateAutofillDirty || !(Time.realtimeSinceStartup - locateAutofillLastRefresh < 1f))
			{
				locateAutofillDirty = false;
				locateAutofillLastRefresh = Time.realtimeSinceStartup;
				BuildLocateAutofill(value);
			}
		}
	}

	private static void BuildLocateAutofill(List<string> autofill)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		autofill.Clear();
		locateAutofillSeen.Clear();
		if ((Object)(object)WorldGeneration.world == (Object)null)
		{
			return;
		}
		Scene activeScene = SceneManager.GetActiveScene();
		if (!((Scene)(ref activeScene)).IsValid())
		{
			return;
		}
		GameObject[] rootGameObjects = ((Scene)(ref activeScene)).GetRootGameObjects();
		foreach (GameObject val in rootGameObjects)
		{
			if (!((Object)(object)val == (Object)null) && !ShouldSkipLocateRoot(val))
			{
				CollectLocateNames(val.transform, autofill);
			}
		}
		autofill.Sort(StringComparer.OrdinalIgnoreCase);
	}

	private static bool ShouldSkipLocateRoot(GameObject root)
	{
		if ((Object)(object)root.GetComponent<Canvas>() != (Object)null)
		{
			return true;
		}
		string name = ((Object)root).name;
		if (name.StartsWith("Player", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Camera", StringComparison.OrdinalIgnoreCase) || LocateBlacklist.Contains(name) || name.IndexOf("particle", StringComparison.OrdinalIgnoreCase) >= 0 || name.Length <= 2)
		{
			return true;
		}
		return false;
	}

	private static void CollectLocateNames(Transform root, List<string> autofill)
	{
		locateTransformStack.Clear();
		locateTransformStack.Add(root);
		for (int i = 0; i < locateTransformStack.Count; i++)
		{
			Transform val = locateTransformStack[i];
			if (!ShouldSkipLocateTransform(val))
			{
				string text = NormalizeLocateName(((Object)val).name);
				if (text.Length > 0 && locateAutofillSeen.Add(text))
				{
					autofill.Add(text);
				}
				int childCount = val.childCount;
				for (int j = 0; j < childCount; j++)
				{
					locateTransformStack.Add(val.GetChild(j));
				}
			}
		}
	}

	private static bool ShouldSkipLocateTransform(Transform t)
	{
		GameObject gameObject = ((Component)t).gameObject;
		if ((Object)(object)gameObject.GetComponent<Canvas>() != (Object)null || (Object)(object)gameObject.GetComponent<CanvasRenderer>() != (Object)null || (Object)(object)gameObject.GetComponent<Graphic>() != (Object)null || (Object)(object)gameObject.GetComponent<LayoutGroup>() != (Object)null || (Object)(object)gameObject.GetComponent<Camera>() != (Object)null || (Object)(object)gameObject.GetComponent<EventSystem>() != (Object)null)
		{
			return true;
		}
		string name = ((Object)gameObject).name;
		if (name.StartsWith("Player", StringComparison.OrdinalIgnoreCase) || LocateBlacklist.Contains(name) || name.IndexOf("particle", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return false;
	}

	private static string NormalizeLocateName(string objectName)
	{
		string text = objectName.TrimEnd(Array.Empty<char>());
		while (text.Length > 0 && text[text.Length - 1] == ')')
		{
			int num = text.LastIndexOf('(');
			if (num < 0)
			{
				break;
			}
			string text2 = text.Substring(num + 1, text.Length - num - 2).Trim();
			if (!text2.Equals("Clone", StringComparison.OrdinalIgnoreCase) && !IsAllDigits(text2))
			{
				break;
			}
			text = text.Substring(0, num).TrimEnd(Array.Empty<char>());
		}
		int num2 = text.IndexOf(' ');
		if (num2 >= 0)
		{
			text = text.Substring(0, num2);
		}
		return text.TrimEnd(Array.Empty<char>());
	}

	private static bool IsAllDigits(string value)
	{
		if (value.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] < '0' || value[i] > '9')
			{
				return false;
			}
		}
		return true;
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	private static void FinishWorldGeneration_RunStartCommands()
	{
		RefreshSkiPlayerCommandAutofill();
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

	private static void WriteConsoleSettings()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		ConsoleScript instance = ConsoleScript.instance;
		if (!((Object)(object)instance == (Object)null) && instance.settings != null)
		{
			instance.settings.hexTextColor = "#" + ColorUtility.ToHtmlStringRGB(instance.settings.textColor);
			instance.settings.hexBackgroundColor = "#" + ColorUtility.ToHtmlStringRGB(instance.settings.backgroundColor);
			File.WriteAllText(ConsoleScript.configPath, JsonConvert.SerializeObject((object)instance.settings, (Formatting)1, new JsonSerializerSettings
			{
				ReferenceLoopHandling = (ReferenceLoopHandling)0
			}));
		}
	}
}
