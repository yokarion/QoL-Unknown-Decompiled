using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class ControllerItemHighlightPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		Logger = logger;
		GameObject val = new GameObject("ItemHighlightManager");
		Object.DontDestroyOnLoad((Object)(object)val);
		val.AddComponent<ItemHighlightManager>();
		Harmony val2 = new Harmony("org.bepinex.plugins.qol.itemhighlight");
		val2.PatchAll(typeof(ControllerItemHighlightPatcher));
		Logger.LogInfo((object)"... with its patchers initialized");
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPrefix]
	private static void Update_Prefix(PlayerCamera __instance)
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ControllerInputManager.Instance == (Object)null || !ControllerInputManager.Instance.IsControllerActive || (Object)(object)__instance.body == (Object)null || !__instance.body.alive || Time.timeScale == 0f || (Object)(object)__instance.dragItem != (Object)null || !Input.GetMouseButtonDown(2) || !((Object)(object)ItemHighlightManager.Instance.HighlightedItem != (Object)null))
		{
			return;
		}
		Item highlightedItem = ItemHighlightManager.Instance.HighlightedItem;
		float num = Vector2.Distance(Vector2.op_Implicit(((Component)__instance.body).transform.position), Vector2.op_Implicit(((Component)highlightedItem).transform.position));
		float num2 = 10f;
		if (num > num2)
		{
			__instance.PlayUISound((UISoundType)6, 1f);
			return;
		}
		for (int i = 0; i < __instance.body.slots.Length; i++)
		{
			if (!__instance.body.HoldingItem(i))
			{
				__instance.body.PickUpItem(highlightedItem, i, true);
				__instance.PlayUISound((UISoundType)0, 1f);
				break;
			}
		}
	}
}
