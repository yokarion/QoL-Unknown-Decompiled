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
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.sortbutton");
		val.PatchAll(typeof(InventorySortButtonPatcher));
	}

	[HarmonyPatch(typeof(PlayerCamera), "Start")]
	[HarmonyPostfix]
	private static void PlayerCamera_Start_Postfix(PlayerCamera __instance)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Expected O, but got Unknown
		Transform radialMenu = (Transform)(object)__instance.radialMenu;
		Transform val = radialMenu.Find("WeightImage");
		GameObject val2 = Object.Instantiate<GameObject>(((Component)val).gameObject, radialMenu);
		((Object)val2).name = "sort";
		RectTransform component = val2.GetComponent<RectTransform>();
		UITooltip component2 = val2.GetComponent<UITooltip>();
		component2.tipName = "Sort";
		component2.tipDesc = "Sorts your inventory to minimize weight use";
		component2.skipLocale = true;
		RectTransform component3 = ((Component)val).GetComponent<RectTransform>();
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
		Button val4 = val2.AddComponent<Button>();
		ColorBlock colors = ((Selectable)val4).colors;
		((ColorBlock)(ref colors)).highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		((ColorBlock)(ref colors)).pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		((Selectable)val4).colors = colors;
		Navigation navigation = ((Selectable)val4).navigation;
		((Navigation)(ref navigation)).mode = (Mode)0;
		((Selectable)val4).navigation = navigation;
		((UnityEvent)val4.onClick).AddListener((UnityAction)delegate
		{
			InventorySortPatcher.RunSort(safeMode: true);
			__instance.PlayUISound((UISoundType)0, 1f);
		});
	}
}
