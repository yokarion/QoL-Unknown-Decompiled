using System;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QoL_Unknown;

internal class LayerTransitionPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.layertransition");
		val.PatchAll(typeof(LayerTransitionPatcher));
		CuCoreLibTransitionCompat.Initialize(val, logger);
	}

	[HarmonyPatch(typeof(WorldGeneration), "RegenerateWorld")]
	[HarmonyPrefix]
	private static bool RegenerateWorld_Prefix(WorldGeneration __instance, bool twice)
	{
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		if (MultiplayerCompat.ShouldBlockQoLLoads)
		{
			return true;
		}
		GlobalDark.main.Darken();
		__instance.IncreaseDepthByLayer();
		if (twice)
		{
			__instance.IncreaseDepthByLayer();
		}
		int num = __instance.biomeDepth + ((!twice) ? 1 : 2);
		__instance.biomeDepth = ((num >= __instance.amountOfLayers) ? 1 : num);
		__instance.lootRarityMultiplier *= WorldGeneration.GetRunSettingFloat("lootmultiplier");
		__instance.trapRarityMultiplier += WorldGeneration.GetRunSettingFloat("trapincrease");
		try
		{
			SaveSystem.SaveGame();
			if (!SaveSystemPatcher.LastSaveRedirectSucceeded)
			{
				throw new IOException("The transition save was not committed.");
			}
			string sourcePath = Path.Combine(Application.persistentDataPath, "save.sv");
			string destinationPath = Path.Combine(Application.persistentDataPath, "autosave.sv");
			if (!SaveFileStore.TryCopyAtomic(sourcePath, destinationPath, Logger, "synchronize autosave after layer transition"))
			{
				throw new IOException("The transition autosave was not committed.");
			}
			if (SaveManager.CurrentSaveFile == "autosave.sv" && !UpdateMetadata(__instance, "autosave.sv"))
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)"Layer transition completed, but autosave metadata could not be updated.");
				}
			}
			if (SaveManager.CurrentSaveFile != "autosave.sv" && !UpdateMetadata(__instance, SaveManager.CurrentSaveFile))
			{
				ManualLogSource logger2 = Logger;
				if (logger2 != null)
				{
					logger2.LogWarning((object)("Layer transition completed, but metadata for '" + SaveManager.CurrentSaveFile + "' could not be updated."));
				}
			}
			SaveSystem.loadedRun = true;
			SaveSystemPatcher.PendingLoadPosition = null;
			SaveSystemPatcher.PendingRadLineY = null;
			CuCoreLibTransitionCompat.SkipWorldRestore = true;
			Scene activeScene = SceneManager.GetActiveScene();
			SceneManager.LoadScene(((Scene)(ref activeScene)).name);
		}
		catch (Exception)
		{
			SaveSystem.loadedRun = false;
			CuCoreLibTransitionCompat.SkipWorldRestore = false;
			if (Object.op_Implicit((Object)(object)PlayerCamera.main))
			{
				PlayerCamera.main.DoAlert("Could not save the layer transition. Reload was cancelled to protect your save.", false);
			}
		}
		return false;
	}

	private static bool UpdateMetadata(WorldGeneration world, string filename)
	{
		if (!SaveFileStore.TryGetSavePath(Application.persistentDataPath, filename, out var path))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Refusing to update metadata for invalid save file name '" + filename + "'."));
			}
			return false;
		}
		try
		{
			SaveMetadata saveMetadata = MultiplayerSaveBundle.BuildBaseMetadata();
			saveMetadata.SaveKind = "Singleplayer";
			saveMetadata.SavedDuringMultiplayer = false;
			saveMetadata.UseVanillaLayerSpawn = true;
			string contents = JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1);
			return SaveFileStore.TryWriteAllTextAtomic(path + ".meta", contents, Logger, "update metadata for '" + filename + "' during transition");
		}
		catch (Exception arg)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogError((object)$"Failed to update metadata during transition: {arg}");
			}
			return false;
		}
	}
}
