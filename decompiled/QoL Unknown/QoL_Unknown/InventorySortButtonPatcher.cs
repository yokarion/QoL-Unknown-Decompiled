using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class InventorySortButtonPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.sortbutton", typeof(InventorySortButtonPatcher), logger);
	}

	[HarmonyPatch(typeof(PlayerCamera), "Start")]
	[HarmonyPostfix]
	private static void PlayerCamera_Start_Postfix(PlayerCamera __instance)
	{
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Expected O, but got Unknown
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Expected O, but got Unknown
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.radialMenu == (Object)null)
		{
			return;
		}
		Transform radialMenu = (Transform)(object)__instance.radialMenu;
		Transform val = radialMenu.Find("WeightImage");
		if ((Object)(object)val == (Object)null || (Object)(object)radialMenu.Find("sort") != (Object)null)
		{
			return;
		}
		GameObject val2 = Object.Instantiate<GameObject>(((Component)val).gameObject, radialMenu);
		if ((Object)(object)val2 == (Object)null)
		{
			return;
		}
		((Object)val2).name = "sort";
		RectTransform component = val2.GetComponent<RectTransform>();
		UITooltip component2 = val2.GetComponent<UITooltip>();
		RectTransform component3 = ((Component)val).GetComponent<RectTransform>();
		if ((Object)(object)component == (Object)null || (Object)(object)component2 == (Object)null || (Object)(object)component3 == (Object)null)
		{
			Object.Destroy((Object)(object)val2);
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)"Inventory sort button could not be created because the radial-menu template changed.");
			}
			return;
		}
		component2.tipName = "Sort";
		component2.tipDesc = "Packs loose items into the lightest containers. Hold Shift to also sort hands and mouth.";
		component2.skipLocale = true;
		component.sizeDelta = new Vector2(component3.sizeDelta.y, component3.sizeDelta.y);
		Rect rect = component3.rect;
		float xMin = ((Rect)(ref rect)).xMin;
		rect = component.rect;
		float num = xMin - ((Rect)(ref rect)).xMax - 4f;
		component.anchoredPosition = component3.anchoredPosition + new Vector2(num, 0f);
		foreach (Transform item in val2.transform)
		{
			Transform val3 = item;
			if ((Object)(object)((Component)val3).GetComponent<TextMeshProUGUI>() != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)val3).gameObject);
			}
		}
		Button obj = val2.GetComponent<Button>() ?? val2.AddComponent<Button>();
		((UnityEventBase)obj.onClick).RemoveAllListeners();
		ColorBlock colors = ((Selectable)obj).colors;
		((ColorBlock)(ref colors)).highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		((ColorBlock)(ref colors)).pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		((Selectable)obj).colors = colors;
		Navigation navigation = ((Selectable)obj).navigation;
		((Navigation)(ref navigation)).mode = (Mode)0;
		((Selectable)obj).navigation = navigation;
		((UnityEvent)obj.onClick).AddListener((UnityAction)delegate
		{
			InventorySortPatcher.RunSort(Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303));
			__instance.PlayUISound((UISoundType)0, 1f);
		});
	}
}
