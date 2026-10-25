using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using QoL_Unknown.Images;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class SaveSaveMenuPatcher
{
	private static ManualLogSource Logger;

	private static GameObject pauseMenuRef;

	private static GameObject savePanelObj;

	private static SaveSlotListView slotList;

	private static PauseHandler pauseHandlerRef;

	internal static readonly Dictionary<int, Sprite> layerBackgrounds = new Dictionary<int, Sprite>();

	internal static readonly Dictionary<int, Sprite> layerIcons = new Dictionary<int, Sprite>();

	private static bool wasPausedByMenu = false;

	private static float previousTimeScale = 1f;

	private static bool initialized;

	private const float PANEL_WIDTH = 560f;

	private const float PANEL_HEIGHT = 360f;

	private const float PANEL_PAUSE_MENU_GAP = 24f;

	private const int LAYER_BACKGROUND_COUNT = 5;

	private static string SaveFolder => Application.persistentDataPath;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		if (!initialized && QoLModuleManager.IsEnabled("qol_save_menu"))
		{
			Logger = logger;
			SaveSlotRegistry.Logger = logger;
			LoadLayerImages();
			Harmony val = new Harmony("org.bepinex.plugins.qol.savesavemenu");
			val.PatchAll(typeof(SaveSaveMenuPatcher));
			initialized = true;
			SceneManager.sceneUnloaded += delegate
			{
				pauseMenuRef = null;
				savePanelObj = null;
				slotList = null;
				pauseHandlerRef = null;
				wasPausedByMenu = false;
				Time.timeScale = 1f;
			};
		}
	}

	private static void LoadLayerImages()
	{
		for (int i = 0; i < 5; i++)
		{
			Sprite val = Helper.LoadEmbeddedSprite($"layer{i}");
			if ((Object)(object)val != (Object)null && !layerBackgrounds.ContainsKey(i))
			{
				layerBackgrounds[i] = val;
			}
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "Start")]
	[HarmonyPostfix]
	private static void Start_Postfix(PlayerCamera __instance)
	{
		Transform val = ((Component)__instance).transform.Find("Canvas/PauseMenu");
		if ((Object)(object)val == (Object)null)
		{
			GameObject obj = GameObject.Find("Main Camera/Canvas/PauseMenu");
			val = ((obj != null) ? obj.transform : null);
		}
		if ((Object)(object)val == (Object)null)
		{
			Transform val2 = ((Component)__instance).transform.Find("Canvas");
			if ((Object)(object)val2 == (Object)null)
			{
				GameObject obj2 = GameObject.Find("Canvas");
				val2 = ((obj2 != null) ? obj2.transform : null);
			}
			val = ((val2 != null) ? val2.Find("PauseMenu") : null);
		}
		if ((Object)(object)val != (Object)null)
		{
			pauseMenuRef = ((Component)val).gameObject;
			pauseHandlerRef = pauseMenuRef.GetComponent<PauseHandler>() ?? PauseHandler.main;
			BuildSaveUI();
			return;
		}
		ManualLogSource logger = Logger;
		if (logger != null)
		{
			logger.LogWarning((object)"[SaveSaveMenuPatcher] PauseMenu not found; skipping save slot panel creation.");
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	private static void Update_Postfix(PlayerCamera __instance)
	{
		if ((Object)(object)pauseMenuRef == (Object)null)
		{
			return;
		}
		PauseHandler val = (((Object)(object)pauseHandlerRef != (Object)null) ? pauseHandlerRef : PauseHandler.main);
		bool flag = (((Object)(object)val != (Object)null) ? (val.pauseProgress > 0f) : pauseMenuRef.activeInHierarchy);
		if ((Object)(object)savePanelObj != (Object)null && savePanelObj.activeSelf != flag)
		{
			savePanelObj.SetActive(flag);
			if (flag)
			{
				try
				{
					RefreshSlots();
				}
				catch (Exception arg)
				{
					Logger.LogError((object)$"Error refreshing slots: {arg}");
				}
			}
		}
		if ((Object)(object)savePanelObj != (Object)null && flag)
		{
			UpdateSavePanelTransform(val);
		}
		if (flag)
		{
			if (!wasPausedByMenu && !MultiplayerCompat.IsActive)
			{
				previousTimeScale = Time.timeScale;
				Time.timeScale = 0f;
				wasPausedByMenu = true;
			}
		}
		else if (wasPausedByMenu)
		{
			Time.timeScale = previousTimeScale;
			wasPausedByMenu = false;
		}
	}

	private static void BuildSaveUI()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Expected O, but got Unknown
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Expected O, but got Unknown
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)savePanelObj != (Object)null)
		{
			return;
		}
		try
		{
			savePanelObj = new GameObject("SaveManagerPanel", new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			savePanelObj.transform.SetParent(pauseMenuRef.transform.parent, false);
			RectTransform component = savePanelObj.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.5f, 0.5f);
			component.anchorMax = new Vector2(0.5f, 0.5f);
			component.pivot = new Vector2(0f, 0.5f);
			component.sizeDelta = new Vector2(560f, 360f);
			UpdateSavePanelTransform(((Object)(object)pauseHandlerRef != (Object)null) ? pauseHandlerRef : PauseHandler.main);
			savePanelObj.transform.SetAsLastSibling();
			Image component2 = savePanelObj.GetComponent<Image>();
			component2.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
			component2.type = (Type)(((Object)(object)component2.sprite != (Object)null) ? 1 : 0);
			((Graphic)component2).color = new Color(0f, 0f, 0f, 0.92f);
			VerticalLayoutGroup val = savePanelObj.AddComponent<VerticalLayoutGroup>();
			((LayoutGroup)val).padding = new RectOffset(14, 14, 14, 14);
			((HorizontalOrVerticalLayoutGroup)val).spacing = 8f;
			((HorizontalOrVerticalLayoutGroup)val).childControlHeight = true;
			((HorizontalOrVerticalLayoutGroup)val).childControlWidth = true;
			((HorizontalOrVerticalLayoutGroup)val).childForceExpandHeight = false;
			((HorizontalOrVerticalLayoutGroup)val).childForceExpandWidth = true;
			TMP_FontAsset saveMenuFont = GetSaveMenuFont();
			Sprite uiSprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
			GameObject val2 = new GameObject("Header", new Type[2]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			val2.transform.SetParent(savePanelObj.transform, false);
			TextMeshProUGUI component3 = val2.GetComponent<TextMeshProUGUI>();
			((TMP_Text)component3).text = "SAVE SLOTS";
			if (Object.op_Implicit((Object)(object)saveMenuFont))
			{
				((TMP_Text)component3).font = saveMenuFont;
			}
			((TMP_Text)component3).fontSize = 24f;
			((TMP_Text)component3).alignment = (TextAlignmentOptions)514;
			((Graphic)component3).color = Color.yellow;
			LayoutElement val3 = val2.AddComponent<LayoutElement>();
			val3.minHeight = 38f;
			val3.preferredHeight = 38f;
			slotList = new SaveSlotListView(savePanelObj.transform, saveMenuFont, uiSprite, delegate(int slot)
			{
				DoSave(SaveSlotRegistry.FileName(slot), slot);
			}, delegate(int slot)
			{
				DoLoad(SaveSlotRegistry.FileName(slot), slot);
			}, delegate(int slot)
			{
				DoDelete(SaveSlotRegistry.FileName(slot), slot);
			});
			slotList.AddAutosaveRow("autosave.sv", delegate(int slot)
			{
				DoSave("autosave.sv", slot);
			}, delegate(int slot)
			{
				DoLoad("autosave.sv", slot);
			}, delegate(int slot)
			{
				DoDelete("autosave.sv", slot);
			});
			savePanelObj.SetActive(pauseMenuRef.activeInHierarchy);
		}
		catch (Exception arg)
		{
			Logger.LogError((object)$"Error building Save UI: {arg}");
		}
	}

	private static void UpdateSavePanelTransform(PauseHandler pauseHandler)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		RectTransform val = (((Object)(object)savePanelObj != (Object)null) ? savePanelObj.GetComponent<RectTransform>() : null);
		object obj;
		if (!((Object)(object)savePanelObj != (Object)null))
		{
			obj = null;
		}
		else
		{
			Transform parent = savePanelObj.transform.parent;
			obj = ((parent is RectTransform) ? parent : null);
		}
		RectTransform val2 = (RectTransform)obj;
		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
		{
			return;
		}
		RectTransform val3 = (((Object)(object)pauseHandler != (Object)null) ? pauseHandler.center : null);
		Vector2 val4;
		Rect rect;
		float num;
		if ((Object)(object)val3 != (Object)null)
		{
			val4 = Vector2.op_Implicit(((Transform)val2).InverseTransformPoint(((Transform)val3).position));
			rect = val3.rect;
			float width = ((Rect)(ref rect)).width;
			rect = val3.rect;
			num = Mathf.Max(width, ((Rect)(ref rect)).height) * 0.5f;
		}
		else
		{
			RectTransform val5 = (((Object)(object)pauseMenuRef != (Object)null) ? pauseMenuRef.GetComponent<RectTransform>() : null);
			val4 = (((Object)(object)val5 != (Object)null) ? val5.anchoredPosition : Vector2.zero);
			float num2;
			if (!((Object)(object)val5 != (Object)null))
			{
				num2 = 300f;
			}
			else
			{
				rect = val5.rect;
				float width2 = ((Rect)(ref rect)).width;
				rect = val5.rect;
				num2 = Mathf.Max(width2, ((Rect)(ref rect)).height) * 0.5f;
			}
			num = num2;
		}
		val.anchoredPosition = new Vector2(val4.x + num + 24f, val4.y);
		float num3 = (((Object)(object)pauseHandler != (Object)null) ? Mathf.Clamp01(pauseHandler.pauseProgress) : 1f);
		((Transform)val).localScale = Vector3.one * Utils.OutCubic(num3);
	}

	private static TMP_FontAsset GetSaveMenuFont()
	{
		TextMeshProUGUI[] componentsInChildren = pauseMenuRef.GetComponentsInChildren<TextMeshProUGUI>(true);
		TextMeshProUGUI[] array = componentsInChildren;
		foreach (TextMeshProUGUI val in array)
		{
			TMP_FontAsset val2 = (((Object)(object)val != (Object)null) ? ((TMP_Text)val).font : null);
			if ((Object)(object)val2 != (Object)null && ((Object)val2).name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return val2;
			}
		}
		return ResourceLookupCache.FindFontContains("Retro GamingPix") ?? ResourceLookupCache.FindFontContains("Retro Gaming") ?? FontPatcher.GetRetroPixelExtendedFont();
	}

	private static void DoSave(string filename, int index)
	{
		try
		{
			DoSaveCore(filename, index);
		}
		catch (Exception arg)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)$"Unexpected error while saving slot {index}: {arg}");
			}
			PlayerCamera main = PlayerCamera.main;
			if (main != null)
			{
				main.DoAlert("Save failed unexpectedly. Your previous slot data was left intact where possible.", false);
			}
			RefreshSlots();
		}
	}

	private static void DoSaveCore(string filename, int index)
	{
		if (!Object.op_Implicit((Object)(object)PlayerCamera.main) || !Object.op_Implicit((Object)(object)PlayerCamera.main.body))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)$"Cannot save slot {index}: player is not available.");
			}
			return;
		}
		if (!PlayerCamera.main.body.alive)
		{
			PlayerCamera.main.DoAlert("Cannot save while dead!", false);
			return;
		}
		if (!PlayerCamera.main.body.standing)
		{
			PlayerCamera.main.DoAlert("Cannot save while ragdolled!", false);
			return;
		}
		string text = Path.Combine(SaveFolder, "save.sv");
		string destinationPath = Path.Combine(SaveFolder, filename);
		string destinationPath2 = Path.Combine(SaveFolder, filename + ".meta");
		SaveMetadata saveMetadata = MultiplayerSaveBundle.BuildBaseMetadata();
		MultiplayerSaveBundle.DeleteSlotExtras(SaveFolder, filename);
		if (MultiplayerCompat.IsNetworkRunning && MultiplayerCompat.IsServer)
		{
			if (!MultiplayerSaveBundle.SaveHostBundle(SaveFolder, filename, saveMetadata, out var sourceRootSave) || !File.Exists(sourceRootSave))
			{
				PlayerCamera.main.DoAlert("Multiplayer save failed!", false);
				RefreshSlots();
			}
			else if (!SaveFileStore.TryCopyAtomic(sourceRootSave, destinationPath, Logger, $"save multiplayer slot {index}") || !SaveFileStore.TryWriteAllTextAtomic(destinationPath2, JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1), Logger, $"write metadata for slot {index}"))
			{
				PlayerCamera.main.DoAlert("Multiplayer save failed while writing the slot!", false);
				RefreshSlots();
			}
			else
			{
				PlayerCamera.main.DoAlert($"Saved MP host bundle to Slot {index}", false);
				RefreshSlots();
			}
			return;
		}
		if (MultiplayerCompat.IsNetworkRunning && MultiplayerCompat.IsClient)
		{
			if (!MultiplayerSaveBundle.SaveClientInventorySnapshot(SaveFolder, filename, saveMetadata, out var _))
			{
				PlayerCamera.main.DoAlert("Client snapshot save failed!", false);
				RefreshSlots();
				return;
			}
			using (MultiplayerCompat.BeginVanillaSaveSnapshot())
			{
				SaveSystem.SaveGame();
			}
			if (File.Exists(text) && !SaveFileStore.TryCopyAtomic(text, destinationPath, Logger, $"save client snapshot slot {index}"))
			{
				PlayerCamera.main.DoAlert("Client snapshot save failed while writing the slot!", false);
				RefreshSlots();
			}
			else if (!SaveFileStore.TryWriteAllTextAtomic(destinationPath2, JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1), Logger, $"write metadata for slot {index}"))
			{
				PlayerCamera.main.DoAlert("Client snapshot metadata could not be saved!", false);
				RefreshSlots();
			}
			else
			{
				PlayerCamera.main.DoAlert($"Saved MP client snapshot to Slot {index}", false);
				RefreshSlots();
			}
			return;
		}
		using (MultiplayerCompat.BeginVanillaSaveSnapshot())
		{
			SaveSystem.SaveGame();
		}
		if (File.Exists(text))
		{
			if (!SaveFileStore.TryCopyAtomic(text, destinationPath, Logger, $"save slot {index}"))
			{
				PlayerCamera.main.DoAlert("Save failed while writing the slot!", false);
				RefreshSlots();
				return;
			}
			saveMetadata.SaveKind = "Singleplayer";
			saveMetadata.SavedDuringMultiplayer = false;
			string contents = JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1);
			if (!SaveFileStore.TryWriteAllTextAtomic(destinationPath2, contents, Logger, $"write metadata for slot {index}"))
			{
				PlayerCamera.main.DoAlert("Save data was written, but its metadata could not be saved.", false);
				RefreshSlots();
				return;
			}
			PlayerCamera.main.DoAlert($"Saved to Slot {index}", false);
		}
		else
		{
			PlayerCamera.main.DoAlert("Save failed!", false);
		}
		RefreshSlots();
	}

	public static void DoLoad(string filename, int index)
	{
		try
		{
			DoLoadCore(filename, index);
		}
		catch (Exception arg)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)$"Unexpected error while loading slot {index}: {arg}");
			}
			PlayerCamera main = PlayerCamera.main;
			if (main != null)
			{
				main.DoAlert("Load failed unexpectedly. The current save was left intact where possible.", false);
			}
			SaveSystem.loadedRun = false;
		}
	}

	private static void DoLoadCore(string filename, int index)
	{
		string text = Path.Combine(SaveFolder, filename);
		string path = Path.Combine(SaveFolder, filename + ".meta");
		string destinationPath = Path.Combine(SaveFolder, "save.sv");
		if (!File.Exists(text))
		{
			if ((Object)(object)PlayerCamera.main != (Object)null)
			{
				PlayerCamera.main.DoAlert("Slot is empty!", false);
			}
			return;
		}
		if (SaveMenuPatcher.ShouldBlockQuickLoad(filename))
		{
			MultiplayerCompat.NotifyLoadBlocked();
			return;
		}
		SaveMetadata saveMetadata = null;
		if (File.Exists(path))
		{
			try
			{
				string text2 = File.ReadAllText(path);
				saveMetadata = JsonConvert.DeserializeObject<SaveMetadata>(text2);
			}
			catch (Exception arg)
			{
				Logger.LogError((object)$"Meta Load Error: {arg}");
			}
		}
		bool flag = saveMetadata != null && saveMetadata.SaveKind == "MultiplayerHostBundle" && MultiplayerSaveBundle.HasBundle(SaveFolder, filename);
		bool flag2 = saveMetadata != null && saveMetadata.SaveKind == "MultiplayerClientInventory";
		if (MultiplayerCompat.IsNetworkRunning && MultiplayerCompat.IsClient)
		{
			if (flag2)
			{
				if (MultiplayerSaveBundle.TryRequestClientInventoryRestore(SaveFolder, filename, out var message) && !string.IsNullOrWhiteSpace(message))
				{
					PlayerCamera main = PlayerCamera.main;
					if (main != null)
					{
						main.DoAlert(message, false);
					}
				}
				else if (!string.IsNullOrWhiteSpace(message))
				{
					PlayerCamera main2 = PlayerCamera.main;
					if (main2 != null)
					{
						main2.DoAlert(message, false);
					}
				}
			}
			else
			{
				PlayerCamera main3 = PlayerCamera.main;
				if (main3 != null)
				{
					main3.DoAlert("Clients can only restore MP item snapshots.", false);
				}
			}
			return;
		}
		if (MultiplayerCompat.IsNetworkRunning && MultiplayerCompat.IsServer && flag)
		{
			BeginHostMultiplayerLoad(filename, saveMetadata, restoreHostBundle: true);
			return;
		}
		bool flag3 = saveMetadata == null || string.IsNullOrEmpty(saveMetadata.SaveKind) || saveMetadata.SaveKind == "Singleplayer";
		if (MultiplayerCompat.CanImportSingleplayerSlotAsHostStart && flag3)
		{
			BeginHostMultiplayerLoad(filename, saveMetadata, restoreHostBundle: false);
			return;
		}
		if (MultiplayerCompat.ShouldBlockQoLLoads && !flag && !flag2)
		{
			MultiplayerCompat.NotifyLoadBlocked();
			return;
		}
		if (saveMetadata != null)
		{
			MultiplayerSaveBundle.ApplyMetadataForLoad(saveMetadata);
		}
		else
		{
			SeedManager.SetSeed("");
			SaveSystemPatcher.PendingLoadPosition = null;
		}
		if (saveMetadata != null && saveMetadata.SavedDuringMultiplayer)
		{
			MultiplayerSaveLoadPatcher.MarkMultiplayerSlotLoad();
		}
		SaveSystemPatcher.TryRestoreRunSettingsFromSave(text);
		if (!SaveFileStore.TryCopyAtomic(text, destinationPath, Logger, $"stage slot {index} for loading"))
		{
			PlayerCamera main4 = PlayerCamera.main;
			if (main4 != null)
			{
				main4.DoAlert("Could not stage this save for loading. The existing save was left untouched.", false);
			}
		}
		else
		{
			SaveSystem.loadedRun = true;
			Time.timeScale = 1f;
			SceneManager.LoadScene("SampleScene");
		}
	}

	private static void BeginHostMultiplayerLoad(string filename, SaveMetadata meta, bool restoreHostBundle)
	{
		if (!(restoreHostBundle ? MultiplayerSaveBundle.RestoreHostBundleToMultiplayerSave(SaveFolder, filename) : MultiplayerSaveBundle.StageSingleplayerSlotAsHostBundle(SaveFolder, filename, meta)))
		{
			PlayerCamera main = PlayerCamera.main;
			if (main != null)
			{
				main.DoAlert(restoreHostBundle ? "MP save bundle is missing or corrupt." : "Could not stage QoL save for multiplayer host.", false);
			}
		}
		else
		{
			SharedCoroutineStarter.StartCoroutine(HostMultiplayerLoadThroughMainMenu(meta));
		}
	}

	private static IEnumerator HostMultiplayerLoadThroughMainMenu(SaveMetadata meta)
	{
		if ((Object)(object)WorldGeneration.world != (Object)null && (Object)(object)PlayerCamera.main != (Object)null)
		{
			Time.timeScale = 1f;
			PlayerCamera.main.ToMainMenu();
			yield return (object)new WaitUntil((Func<bool>)(() => (Object)(object)PreRunScript.instance != (Object)null && (Object)(object)WorldGeneration.world == (Object)null));
			yield return (object)new WaitForSecondsRealtime(1f);
		}
		if (meta != null)
		{
			MultiplayerSaveBundle.ApplyMetadataForLoad(meta);
		}
		else
		{
			SeedManager.SetSeed("");
			SaveSystemPatcher.PendingLoadPosition = null;
		}
		MultiplayerSaveLoadPatcher.MarkMultiplayerSlotLoad();
		Time.timeScale = 1f;
		PreRunScript.instance.LoadRun();
	}

	private static void DoDelete(string filename, int index)
	{
		bool flag = !File.Exists(Path.Combine(SaveFolder, filename));
		SaveManager.DeleteSave(filename);
		try
		{
			MultiplayerSaveBundle.DeleteSlotExtras(SaveFolder, filename);
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)$"Deleted slot {index}, but could not remove all multiplayer extras: {ex.Message}");
			}
		}
		if (flag && index > 0)
		{
			SaveSlotRegistry.TryRemoveEmptySlot(index);
		}
		RefreshSlots();
	}

	private static void RefreshSlots()
	{
		slotList?.Refresh();
	}
}
