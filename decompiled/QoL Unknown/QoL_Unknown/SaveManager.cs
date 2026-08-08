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

	public static string SaveFolder => Application.persistentDataPath;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.savemanager");
		val.PatchAll(typeof(SaveManager));
		Logger.LogInfo((object)"SaveManager initialized");
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
		DirectoryInfo directoryInfo = new DirectoryInfo(SaveFolder);
		return (from f in directoryInfo.GetFiles("*.sv")
			orderby f.LastWriteTime descending
			select f.Name).ToList();
	}

	private static void UpdateAutosave()
	{
		if (!AutosaveEnabled || !Object.op_Implicit((Object)(object)PlayerCamera.main) || !PlayerCamera.main.body.alive)
		{
			return;
		}
		if (KrokoshaMpCompat.IsActive)
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
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		if (!QoLModuleManager.IsEnabled("qol_autosave") || KrokoshaMpCompat.IsActive || (Object)(object)WorldGeneration.world == (Object)null || !WorldGeneration.world.worldExists || !Object.op_Implicit((Object)(object)PlayerCamera.main) || !PlayerCamera.main.body.alive || !PlayerCamera.main.body.standing)
		{
			return;
		}
		string path = Path.Combine(SaveFolder, "autosave.sv");
		string path2 = Path.Combine(SaveFolder, "autosave.sv.meta");
		string text = Path.Combine(SaveFolder, "save.sv");
		string text2 = Path.Combine(SaveFolder, "save_backup_temp.sv");
		bool flag = File.Exists(text);
		if (flag)
		{
			File.Copy(text, text2, overwrite: true);
		}
		string currentSaveFile = CurrentSaveFile;
		try
		{
			CurrentSaveFile = "autosave.sv";
			SaveSystemPatcher.IsAutosaveOperation = true;
			using (KrokoshaMpCompat.BeginVanillaSaveSnapshot())
			{
				SaveSystem.SaveGame();
			}
		}
		finally
		{
			CurrentSaveFile = currentSaveFile;
			SaveSystemPatcher.IsAutosaveOperation = false;
		}
		if (File.Exists(path))
		{
			float radLinePosY = 0f;
			bool radLineActive = false;
			if ((Object)(object)RadiationLine.line != (Object)null)
			{
				radLinePosY = ((Component)RadiationLine.line).transform.position.y;
				radLineActive = RadiationLine.line.active;
			}
			SaveMetadata saveMetadata = new SaveMetadata
			{
				SeedInput = SeedManager.InputString,
				SeedValue = SeedManager.CurrentSeed,
				PlayerPosition = new SerialVec3(((Component)PlayerCamera.main.body).transform.position),
				LocationName = WorldGeneration.world.TotalDepthString() + " Deep",
				Timestamp = DateTime.Now.ToString("g"),
				BiomeIndex = WorldGeneration.world.biomeDepth,
				RadLinePosY = radLinePosY,
				RadLineActive = radLineActive,
				SavedDuringMultiplayer = KrokoshaMpCompat.IsNetworkRunning
			};
			string contents = JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1);
			File.WriteAllText(path2, contents);
			if (flag)
			{
				File.Copy(text2, text, overwrite: true);
				File.Delete(text2);
			}
			else if (File.Exists(text))
			{
				File.Delete(text);
			}
			if (showMessage)
			{
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=yellow>Autosaved game.</color>");
			}
		}
		else if (flag)
		{
			File.Copy(text2, text, overwrite: true);
			File.Delete(text2);
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
		string text = Path.Combine(SaveFolder, filename);
		string path = text + ".meta";
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}
}
