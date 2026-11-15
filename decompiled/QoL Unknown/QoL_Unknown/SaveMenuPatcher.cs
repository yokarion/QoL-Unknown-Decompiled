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
using UnityEngine.UI;

namespace QoL_Unknown;

internal class SaveMenuPatcher
{
	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (!initialized)
		{
			Logger = logger;
			MainMenuSavePanel.Logger = logger;
			new Harmony("org.bepinex.plugins.qol.savemenu").PatchAll(typeof(SaveMenuPatcher));
			initialized = true;
		}
	}

	[HarmonyPatch(typeof(PreRunScript), "Start")]
	[HarmonyPostfix]
	private static void Start_Postfix(PreRunScript __instance)
	{
		if (QoLModuleManager.IsEnabled("qol_save_menu"))
		{
			SharedCoroutineStarter.StartCoroutine(CreateQuickLoadButtonsDelayed(__instance));
		}
	}

	private static IEnumerator CreateQuickLoadButtonsDelayed(PreRunScript context)
	{
		if ((Object)(object)Object.FindObjectOfType<ScrollableText>() != (Object)null)
		{
			while ((Object)(object)Object.FindObjectOfType<ScrollableText>() != (Object)null)
			{
				yield return (object)new WaitForSeconds(0.2f);
			}
		}
		CreateQuickLoadButtons(context);
	}

	private static void CreateQuickLoadButtons(PreRunScript context)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		Canvas val = (((Object)(object)context != (Object)null) ? context.mainCanvas : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Transform val2 = ((Component)val).transform.Find("RunSettings");
		if ((Object)(object)val2 == (Object)null)
		{
			return;
		}
		RemoveExistingQuickLoadButtons(val2);
		CreateSeedInput(((Component)val).transform);
		Button val3 = PreRunScript.instance?.loadButton;
		if (!((Object)(object)val3 == (Object)null))
		{
			((UnityEventBase)val3.onClick).RemoveAllListeners();
			val3.onClick = new ButtonClickedEvent();
			((UnityEvent)val3.onClick).AddListener(new UnityAction(MainMenuSavePanel.Show));
			((Selectable)val3).interactable = true;
			Image val4 = PreRunScript.instance?.loadButtonImage;
			if ((Object)(object)val4 != (Object)null)
			{
				((Behaviour)val4).enabled = true;
			}
			Sprite val5 = Helper.LoadEmbeddedSprite("expand");
			if ((Object)(object)val5 != (Object)null)
			{
				GameObject val6 = new GameObject("ExpandIcon", new Type[3]
				{
					typeof(RectTransform),
					typeof(CanvasRenderer),
					typeof(Image)
				});
				val6.transform.SetParent(((Component)val3).transform, false);
				RectTransform val7 = (RectTransform)val6.transform;
				val7.sizeDelta = new Vector2(24f, 24f);
				val7.anchorMin = new Vector2(1f, 0.5f);
				val7.anchorMax = new Vector2(1f, 0.5f);
				val7.pivot = new Vector2(1f, 0.5f);
				val7.anchoredPosition = new Vector2(-16f, 0f);
				Image component = val6.GetComponent<Image>();
				component.sprite = val5;
				component.preserveAspect = true;
				((Graphic)component).color = Color.white;
				((Graphic)component).raycastTarget = false;
			}
		}
	}

	private static Sprite LoadLayerIcon(string metaPath)
	{
		if (!File.Exists(metaPath))
		{
			return null;
		}
		try
		{
			SaveMetadata saveMetadata = JsonConvert.DeserializeObject<SaveMetadata>(File.ReadAllText(metaPath));
			if (saveMetadata == null)
			{
				return null;
			}
			Sprite value;
			return SaveSaveMenuPatcher.layerIcons.TryGetValue(saveMetadata.BiomeIndex, out value) ? value : Helper.LoadEmbeddedSprite($"layer{saveMetadata.BiomeIndex}ui");
		}
		catch
		{
			return null;
		}
	}

	private static void ApplyQuickLoadLayerIcon(GameObject buttonObject, Sprite layerSprite, bool saveExists)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		Image val = EnsureQuickLoadLayerIcon(buttonObject);
		if ((Object)(object)val == (Object)null)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[SaveMenuPatcher] " + ((Object)buttonObject).name + " has no Image for its layer icon."));
			}
			return;
		}
		Transform transform = ((Component)val).transform;
		((Component)transform).gameObject.SetActive(true);
		RectTransform rectTransform = ((Graphic)val).rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.offsetMin = new Vector2(4f, 4f);
		rectTransform.offsetMax = new Vector2(-4f, -4f);
		((Transform)rectTransform).localScale = Vector3.one;
		val.sprite = layerSprite;
		val.type = (Type)0;
		val.preserveAspect = true;
		((Graphic)val).material = null;
		((Graphic)val).raycastTarget = false;
		((Graphic)val).color = (Color)((!((Object)(object)layerSprite != (Object)null)) ? Color.clear : (saveExists ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f)));
		transform.SetAsLastSibling();
		TextMeshProUGUI[] componentsInChildren = buttonObject.GetComponentsInChildren<TextMeshProUGUI>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			((TMP_Text)componentsInChildren[i]).transform.SetAsLastSibling();
		}
	}

	private static Image EnsureQuickLoadLayerIcon(GameObject buttonObject)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		Transform val = buttonObject.transform.Find("LayerIcon");
		Image val2 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Image>() : null);
		if ((Object)(object)val2 == (Object)null)
		{
			val = buttonObject.transform.Find("Image");
			val2 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Image>() : null);
			if ((Object)(object)val2 != (Object)null)
			{
				((Object)val).name = "LayerIcon";
			}
		}
		if ((Object)(object)val2 == (Object)null)
		{
			GameObject val3 = new GameObject("LayerIcon", new Type[3]
			{
				typeof(RectTransform),
				typeof(CanvasRenderer),
				typeof(Image)
			});
			val3.transform.SetParent(buttonObject.transform, false);
			val2 = val3.GetComponent<Image>();
		}
		for (int i = 0; i < buttonObject.transform.childCount; i++)
		{
			Image component = ((Component)buttonObject.transform.GetChild(i)).GetComponent<Image>();
			if ((Object)(object)component != (Object)null && (Object)(object)component != (Object)(object)val2)
			{
				((Behaviour)component).enabled = false;
			}
		}
		((Behaviour)val2).enabled = true;
		return val2;
	}

	private static void CreateSeedInput(Transform canvas)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)FindExistingSeedInput(canvas) != (Object)null))
		{
			Transform val = canvas.Find("RunSettings/CharSelect/Bottom/Start");
			Button val2 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Button>() : null);
			RectTransform val3 = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponent<RectTransform>() : null);
			if (!((Object)(object)val3 == (Object)null) && !((Object)(object)((Transform)val3).parent == (Object)null))
			{
				GameObject val4 = new GameObject("SeedInput", new Type[4]
				{
					typeof(RectTransform),
					typeof(Image),
					typeof(TMP_InputField),
					typeof(UITooltip)
				});
				val4.transform.SetParent(((Transform)val3).parent, false);
				RectTransform component = val4.GetComponent<RectTransform>();
				SeedInputLayoutFollower seedInputLayoutFollower = val4.AddComponent<SeedInputLayoutFollower>();
				seedInputLayoutFollower.sourceRect = val3;
				seedInputLayoutFollower.originalOffsetMin = val3.offsetMin;
				seedInputLayoutFollower.widthFactor = 0.32f;
				seedInputLayoutFollower.maxWidth = 175f;
				seedInputLayoutFollower.spacingFactor = 0.16f;
				ConfigureSeedInputRect(component, val3, seedInputLayoutFollower.originalOffsetMin, 0.32f, 175f, 0.16f);
				Image component2 = val4.GetComponent<Image>();
				Image component3 = ((Component)val2).GetComponent<Image>();
				component2.sprite = ((component3 != null) ? component3.sprite : null) ?? ResourceLookupCache.FindSpriteContains("uiBlockNano");
				component2.type = (Type)1;
				((Graphic)component2).color = Color.white;
				GameObject val5 = new GameObject("Text Area", new Type[2]
				{
					typeof(RectTransform),
					typeof(RectMask2D)
				});
				val5.transform.SetParent(val4.transform, false);
				RectTransform component4 = val5.GetComponent<RectTransform>();
				component4.anchorMin = Vector2.zero;
				component4.anchorMax = Vector2.one;
				component4.offsetMin = new Vector2(12f, 5f);
				component4.offsetMax = new Vector2(-12f, -5f);
				TextMeshProUGUI componentInChildren = ((Component)val2).GetComponentInChildren<TextMeshProUGUI>(true);
				TMP_FontAsset font = ((componentInChildren != null) ? ((TMP_Text)componentInChildren).font : null);
				TextMeshProUGUI textComponent = CreateSeedInputText(val5.transform, "Text", string.Empty, font, new Color(0.33f, 0.7f, 1f));
				TextMeshProUGUI placeholder = CreateSeedInputText(val5.transform, "Placeholder", "Seed", font, new Color(1f, 1f, 1f, 0.45f));
				TMP_InputField component5 = val4.GetComponent<TMP_InputField>();
				component5.textViewport = component4;
				component5.textComponent = (TMP_Text)(object)textComponent;
				component5.placeholder = (Graphic)(object)placeholder;
				component5.lineType = (LineType)0;
				component5.characterLimit = 64;
				((Selectable)component5).targetGraphic = (Graphic)(object)component2;
				component5.selectionColor = new Color(0.35f, 0.62f, 1f, 0.45f);
				component5.caretColor = Color.white;
				component5.SetTextWithoutNotify(SeedManager.InputString ?? string.Empty);
				SeedManager.SetSeed(component5.text);
				((UnityEvent<string>)(object)component5.onValueChanged).AddListener((UnityAction<string>)SeedManager.SetSeed);
				UITooltip component6 = val4.GetComponent<UITooltip>();
				component6.skipLocale = true;
				component6.tipName = "Seed";
				component6.tipDesc = "Start a deterministic seeded run";
			}
		}
	}

	internal static void ConfigureSeedInputRect(RectTransform rect, RectTransform sourceRect, Vector2 originalOffsetMin, float widthFactor, float maxWidth, float spacingFactor)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rect == (Object)null) && !((Object)(object)sourceRect == (Object)null) && !((Object)(object)((Transform)sourceRect).parent == (Object)null))
		{
			Rect rect2 = sourceRect.rect;
			float num = Mathf.Min((((Rect)(ref rect2)).width + sourceRect.offsetMin.x - originalOffsetMin.x) * widthFactor, maxWidth);
			rect2 = sourceRect.rect;
			float num2 = ((Rect)(ref rect2)).height * spacingFactor;
			rect.anchorMin = sourceRect.anchorMin;
			rect.anchorMax = new Vector2(sourceRect.anchorMin.x, sourceRect.anchorMax.y);
			rect.offsetMin = originalOffsetMin;
			rect.offsetMax = new Vector2(originalOffsetMin.x + num, sourceRect.offsetMax.y);
			((Transform)rect).localRotation = ((Transform)sourceRect).localRotation;
			((Transform)rect).localScale = ((Transform)sourceRect).localScale;
			sourceRect.offsetMin = originalOffsetMin + new Vector2(num + num2, 0f);
		}
	}

	private static TextMeshProUGUI CreateSeedInputText(Transform parent, string name, string value, TMP_FontAsset font, Color color)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		TextMeshProUGUI component2 = val.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).text = value;
		((TMP_Text)component2).font = font;
		((TMP_Text)component2).fontSize = 20f;
		((TMP_Text)component2).alignment = (TextAlignmentOptions)514;
		((TMP_Text)component2).enableWordWrapping = false;
		((TMP_Text)component2).overflowMode = (TextOverflowModes)1;
		((Graphic)component2).color = color;
		((Graphic)component2).raycastTarget = false;
		return component2;
	}

	private static Transform FindExistingSeedInput(Transform parent)
	{
		if ((Object)(object)parent == (Object)null)
		{
			return null;
		}
		Transform val = parent.Find("SeedInput");
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		Transform[] componentsInChildren = ((Component)parent).GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((Object)(object)componentsInChildren[i] != (Object)null && ((Object)componentsInChildren[i]).name == "SeedInput")
			{
				return componentsInChildren[i];
			}
		}
		return null;
	}

	private static void RemoveExistingQuickLoadButtons(Transform parent)
	{
		if ((Object)(object)parent == (Object)null)
		{
			return;
		}
		for (int num = parent.childCount - 1; num >= 0; num--)
		{
			Transform child = parent.GetChild(num);
			if ((Object)(object)child != (Object)null && ((Object)child).name.StartsWith("QuickLoadSlot_"))
			{
				Object.Destroy((Object)(object)((Component)child).gameObject);
			}
		}
	}

	internal static void ConfigureQuickLoadRect(GameObject buttonObject, RectTransform sourceRect, int index, float width, float height, float spacing)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		RectTransform component = buttonObject.GetComponent<RectTransform>();
		if (!((Object)(object)component == (Object)null) && !((Object)(object)sourceRect == (Object)null) && !((Object)(object)((Transform)sourceRect).parent == (Object)null))
		{
			Vector3[] array = (Vector3[])(object)new Vector3[4];
			sourceRect.GetWorldCorners(array);
			Transform parent = ((Transform)sourceRect).parent;
			Vector3 val = parent.InverseTransformPoint(array[0]);
			Vector3 val2 = parent.InverseTransformPoint(array[2]);
			float x = val2.x;
			float num = (val.y + val2.y) * 0.5f;
			((Transform)component).SetParent(parent, false);
			component.anchorMin = new Vector2(0.5f, 0.5f);
			component.anchorMax = new Vector2(0.5f, 0.5f);
			component.pivot = new Vector2(0.5f, 0.5f);
			component.sizeDelta = new Vector2(width, height);
			((Transform)component).localPosition = new Vector3(x + spacing + width * 0.5f + (width + spacing) * (float)index, num, ((Transform)sourceRect).localPosition.z);
			((Transform)component).localRotation = ((Transform)sourceRect).localRotation;
			((Transform)component).localScale = ((Transform)sourceRect).localScale;
			LayoutElement val3 = buttonObject.GetComponent<LayoutElement>();
			if ((Object)(object)val3 == (Object)null)
			{
				val3 = buttonObject.AddComponent<LayoutElement>();
			}
			val3.ignoreLayout = true;
			val3.preferredWidth = width;
			val3.preferredHeight = height;
			val3.minWidth = width;
			val3.minHeight = height;
		}
	}

	private static void ConfigureQuickLoadFollower(GameObject buttonObject, RectTransform sourceRect, int index, float width, float height, float spacing)
	{
		QuickLoadButtonLayoutFollower quickLoadButtonLayoutFollower = buttonObject.GetComponent<QuickLoadButtonLayoutFollower>();
		if ((Object)(object)quickLoadButtonLayoutFollower == (Object)null)
		{
			quickLoadButtonLayoutFollower = buttonObject.AddComponent<QuickLoadButtonLayoutFollower>();
		}
		quickLoadButtonLayoutFollower.sourceRect = sourceRect;
		quickLoadButtonLayoutFollower.index = index;
		quickLoadButtonLayoutFollower.width = width;
		quickLoadButtonLayoutFollower.height = height;
		quickLoadButtonLayoutFollower.spacing = spacing;
	}

	private static Transform FindQuickLoadTemplate(Transform runSettings)
	{
		Transform val = runSettings.Find("Presets/Load");
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		Transform val2 = runSettings.Find("Button (7)");
		if ((Object)(object)val2 != (Object)null)
		{
			return val2;
		}
		Button[] componentsInChildren = ((Component)runSettings).GetComponentsInChildren<Button>(true);
		foreach (Button val3 in componentsInChildren)
		{
			if ((Object)(object)val3 == (Object)null)
			{
				continue;
			}
			TextMeshProUGUI componentInChildren = ((Component)val3).GetComponentInChildren<TextMeshProUGUI>(true);
			string text = (((Object)(object)componentInChildren != (Object)null) ? ((TMP_Text)componentInChildren).text : string.Empty);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string text2 = text.Replace("\n", " ").Trim().ToLowerInvariant();
				if (text2.Contains("new run") || text2.Contains("continue") || text2.Contains("start"))
				{
					return ((Component)val3).transform;
				}
			}
		}
		return null;
	}

	public static bool ShouldBlockQuickLoad(string filename)
	{
		if (!MultiplayerCompat.ShouldBlockQoLLoads)
		{
			return false;
		}
		string path = Path.Combine(SaveManager.SaveFolder, filename + ".meta");
		if (!File.Exists(path))
		{
			return true;
		}
		try
		{
			SaveMetadata saveMetadata = JsonConvert.DeserializeObject<SaveMetadata>(File.ReadAllText(path));
			if (MultiplayerCompat.IsServer && saveMetadata?.SaveKind == "MultiplayerHostBundle" && MultiplayerSaveBundle.HasBundle(SaveManager.SaveFolder, filename))
			{
				return false;
			}
			if (MultiplayerCompat.CanImportSingleplayerSlotAsHostStart && (saveMetadata == null || string.IsNullOrEmpty(saveMetadata.SaveKind) || saveMetadata.SaveKind == "Singleplayer"))
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static void BuildSlotTooltip(int slotIndex, string savePath, string metaPath, bool saveExists, out string tipName, out string tipDesc)
	{
		tipName = "Continue";
		if (!saveExists)
		{
			tipDesc = $"No expedition data in Slot {slotIndex}.";
			return;
		}
		string text = "unknown-depth";
		string text2 = "Unknown";
		string text3 = "Unknown";
		string text4 = "0h00m00s";
		if (File.Exists(metaPath))
		{
			try
			{
				SaveMetadata saveMetadata = JsonConvert.DeserializeObject<SaveMetadata>(File.ReadAllText(metaPath));
				if (saveMetadata != null)
				{
					if (!string.IsNullOrWhiteSpace(saveMetadata.LocationName))
					{
						Match match = Regex.Match(saveMetadata.LocationName, "(\\d+)\\s*m", RegexOptions.IgnoreCase);
						if (match.Success)
						{
							text = match.Groups[1].Value + "m";
						}
					}
					if (!string.IsNullOrWhiteSpace(saveMetadata.SeedInput))
					{
						text3 = saveMetadata.SeedInput.Trim();
					}
					else if (saveMetadata.SeedValue != 0)
					{
						text3 = saveMetadata.SeedValue.ToString();
					}
					if (saveMetadata.SaveKind == "MultiplayerHostBundle")
					{
						tipName = "Continue MP Host";
					}
					else if (saveMetadata.SaveKind == "MultiplayerClientInventory")
					{
						tipName = "MP Client Save";
					}
				}
			}
			catch
			{
			}
		}
		try
		{
			byte[] array = File.ReadAllBytes(savePath);
			if (array.Length != 0)
			{
				string input = SaveSystem.Unzip(array);
				Match match2 = Regex.Match(input, "\"totalTraveled\":\\s*(\\d+)");
				if (match2.Success && text == "unknown-depth")
				{
					text = match2.Groups[1].Value + "m";
				}
				Match match3 = Regex.Match(input, "\"cId\":\\s*(\\d+)");
				if (match3.Success)
				{
					text2 = match3.Groups[1].Value;
				}
				Match match4 = Regex.Match(input, "\"runTime\":\\s*([0-9.]+)");
				if (match4.Success && float.TryParse(match4.Groups[1].Value, out var result))
				{
					text4 = FormatRuntime(result);
				}
			}
		}
		catch
		{
		}
		tipDesc = "Resume your " + text + " expedition with Experiment #" + text2 + ".\n\nSeed: " + text3 + "\nIn-game time: " + text4;
	}

	private static string FormatRuntime(float seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
		return $"{(int)timeSpan.TotalHours}h{timeSpan.Minutes:00}m{timeSpan.Seconds:00}s";
	}
}
