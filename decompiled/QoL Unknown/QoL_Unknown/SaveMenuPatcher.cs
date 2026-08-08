using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
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
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static UnityAction _003C_003E9__5_1;

		internal void _003CCreateQuickLoadButtons_003Eb__5_1()
		{
			SaveSaveMenuPatcher.DoLoad("autosave.sv", 0);
		}
	}

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if (!initialized)
		{
			initialized = true;
			Logger = logger;
			Harmony val = new Harmony("org.bepinex.plugins.qol.savemenu");
			val.PatchAll(typeof(SaveMenuPatcher));
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
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_048b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Expected O, but got Unknown
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Expected O, but got Unknown
		GameObject val = GameObject.Find("Canvas");
		if ((Object)(object)val == (Object)null)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)"[SaveMenuPatcher] Canvas not found; skipping quick load button creation.");
			}
			return;
		}
		Transform val2 = val.transform.Find("RunSettings");
		if ((Object)(object)val2 == (Object)null)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogWarning((object)"[SaveMenuPatcher] RunSettings container not found; skipping quick load button creation.");
			}
			return;
		}
		Transform val3 = FindQuickLoadTemplate(val2);
		if ((Object)(object)val3 == (Object)null)
		{
			ManualLogSource logger3 = Logger;
			if (logger3 != null)
			{
				logger3.LogWarning((object)"[SaveMenuPatcher] Source quick load button template not found; skipping quick load button creation.");
			}
			return;
		}
		GameObject gameObject = ((Component)val3).gameObject;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		if ((Object)(object)component == (Object)null)
		{
			ManualLogSource logger4 = Logger;
			if (logger4 != null)
			{
				logger4.LogWarning((object)"[SaveMenuPatcher] Source quick load button has no RectTransform; skipping quick load button creation.");
			}
			return;
		}
		RemoveExistingQuickLoadButtons(gameObject.transform.parent);
		Rect rect = component.rect;
		float width = ((Rect)(ref rect)).width;
		rect = component.rect;
		float height = ((Rect)(ref rect)).height;
		float spacing = height * 0.08f;
		float width2 = height;
		CreateSeedInput(val.transform);
		for (int i = 1; i <= 3; i++)
		{
			GameObject val4 = Object.Instantiate<GameObject>(gameObject, gameObject.transform.parent);
			((Object)val4).name = $"QuickLoadSlot_{i}";
			Button component2 = val4.GetComponent<Button>();
			((UnityEventBase)component2.onClick).RemoveAllListeners();
			((Selectable)component2).interactable = true;
			ConfigureQuickLoadRect(val4, component, i - 1, width2, height, spacing);
			ConfigureQuickLoadFollower(val4, component, i - 1, width2, height, spacing);
			val4.SetActive(true);
			TextMeshProUGUI[] componentsInChildren = val4.GetComponentsInChildren<TextMeshProUGUI>(true);
			TextMeshProUGUI[] array = componentsInChildren;
			foreach (TextMeshProUGUI val5 in array)
			{
				((TMP_Text)val5).text = $"{i}";
				((TMP_Text)val5).fontSize = 24f;
				((TMP_Text)val5).enableAutoSizing = false;
				((Graphic)val5).color = Color.white;
				RectTransform component3 = ((Component)val5).GetComponent<RectTransform>();
				if ((Object)(object)component3 != (Object)null)
				{
					component3.anchorMin = Vector2.zero;
					component3.anchorMax = Vector2.one;
					component3.offsetMin = Vector2.zero;
					component3.offsetMax = Vector2.zero;
					component3.anchoredPosition = Vector2.zero;
					component3.sizeDelta = Vector2.zero;
				}
				SaveSlotButtonTextEnforcer saveSlotButtonTextEnforcer = ((Component)val5).gameObject.AddComponent<SaveSlotButtonTextEnforcer>();
				saveSlotButtonTextEnforcer.textToEnforce = $"{i}";
			}
			string filename = $"slot_{i}.sv";
			string text = Path.Combine(SaveManager.SaveFolder, filename);
			string metaPath = text + ".meta";
			bool flag = File.Exists(text);
			BuildSlotTooltip(i, text, metaPath, flag, out var tipName, out var tipDesc);
			UITooltip val6 = val4.GetComponent<UITooltip>();
			if ((Object)(object)val6 == (Object)null)
			{
				val6 = val4.AddComponent<UITooltip>();
			}
			val6.skipLocale = true;
			val6.tipName = tipName;
			val6.tipDesc = tipDesc;
			if (!flag)
			{
				((Selectable)component2).interactable = false;
			}
			ApplyQuickLoadLayerIcon(val4, LoadLayerIcon(metaPath), flag);
			int slotIndex = i;
			((UnityEvent)component2.onClick).AddListener((UnityAction)delegate
			{
				SaveSaveMenuPatcher.DoLoad(filename, slotIndex);
			});
		}
		int num = 4;
		GameObject val7 = Object.Instantiate<GameObject>(gameObject, gameObject.transform.parent);
		((Object)val7).name = "QuickLoadSlot_A";
		Button component4 = val7.GetComponent<Button>();
		((UnityEventBase)component4.onClick).RemoveAllListeners();
		ConfigureQuickLoadRect(val7, component, num - 1, width2, height, spacing);
		ConfigureQuickLoadFollower(val7, component, num - 1, width2, height, spacing);
		val7.SetActive(true);
		TextMeshProUGUI[] componentsInChildren2 = val7.GetComponentsInChildren<TextMeshProUGUI>(true);
		TextMeshProUGUI[] array2 = componentsInChildren2;
		foreach (TextMeshProUGUI val8 in array2)
		{
			((TMP_Text)val8).text = "A";
			((TMP_Text)val8).fontSize = 24f;
			((TMP_Text)val8).enableAutoSizing = false;
			((Graphic)val8).color = Color.white;
			RectTransform component5 = ((Component)val8).GetComponent<RectTransform>();
			if ((Object)(object)component5 != (Object)null)
			{
				component5.anchorMin = Vector2.zero;
				component5.anchorMax = Vector2.one;
				component5.offsetMin = Vector2.zero;
				component5.offsetMax = Vector2.zero;
				component5.anchoredPosition = Vector2.zero;
				component5.sizeDelta = Vector2.zero;
			}
			SaveSlotButtonTextEnforcer saveSlotButtonTextEnforcer2 = ((Component)val8).gameObject.AddComponent<SaveSlotButtonTextEnforcer>();
			saveSlotButtonTextEnforcer2.textToEnforce = "A";
		}
		string text2 = Path.Combine(SaveManager.SaveFolder, "autosave.sv");
		string metaPath2 = text2 + ".meta";
		ApplyQuickLoadLayerIcon(saveExists: ((Selectable)component4).interactable = File.Exists(text2), buttonObject: val7, layerSprite: LoadLayerIcon(metaPath2));
		ButtonClickedEvent onClick = component4.onClick;
		object obj = _003C_003Ec._003C_003E9__5_1;
		if (obj == null)
		{
			UnityAction val9 = delegate
			{
				SaveSaveMenuPatcher.DoLoad("autosave.sv", 0);
			};
			_003C_003Ec._003C_003E9__5_1 = val9;
			obj = (object)val9;
		}
		((UnityEvent)onClick).AddListener((UnityAction)obj);
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
			return (saveMetadata != null) ? Helper.LoadEmbeddedSprite($"layer{saveMetadata.BiomeIndex}ui") : null;
		}
		catch
		{
			return null;
		}
	}

	private static void ApplyQuickLoadLayerIcon(GameObject buttonObject, Sprite layerSprite, bool saveExists)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
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
		foreach (TextMeshProUGUI val2 in componentsInChildren)
		{
			((TMP_Text)val2).transform.SetAsLastSibling();
		}
	}

	private static Image EnsureQuickLoadLayerIcon(GameObject buttonObject)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
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
			Transform child = buttonObject.transform.GetChild(i);
			Image component = ((Component)child).GetComponent<Image>();
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
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Expected O, but got Unknown
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		Transform val = FindExistingSeedInput(canvas);
		if ((Object)(object)val != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)val).gameObject);
		}
		Button val2 = FindButtonByText(canvas, "start");
		RectTransform val3 = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponent<RectTransform>() : null);
		if ((Object)(object)val3 == (Object)null || (Object)(object)((Transform)val3).parent == (Object)null)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)"[SaveMenuPatcher] Start button not found; skipping seed input creation.");
			}
			return;
		}
		GameObject val4 = new GameObject("SeedInput", new Type[4]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(TMP_InputField),
			typeof(UITooltip)
		});
		val4.transform.SetParent(((Transform)val3).parent, false);
		RectTransform component = val4.GetComponent<RectTransform>();
		RectTransform sourceRect = val3;
		ConfigureSeedInputRect(component, sourceRect, 0.32f, 175f, 0.16f);
		SeedInputLayoutFollower seedInputLayoutFollower = val4.AddComponent<SeedInputLayoutFollower>();
		seedInputLayoutFollower.sourceRect = sourceRect;
		seedInputLayoutFollower.widthFactor = 0.32f;
		seedInputLayoutFollower.maxWidth = 175f;
		seedInputLayoutFollower.spacingFactor = 0.16f;
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

	internal static void ConfigureSeedInputRect(RectTransform rect, RectTransform sourceRect, float widthFactor, float maxWidth, float spacingFactor)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rect == (Object)null) && !((Object)(object)sourceRect == (Object)null) && !((Object)(object)((Transform)sourceRect).parent == (Object)null))
		{
			Vector3[] array = (Vector3[])(object)new Vector3[4];
			sourceRect.GetWorldCorners(array);
			Transform parent = ((Transform)sourceRect).parent;
			Vector3 val = parent.InverseTransformPoint(array[0]);
			Vector3 val2 = parent.InverseTransformPoint(array[2]);
			float num = val2.y - val.y;
			float num2 = Mathf.Min((val2.x - val.x) * widthFactor, maxWidth);
			float num3 = num * spacingFactor;
			((Transform)rect).SetParent(parent, false);
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(num2, num);
			((Transform)rect).localPosition = new Vector3(val.x - num3 - num2 * 0.5f, (val.y + val2.y) * 0.5f, ((Transform)sourceRect).localPosition.z);
			((Transform)rect).localRotation = ((Transform)sourceRect).localRotation;
			((Transform)rect).localScale = ((Transform)sourceRect).localScale;
		}
	}

	private static TextMeshProUGUI CreateSeedInputText(Transform parent, string name, string value, TMP_FontAsset font, Color color)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
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

	private static Button FindButtonByText(Transform parent, string expectedText)
	{
		Button[] componentsInChildren = ((Component)parent).GetComponentsInChildren<Button>(true);
		Button[] array = componentsInChildren;
		foreach (Button val in array)
		{
			TextMeshProUGUI componentInChildren = ((Component)val).GetComponentInChildren<TextMeshProUGUI>(true);
			string text = (((Object)(object)componentInChildren != (Object)null) ? ((TMP_Text)componentInChildren).text.Replace("\n", " ").Trim().ToLowerInvariant() : string.Empty);
			if (text == expectedText)
			{
				return val;
			}
		}
		return null;
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
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
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
		Button[] array = componentsInChildren;
		foreach (Button val3 in array)
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

	[HarmonyPatch(typeof(PreRunScript), "LoadRun")]
	[HarmonyPrefix]
	private static bool LoadRun_Prefix(PreRunScript __instance)
	{
		if (KrokoshaMpCompat.ShouldBlockQoLLoads)
		{
			KrokoshaMpCompat.NotifyLoadBlocked();
			return false;
		}
		return true;
	}

	public static bool ShouldBlockQuickLoad(string filename)
	{
		if (!KrokoshaMpCompat.ShouldBlockQoLLoads)
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
			if (KrokoshaMpCompat.IsServer && saveMetadata?.SaveKind == "MultiplayerHostBundle" && KrokoshaMpSaveBundle.HasBundle(SaveManager.SaveFolder, filename))
			{
				return false;
			}
			if (KrokoshaMpCompat.CanImportSingleplayerSlotAsHostStart && (saveMetadata == null || string.IsNullOrEmpty(saveMetadata.SaveKind) || saveMetadata.SaveKind == "Singleplayer"))
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
