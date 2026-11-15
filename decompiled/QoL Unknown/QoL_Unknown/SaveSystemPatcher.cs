using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
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

		public static Action _003C_003E9__28_0;

		public static Predicate<Command> _003C_003E9__36_0;

		public static Action _003C_003E9__36_1;

		internal void _003CRegisterCommands_Postfix_003Eb__28_0(string[] args)
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

		internal bool _003CRegisterAllCommands_SaveAndQuitPatch_003Eb__36_0(Command c)
		{
			if (c != null)
			{
				return c.name == "saveandquit";
			}
			return false;
		}

		internal void _003CRegisterAllCommands_SaveAndQuitPatch_003Eb__36_1(string[] args)
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

	public static bool IsAutosaveOperation = false;

	private static bool deletedAutosaveForCurrentDeath;

	internal static string PendingVanillaSaveRestorePath;

	private static int saveGameNesting;

	private static DateTime saveSvWriteBaselineUtc;

	public static Vector3? PendingLoadPosition = null;

	public static float? PendingRadLineY = null;

	public static bool? PendingRadLineActive = null;

	public static float? PendingRadLineRemainingSeconds = null;

	public static bool BypassDepthFix = false;

	public static float? PendingIGTSeconds = null;

	public static float? PendingRTASeconds = null;

	public static bool LoadedFromMenu = false;

	internal static bool LastSaveRedirectSucceeded { get; private set; } = true;

	private static string DefaultSavePath => Path.Combine(Application.persistentDataPath, "save.sv");

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.savesystem").PatchAll(typeof(SaveSystemPatcher));
	}

	[HarmonyPatch(typeof(SaveSystem), "SaveGame")]
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void SaveGame_Prefix_AdjustDepth(out int? __state)
	{
		__state = null;
		LastSaveRedirectSucceeded = false;
		if (saveGameNesting++ <= 0)
		{
			saveSvWriteBaselineUtc = File.GetLastWriteTimeUtc(DefaultSavePath);
			if (!BypassDepthFix && (Object)(object)WorldGeneration.world != (Object)null)
			{
				__state = WorldGeneration.world.biomeDepth;
				WorldGeneration world = WorldGeneration.world;
				world.biomeDepth--;
			}
		}
	}

	[HarmonyPatch(typeof(SaveSystem), "SaveGame")]
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	[HarmonyBefore(new string[] { "net.cucorelib" })]
	private static void SaveGame_Postfix_RestoreDepthForCapture(int? __state)
	{
		RestoreSavedDepth(__state);
		PersistCargoState();
	}

	[HarmonyPatch(typeof(SaveSystem), "SaveGame")]
	[HarmonyFinalizer]
	private static Exception SaveGame_Finalizer_RestoreState(Exception __exception, int? __state)
	{
		saveGameNesting--;
		try
		{
			RestoreSavedDepth(__state);
			if (__exception == null)
			{
				LastSaveRedirectSucceeded = File.GetLastWriteTimeUtc(DefaultSavePath) > saveSvWriteBaselineUtc && HandleFileMove();
			}
		}
		catch (Exception arg)
		{
			LastSaveRedirectSucceeded = false;
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)$"Failed to restore state after SaveGame: {arg}");
			}
		}
		return __exception;
	}

	[HarmonyPatch(typeof(SaveSystem), "TryLoadGame")]
	[HarmonyFinalizer]
	private static Exception TryLoadGame_Finalizer(Exception __exception)
	{
		RestorePendingVanillaSave();
		return __exception;
	}

	internal static void RestorePendingVanillaSave()
	{
		string pendingVanillaSaveRestorePath = PendingVanillaSaveRestorePath;
		if (pendingVanillaSaveRestorePath != null)
		{
			PendingVanillaSaveRestorePath = null;
			string destinationPath = Path.Combine(Application.persistentDataPath, "save.sv");
			if (SaveFileStore.TryCopyAtomic(pendingVanillaSaveRestorePath, destinationPath, Logger, "restore vanilla save after layer transition"))
			{
				SaveFileStore.TryDelete(pendingVanillaSaveRestorePath, Logger, "remove vanilla save recovery copy");
			}
		}
	}

	private static void RestoreSavedDepth(int? state)
	{
		if (state.HasValue && !((Object)(object)WorldGeneration.world == (Object)null))
		{
			WorldGeneration.world.biomeDepth = state.Value;
		}
	}

	private static void PersistCargoState()
	{
		if ((Object)(object)PlayerCamera.main == (Object)null || (Object)(object)PlayerCamera.main.body == (Object)null || !PlayerCamera.main.body.hasCargo)
		{
			return;
		}
		string defaultSavePath = DefaultSavePath;
		if (!File.Exists(defaultSavePath))
		{
			return;
		}
		try
		{
			JObject val = JObject.Parse(SaveSystem.Unzip(File.ReadAllBytes(defaultSavePath)));
			JToken obj = val["body"];
			JObject val2 = (JObject)(object)((obj is JObject) ? obj : null);
			if (val2 != null)
			{
				val2["hasCargo"] = JToken.op_Implicit(true);
				File.WriteAllBytes(defaultSavePath, SaveSystem.Zip(JsonConvert.SerializeObject((object)val, (Formatting)0)));
			}
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to persist cargo state into save: " + ex.Message));
			}
		}
	}

	private static bool HandleFileMove()
	{
		string persistentDataPath = Application.persistentDataPath;
		string text = Path.Combine(persistentDataPath, "save.sv");
		if (!SaveFileStore.TryGetSavePath(persistentDataPath, SaveManager.CurrentSaveFile, out var path))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)("Refusing to redirect save to invalid file name '" + SaveManager.CurrentSaveFile + "'."));
			}
			return false;
		}
		if (!string.Equals(path, text, StringComparison.OrdinalIgnoreCase) && File.Exists(text))
		{
			if (!SaveFileStore.TryCopyAtomic(text, path, Logger, "write redirected save '" + Path.GetFileName(path) + "'"))
			{
				return false;
			}
			if (IsAutosaveOperation)
			{
				SaveFileStore.TryDelete(text, Logger, "remove autosave staging file");
			}
		}
		return File.Exists(path);
	}

	[HarmonyPatch(typeof(SaveSystem), "TryLoadGame")]
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	[HarmonyBefore(new string[] { "net.cucorelib" })]
	private static void TryLoadGame_Prefix()
	{
		if (!SaveSystem.loadedRun)
		{
			return;
		}
		string persistentDataPath = Application.persistentDataPath;
		string text = Path.Combine(persistentDataPath, "save.sv");
		if (!SaveFileStore.TryGetSavePath(persistentDataPath, SaveManager.CurrentSaveFile, out var path))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)("Refusing to load invalid save file name '" + SaveManager.CurrentSaveFile + "'."));
			}
			SaveSystem.loadedRun = false;
		}
		else if (!string.Equals(path, text, StringComparison.OrdinalIgnoreCase) && !SaveFileStore.TryCopyAtomic(path, text, Logger, "stage save '" + Path.GetFileName(path) + "' for loading"))
		{
			SaveSystem.loadedRun = false;
		}
		else
		{
			TryRestoreRunSettingsFromSave(text);
		}
	}

	internal static bool TryRestoreRunSettingsFromSave(string savePath)
	{
		if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
		{
			return false;
		}
		try
		{
			JToken val = JObject.Parse(SaveSystem.Unzip(File.ReadAllBytes(savePath)))["runSettings"];
			if (val == null)
			{
				return false;
			}
			List<(string, string)> list = val.ToObject<List<(string, string)>>();
			if (list != null && list.Count > 0)
			{
				WorldGeneration.runSettings = SaveSystem.TupleListToDic(list);
				return true;
			}
			JArray val2 = (JArray)(object)((val is JArray) ? val : null);
			if (val2 == null)
			{
				return false;
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>(RunSettings.GetPreset("normal").presetValues);
			for (int i = 0; i < ((JContainer)val2).Count; i++)
			{
				JToken obj = val2[i];
				JObject val3 = (JObject)(object)((obj is JObject) ? obj : null);
				string text = ((val3 == null) ? null : ((object)val3["Item1"])?.ToString()) ?? ((val3 == null) ? null : ((object)val3["Key"])?.ToString());
				JToken val4 = ((val3 != null) ? val3["Item2"] : null) ?? ((val3 != null) ? val3["Value"] : null);
				if (!string.IsNullOrEmpty(text) && val4 != null)
				{
					RunSetting setting = RunSettings.GetSetting(text);
					if (setting is RunSettingFloat)
					{
						dictionary[text] = Extensions.Value<float>((IEnumerable<JToken>)val4);
					}
					else if (setting is RunSettingBool)
					{
						dictionary[text] = Extensions.Value<bool>((IEnumerable<JToken>)val4);
					}
					else if (setting is RunSettingDropdown)
					{
						dictionary[text] = Extensions.Value<int>((IEnumerable<JToken>)val4);
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
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		List<Command> commands = ConsoleScript.Commands;
		object obj = _003C_003Ec._003C_003E9__28_0;
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
			_003C_003Ec._003C_003E9__28_0 = val;
			obj = (object)val;
		}
		commands.Add(new Command("seed", "Displays the current world seed.", (Action)obj, (Dictionary<int, List<string>>)null, Array.Empty<(string, string)>()));
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void FinishWorldGeneration_Postfix(ref IEnumerator __result)
	{
		if (__result == null)
		{
			ApplyPendingLoadStateAfterWorldGeneration();
		}
		else
		{
			__result = RunAfterCoroutineCompletes(__result, ApplyPendingLoadStateAfterWorldGeneration);
		}
	}

	private static IEnumerator RunAfterCoroutineCompletes(IEnumerator inner, Action onCompleted)
	{
		while (inner.MoveNext())
		{
			yield return inner.Current;
		}
		onCompleted?.Invoke();
	}

	private static void ApplyPendingLoadStateAfterWorldGeneration()
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		deletedAutosaveForCurrentDeath = false;
		bool isLoadingMultiplayerSlot = MultiplayerSaveLoadPatcher.IsLoadingMultiplayerSlot;
		MultiplayerSaveLoadPatcher.ClearMultiplayerSlotLoad();
		if (isLoadingMultiplayerSlot && MultiplayerCompat.IsNetworkRunning && MultiplayerCompat.IsServer)
		{
			MultiplayerCompat.TeleportClientsToHostAfterDelay(1f);
		}
		if ((Object)(object)WorldGeneration.world != (Object)null && WorldGeneration.world.biomeDepth == 10)
		{
			WorldGeneration.world.totalTraveled = 0;
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
		if (PendingRadLineRemainingSeconds.HasValue && (Object)(object)WorldGeneration.world != (Object)null)
		{
			float maxTimePerLayer = WorldGeneration.world.maxTimePerLayer;
			float num = Mathf.Clamp(PendingRadLineRemainingSeconds.Value, 0f, maxTimePerLayer);
			WorldGeneration.world.layerTimeSpent = maxTimePerLayer - num;
			PendingRadLineRemainingSeconds = null;
		}
		if ((Object)(object)PlayerCamera.main != (Object)null && (Object)(object)PlayerCamera.main.body != (Object)null)
		{
			Body body = PlayerCamera.main.body;
			if (body.hasCargo && (Object)(object)body.cargo != (Object)null)
			{
				body.cargo.SetActive(true);
			}
		}
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
			SteamManager.disableAchievements = false;
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterAllCommands_SaveAndQuitPatch()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		try
		{
			List<Command> commands = ConsoleScript.Commands;
			int num = commands.FindIndex((Command c) => c != null && c.name == "saveandquit");
			if (num < 0)
			{
				return;
			}
			object obj = _003C_003Ec._003C_003E9__36_1;
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
				_003C_003Ec._003C_003E9__36_1 = val;
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
		SaveManager.PerformAutosaveOnExit();
		BypassDepthFix = true;
	}

	[HarmonyPatch(typeof(WorldGeneration), "SaveAndExit")]
	[HarmonyFinalizer]
	private static void SaveAndExit_Finalizer()
	{
		BypassDepthFix = false;
	}

	[HarmonyPatch(typeof(PlayerCamera), "ToMainMenu")]
	[HarmonyPrefix]
	private static void ToMainMenu_Prefix()
	{
		ResetRunLoadStateForMainMenu();
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
		SaveManager.CurrentSaveFile = "save.sv";
		SeedManager.SetSeed("");
		IsAutosaveOperation = false;
		LastSaveRedirectSucceeded = true;
		BypassDepthFix = false;
		deletedAutosaveForCurrentDeath = false;
		PendingLoadPosition = null;
		PendingRadLineY = null;
		PendingRadLineActive = null;
		PendingRadLineRemainingSeconds = null;
		PendingIGTSeconds = null;
		PendingRTASeconds = null;
		MultiplayerSaveLoadPatcher.ClearMultiplayerSlotLoad();
	}
}
