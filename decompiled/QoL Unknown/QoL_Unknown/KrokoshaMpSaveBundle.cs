using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace QoL_Unknown;

internal static class KrokoshaMpSaveBundle
{
	public const string SaveKindSingleplayer = "Singleplayer";

	public const string SaveKindMultiplayerHostBundle = "MultiplayerHostBundle";

	public const string SaveKindMultiplayerClientInventory = "MultiplayerClientInventory";

	private const string MpSaveFolderName = "mp_save";

	private const string WorldgenStateFileName = "qol_worldgen_state.txt";

	public static string GetBundleName(string slotFileName)
	{
		return Path.GetFileNameWithoutExtension(slotFileName) + ".mp";
	}

	public static string GetBundlePath(string saveFolder, string slotFileName)
	{
		return Path.Combine(saveFolder, GetBundleName(slotFileName));
	}

	public static string GetClientInventoryPath(string saveFolder, string slotFileName)
	{
		return Path.Combine(saveFolder, Path.GetFileNameWithoutExtension(slotFileName) + ".client.json");
	}

	public static string GetLiveMpSavePath()
	{
		return Path.Combine(Application.persistentDataPath, "mp_save");
	}

	public static bool HasBundle(string saveFolder, string slotFileName)
	{
		string bundlePath = GetBundlePath(saveFolder, slotFileName);
		return Directory.Exists(bundlePath) && File.Exists(Path.Combine(bundlePath, "save.sv")) && File.Exists(Path.Combine(bundlePath, "mp_rules.json"));
	}

	public static void DeleteSlotExtras(string saveFolder, string slotFileName)
	{
		string bundlePath = GetBundlePath(saveFolder, slotFileName);
		if (Directory.Exists(bundlePath))
		{
			Directory.Delete(bundlePath, recursive: true);
		}
		string clientInventoryPath = GetClientInventoryPath(saveFolder, slotFileName);
		if (File.Exists(clientInventoryPath))
		{
			File.Delete(clientInventoryPath);
		}
	}

	public static bool SaveHostBundle(string saveFolder, string slotFileName, SaveMetadata meta, out string sourceRootSave)
	{
		sourceRootSave = null;
		SaveSystem.SaveGame();
		string liveMpSavePath = GetLiveMpSavePath();
		sourceRootSave = Path.Combine(liveMpSavePath, "save.sv");
		if (!Directory.Exists(liveMpSavePath) || !File.Exists(sourceRootSave))
		{
			return false;
		}
		string bundlePath = GetBundlePath(saveFolder, slotFileName);
		if (Directory.Exists(bundlePath))
		{
			Directory.Delete(bundlePath, recursive: true);
		}
		CopyDirectory(liveMpSavePath, bundlePath);
		if (KrokoshaMpCompat.TryCaptureHostWorldgenState(out var payload))
		{
			File.WriteAllText(Path.Combine(bundlePath, "qol_worldgen_state.txt"), payload);
		}
		meta.SaveKind = "MultiplayerHostBundle";
		meta.MultiplayerBundleName = GetBundleName(slotFileName);
		FillMpRulesMetadata(Path.Combine(bundlePath, "mp_rules.json"), meta);
		return true;
	}

	public static bool SaveClientInventorySnapshot(string saveFolder, string slotFileName, SaveMetadata meta, out string snapshotPath)
	{
		snapshotPath = GetClientInventoryPath(saveFolder, slotFileName);
		string text = CaptureLocalPlayerSnapshotJson();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		File.WriteAllText(snapshotPath, text);
		meta.SaveKind = "MultiplayerClientInventory";
		meta.MultiplayerBundleName = Path.GetFileName(snapshotPath);
		meta.LocalPersistentPlayerId = KrokoshaMpCompat.TryGetLocalPersistentPlayerId();
		return true;
	}

