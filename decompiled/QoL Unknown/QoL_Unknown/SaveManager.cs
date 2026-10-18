using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace QoL_Unknown;

public static class SaveManager
{
	private const string AutosaveOnExitPrefKey = "Setting_AutosaveOnExit";

	private const string DeleteAutosaveOnDeathPrefKey = "Setting_DeleteAutosaveOnDeath";

	private static ManualLogSource Logger;

	public static string CurrentSaveFile = "save.sv";

	public static bool AutosaveEnabled = true;

	public static float AutosaveInterval = 180f;

	private static float autosaveTimer = 0f;

	private static bool autosaveInProgress;

	public static string SaveFolder => Application.persistentDataPath;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.savemanager");
		val.PatchAll(typeof(SaveManager));
	}

	public static bool IsAutosaveOnExitEnabled()
	{
		return PlayerPrefs.GetInt("Setting_AutosaveOnExit", 0) == 1;
	}

	public static void SetAutosaveOnExitEnabled(bool enabled)
	{
		PlayerPrefs.SetInt("Setting_AutosaveOnExit", enabled ? 1 : 0);
		PlayerPrefs.Save();
	}

	public static bool IsDeleteAutosaveOnDeathEnabled()
	{
		return PlayerPrefs.GetInt("Setting_DeleteAutosaveOnDeath", 0) == 1;
	}

	public static void SetDeleteAutosaveOnDeathEnabled(bool enabled)
	{
		PlayerPrefs.SetInt("Setting_DeleteAutosaveOnDeath", enabled ? 1 : 0);
		PlayerPrefs.Save();
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	private static void PlayerCamera_Update_Postfix()
	{
		UpdateAutosave();
	}

	public static List<string> GetSaveFiles()
	{
		try
		{
			Directory.CreateDirectory(SaveFolder);
			return (from file in new DirectoryInfo(SaveFolder).GetFiles("*.sv")
				orderby file.LastWriteTimeUtc descending
				select file.Name).ToList();
		}
		catch (Exception arg)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)$"Failed to enumerate save files: {arg}");
			}
			return new List<string>();
		}
	}

	private static void UpdateAutosave()
	{
		if (!AutosaveEnabled || autosaveInProgress || !Object.op_Implicit((Object)(object)PlayerCamera.main) || !Object.op_Implicit((Object)(object)PlayerCamera.main.body) || !PlayerCamera.main.body.alive)
		{
			return;
		}
		if (MultiplayerCompat.IsActive)
		{
			autosaveTimer = 0f;
			return;
		}
		autosaveTimer += Time.deltaTime;
		if (autosaveTimer >= AutosaveInterval)
		{
			autosaveTimer = 0f;
			PerformAutosave();
		}
	}

	public static void PerformAutosave(bool showMessage = true)
	{
		if (!QoLModuleManager.IsEnabled("qol_autosave") || autosaveInProgress || MultiplayerCompat.IsActive || (Object)(object)WorldGeneration.world == (Object)null || !WorldGeneration.world.worldExists || !Object.op_Implicit((Object)(object)PlayerCamera.main) || !Object.op_Implicit((Object)(object)PlayerCamera.main.body) || !PlayerCamera.main.body.alive || !PlayerCamera.main.body.standing)
		{
			return;
		}
		string path = Path.Combine(SaveFolder, "autosave.sv");
		string destinationPath = Path.Combine(SaveFolder, "autosave.sv.meta");
		string text = Path.Combine(SaveFolder, "save.sv");
		string text2 = Path.Combine(SaveFolder, "save.sv.qol-autosave-backup-" + Guid.NewGuid().ToString("N"));
		bool flag = File.Exists(text);
		string currentSaveFile = CurrentSaveFile;
		bool flag2 = false;
		try
		{
			autosaveInProgress = true;
			if (flag && !SaveFileStore.TryCopyAtomic(text, text2, Logger, "back up vanilla save before autosave"))
			{
				return;
			}
			CurrentSaveFile = "autosave.sv";
			SaveSystemPatcher.IsAutosaveOperation = true;
			using (MultiplayerCompat.BeginVanillaSaveSnapshot())
			{
				SaveSystem.SaveGame();
			}
			if (!SaveSystemPatcher.LastSaveRedirectSucceeded || !File.Exists(path))
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogError((object)"Autosave did not commit a new autosave.sv.");
				}
				return;
			}
			SaveMetadata saveMetadata = MultiplayerSaveBundle.BuildBaseMetadata();
			saveMetadata.SaveKind = "Singleplayer";
			saveMetadata.SavedDuringMultiplayer = false;
			string contents = JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1);
			flag2 = SaveFileStore.TryWriteAllTextAtomic(destinationPath, contents, Logger, "write autosave metadata");
		}
		catch (Exception arg)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogError((object)$"Autosave failed: {arg}");
			}
		}
		finally
		{
			CurrentSaveFile = currentSaveFile;
			SaveSystemPatcher.IsAutosaveOperation = false;
			bool flag3 = true;
			if (flag && File.Exists(text2))
			{
				if (!SaveFileStore.TryCopyAtomic(text2, text, Logger, "restore vanilla save after autosave"))
				{
					ManualLogSource logger3 = Logger;
					if (logger3 != null)
					{
						logger3.LogError((object)("Vanilla save recovery copy remains at '" + text2 + "'."));
					}
					flag3 = false;
				}
			}
			else if (!flag)
			{
				SaveFileStore.TryDelete(text, Logger, "remove autosave staging file");
			}
			if (flag3)
			{
				SaveFileStore.TryDelete(text2, Logger, "remove autosave recovery copy");
			}
			autosaveInProgress = false;
		}
		if (flag2 && showMessage)
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=yellow>Autosaved game.</color>");
		}
	}

	public static void PerformAutosaveOnExit()
	{
		if (IsAutosaveOnExitEnabled())
		{
			PerformAutosave(showMessage: false);
		}
	}

	public static void DeleteAutosave()
	{
		DeleteSave("autosave.sv");
	}

	public static void DeleteSave(string filename)
	{
		if (!SaveFileStore.TryGetSavePath(SaveFolder, filename, out var path))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Refusing to delete invalid save file name '" + filename + "'."));
			}
		}
		else
		{
			string path2 = path + ".meta";
			SaveFileStore.TryDelete(path, Logger, "delete save '" + filename + "'");
			SaveFileStore.TryDelete(path2, Logger, "delete metadata for '" + filename + "'");
		}
	}
}
