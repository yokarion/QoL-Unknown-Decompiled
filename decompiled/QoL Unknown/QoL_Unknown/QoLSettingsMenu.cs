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

		public static UnityAction _003C_003E9__12_0;

		public static Func<TextMeshProUGUI, bool> _003C_003E9__27_0;

		internal void _003CBuildSettingsUI_003Eb__12_0()
		{
			EnhancedMenuController.Instance.OpenMenu();
		}

		internal bool _003CTryGetFooterText_003Eb__27_0(TextMeshProUGUI tmp)
		{
			int result;
			if ((Object)(object)tmp != (Object)null)
			{
				Transform transform = ((TMP_Text)tmp).transform;
				GameObject obj = GameObject.Find("Canvas");
				if (transform.IsChildOf((obj != null) ? obj.transform : null) && !string.IsNullOrEmpty(((TMP_Text)tmp).text))
				{
					result = (((TMP_Text)tmp).text.Contains("Wickflix") ? 1 : 0);
					goto IL_004c;
				}
			}
			result = 0;
			goto IL_004c;
			IL_004c:
			return (byte)result != 0;
		}
	}

	private static ManualLogSource Logger;

	public static HashSet<string> _ModulesChanged = new HashSet<string>();

	private const string ModdedInsertToken = "This is a ";

	private const string FooterDefaultText = "Made by Orsoniks\nLoading art by Wickflix\nMenu art by @GatoCataco ~";

	private static GameObject geofruitOptionsList;

	private static int geofruitOptionsLookupFrame = -1;

	internal static UnityAction QoLToggleAction;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.settingsmenu");
		val.PatchAll(typeof(QoLSettingsMenu));
		Logger.LogInfo((object)"QoLSettingsMenu initialized");
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
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		GameObject val = GameObject.Find("Main Camera/Canvas/GammaPanel/Button");
		if ((Object)(object)val == (Object)null || (Object)(object)val.transform.parent == (Object)null || (Object)(object)GameObject.Find("EnhancedSettingsButton") != (Object)null)
		{
			return;
		}
		GameObject val2 = Object.Instantiate<GameObject>(val, val.transform.parent);
		((Object)val2).name = "EnhancedSettingsButton";
		RectTransform component = val2.GetComponent<RectTransform>();
		Vector2 anchoredPosition = component.anchoredPosition;
		Rect rect = component.rect;
		component.anchoredPosition = anchoredPosition + new Vector2(0f, ((Rect)(ref rect)).height + 10f);
		Button component2 = val2.GetComponent<Button>();
		component2.onClick = new ButtonClickedEvent();
		ButtonClickedEvent onClick = component2.onClick;
		object obj = _003C_003Ec._003C_003E9__12_0;
		if (obj == null)
		{
			UnityAction val3 = delegate
			{
				EnhancedMenuController.Instance.OpenMenu();
			};
			_003C_003Ec._003C_003E9__12_0 = val3;
			obj = (object)val3;
		}
		((UnityEvent)onClick).AddListener((UnityAction)obj);
		TextMeshProUGUI componentInChildren = val2.GetComponentInChildren<TextMeshProUGUI>(true);
		Object.Destroy((Object)(object)((Component)componentInChildren).GetComponent("UILocalizer"));
		((TMP_Text)componentInChildren).text = "OPTION\nMENU";
	}

	private static IEnumerator SetupCustomMenu()
	{
		Canvas canvas = null;
		while ((Object)(object)canvas == (Object)null)
		{
			GameObject found = GameObject.Find("Canvas");
			if ((Object)(object)found != (Object)null)
			{
				canvas = found.GetComponent<Canvas>();
			}
			yield return null;
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
		Transform mp3 = menuBg.Find("MP3");
		Transform experiment = menuBg.Find("Experiment");
		if ((Object)(object)mp3 == (Object)null)
		{
			yield break;
		}
		GameObject qolObj = Object.Instantiate<GameObject>(((Component)mp3).gameObject, menuBg);
		((Object)qolObj).name = "QoL_Fruit";
		qolObj.transform.SetAsLastSibling();
		RectTransform qolRect = qolObj.GetComponent<RectTransform>();
		RectTransform mp3Rect = ((Component)mp3).GetComponent<RectTransform>();
		Vector3 stableBasePosition = (((Object)(object)mp3Rect != (Object)null) ? ((Transform)mp3Rect).localPosition : ((Transform)qolRect).localPosition);
		MenuParallax parallaxScript = ((Component)canvas).GetComponentInChildren<MenuParallax>();
		if ((Object)(object)parallaxScript != (Object)null && (Object)(object)mp3Rect != (Object)null)
		{
			foreach (ParallaxLayerInfo layer in parallaxScript.parallax.layers)
			{
				if ((Object)(object)layer.reference == (Object)(object)mp3Rect)
				{
					stableBasePosition = Vector2.op_Implicit(layer.originalPos);
					break;
				}
			}
		}
		((Transform)qolRect).localPosition = stableBasePosition;
		RectTransform clickableRect = ((Component)qolObj.transform.Find("Clickable")).GetComponent<RectTransform>();
		RectTransform textRect = ((Component)qolObj.transform.Find("Text")).GetComponent<RectTransform>();
		clickableRect.anchoredPosition = new Vector2(-621f, -356f);
		textRect.anchoredPosition = clickableRect.anchoredPosition + new Vector2(0f, -110f);
		Image rootImg = qolObj.GetComponent<Image>();
		if ((Object)(object)rootImg != (Object)null)
		{
			rootImg.sprite = Helper.LoadEmbeddedSprite("backQoL", 100f, (FilterMode)0);
			((Graphic)rootImg).raycastTarget = false;
		}
		Transform clickable = qolObj.transform.Find("Clickable");
		yield return null;
		AdaptiveButton adaptiveButton = ((Component)clickable).GetComponent<AdaptiveButton>();
		if ((Object)(object)adaptiveButton != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)adaptiveButton);
		}
		EventTrigger[] components = ((Component)clickable).GetComponents<EventTrigger>();
		foreach (EventTrigger trigger in components)
		{
			Object.DestroyImmediate((Object)(object)trigger);
		}
		Object.DestroyImmediate((Object)(object)((Component)clickable).GetComponent<Image>());
		Object.DestroyImmediate((Object)(object)((Component)clickable).GetComponent<UITooltip>());
		Image clickableImg = ((Component)clickable).GetComponent<Image>();
		if ((Object)(object)clickableImg != (Object)null)
		{
			clickableImg.sprite = null;
			((Graphic)clickableImg).color = Color.clear;
		}
		Transform overlayChild = qolObj.transform.Find("Overlay");
		Image overlayImg = ((overlayChild != null) ? ((Component)overlayChild).GetComponent<Image>() : null);
		if ((Object)(object)overlayImg != (Object)null)
		{
			overlayImg.sprite = Helper.LoadEmbeddedSprite("geofruitOverlay", 100f, (FilterMode)0);
			((Graphic)overlayImg).raycastTarget = false;
		}
		Transform textChild = qolObj.transform.Find("Text");
		TextMeshProUGUI label = null;
		if ((Object)(object)textChild != (Object)null)
		{
			Image textImg = ((Component)textChild).GetComponent<Image>();
			if ((Object)(object)textImg != (Object)null)
			{
				textImg.sprite = null;
				((Graphic)textImg).color = Color.clear;
			}
			label = ((Component)textChild).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)label != (Object)null)
			{
				Component localizer = ((Component)label).GetComponent("UILocalizer");
				if ((Object)(object)localizer != (Object)null)
				{
					Object.Destroy((Object)(object)localizer);
				}
				((TMP_Text)label).text = "QoL\nSettings";
				ApplyGeofruitFont((TMP_Text)(object)label);
			}
		}
		if ((Object)(object)clickable != (Object)null)
		{
			QoLMenuButton qolBtn = ((Component)clickable).gameObject.AddComponent<QoLMenuButton>();
			qolBtn.overlay = overlayImg;
			qolBtn.label = label;
		}
		if ((Object)(object)parallaxScript != (Object)null && (Object)(object)experiment != (Object)null)
		{
			RectTransform experimentRect = ((Component)experiment).GetComponent<RectTransform>();
			float targetScale = 0f;
			foreach (ParallaxLayerInfo layer2 in parallaxScript.parallax.layers)
			{
				if ((Object)(object)layer2.reference == (Object)(object)experimentRect)
				{
					targetScale = layer2.mouseMoveScale;
					break;
				}
			}
			parallaxScript.parallax.layers.Add(new ParallaxLayerInfo
			{
				reference = qolRect,
				mouseMoveScale = targetScale,
				originalPos = Vector2.op_Implicit(stableBasePosition)
			});
		}
		TMP_FontAsset font = GetGeofruitFont();
		Sprite uiSprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		GameObject mainContainer = new GameObject("QoLOptionsButton");
		RectTransform dropdownRect = mainContainer.AddComponent<RectTransform>();
		((Transform)dropdownRect).SetParent(((Component)canvas).transform, false);
		((Transform)dropdownRect).SetAsLastSibling();
		dropdownRect.anchorMax = new Vector2(0f, 1f);
		dropdownRect.anchorMin = new Vector2(0f, 1f);
		dropdownRect.pivot = new Vector2(0.5f, 1f);
		dropdownRect.anchoredPosition = new Vector2(1630f, -90f);
		VerticalLayoutGroup vlg = mainContainer.AddComponent<VerticalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)vlg).spacing = 15f;
		((LayoutGroup)vlg).childAlignment = (TextAnchor)1;
		((HorizontalOrVerticalLayoutGroup)vlg).childControlHeight = false;
		((HorizontalOrVerticalLayoutGroup)vlg).childForceExpandHeight = false;
		((HorizontalOrVerticalLayoutGroup)vlg).childControlWidth = false;
		((HorizontalOrVerticalLayoutGroup)vlg).childForceExpandWidth = false;
		ContentSizeFitter mainFitter = mainContainer.AddComponent<ContentSizeFitter>();
		mainFitter.horizontalFit = (FitMode)2;
		mainFitter.verticalFit = (FitMode)2;
		GameObject optionsListObj = new GameObject("QoLOptionsList");
		geofruitOptionsList = optionsListObj;
		optionsListObj.transform.SetParent(mainContainer.transform, false);
		Image bgPanelImg = optionsListObj.AddComponent<Image>();
		if (Object.op_Implicit((Object)(object)uiSprite))
		{
			bgPanelImg.sprite = uiSprite;
		}
		bgPanelImg.type = (Type)1;
		((Graphic)bgPanelImg).color = new Color(1f, 1f, 1f, 0.5f);
		LayoutElement le = optionsListObj.AddComponent<LayoutElement>();
		le.minWidth = 580f;
		VerticalLayoutGroup listLayout = optionsListObj.AddComponent<VerticalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)listLayout).childControlHeight = false;
		((HorizontalOrVerticalLayoutGroup)listLayout).childControlWidth = false;
		((HorizontalOrVerticalLayoutGroup)listLayout).childForceExpandHeight = false;
		((HorizontalOrVerticalLayoutGroup)listLayout).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)listLayout).spacing = 10f;
		((LayoutGroup)listLayout).padding = new RectOffset(15, 15, 10, 10);
		ContentSizeFitter listFitter = optionsListObj.AddComponent<ContentSizeFitter>();
		listFitter.verticalFit = (FitMode)2;
		listFitter.horizontalFit = (FitMode)2;
		foreach (QoLModuleDef module in QoLModuleManager.Modules)
		{
			if (module.IsCore)
			{
				continue;
			}
			GameObject rowObj = new GameObject("Toggle_" + module.ID);
			rowObj.transform.SetParent(optionsListObj.transform, false);
			RectTransform rowRect = rowObj.AddComponent<RectTransform>();
			rowRect.sizeDelta = new Vector2(550f, 40f);
			HorizontalLayoutGroup rowGroup = rowObj.AddComponent<HorizontalLayoutGroup>();
			((HorizontalOrVerticalLayoutGroup)rowGroup).spacing = 8f;
			((HorizontalOrVerticalLayoutGroup)rowGroup).childForceExpandWidth = false;
			((HorizontalOrVerticalLayoutGroup)rowGroup).childForceExpandHeight = false;
			((HorizontalOrVerticalLayoutGroup)rowGroup).childControlHeight = false;
			((HorizontalOrVerticalLayoutGroup)rowGroup).childControlWidth = false;
			((LayoutGroup)rowGroup).childAlignment = (TextAnchor)3;
			((LayoutGroup)rowGroup).padding = new RectOffset(0, 0, 4, 4);
			Toggle toggle = rowObj.AddComponent<Toggle>();
			GameObject bgObj = new GameObject("Background");
			bgObj.transform.SetParent(rowObj.transform, false);
			RectTransform bgRect = bgObj.AddComponent<RectTransform>();
			bgRect.sizeDelta = new Vector2(32.4f, 32.4f);
			Image bgImg = bgObj.AddComponent<Image>();
			if (Object.op_Implicit((Object)(object)uiSprite))
			{
				bgImg.sprite = uiSprite;
			}
			bgImg.type = (Type)1;
			GameObject checkObj = new GameObject("Checkmark");
			checkObj.transform.SetParent(bgObj.transform, false);
			RectTransform checkRect = checkObj.AddComponent<RectTransform>();
			checkRect.sizeDelta = new Vector2(20f, 20f);
			checkRect.anchorMin = new Vector2(0.5f, 0.5f);
			checkRect.anchorMax = new Vector2(0.5f, 0.5f);
			checkRect.anchoredPosition = Vector2.zero;
			Image checkImg = checkObj.AddComponent<Image>();
			((Selectable)toggle).targetGraphic = (Graphic)(object)bgImg;
			toggle.graphic = (Graphic)(object)checkImg;
			toggle.isOn = QoLModuleManager.IsEnabled(module.ID);
			string moduleId = module.ID;
			GameObject labelObj = new GameObject("Label");
			labelObj.transform.SetParent(rowObj.transform, false);
			RectTransform labelRect = labelObj.AddComponent<RectTransform>();
			labelRect.sizeDelta = new Vector2(550f, 40f);
			TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
			ApplyGeofruitFont((TMP_Text)(object)labelText, font);
			((TMP_Text)labelText).fontSize = 16f;
			((TMP_Text)labelText).enableWordWrapping = false;
			((TMP_Text)labelText).text = module.Name + "\n<color=#aaaaaa><size=80%>" + module.Description + "</size></color>";
			((UnityEventBase)toggle.onValueChanged).RemoveAllListeners();
			((UnityEvent<bool>)(object)toggle.onValueChanged).AddListener((UnityAction<bool>)delegate(bool isOn)
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
		UnityAction toggleMenuAction = (UnityAction)delegate
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
					GameObject val = GameObject.Find("CustomOptionsButton");
					if ((Object)(object)val != (Object)null)
					{
						Transform val2 = val.transform.Find("dropdownOptions");
						if ((Object)(object)val2 != (Object)null && ((Component)val2).gameObject.activeSelf)
						{
							((Component)val2).gameObject.SetActive(false);
							Transform val3 = val.transform.Find("CustomOptionsToggle/Label");
							if ((Object)(object)val3 != (Object)null)
							{
								TextMeshProUGUI component = ((Component)val3).GetComponent<TextMeshProUGUI>();
								if ((Object)(object)component != (Object)null)
								{
									((TMP_Text)component).text = "Show Options";
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
		QoLToggleAction = toggleMenuAction;
		Canvas.ForceUpdateCanvases();
		Transform transform = mainContainer.transform;
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)(object)((transform is RectTransform) ? transform : null));
		Finish();
	}

	[HarmonyPatch(typeof(AdaptiveButton), "Clicked")]
	[HarmonyPrefix]
	private static bool AdaptiveButton_Clicked_Prefix(AdaptiveButton __instance)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
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
		return (Object)(object)val != (Object)null && val.activeInHierarchy;
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
		return (Object)(object)PreRunScript.instance != (Object)null && (Object)(object)PreRunScript.instance.runSettingsScreen != (Object)null && PreRunScript.instance.runSettingsScreen.activeSelf;
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
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		GlobalDark val = Object.FindObjectOfType<GlobalDark>();
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		TMP_Text val2 = (TMP_Text)(object)val.betaBuild;
		if ((Object)(object)val2 == (Object)null)
		{
			val2 = (TMP_Text)(object)((Component)val).GetComponentInChildren<TextMeshProUGUI>(true);
		}
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
		GameObject obj = GameObject.Find("Canvas/Text (TMP) (18)");
		footerText = (TMP_Text)(object)((obj != null) ? obj.GetComponent<TextMeshProUGUI>() : null);
		if ((Object)(object)footerText != (Object)null)
		{
			return true;
		}
		footerText = (TMP_Text)(object)Object.FindObjectsOfType<TextMeshProUGUI>(true).FirstOrDefault(delegate(TextMeshProUGUI tmp)
		{
			int result;
			if ((Object)(object)tmp != (Object)null)
			{
				Transform transform = ((TMP_Text)tmp).transform;
				GameObject obj2 = GameObject.Find("Canvas");
				if (transform.IsChildOf((obj2 != null) ? obj2.transform : null) && !string.IsNullOrEmpty(((TMP_Text)tmp).text))
				{
					result = (((TMP_Text)tmp).text.Contains("Wickflix") ? 1 : 0);
					goto IL_004c;
				}
			}
			result = 0;
			goto IL_004c;
			IL_004c:
			return (byte)result != 0;
		});
		return (Object)(object)footerText != (Object)null;
	}
}
