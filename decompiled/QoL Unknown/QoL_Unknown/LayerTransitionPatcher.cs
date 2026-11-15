using System;
using System.Collections;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace QoL_Unknown;

internal class LayerTransitionPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.layertransition").PatchAll(typeof(LayerTransitionPatcher));
	}

	[HarmonyPatch(typeof(WorldGeneration), "RegenerateWorld")]
	[HarmonyPrefix]
	private static bool RegenerateWorld_Prefix(WorldGeneration __instance, ref IEnumerator __result)
	{
		if (MultiplayerCompat.ShouldBlockQoLLoads)
		{
			return true;
		}
		if (WorldGeneration.GetRunSettingBool("endless") && __instance.biomeDepth == 0)
		{
			return true;
		}
		string text = Path.Combine(Application.persistentDataPath, "save.sv");
		string destinationPath = Path.Combine(Application.persistentDataPath, "autosave.sv");
		try
		{
			string text2 = text + ".qol-transition-backup";
			if (File.Exists(text))
			{
				if (!SaveFileStore.TryCopyAtomic(text, text2, Logger, "back up vanilla save before layer transition"))
				{
					throw new IOException("The vanilla save could not be backed up.");
				}
				SaveSystemPatcher.PendingVanillaSaveRestorePath = text2;
			}
			SaveSystemPatcher.BypassDepthFix = true;
			try
			{
				SaveSystem.SaveGame();
			}
			finally
			{
				SaveSystemPatcher.BypassDepthFix = false;
			}
			if (!SaveSystemPatcher.LastSaveRedirectSucceeded)
			{
				throw new IOException("The transition save was not committed.");
			}
			if (!SaveFileStore.TryCopyAtomic(text, destinationPath, Logger, "synchronize autosave after layer transition"))
			{
				throw new IOException("The transition autosave was not committed.");
			}
			if (!UpdateMetadata(__instance, "autosave.sv"))
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)"Layer transition completed, but autosave metadata could not be updated.");
				}
			}
			bool flag = SaveSystemPatcher.PendingVanillaSaveRestorePath != null && SaveManager.CurrentSaveFile == "save.sv";
			if (SaveManager.CurrentSaveFile != "autosave.sv" && !flag && !UpdateMetadata(__instance, SaveManager.CurrentSaveFile))
			{
				ManualLogSource logger2 = Logger;
				if (logger2 != null)
				{
					logger2.LogWarning((object)("Layer transition completed, but metadata for '" + SaveManager.CurrentSaveFile + "' could not be updated."));
				}
			}
			SaveSystemPatcher.RestorePendingVanillaSave();
		}
		catch (Exception)
		{
			SaveSystemPatcher.RestorePendingVanillaSave();
			if (Object.op_Implicit((Object)(object)PlayerCamera.main))
			{
				PlayerCamera.main.DoAlert("Could not save the layer transition. Descent was cancelled to protect your save.", false);
			}
			__result = CompletedRoutine();
			return false;
		}
		return true;
	}

	private static IEnumerator CompletedRoutine()
	{
		yield break;
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
			int biomeDepth = world.biomeDepth;
			int totalTraveled = world.totalTraveled;
			world.totalTraveled = (((world.biomeDepth = ((biomeDepth + 1 >= world.amountOfLayers) ? 1 : (biomeDepth + 1))) != 10) ? (totalTraveled + (int)((float)world.height * 0.3f)) : 0);
			SaveMetadata saveMetadata;
			try
			{
				saveMetadata = MultiplayerSaveBundle.BuildBaseMetadata();
			}
			finally
			{
				world.biomeDepth = biomeDepth;
				world.totalTraveled = totalTraveled;
			}
			saveMetadata.SaveKind = "Singleplayer";
			saveMetadata.SavedDuringMultiplayer = false;
			saveMetadata.UseVanillaLayerSpawn = true;
			saveMetadata.RadLineRemainingSeconds = world.maxTimePerLayer;
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