	public static bool RestoreHostBundleToKrokoshaMpSave(string saveFolder, string slotFileName)
	{
		string bundlePath = GetBundlePath(saveFolder, slotFileName);
		if (!Directory.Exists(bundlePath))
		{
			return false;
		}
		string liveMpSavePath = GetLiveMpSavePath();
		if (Directory.Exists(liveMpSavePath))
		{
			Directory.Delete(liveMpSavePath, recursive: true);
		}
		CopyDirectory(bundlePath, liveMpSavePath);
		string path = Path.Combine(liveMpSavePath, "qol_worldgen_state.txt");
		if (File.Exists(path))
		{
			KrokoshaMpCompat.SetPendingHostWorldgenStatePayload(File.ReadAllText(path));
		}
		else
		{
			KrokoshaMpCompat.ClearPendingHostWorldgenStatePayload();
		}
		return File.Exists(Path.Combine(liveMpSavePath, "save.sv")) && File.Exists(Path.Combine(liveMpSavePath, "mp_rules.json"));
	}

	public static bool StageSingleplayerSlotAsHostBundle(string saveFolder, string slotFileName, SaveMetadata meta)
	{
		string text = Path.Combine(saveFolder, slotFileName);
		if (!File.Exists(text))
		{
			return false;
		}
		string liveMpSavePath = GetLiveMpSavePath();
		if (Directory.Exists(liveMpSavePath))
		{
			Directory.Delete(liveMpSavePath, recursive: true);
		}
		Directory.CreateDirectory(liveMpSavePath);
		File.Copy(text, Path.Combine(liveMpSavePath, "save.sv"), overwrite: true);
		string text2 = KrokoshaMpCompat.TryGetExpectedHostPersistentPlayerId();
		if (!string.IsNullOrEmpty(text2))
		{
			string text3 = Path.Combine(liveMpSavePath, text2);
			Directory.CreateDirectory(text3);
			File.Copy(text, Path.Combine(text3, "save.sv"), overwrite: true);
		}
		WriteImportedSingleplayerRules(Path.Combine(liveMpSavePath, "mp_rules.json"), meta, text2);
		KrokoshaMpCompat.ClearPendingHostWorldgenStatePayload();
		return true;
	}

	public static bool TryRequestClientInventoryRestore(string saveFolder, string slotFileName, out string message)
	{
		message = null;
		string clientInventoryPath = GetClientInventoryPath(saveFolder, slotFileName);
		if (!File.Exists(clientInventoryPath))
		{
			message = "This slot has no client inventory snapshot.";
			return false;
		}
		string text = File.ReadAllText(clientInventoryPath);
		if (string.IsNullOrWhiteSpace(text))
		{
			message = "Client inventory snapshot is empty.";
			return false;
		}
		if (!KrokoshaMpCompat.SendClientInventoryRestoreRequest(text))
		{
			message = "Could not send inventory restore request. The host needs QoL and client commands enabled.";
			return false;
		}
		message = "Client saves can only be loaded in a singleplayer game";
		return true;
	}

	public static bool TryApplyClientInventoryPayload(Body body, string payload, out string message)
	{
		message = null;
		if ((Object)(object)body == (Object)null)
		{
			message = "No player body found.";
			return false;
		}
		JObject save;
		try
		{
			save = JObject.Parse(payload);
		}
		catch (Exception ex)
		{
			message = "Snapshot could not be parsed: " + ex.Message;
			return false;
		}
		try
		{
			RemoveInventoryItems(body);
			ApplySavedItems(body, save);
			return true;
		}
		catch (Exception ex2)
		{
			message = "Inventory restore failed: " + ex2.Message;
			return false;
		}
	}

