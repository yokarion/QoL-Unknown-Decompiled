using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace QoL_Unknown;

internal class SaveSystemPatcher
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Action _003C_003E9__13_0;

		public static Predicate<Command> _003C_003E9__19_0;

		public static Action _003C_003E9__19_1;

		internal void _003CRegisterCommands_Postfix_003Eb__13_0(string[] args)
		{
			if (SeedManager.IsSeeded && !string.IsNullOrEmpty(SeedManager.InputString))
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, $"<color=green>Seed: {SeedManager.InputString} ({SeedManager.CurrentSeed})</color>");
			}
			else
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, $"<color=grey>{SeedManager.CurrentSeed}</color>");
			}
		}

		internal bool _003CRegisterAllCommands_SaveAndQuitPatch_003Eb__19_0(Command c)
		{
			return c != null && c.name == "saveandquit";
		}

		internal void _003CRegisterAllCommands_SaveAndQuitPatch_003Eb__19_1(string[] args)
		{
			ConsoleHelper.CheckForWorld(ConsoleScript.instance);
			SaveSystem.SaveGame();
			SaveManager.PerformAutosaveOnExit();
			ResetRunLoadStateForMainMenu();
			PlayerCamera.main.ToMainMenu();
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "Saved the game and quit to menu.");
		}
	}

	private static ManualLogSource Logger;

	public static bool IsAutosaveOperation;

	private static bool deletedAutosaveForCurrentDeath;

	public static Vector3? PendingLoadPosition;

	public static float? PendingRadLineY;

	public static bool? PendingRadLineActive;

	public static bool BypassDepthFix;

	public static float? PendingIGTSeconds;

	public static float? PendingRTASeconds;

	public static bool LoadedFromMenu;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.savesystem");
		val.PatchAll(typeof(SaveSystemPatcher));
		Logger.LogInfo((object)"SaveSystemPatcher initialized");
	}

	[HarmonyPatch(typeof(SaveSystem), "SaveGame")]
	[HarmonyPrefix]
	private static void SaveGame_Prefix_AdjustDepth()
	{
		if (!BypassDepthFix && (Object)(object)WorldGeneration.world != (Object)null)
		{
			WorldGeneration world = WorldGeneration.world;
			world.biomeDepth--;
		}
	}

	[HarmonyPatch(typeof(SaveSystem), "SaveGame")]
	[HarmonyPostfix]
	private static void SaveGame_Postfix_RestoreDepth()
	{
		if (BypassDepthFix)
		{
			HandleFileMove();
			return;
		}
		if ((Object)(object)WorldGeneration.world != (Object)null)
		{
			WorldGeneration world = WorldGeneration.world;
			world.biomeDepth++;
		}
		HandleFileMove();
	}

	private static void HandleFileMove()
	{
		string text = Application.persistentDataPath + "\\save.sv";
		string text2 = Path.Combine(Application.persistentDataPath, SaveManager.CurrentSaveFile);
		if (SaveManager.CurrentSaveFile != "save.sv" && File.Exists(text))
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			File.Move(text, text2);
			if (!IsAutosaveOperation)
			{
				File.Copy(text2, text, overwrite: true);
			}
		}
	}

	[HarmonyPatch(typeof(SaveSystem), "TryLoadGame")]
	[HarmonyPrefix]
	private static void TryLoadGame_Prefix()
	{
		if (!SaveSystem.loadedRun)
		{
			return;
		}
		string text = Application.persistentDataPath + "\\save.sv";
		string text2 = Path.Combine(Application.persistentDataPath, SaveManager.CurrentSaveFile);
		if (SaveManager.CurrentSaveFile != "save.sv" && File.Exists(text2))
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Copy(text2, text);
		}
		TryRestoreRunSettingsFromSave(text);
	}

	internal static bool TryRestoreRunSettingsFromSave(string savePath)
	{
		if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
		{
			return false;
		}
		try
		{
			JObject val = JObject.Parse(SaveSystem.Unzip(File.ReadAllBytes(savePath)));
			JToken val2 = val["runSettings"];
			if (val2 == null)
			{
				return false;
			}
			List<(string, string)> list = val2.ToObject<List<(string, string)>>();
			if (list != null && list.Count > 0)
			{
				WorldGeneration.runSettings = SaveSystem.TupleListToDic(list);
				return true;
			}
			JArray val3 = (JArray)(object)((val2 is JArray) ? val2 : null);
			if (val3 == null)
			{
				return false;
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>(RunSettings.GetPreset("normal").presetValues);
			for (int i = 0; i < ((JContainer)val3).Count; i++)
			{
				JToken obj = val3[i];
				JObject val4 = (JObject)(object)((obj is JObject) ? obj : null);
				string text = ((val4 == null) ? null : ((object)val4["Item1"])?.ToString()) ?? ((val4 == null) ? null : ((object)val4["Key"])?.ToString());
				JToken val5 = ((val4 != null) ? val4["Item2"] : null) ?? ((val4 != null) ? val4["Value"] : null);
				if (!string.IsNullOrEmpty(text) && val5 != null)
				{
					RunSetting setting = RunSettings.GetSetting(text);
					if (setting is RunSettingFloat)
					{
						dictionary[text] = Extensions.Value<float>((IEnumerable<JToken>)val5);
					}
					else if (setting is RunSettingBool)
					{
						dictionary[text] = Extensions.Value<bool>((IEnumerable<JToken>)val5);
					}
					else if (setting is RunSettingDropdown)
					{
						dictionary[text] = Extensions.Value<int>((IEnumerable<JToken>)val5);
					}
				}
			}
			WorldGeneration.runSettings = dictionary;
			return true;
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to pre-restore saved run settings: " + ex.Message));
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterCommands_Postfix()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		List<Command> commands = ConsoleScript.Commands;
		object obj = _003C_003Ec._003C_003E9__13_0;
		if (obj == null)
		{
			Action val = delegate
			{
				if (SeedManager.IsSeeded && !string.IsNullOrEmpty(SeedManager.InputString))
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, $"<color=green>Seed: {SeedManager.InputString} ({SeedManager.CurrentSeed})</color>");
				}
				else
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, $"<color=grey>{SeedManager.CurrentSeed}</color>");
				}
			};
			_003C_003Ec._003C_003E9__13_0 = val;
			obj = (object)val;
		}
		commands.Add(new Command("seed", "Displays the current world seed.", (Action)obj, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	private static void FinishWorldGeneration_Postfix()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		deletedAutosaveForCurrentDeath = false;
		bool isLoadingMultiplayerSlot = MultiplayerSaveLoadPatcher.IsLoadingMultiplayerSlot;
		MultiplayerSaveLoadPatcher.ClearMultiplayerSlotLoad();
		if (isLoadingMultiplayerSlot && KrokoshaMpCompat.IsNetworkRunning && KrokoshaMpCompat.IsServer)
		{
			KrokoshaMpCompat.TeleportClientsToHostAfterDelay(1f);
		}
		if (PendingLoadPosition.HasValue)
		{
			Vector3 value = PendingLoadPosition.Value;
			if (Object.op_Implicit((Object)(object)PlayerCamera.main) && Object.op_Implicit((Object)(object)PlayerCamera.main.body))
			{
				((Component)PlayerCamera.main.body).transform.position = value;
				((Component)PlayerCamera.main).transform.position = new Vector3(value.x, value.y, -10f);
				if (Object.op_Implicit((Object)(object)PlayerCamera.main.body.rb))
				{
					PlayerCamera.main.body.rb.velocity = Vector2.zero;
				}
			}
			PendingLoadPosition = null;
		}
		if (PendingRadLineY.HasValue && (Object)(object)RadiationLine.line != (Object)null)
		{
			Vector3 position = ((Component)RadiationLine.line).transform.position;
			position.y = PendingRadLineY.Value;
			((Component)RadiationLine.line).transform.position = position;
			if (PendingRadLineActive.HasValue)
			{
				if (PendingRadLineActive.Value)
				{
					RadiationLine.line.Activate();
				}
				else
				{
					RadiationLine.line.Deactivate();
				}
			}
			PendingRadLineY = null;
			PendingRadLineActive = null;
		}
		if (PendingIGTSeconds.HasValue || PendingRTASeconds.HasValue)
		{
			float valueOrDefault = PendingIGTSeconds.GetValueOrDefault();
			float valueOrDefault2 = PendingRTASeconds.GetValueOrDefault();
			TimerMenuPatcher.SetTimers(valueOrDefault, valueOrDefault2);
			PendingIGTSeconds = null;
			PendingRTASeconds = null;
		}
		RadlinePatcher.ApplyCurrentState();
	}

	[HarmonyPatch(typeof(WorldGeneration), "Start")]
	[HarmonyPostfix]
	private static void WorldGeneration_Start_Postfix()
	{
		deletedAutosaveForCurrentDeath = false;
		if (!SaveSystem.loadedRun)
		{
			LoadedFromMenu = false;
			MultiplayerSaveLoadPatcher.ClearMultiplayerSlotLoad();
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterAllCommands_SaveAndQuitPatch()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		try
		{
			List<Command> commands = ConsoleScript.Commands;
			int num = commands.FindIndex((Command c) => c != null && c.name == "saveandquit");
			if (num < 0)
			{
				return;
			}
			object obj = _003C_003Ec._003C_003E9__19_1;
			if (obj == null)
			{
				Action val = delegate
				{
					ConsoleHelper.CheckForWorld(ConsoleScript.instance);
					SaveSystem.SaveGame();
					SaveManager.PerformAutosaveOnExit();
					ResetRunLoadStateForMainMenu();
					PlayerCamera.main.ToMainMenu();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Saved the game and quit to menu.");
				};
				_003C_003Ec._003C_003E9__19_1 = val;
				obj = (object)val;
			}
			commands[num] = new Command("saveandquit", "!! NOT RECOMMENDED !!. Instantly saves the game and exits to the main menu as per vanilla. Loading the save will put you at the beginning of the current layer.", (Action)obj, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>());
		}
		catch (Exception arg)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)$"Failed to patch saveandquit command: {arg}");
			}
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "SaveAndExit")]
	[HarmonyPrefix]
	private static void SaveAndExit_Prefix()
	{
		WorldGeneration world = WorldGeneration.world;
		world.biomeDepth++;
		SaveManager.PerformAutosaveOnExit();
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleDeathScreen")]
	[HarmonyPostfix]
	private static void HandleDeathScreen_Postfix(PlayerCamera __instance)
	{
		if (!((Object)(object)__instance == (Object)null) && !((Object)(object)__instance.body == (Object)null))
		{
			if (__instance.body.alive)
			{
				deletedAutosaveForCurrentDeath = false;
			}
			else if (SaveManager.IsDeleteAutosaveOnDeathEnabled() && __instance.didDeathScreen && !deletedAutosaveForCurrentDeath)
			{
				SaveManager.DeleteAutosave();
				deletedAutosaveForCurrentDeath = true;
			}
		}
	}

	private static void ResetRunLoadStateForMainMenu()
	{
		SaveSystem.loadedRun = false;
		LoadedFromMenu = false;
		deletedAutosaveForCurrentDeath = false;
		PendingLoadPosition = null;
		PendingRadLineY = null;
		PendingRadLineActive = null;
		PendingIGTSeconds = null;
		PendingRTASeconds = null;
		MultiplayerSaveLoadPatcher.ClearMultiplayerSlotLoad();
	}
}
