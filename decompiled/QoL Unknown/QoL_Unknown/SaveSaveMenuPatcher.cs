using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using QoL_Unknown.Images;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class SaveSaveMenuPatcher
{
	private static ManualLogSource Logger;

	private static GameObject pauseMenuRef;

	private static GameObject savePanelObj;

	private static Transform slotContainer;

	private static PauseHandler pauseHandlerRef;

	public static Sprite[] layerBackgrounds;

	private static bool wasPausedByMenu = false;

	private static float previousTimeScale = 1f;

	private static bool initialized;

	private const int SLOT_COUNT = 3;

	private const float PANEL_WIDTH = 560f;

	private const float PANEL_HEIGHT = 360f;

	private const float PANEL_PAUSE_MENU_GAP = 24f;

	private static string SaveFolder => Application.persistentDataPath;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		if (!initialized && QoLModuleManager.IsEnabled("qol_save_menu"))
		{
			initialized = true;
			Logger = logger;
			LoadLayerImages();
			Harmony val = new Harmony("org.bepinex.plugins.qol.savesavemenu");
			val.PatchAll(typeof(SaveSaveMenuPatcher));
			Logger.LogInfo((object)"SaveSaveMenuPatcher initialized");
			SceneManager.sceneUnloaded += delegate
			{
				pauseMenuRef = null;
				savePanelObj = null;
				slotContainer = null;
				pauseHandlerRef = null;
				wasPausedByMenu = false;
				Time.timeScale = 1f;
			};
		}
	}

	private static void LoadLayerImages()
	{
		layerBackgrounds = (Sprite[])(object)new Sprite[5];
		for (int i = 0; i < 5; i++)
		{
			layerBackgrounds[i] = Helper.LoadEmbeddedSprite($"layer{i}");
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
			if (!wasPausedByMenu)
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
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Expected O, but got Unknown
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
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
			slotContainer = savePanelObj.transform;
			TMP_FontAsset saveMenuFont = GetSaveMenuFont();
			Sprite uiSprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
			GameObject val2 = new GameObject("Header", new Type[2]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			val2.transform.SetParent(slotContainer, false);
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
			for (int i = 1; i <= 3; i++)
			{
				CreateSlot(i, saveMenuFont, uiSprite);
			}
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

	private static void CreateSlot(int index, TMP_FontAsset font, Sprite uiSprite)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Expected O, but got Unknown
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Expected O, but got Unknown
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Expected O, but got Unknown
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Expected O, but got Unknown
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Expected O, but got Unknown
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Expected O, but got Unknown
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Expected O, but got Unknown
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Expected O, but got Unknown
		string text = $"slot_{index}.sv";
		string fName = text;
		int i = index;
		GameObject val = new GameObject($"Slot_{index}", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(RectMask2D)
		});
		val.transform.SetParent(slotContainer, false);
		Image component = val.GetComponent<Image>();
		((Graphic)component).color = new Color(0f, 0f, 0f, 0f);
		LayoutElement val2 = val.AddComponent<LayoutElement>();
		val2.minHeight = 80f;
		val2.preferredHeight = 80f;
		GameObject val3 = new GameObject("Background", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(AspectRatioFitter)
		});
		val3.transform.SetParent(val.transform, false);
		RectTransform component2 = val3.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.sizeDelta = Vector2.zero;
		Image component3 = val3.GetComponent<Image>();
		if (Object.op_Implicit((Object)(object)uiSprite))
		{
			component3.sprite = uiSprite;
		}
		component3.type = (Type)1;
		((Graphic)component3).color = new Color(0f, 0f, 0f, 0.5f);
		AspectRatioFitter component4 = val3.GetComponent<AspectRatioFitter>();
		component4.aspectMode = (AspectMode)4;
		((Behaviour)component4).enabled = false;
		GameObject val4 = new GameObject("Dimmer", new Type[2]
		{
			typeof(RectTransform),
			typeof(Image)
		});
		val4.transform.SetParent(val.transform, false);
		RectTransform component5 = val4.GetComponent<RectTransform>();
		component5.anchorMin = Vector2.zero;
		component5.anchorMax = Vector2.one;
		component5.sizeDelta = Vector2.zero;
		((Graphic)val4.GetComponent<Image>()).color = new Color(0f, 0f, 0f, 0.6f);
		GameObject val5 = new GameObject("Content", new Type[1] { typeof(RectTransform) });
		val5.transform.SetParent(val.transform, false);
		RectTransform component6 = val5.GetComponent<RectTransform>();
		component6.anchorMin = Vector2.zero;
		component6.anchorMax = Vector2.one;
		component6.sizeDelta = Vector2.zero;
		HorizontalLayoutGroup val6 = val5.AddComponent<HorizontalLayoutGroup>();
		((LayoutGroup)val6).padding = new RectOffset(10, 5, 5, 5);
		((HorizontalOrVerticalLayoutGroup)val6).spacing = 10f;
		((HorizontalOrVerticalLayoutGroup)val6).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)val6).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)val6).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)val6).childForceExpandHeight = false;
		((LayoutGroup)val6).childAlignment = (TextAnchor)3;
		GameObject val7 = new GameObject("Info", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val7.transform.SetParent(val5.transform, false);
		TextMeshProUGUI component7 = val7.GetComponent<TextMeshProUGUI>();
		if (Object.op_Implicit((Object)(object)font))
		{
			((TMP_Text)component7).font = font;
		}
		((TMP_Text)component7).fontSize = 18f;
		((Graphic)component7).color = Color.white;
		((TMP_Text)component7).alignment = (TextAlignmentOptions)4097;
		((TMP_Text)component7).richText = true;
		((TMP_Text)component7).text = $"Slot {index}";
		LayoutElement val8 = val7.AddComponent<LayoutElement>();
		val8.minWidth = 320f;
		val8.flexibleWidth = 1f;
		GameObject val9 = new GameObject("Buttons", new Type[1] { typeof(RectTransform) });
		val9.transform.SetParent(val5.transform, false);
		HorizontalLayoutGroup val10 = val9.AddComponent<HorizontalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)val10).spacing = 5f;
		((LayoutGroup)val10).childAlignment = (TextAnchor)5;
		((HorizontalOrVerticalLayoutGroup)val10).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)val10).childControlWidth = false;
		((HorizontalOrVerticalLayoutGroup)val10).childControlHeight = false;
		LayoutElement val11 = val9.AddComponent<LayoutElement>();
		val11.minWidth = 118f;
		val11.preferredWidth = 118f;
		CreateIconButton(val9.transform, "save", (UnityAction)delegate
		{
			DoSave(fName, i);
		});
		CreateIconButton(val9.transform, "load", (UnityAction)delegate
		{
			DoLoad(fName, i);
		});
		CreateIconButton(val9.transform, "delete", (UnityAction)delegate
		{
			DoDelete(fName, i);
		});
	}

	private static void CreateIconButton(Transform parent, string spriteName, UnityAction action)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Btn_" + spriteName, new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		float num = 36f;
		RectTransform component = val.GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(num, num);
		LayoutElement val2 = val.AddComponent<LayoutElement>();
		val2.minWidth = num;
		val2.minHeight = num;
		val2.preferredWidth = num;
		val2.preferredHeight = num;
		Image component2 = val.GetComponent<Image>();
		component2.sprite = Helper.LoadEmbeddedSprite(spriteName);
		component2.preserveAspect = true;
		((Graphic)component2).color = Color.white;
		Button component3 = val.GetComponent<Button>();
		ColorBlock colors = ((Selectable)component3).colors;
		((ColorBlock)(ref colors)).normalColor = Color.white;
		((ColorBlock)(ref colors)).highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		((ColorBlock)(ref colors)).pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
		((ColorBlock)(ref colors)).colorMultiplier = 1f;
		((Selectable)component3).colors = colors;
		((UnityEvent)component3.onClick).AddListener(action);
	}

	private static void DoSave(string filename, int index)
	{
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
		string destFileName = Path.Combine(SaveFolder, filename);
		string path = Path.Combine(SaveFolder, filename + ".meta");
		SaveMetadata saveMetadata = KrokoshaMpSaveBundle.BuildBaseMetadata();
		KrokoshaMpSaveBundle.DeleteSlotExtras(SaveFolder, filename);
		if (KrokoshaMpCompat.IsNetworkRunning && KrokoshaMpCompat.IsServer)
		{
			if (!KrokoshaMpSaveBundle.SaveHostBundle(SaveFolder, filename, saveMetadata, out var sourceRootSave) || !File.Exists(sourceRootSave))
			{
				PlayerCamera.main.DoAlert("Multiplayer save failed!", false);
				RefreshSlots();
				return;
			}
			File.Copy(sourceRootSave, destFileName, overwrite: true);
			File.WriteAllText(path, JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1));
			PlayerCamera.main.DoAlert($"Saved MP host bundle to Slot {index}", false);
			RefreshSlots();
		}
		else if (KrokoshaMpCompat.IsNetworkRunning && KrokoshaMpCompat.IsClient)
		{
			if (!KrokoshaMpSaveBundle.SaveClientInventorySnapshot(SaveFolder, filename, saveMetadata, out var _))
			{
				PlayerCamera.main.DoAlert("Client snapshot save failed!", false);
				RefreshSlots();
				return;
			}
			using (KrokoshaMpCompat.BeginVanillaSaveSnapshot())
			{
				SaveSystem.SaveGame();
			}
			if (File.Exists(text))
			{
				File.Copy(text, destFileName, overwrite: true);
			}
			File.WriteAllText(path, JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1));
			PlayerCamera.main.DoAlert($"Saved MP client snapshot to Slot {index}", false);
			RefreshSlots();
		}
		else
		{
			using (KrokoshaMpCompat.BeginVanillaSaveSnapshot())
			{
				SaveSystem.SaveGame();
			}
			if (File.Exists(text))
			{
				File.Copy(text, destFileName, overwrite: true);
				saveMetadata.SaveKind = "Singleplayer";
				saveMetadata.SavedDuringMultiplayer = false;
				string contents = JsonConvert.SerializeObject((object)saveMetadata, (Formatting)1);
				File.WriteAllText(path, contents);
				PlayerCamera.main.DoAlert($"Saved to Slot {index}", false);
			}
			else
			{
				PlayerCamera.main.DoAlert("Save failed!", false);
			}
			RefreshSlots();
		}
	}

	public static void DoLoad(string filename, int index)
	{
		string text = Path.Combine(SaveFolder, filename);
		string path = Path.Combine(SaveFolder, filename + ".meta");
		string destFileName = Path.Combine(SaveFolder, "save.sv");
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
			KrokoshaMpCompat.NotifyLoadBlocked();
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
		bool flag = saveMetadata != null && saveMetadata.SaveKind == "MultiplayerHostBundle" && KrokoshaMpSaveBundle.HasBundle(SaveFolder, filename);
		bool flag2 = saveMetadata != null && saveMetadata.SaveKind == "MultiplayerClientInventory";
		if (KrokoshaMpCompat.IsNetworkRunning && KrokoshaMpCompat.IsClient)
		{
			if (flag2)
			{
				if (KrokoshaMpSaveBundle.TryRequestClientInventoryRestore(SaveFolder, filename, out var message) && !string.IsNullOrWhiteSpace(message))
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
		if (KrokoshaMpCompat.IsNetworkRunning && KrokoshaMpCompat.IsServer && flag)
		{
			BeginHostMultiplayerLoad(filename, saveMetadata, restoreHostBundle: true);
			return;
		}
		bool flag3 = saveMetadata == null || string.IsNullOrEmpty(saveMetadata.SaveKind) || saveMetadata.SaveKind == "Singleplayer";
		if (KrokoshaMpCompat.CanImportSingleplayerSlotAsHostStart && flag3)
		{
			BeginHostMultiplayerLoad(filename, saveMetadata, restoreHostBundle: false);
			return;
		}
		if (KrokoshaMpCompat.ShouldBlockQoLLoads && !flag && !flag2)
		{
			KrokoshaMpCompat.NotifyLoadBlocked();
			return;
		}
		if (saveMetadata != null)
		{
			KrokoshaMpSaveBundle.ApplyMetadataForLoad(saveMetadata);
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
		File.Copy(text, destFileName, overwrite: true);
		SaveSystem.loadedRun = true;
		Time.timeScale = 1f;
		SceneManager.LoadScene("SampleScene");
	}

	private static void BeginHostMultiplayerLoad(string filename, SaveMetadata meta, bool restoreHostBundle)
	{
		bool flag = KrokoshaMpCompat.IsNetworkRunning && KrokoshaMpCompat.IsServer;
		int num = (flag ? KrokoshaMpCompat.PrepareClientsForHostSaveLoad() : 0);
		if (num > 0)
		{
			PlayerCamera main = PlayerCamera.main;
			if (main != null)
			{
				main.DoAlert(string.Format("Returning {0} client{1} to the main menu before host save load.", num, (num == 1) ? "" : "s"), false);
			}
		}
		if (flag)
		{
			KrokoshaMpCompat.BeginConnectedHostSaveLoad();
		}
		SharedCoroutineStarter.StartCoroutine(CompleteHostMultiplayerLoadAfterClientReturn(filename, meta, restoreHostBundle, num > 0, flag));
	}

	private static IEnumerator CompleteHostMultiplayerLoadAfterClientReturn(string filename, SaveMetadata meta, bool restoreHostBundle, bool waitForClients, bool keepClientsConnected)
	{
		if (waitForClients)
		{
			yield return (object)new WaitForSecondsRealtime(0.75f);
		}
		if (restoreHostBundle)
		{
			if (!KrokoshaMpSaveBundle.RestoreHostBundleToKrokoshaMpSave(SaveFolder, filename))
			{
				KrokoshaMpCompat.CancelConnectedHostSaveLoad();
				PlayerCamera main = PlayerCamera.main;
				if (main != null)
				{
					main.DoAlert("MP save bundle is missing or corrupt.", false);
				}
				yield break;
			}
			KrokoshaMpSaveBundle.ApplyMetadataForLoad(meta);
		}
		else
		{
			if (!KrokoshaMpSaveBundle.StageSingleplayerSlotAsHostBundle(SaveFolder, filename, meta))
			{
				KrokoshaMpCompat.CancelConnectedHostSaveLoad();
				PlayerCamera main2 = PlayerCamera.main;
				if (main2 != null)
				{
					main2.DoAlert("Could not stage QoL save for multiplayer host.", false);
				}
				yield break;
			}
			if (meta != null)
			{
				KrokoshaMpSaveBundle.ApplyMetadataForLoad(meta);
			}
			else
			{
				SeedManager.SetSeed("");
				SaveSystemPatcher.PendingLoadPosition = null;
			}
		}
		MultiplayerSaveLoadPatcher.MarkMultiplayerSlotLoad();
		if (keepClientsConnected)
		{
			KrokoshaMpCompat.PreparePostWorldgenClientInventoryRestores();
		}
		SaveSystem.loadedRun = true;
		Time.timeScale = 1f;
		if (keepClientsConnected)
		{
			KrokoshaMpCompat.AnnounceHostSaveLoadRestart();
		}
		SceneManager.LoadScene("SampleScene");
	}

	private static void DoDelete(string filename, int index)
	{
		string path = Path.Combine(SaveFolder, filename);
		string path2 = Path.Combine(SaveFolder, filename + ".meta");
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		if (File.Exists(path2))
		{
			File.Delete(path2);
		}
		KrokoshaMpSaveBundle.DeleteSlotExtras(SaveFolder, filename);
		RefreshSlots();
	}

	private static void RefreshSlots()
	{
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_0437: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0515: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)slotContainer == (Object)null)
		{
			return;
		}
		Sprite val = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		for (int i = 1; i < slotContainer.childCount; i++)
		{
			Transform child = slotContainer.GetChild(i);
			Transform val2 = child.Find("Background");
			Image val3 = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponent<Image>() : ((Component)child).GetComponent<Image>());
			AspectRatioFitter val4 = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponent<AspectRatioFitter>() : null);
			Transform val5 = child.Find("Content");
			if ((Object)(object)val5 == (Object)null)
			{
				continue;
			}
			Transform val6 = val5.Find("Info");
			if ((Object)(object)val6 == (Object)null)
			{
				continue;
			}
			TextMeshProUGUI component = ((Component)val6).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component == (Object)null)
			{
				continue;
			}
			string text = $"slot_{i}.sv";
			string text2 = Path.Combine(SaveFolder, text);
			string path = Path.Combine(SaveFolder, text + ".meta");
			if (File.Exists(text2))
			{
				string text3 = "Unknown";
				string text4 = "";
				string text5 = "";
				string text6 = "";
				int num = -1;
				string text7 = string.Empty;
				try
				{
					byte[] array = File.ReadAllBytes(text2);
					if (array.Length != 0)
					{
						string input = SaveSystem.Unzip(array);
						Match match = Regex.Match(input, "\"runTime\":\\s*([0-9.]+)");
						if (match.Success && float.TryParse(match.Groups[1].Value, out var result))
						{
							TimeSpan timeSpan = TimeSpan.FromSeconds(result);
							text6 = $"{timeSpan:hh\\:mm\\:ss}";
						}
					}
				}
				catch
				{
				}
				if (File.Exists(path))
				{
					try
					{
						SaveMetadata saveMetadata = JsonConvert.DeserializeObject<SaveMetadata>(File.ReadAllText(path));
						text3 = saveMetadata.LocationName;
						text5 = saveMetadata.Timestamp;
						num = saveMetadata.BiomeIndex;
						bool loadedFromMenu = saveMetadata.LoadedFromMenu;
						if (!string.IsNullOrWhiteSpace(saveMetadata.SeedInput))
						{
							text4 = "<color=yellow>[ID: " + saveMetadata.SeedInput.Trim() + "]</color> ";
						}
						else if (saveMetadata.SeedValue != 0)
						{
							text4 = $"<color=yellow>[ID: {saveMetadata.SeedValue}]</color> ";
						}
						if (saveMetadata.SaveKind == "MultiplayerHostBundle")
						{
							text4 += "<color=#66ccff>[MP Host]</color> ";
						}
						else if (saveMetadata.SaveKind == "MultiplayerClientInventory")
						{
							text4 += "<color=#66ccff>[MP Client]</color> ";
						}
						else if (saveMetadata.SavedDuringMultiplayer)
						{
							text4 += "<color=#66ccff>[MP]</color> ";
						}
						text7 = (loadedFromMenu ? "*" : string.Empty);
					}
					catch
					{
					}
				}
				else
				{
					try
					{
						byte[] array2 = File.ReadAllBytes(text2);
						if (array2.Length != 0)
						{
							string input2 = SaveSystem.Unzip(array2);
							Match match2 = Regex.Match(input2, "\"totalTraveled\":\\s*(\\d+)");
							if (match2.Success)
							{
								text3 = match2.Groups[1].Value + "m deep";
							}
						}
						text5 = new FileInfo(text2).LastWriteTime.ToString("g");
					}
					catch
					{
						text3 = "Corrupt";
					}
				}
				bool flag = false;
				if (num >= 0 && layerBackgrounds != null && num < layerBackgrounds.Length && (Object)(object)layerBackgrounds[num] != (Object)null)
				{
					val3.sprite = layerBackgrounds[num];
					val3.type = (Type)0;
					((Graphic)val3).color = Color.white;
					if (Object.op_Implicit((Object)(object)val4))
					{
						((Behaviour)val4).enabled = true;
						if ((Object)(object)val3.sprite != (Object)null)
						{
							Rect rect = val3.sprite.rect;
							if (((Rect)(ref rect)).height > 0f)
							{
								rect = val3.sprite.rect;
								float width = ((Rect)(ref rect)).width;
								rect = val3.sprite.rect;
								val4.aspectRatio = width / ((Rect)(ref rect)).height;
							}
						}
					}
					flag = true;
				}
				if (!flag)
				{
					if (Object.op_Implicit((Object)(object)val))
					{
						val3.sprite = val;
					}
					val3.type = (Type)1;
					if (Object.op_Implicit((Object)(object)val4))
					{
						((Behaviour)val4).enabled = false;
					}
				}
				((TMP_Text)component).text = $"<b>Slot {i}{text7}</b> {text4}<size=70%>\n{text3} | {text6} run time</size>\n<size=60%><color=grey>{text5}</color></size>";
			}
			else
			{
				if (Object.op_Implicit((Object)(object)val))
				{
					val3.sprite = val;
				}
				val3.type = (Type)1;
				((Graphic)val3).color = new Color(0f, 0f, 0f, 0.5f);
				if (Object.op_Implicit((Object)(object)val4))
				{
					((Behaviour)val4).enabled = false;
				}
				((TMP_Text)component).text = $"<b>Slot {i}</b>\n<size=70%><color=grey>Empty</color></size>";
			}
		}
	}
}