	public static bool TryReadLiveMpPlayerInventorySnapshot(string persistentId, out string payload)
	{
		payload = null;
		if (string.IsNullOrWhiteSpace(persistentId))
		{
			return false;
		}
		string path = Path.Combine(GetLiveMpSavePath(), persistentId, "save.sv");
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			payload = SaveSystem.Unzip(File.ReadAllBytes(path));
			return !string.IsNullOrWhiteSpace(payload);
		}
		catch
		{
			payload = null;
			return false;
		}
	}

	public static SaveMetadata BuildBaseMetadata()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		float radLinePosY = 0f;
		bool radLineActive = false;
		if ((Object)(object)RadiationLine.line != (Object)null)
		{
			radLinePosY = ((Component)RadiationLine.line).transform.position.y;
			radLineActive = RadiationLine.line.active;
		}
		return new SaveMetadata
		{
			SeedInput = SeedManager.InputString,
			SeedValue = SeedManager.CurrentSeed,
			PlayerPosition = new SerialVec3(((Component)PlayerCamera.main.body).transform.position),
			LocationName = WorldGeneration.world.TotalDepthString() + " Deep",
			Timestamp = DateTime.Now.ToString("g"),
			BiomeIndex = WorldGeneration.world.biomeDepth,
			RadLinePosY = radLinePosY,
			RadLineActive = radLineActive,
			IGTSeconds = TimerMenuPatcher.GetIGT(),
			RTASeconds = (float)TimerMenuPatcher.GetRTA().TotalSeconds,
			LoadedFromMenu = SaveSystemPatcher.LoadedFromMenu,
			SavedDuringMultiplayer = KrokoshaMpCompat.IsNetworkRunning,
			SaveKind = "Singleplayer"
		};
	}

	public static void ApplyMetadataForLoad(SaveMetadata meta)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (meta == null)
		{
			SeedManager.SetSeed("");
			SaveSystemPatcher.PendingLoadPosition = null;
			return;
		}
		if (string.IsNullOrEmpty(meta.SeedInput) && meta.SeedValue != 0)
		{
			SeedManager.IsSeeded = true;
			SeedManager.CurrentSeed = meta.SeedValue;
			SeedManager.InputString = "";
		}
		else
		{
			SeedManager.SetSeed(meta.SeedInput);
		}
		SaveSystemPatcher.PendingRadLineY = meta.RadLinePosY;
		SaveSystemPatcher.PendingRadLineActive = meta.RadLineActive;
		SaveSystemPatcher.PendingLoadPosition = meta.PlayerPosition.ToVector3();
		SaveSystemPatcher.PendingIGTSeconds = meta.IGTSeconds;
		SaveSystemPatcher.PendingRTASeconds = meta.RTASeconds;
		SaveSystemPatcher.LoadedFromMenu = true;
	}

	private static string CaptureLocalPlayerSnapshotJson()
	{
		string text = Path.Combine(Application.persistentDataPath, "save.sv");
		string text2 = Path.Combine(Application.persistentDataPath, "qol_mp_client_restore_backup.sv");
		bool flag = File.Exists(text);
		try
		{
			if (flag)
			{
				File.Copy(text, text2, overwrite: true);
			}
			using (KrokoshaMpCompat.BeginVanillaSaveSnapshot())
			{
				SaveSystem.SaveGame();
			}
			if (!File.Exists(text))
			{
				return null;
			}
			return SaveSystem.Unzip(File.ReadAllBytes(text));
		}
		finally
		{
			if (flag && File.Exists(text2))
			{
				File.Copy(text2, text, overwrite: true);
				File.Delete(text2);
			}
			else if (!flag && File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	private static void FillMpRulesMetadata(string rulesPath, SaveMetadata meta)
	{
		if (!File.Exists(rulesPath) || meta == null)
		{
			return;
		}
		try
		{
			JObject val = JObject.Parse(File.ReadAllText(rulesPath));
			meta.KrokoshaSaveId = ((object)val["SAVEID"])?.ToString();
			meta.KrokoshaMpVersion = ((object)val["MPVERSION"])?.ToString();
			meta.KrokoshaGameVersion = ((object)val["GAMEVERSION"])?.ToString();
		}
		catch
		{
		}
	}

	private static void WriteImportedSingleplayerRules(string rulesPath, SaveMetadata meta, string hostId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00a4: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		JObject val = new JObject
		{
			["hello"] = JToken.op_Implicit("QoL Unknown imported singleplayer slot for Krokosha MP host"),
			["MPVERSION"] = JToken.op_Implicit("3.0.0"),
			["GAMEVERSION"] = JToken.op_Implicit(Application.version),
			["SAVEID"] = JToken.op_Implicit(meta?.KrokoshaSaveId ?? "0"),
			["LEVEL"] = JToken.op_Implicit(false),
			["MODIFIER"] = JToken.op_Implicit(-1),
			["PLRPOS"] = (JToken)new JObject()
		};
		if (meta != null)
		{
			_ = meta.PlayerPosition;
			if (!string.IsNullOrEmpty(hostId))
			{
				val["PLRPOS"][(object)hostId] = (JToken)new JObject
				{
					["x"] = JToken.op_Implicit(meta.PlayerPosition.x),
					["y"] = JToken.op_Implicit(meta.PlayerPosition.y)
				};
			}
		}
		File.WriteAllText(rulesPath, ((JToken)val).ToString((Formatting)1, Array.Empty<JsonConverter>()));
	}

	private static void CopyDirectory(string source, string destination)
	{
		Directory.CreateDirectory(destination);
		string[] directories = Directory.GetDirectories(source, "*", SearchOption.AllDirectories);
		foreach (string text in directories)
		{
			Directory.CreateDirectory(text.Replace(source, destination));
		}
		string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
		foreach (string text2 in files)
		{
			File.Copy(text2, text2.Replace(source, destination), overwrite: true);
		}
	}

	private static void RemoveInventoryItems(Body body)
	{
		List<Item> list = (from item in ((Component)body).GetComponentsInChildren<Item>(true)
			where (Object)(object)item != (Object)null && ((Component)item).transform.IsChildOf(((Component)body).transform)
			select item).ToList();
		foreach (Item item in list)
		{
			Object.DestroyImmediate((Object)(object)((Component)item).gameObject);
		}
	}

	private static void ApplySavedItems(Body body, JObject save)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		JToken obj = save["items"];
		SavedItem[] array = (SavedItem[])(((object)((obj != null) ? obj.ToObject<SavedItem[]>() : null)) ?? ((object)new SavedItem[0]));
		JToken obj2 = save["itemComponents"];
		JArray val = (JArray)(object)((obj2 is JArray) ? obj2 : null);
		Item val4 = default(Item);
		for (int i = 0; i < array.Length; i++)
		{
			SavedItem val2 = array[i];
			Object obj3 = Object.Instantiate(Resources.Load(val2.id), ((Component)body).transform.position, Quaternion.identity);
			GameObject val3 = (GameObject)(object)((obj3 is GameObject) ? obj3 : null);
			if ((Object)(object)val3 == (Object)null || !val3.TryGetComponent<Item>(ref val4))
			{
				continue;
			}
			val4.condition = val2.condition;
			val4.favourited = val2.favourited;
			if (val2.slot >= 0)
			{
				if (body.HoldingItem(val2.slot))
				{
					Container component = ((Component)body.GetItem(val2.slot)).GetComponent<Container>();
					if ((Object)(object)component != (Object)null)
					{
						component.LoadItem(val4);
					}
				}
				else
				{
					body.PickUpItem(val4, val2.slot, true);
				}
			}
			else if (Object.op_Implicit((Object)(object)body.GetWearableBySlotID(val2.wearSlot)))
			{
				Container component2 = ((Component)body.GetWearableBySlotID(val2.wearSlot)).GetComponent<Container>();
				if ((Object)(object)component2 != (Object)null)
				{
					component2.LoadItem(val4);
				}
			}
			else
			{
				body.WearWearable(val4);
			}
			object components;
			if (val == null || i >= ((JContainer)val).Count)
			{
				components = null;
			}
			else
			{
				JToken obj4 = val[i];
				components = ((obj4 is JObject) ? obj4 : null);
			}
			ApplyItemComponents(val3, (JObject)components);
			KrokoshaMpCompat.RegisterAndSyncItemTree(val4);
		}
	}

	private static void ApplyItemComponents(GameObject gameObject, JObject components)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameObject == (Object)null || components == null)
		{
			return;
		}
		foreach (JProperty item in ((JToken)components).Children<JProperty>())
		{
			Type type = Type.GetType(item.Name);
			Component val = ((type == null) ? null : gameObject.GetComponent(type));
			JToken value = item.Value;
			JObject val2 = (JObject)(object)((value is JObject) ? value : null);
			if ((Object)(object)val == (Object)null || val2 == null)
			{
				continue;
			}
			foreach (JProperty item2 in ((JToken)val2).Children<JProperty>())
			{
				FieldInfo field = type.GetField(item2.Name);
				if (!(field == null))
				{
					field.SetValue(val, item2.Value.ToObject(field.FieldType));
				}
			}
		}
	}
}
