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
		Logger.LogInfo((object)"LayerTransitionPatcher initialized");
	}

	[HarmonyPatch(typeof(WorldGeneration), "RegenerateWorld")]
	[HarmonyPrefix]
	private static bool RegenerateWorld_Prefix(WorldGeneration __instance, bool twice)
	{
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		if (KrokoshaMpCompat.ShouldBlockQoLLoads)
		{
			return true;
		}
		GlobalDark.main.Darken();
		__instance.IncreaseDepthByLayer();
		if (twice)
		{
			__instance.IncreaseDepthByLayer();
		}
		if (__instance.biomeDepth < 4)
		{
			__instance.biomeDepth += ((!twice) ? 1 : 2);
		}
		else
		{
			__instance.biomeDepth = 1;
		}
		__instance.lootRarityMultiplier *= WorldGeneration.GetRunSettingFloat("lootmultiplier");
		__instance.trapRarityMultiplier += WorldGeneration.GetRunSettingFloat("trapincrease");
		try
		{
			SaveSystem.SaveGame();
			string text = Path.Combine(Application.persistentDataPath, "save.sv");
			string destFileName = Path.Combine(Application.persistentDataPath, "autosave.sv");
			if (File.Exists(text))
			{
				File.Copy(text, destFileName, overwrite: true);
				if (SaveManager.CurrentSaveFile == "autosave.sv")
				{
					UpdateMetadata(__instance, "autosave.sv");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Error saving transition: " + ex));
		}
		if (SaveManager.CurrentSaveFile != "autosave.sv")
		{
			UpdateMetadata(__instance, SaveManager.CurrentSaveFile);
		}
		SaveSystem.loadedRun = true;
		SaveSystemPatcher.PendingLoadPosition = null;
		SaveSystemPatcher.PendingRadLineY = null;
		Scene activeScene = SceneManager.GetActiveScene();
		SceneManager.LoadScene(((Scene)(ref activeScene)).name);
		return false;
	}

	private static void UpdateMetadata(WorldGeneration world, string filename)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		string path = Path.Combine(Application.persistentDataPath, filename + ".meta");
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
			LocationName = world.TotalDepthString() + " Deep",
			Timestamp = DateTime.Now.ToString("g"),
			BiomeIndex = world.biomeDepth,
			RadLinePosY = radLinePosY,
			RadLineActive = radLineActive
		};
		try
		{
			string contents = JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1);
			File.WriteAllText(path, contents);
		}
		catch (Exception arg)
		{
			Logger.LogError((object)$"Failed to update metadata during transition: {arg}");
		}
	}
}
