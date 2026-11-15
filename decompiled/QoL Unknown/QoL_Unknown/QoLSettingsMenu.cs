using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using QoL_Unknown.Images;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class QoLSettingsMenu
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static UnityAction _003C_003E9__13_0;

		public static Func<TextMeshProUGUI, bool> _003C_003E9__27_0;

		internal void _003CBuildSettingsUI_003Eb__13_0()
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogInfo((object)"Button clicked!");
			}
			EnhancedMenuController.Instance.OpenMenu();
		}

		internal bool _003CTrySetModdedBetaBuildText_003Eb__27_0(TextMeshProUGUI text)
		{
			if ((Object)(object)text != (Object)null)
			{
				string text2 = ((TMP_Text)text).text;
				if (text2 == null || !text2.StartsWith("This is a beta build (", StringComparison.Ordinal))
				{
					return ((TMP_Text)text).text?.StartsWith("This is a modded beta build (", StringComparison.Ordinal) ?? false;
				}
				return true;
			}
			return false;
		}
	}

	private static ManualLogSource Logger;

	public static HashSet<string> _ModulesChanged = new HashSet<string>();

	private const string ModdedInsertToken = "This is a ";

	private const string FooterDefaultText = "Made by Orsoniks\nLoading art by Wickflix\nMenu art by @GatoCataco ~";

	private static GameObject geofruitOptionsList;

	private static int geofruitOptionsLookupFrame = -1;

	private static readonly HashSet<string> GeofruitBlacklist = new HashSet<string> { "qol_ui_ctrlitemlabels", "qol_game_modifiers", "qol_ui_scroll", "qol_craft_uncrafted" };

	internal static UnityAction QoLToggleAction;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.settingsmenu").PatchAll(typeof(QoLSettingsMenu));
	}

	[HarmonyPatch(typeof(PreRunScript), "Start")]
	[HarmonyPostfix]
	private static void Start_Postfix()
	{
		SharedCoroutineStarter.StartCoroutine(SetupCustomMenu());
		SharedCoroutineStarter.StartCoroutine(ApplyModdedBetaBuildText());
	}

	[HarmonyPatch(typeof(PlayerCamera), "Start")]
	[HarmonyPostfix]
	private static void PlayerCamera_Start_Postfix(PlayerCamera __instance)
	{
		BuildSettingsUI();
	}

	[HarmonyPatch(typeof(WorldGeneration), "DisableAllChunks")]
	[HarmonyPostfix]
	private static void WorldGeneration_DisableAllChunks_Postfix()
	{
		if (_ModulesChanged.Count > 1)
		{
			SharedCoroutineStarter.StartCoroutine(ShowRestartAlert());
			_ModulesChanged.Clear();
		}
	}

	private static IEnumerator ShowRestartAlert()
	{
		yield return (object)new WaitForSeconds(2f);
		while ((Object)(object)PlayerCamera.main == (Object)null)
		{
			yield return (object)new WaitForSeconds(3f);
		}
		PlayerCamera.main.DoAlert("<color=#54a5dc>Please restart the game for QoL module settings changes to take effect!", false);
	}

	private static void BuildSettingsUI()
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Expected O, but got Unknown
		if ((Object)(object)PreRunScript.instance == (Object)null || (Object)(object)PreRunScript.instance.mainCanvas == (Object)null)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogInfo((object)"Looks like we're starting a run. Good luck!");
			}
		}
		else
		{
			if ((Object)(object)GameObject.Find("EnhancedSettingsButton") != (Object)null)
			{
				return;
			}
			Canvas mainCanvas = PreRunScript.instance.mainCanvas;
			Button componentInChildren = ((Component)mainCanvas).GetComponentInChildren<Button>(true);
			if ((Object)(object)componentInChildren == (Object)null)
			{
				ManualLogSource logger2 = Logger;
				if (logger2 != null)
				{
					logger2.LogWarning((object)"BuildSettingsUI: No button found on canvas to clone");
				}
				return;
			}
			ManualLogSource logger3 = Logger;
			if (logger3 != null)
			{
				logger3.LogInfo((object)("BuildSettingsUI: Cloning button from " + ((Object)((Component)componentInChildren).gameObject).name));
			}
			GameObject obj = Object.Instantiate<GameObject>(((Component)componentInChildren).gameObject, ((Component)mainCanvas).transform);
			((Object)obj).name = "EnhancedSettingsButton";
			RectTransform component = obj.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0f, 1f);
			component.anchorMax = new Vector2(0f, 1f);
			component.pivot = new Vector2(0.5f, 1f);
			component.anchoredPosition = new Vector2(150f, -100f);
			component.sizeDelta = new Vector2(200f, 60f);
			Button component2 = obj.GetComponent<Button>();
			component2.onClick = new ButtonClickedEvent();
			ButtonClickedEvent onClick = component2.onClick;
			object obj2 = _003C_003Ec._003C_003E9__13_0;
			if (obj2 == null)
			{
				UnityAction val = delegate
				{
					ManualLogSource logger4 = Logger;
					if (logger4 != null)
					{
						logger4.LogInfo((object)"Button clicked!");
					}
					EnhancedMenuController.Instance.OpenMenu();
				};
				_003C_003Ec._003C_003E9__13_0 = val;
				obj2 = (object)val;
			}
			((UnityEvent)onClick).AddListener((UnityAction)obj2);
			TextMeshProUGUI componentInChildren2 = obj.GetComponentInChildren<TextMeshProUGUI>(true);
			if ((Object)(object)componentInChildren2 != (Object)null)
			{
				Component component3 = ((Component)componentInChildren2).GetComponent("UILocalizer");
				if ((Object)(object)component3 != (Object)null)
				{
					Object.Destroy((Object)(object)component3);
				}
				((TMP_Text)componentInChildren2).text = "OPTION\nMENU";
			}
		}
	}

	private static IEnumerator SetupCustomMenu()
	{
		Canvas canvas = (((Object)(object)PreRunScript.instance != (Object)null) ? PreRunScript.instance.mainCanvas : null);
		if ((Object)(object)canvas == (Object)null)
		{
			yield break;
		}
		Transform menuBg = ((Component)canvas).transform.Find("MenuBackground");
		if ((Object)(object)menuBg == (Object)null || (Object)(object)menuBg.Find("QoL_Fruit") != (Object)null)
		{
			yield break;
		}
		while ((Object)(object)Object.FindObjectOfType<PreRunScript>() == (Object)null)
		{
			yield return (object)new WaitForSeconds(0.2f);
		}
		yield return null;
		Transform val = menuBg.Find("MP3");
		Transform experiment = menuBg.Find("Experiment");
		if ((Object)(object)val == (Object)null)
		{
			yield break;
		}
		GameObject qolObj = Object.Instantiate<GameObject>(((Component)val).gameObject, menuBg);
		((Object)qolObj).name = "QoL_Fruit";
		qolObj.transform.SetAsLastSibling();
		RectTransform qolRect = qolObj.GetComponent<RectTransform>();
		RectTransform component = ((Component)val).GetComponent<RectTransform>();
		Vector3 stableBasePosition = (((Object)(object)component != (Object)null) ? ((Transform)component).localPosition : ((Transform)qolRect).localPosition);
		MenuParallax parallaxScript = ((Component)canvas).GetComponentInChildren<MenuParallax>();
		if ((Object)(object)parallaxScript != (Object)null && (Object)(object)component != (Object)null)
		{
			foreach (ParallaxLayerInfo layer in parallaxScript.parallax.layers)
			{
				if ((Object)(object)layer.reference == (Object)(object)component)
				{
					stableBasePosition = Vector2.op_Implicit(layer.originalPos);
					break;
				}
			}
		}
		((Transform)qolRect).localPosition = stableBasePosition;
		RectTransform component2 = ((Component)qolObj.transform.Find("Clickable")).GetComponent<RectTransform>();
		RectTransform component3 = ((Component)qolObj.transform.Find("Text")).GetComponent<RectTransform>();
		component2.anchoredPosition = new Vector2(-621f, -356f);
		component3.anchoredPosition = component2.anchoredPosition + new Vector2(0f, -110f);
		Image component4 = qolObj.GetComponent<Image>();
		if ((Object)(object)component4 != (Object)null)
		{
			component4.sprite = Helper.LoadEmbeddedSprite("backQoL", 100f, (FilterMode)0);
			((Graphic)component4).raycastTarget = false;
		}
		Transform clickable = qolObj.transform.Find("Clickable");
		yield return null;
		AdaptiveButton component5 = ((Component)clickable).GetComponent<AdaptiveButton>();
		if ((Object)(object)component5 != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)component5);
		}
		EventTrigger[] components = ((Component)clickable).GetComponents<EventTrigger>();
		for (int i = 0; i < components.Length; i++)
		{
			Object.DestroyImmediate((Object)(object)components[i]);
		}
		Object.DestroyImmediate((Object)(object)((Component)clickable).GetComponent<Image>());
		Object.DestroyImmediate((Object)(object)((Component)clickable).GetComponent<UITooltip>());
		Image component6 = ((Component)clickable).GetComponent<Image>();
		if ((Object)(object)component6 != (Object)null)
		{
			component6.sprite = null;
			((Graphic)component6).color = Color.clear;
		}
		Transform obj = qolObj.transform.Find("Overlay");
		Image val2 = ((obj != null) ? ((Component)obj).GetComponent<Image>() : null);
		if ((Object)(object)val2 != (Object)null)
		{
			val2.sprite = Helper.LoadEmbeddedSprite("geofruitOverlay", 100f, (FilterMode)0);
			((Graphic)val2).raycastTarget = false;
		}
		Transform val3 = qolObj.transform.Find("Text");
		TextMeshProUGUI val4 = null;
		if ((Object)(object)val3 != (Object)null)
		{
			Image component7 = ((Component)val3).GetComponent<Image>();
			if ((Object)(object)component7 != (Object)null)
			{
				component7.sprite = null;
				((Graphic)component7).color = Color.clear;
			}
			val4 = ((Component)val3).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)val4 != (Object)null)
			{
				Component component8 = ((Component)val4).GetComponent("UILocalizer");
				if ((Object)(object)component8 != (Object)null)
				{
					Object.Destroy((Object)(object)component8);
				}
				((TMP_Text)val4).text = "QoL\nSettings";
				ApplyGeofruitFont((TMP_Text)(object)val4);
			}
		}
		if ((Object)(object)clickable != (Object)null)
		{
			QoLMenuButton qoLMenuButton = ((Component)clickable).gameObject.AddComponent<QoLMenuButton>();
			qoLMenuButton.overlay = val2;
			qoLMenuButton.label = val4;
		}
		if ((Object)(object)parallaxScript != (Object)null && (Object)(object)experiment != (Object)null)
		{
			RectTransform component9 = ((Component)experiment).GetComponent<RectTransform>();
			float mouseMoveScale = 0f;
			foreach (ParallaxLayerInfo layer2 in parallaxScript.parallax.layers)
			{
				if ((Object)(object)layer2.reference == (Object)(object)component9)
				{
					mouseMoveScale = layer2.mouseMoveScale;
					break;
				}
			}
			parallaxScript.parallax.layers.Add(new ParallaxLayerInfo
			{
				reference = qolRect,
				mouseMoveScale = mouseMoveScale,
				originalPos = Vector2.op_Implicit(stableBasePosition)
			});
		}
		TMP_FontAsset geofruitFont = GetGeofruitFont();
		Sprite val5 = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		GameObject mainContainer = new GameObject("QoLOptionsButton");
		RectTransform obj2 = mainContainer.AddComponent<RectTransform>();
		((Transform)obj2).SetParent(((Component)canvas).transform, false);
		((Transform)obj2).SetAsLastSibling();
		obj2.anchorMax = new Vector2(0f, 1f);
		obj2.anchorMin = new Vector2(0f, 1f);
		obj2.pivot = new Vector2(0.5f, 1f);
		obj2.anchoredPosition = new Vector2(1630f, -90f);
		VerticalLayoutGroup obj3 = mainContainer.AddComponent<VerticalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)obj3).spacing = 15f;
		((LayoutGroup)obj3).childAlignment = (TextAnchor)1;
		((HorizontalOrVerticalLayoutGroup)obj3).childControlHeight = false;
		((HorizontalOrVerticalLayoutGroup)obj3).childForceExpandHeight = false;
		((HorizontalOrVerticalLayoutGroup)obj3).childControlWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj3).childForceExpandWidth = false;
		ContentSizeFitter obj4 = mainContainer.AddComponent<ContentSizeFitter>();
		obj4.horizontalFit = (FitMode)2;
		obj4.verticalFit = (FitMode)2;
		GameObject optionsListObj = new GameObject("QoLOptionsList");
		geofruitOptionsList = optionsListObj;
		optionsListObj.transform.SetParent(mainContainer.transform, false);
		Image val6 = optionsListObj.AddComponent<Image>();
		if (Object.op_Implicit((Object)(object)val5))
		{
			val6.sprite = val5;
		}
		val6.type = (Type)1;
		((Graphic)val6).color = new Color(1f, 1f, 1f, 0.5f);
		optionsListObj.AddComponent<LayoutElement>().minWidth = 580f;
		VerticalLayoutGroup obj5 = optionsListObj.AddComponent<VerticalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)obj5).childControlHeight = false;
		((HorizontalOrVerticalLayoutGroup)obj5).childControlWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj5).childForceExpandHeight = false;
		((HorizontalOrVerticalLayoutGroup)obj5).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj5).spacing = 10f;
		((LayoutGroup)obj5).padding = new RectOffset(15, 15, 10, 10);
		ContentSizeFitter obj6 = optionsListObj.AddComponent<ContentSizeFitter>();
		obj6.verticalFit = (FitMode)2;
		obj6.horizontalFit = (FitMode)2;
		foreach (QoLModuleDef module in QoLModuleManager.Modules)
		{
			if (module.IsCore || GeofruitBlacklist.Contains(module.ID))
			{
				continue;
			}
			GameObject val7 = new GameObject("Toggle_" + module.ID);
			val7.transform.SetParent(optionsListObj.transform, false);
			val7.AddComponent<RectTransform>().sizeDelta = new Vector2(550f, 40f);
			HorizontalLayoutGroup obj7 = val7.AddComponent<HorizontalLayoutGroup>();
			((HorizontalOrVerticalLayoutGroup)obj7).spacing = 8f;
			((HorizontalOrVerticalLayoutGroup)obj7).childForceExpandWidth = false;
			((HorizontalOrVerticalLayoutGroup)obj7).childForceExpandHeight = false;
			((HorizontalOrVerticalLayoutGroup)obj7).childControlHeight = false;
			((HorizontalOrVerticalLayoutGroup)obj7).childControlWidth = false;
			((LayoutGroup)obj7).childAlignment = (TextAnchor)3;
			((LayoutGroup)obj7).padding = new RectOffset(0, 0, 4, 4);
			Toggle obj8 = val7.AddComponent<Toggle>();
			GameObject val8 = new GameObject("Background");
			val8.transform.SetParent(val7.transform, false);
			val8.AddComponent<RectTransform>().sizeDelta = new Vector2(32.4f, 32.4f);
			Image val9 = val8.AddComponent<Image>();
			if (Object.op_Implicit((Object)(object)val5))
			{
				val9.sprite = val5;
			}
			val9.type = (Type)1;
			GameObject val10 = new GameObject("Checkmark");
			val10.transform.SetParent(val8.transform, false);
			RectTransform obj9 = val10.AddComponent<RectTransform>();
			obj9.sizeDelta = new Vector2(20f, 20f);
			obj9.anchorMin = new Vector2(0.5f, 0.5f);
			obj9.anchorMax = new Vector2(0.5f, 0.5f);
			obj9.anchoredPosition = Vector2.zero;
			Image graphic = val10.AddComponent<Image>();
			((Selectable)obj8).targetGraphic = (Graphic)(object)val9;
			obj8.graphic = (Graphic)(object)graphic;
			obj8.isOn = QoLModuleManager.IsEnabled(module.ID);
			string moduleId = module.ID;
			GameObject val11 = new GameObject("Label");
			val11.transform.SetParent(val7.transform, false);
			val11.AddComponent<RectTransform>().sizeDelta = new Vector2(550f, 40f);
			TextMeshProUGUI labelText = val11.AddComponent<TextMeshProUGUI>();
			ApplyGeofruitFont((TMP_Text)(object)labelText, geofruitFont);
			((TMP_Text)labelText).fontSize = 16f;
			((TMP_Text)labelText).enableWordWrapping = false;
			((TMP_Text)labelText).text = module.Name + "\n<color=#aaaaaa><size=80%>" + module.Description + "</size></color>";
			((UnityEventBase)obj8.onValueChanged).RemoveAllListeners();
			((UnityEvent<bool>)(object)obj8.onValueChanged).AddListener((UnityAction<bool>)delegate(bool isOn)
			{
				QoLModuleManager.SetEnabled(moduleId, isOn);
				if (!_ModulesChanged.Contains(moduleId))
				{
					_ModulesChanged.Add(moduleId);
					((TMP_Text)labelText).text = "<color=#FFEA00>" + module.Name + "</color>\n<color=#aaaaaa><size=80%>" + module.Description + "</size></color>";
				}
				else
				{
					((TMP_Text)labelText).text = module.Name + "\n<color=#aaaaaa><size=80%>" + module.Description + "</size></color>";
					_ModulesChanged.Remove(moduleId);
				}
				if (TryGetFooterText(out var footerText))
				{
					footerText.text = ((_ModulesChanged.Count == 0) ? "Made by Orsoniks\nLoading art by Wickflix\nMenu art by @GatoCataco ~" : " <color=#FFEA00>\nPlease restart your game for changes to take effect!");
				}
			});
		}
		optionsListObj.SetActive(false);
		QoLToggleAction = (UnityAction)delegate
		{
			if (IsRunSettingsScreenActive())
			{
				SetGeofruitMenuActive(optionsListObj, active: false);
				Transform transform2 = mainContainer.transform;
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)(object)((transform2 is RectTransform) ? transform2 : null));
			}
			else
			{
				bool flag = !optionsListObj.activeSelf;
				if (flag)
				{
					GameObject val12 = GameObject.Find("CustomOptionsButton");
					if ((Object)(object)val12 != (Object)null)
					{
						Transform val13 = val12.transform.Find("dropdownOptions");
						if ((Object)(object)val13 != (Object)null && ((Component)val13).gameObject.activeSelf)
						{
							((Component)val13).gameObject.SetActive(false);
							Transform val14 = val12.transform.Find("CustomOptionsToggle/Label");
							if ((Object)(object)val14 != (Object)null)
							{
								TextMeshProUGUI component10 = ((Component)val14).GetComponent<TextMeshProUGUI>();
								if ((Object)(object)component10 != (Object)null)
								{
									((TMP_Text)component10).text = "Show Options";
								}
							}
						}
					}
				}
				SetGeofruitMenuActive(optionsListObj, flag);
				Transform transform3 = mainContainer.transform;
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)(object)((transform3 is RectTransform) ? transform3 : null));
			}
		};
		Canvas.ForceUpdateCanvases();
		Transform transform = mainContainer.transform;
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)(object)((transform is RectTransform) ? transform : null));
		Finish();
	}

	[HarmonyPatch(typeof(AdaptiveButton), "Clicked")]
	[HarmonyPrefix]
	private static bool AdaptiveButton_Clicked_Prefix(AdaptiveButton __instance)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((Object)(object)__instance != (Object)null && (int)__instance.action == 1 && IsGeofruitMenuActive())
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static void AdaptiveButton_OverlayActive_Postfix(ref bool __result)
	{
		if (IsGeofruitMenuActive())
		{
			__result = true;
		}
	}

	[HarmonyPatch(typeof(PauseHandler), "TogglePause")]
	[HarmonyPrefix]
	private static bool PauseHandler_TogglePause_Prefix()
	{
		if ((Object)(object)SettingsMenu.instance == (Object)null)
		{
			return true;
		}
		SettingsMenu.instance.Close();
		return false;
	}

	internal static void CloseGeofruitMenuIfRunSettingsActive()
	{
		if (IsRunSettingsScreenActive())
		{
			GameObject val = GetGeofruitOptionsList();
			if ((Object)(object)val != (Object)null && val.activeSelf)
			{
				SetGeofruitMenuActive(val, active: false);
			}
		}
	}

	internal static bool IsGeofruitMenuActive()
	{
		GameObject val = GetGeofruitOptionsList();
		if ((Object)(object)val != (Object)null)
		{
			return val.activeInHierarchy;
		}
		return false;
	}

	private static GameObject GetGeofruitOptionsList()
	{
		if ((Object)(object)geofruitOptionsList == (Object)null && geofruitOptionsLookupFrame != Time.frameCount)
		{
			geofruitOptionsLookupFrame = Time.frameCount;
			geofruitOptionsList = GameObject.Find("QoLOptionsList");
		}
		return geofruitOptionsList;
	}

	private static void SetGeofruitMenuActive(GameObject optionsList, bool active)
	{
		if (!((Object)(object)optionsList == (Object)null))
		{
			geofruitOptionsList = optionsList;
			geofruitOptionsLookupFrame = Time.frameCount;
			optionsList.SetActive(active);
		}
	}

	private static bool IsRunSettingsScreenActive()
	{
		if ((Object)(object)PreRunScript.instance != (Object)null && (Object)(object)PreRunScript.instance.runSettingsScreen != (Object)null)
		{
			return PreRunScript.instance.runSettingsScreen.activeSelf;
		}
		return false;
	}

	private static TMP_FontAsset GetGeofruitFont()
	{
		return FontPatcher.GetRetroPixelExtendedFont() ?? ResourceLookupCache.FindFontContains("Retro Pixel Extended") ?? ResourceLookupCache.FindFontContains("Retro Gaming Extended") ?? ResourceLookupCache.FindFontContains("retrogamingextended") ?? ResourceLookupCache.FindFontContains("Retro GamingPix");
	}

	private static void ApplyGeofruitFont(TMP_Text text, TMP_FontAsset font = null)
	{
		if (!((Object)(object)text == (Object)null))
		{
			font = font ?? GetGeofruitFont();
			if ((Object)(object)font != (Object)null)
			{
				text.font = font;
			}
		}
	}

	public static void Finish()
	{
		TrySetModdedBetaBuildText();
	}

	private static IEnumerator ApplyModdedBetaBuildText()
	{
		float timeout = Time.unscaledTime + 5f;
		while (Time.unscaledTime < timeout && !TrySetModdedBetaBuildText())
		{
			yield return null;
		}
	}

	private static bool TrySetModdedBetaBuildText()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		GlobalDark val = Object.FindObjectOfType<GlobalDark>();
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		TMP_Text val2 = (TMP_Text)(object)((Component)val).GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(delegate(TextMeshProUGUI val3)
		{
			if ((Object)(object)val3 != (Object)null)
			{
				string text2 = ((TMP_Text)val3).text;
				if (text2 == null || !text2.StartsWith("This is a beta build (", StringComparison.Ordinal))
				{
					return ((TMP_Text)val3).text?.StartsWith("This is a modded beta build (", StringComparison.Ordinal) ?? false;
				}
				return true;
			}
			return false;
		});
		if ((Object)(object)val2 == (Object)null)
		{
			return false;
		}
		Component component = ((Component)val2).GetComponent("UILocalizer");
		if ((Object)(object)component != (Object)null)
		{
			Object.Destroy((Object)(object)component);
		}
		string text = val2.text ?? string.Empty;
		if (!text.Contains(" modded"))
		{
			val2.text = text.Replace("This is a ", "This is a modded ");
		}
		Color color = ((Graphic)val2).color;
		color.a = 0.0227f;
		((Graphic)val2).color = color;
		return true;
	}

	private static bool TryGetFooterText(out TMP_Text footerText)
	{
		footerText = null;
		Transform canvas = (((Object)(object)PreRunScript.instance != (Object)null && (Object)(object)PreRunScript.instance.mainCanvas != (Object)null) ? ((Component)PreRunScript.instance.mainCanvas).transform : null);
		if ((Object)(object)canvas == (Object)null)
		{
			return false;
		}
		Transform obj = canvas.Find("Text (TMP) (18)");
		TextMeshProUGUI val = ((obj != null) ? ((Component)obj).GetComponent<TextMeshProUGUI>() : null);
		if ((Object)(object)val != (Object)null)
		{
			footerText = (TMP_Text)(object)val;
			return true;
		}
		footerText = (TMP_Text)(object)Object.FindObjectsOfType<TextMeshProUGUI>(true).FirstOrDefault((TextMeshProUGUI tmp) => (Object)(object)tmp != (Object)null && ((TMP_Text)tmp).transform.IsChildOf(canvas) && !string.IsNullOrEmpty(((TMP_Text)tmp).text) && ((TMP_Text)tmp).text.Contains("Wickflix"));
		return (Object)(object)footerText != (Object)null;
	}
}
