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
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Expected O, but got Unknown
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Expected O, but got Unknown
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
		float num = component3.sizeDelta.x - 35f;
		component.anchoredPosition = component3.anchoredPosition + new Vector2(num, 0f);
		component.sizeDelta = new Vector2(component3.sizeDelta.y, component3.sizeDelta.y);
		foreach (Transform item in val2.transform)
		{
			Transform val3 = item;
			if ((Object)(object)((Component)val3).GetComponent<TextMeshProUGUI>() != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)val3).gameObject);
			}
		}
		Button val4 = val2.GetComponent<Button>() ?? val2.AddComponent<Button>();
		((UnityEventBase)val4.onClick).RemoveAllListeners();
		ColorBlock colors = ((Selectable)val4).colors;
		((ColorBlock)(ref colors)).highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		((ColorBlock)(ref colors)).pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		((Selectable)val4).colors = colors;
		Navigation navigation = ((Selectable)val4).navigation;
		((Navigation)(ref navigation)).mode = (Mode)0;
		((Selectable)val4).navigation = navigation;
		((UnityEvent)val4.onClick).AddListener((UnityAction)delegate
		{
			bool includeHandsAndMouth = Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
			InventorySortPatcher.RunSort(includeHandsAndMouth);
			__instance.PlayUISound((UISoundType)0, 1f);
		});
	}
}
