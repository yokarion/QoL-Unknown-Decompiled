using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class ContainerScrollPatcher
{
	private static ManualLogSource Logger;

	private static float currentScrollY;

	private static float targetScrollY;

	private static Vector2 textOrigPos;

	private static Vector2 populateOrigPos;

	private static Vector2 imageOrigPos;

	private static bool positionsCaptured;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.containerscroll");
		val.PatchAll(typeof(ContainerScrollPatcher));
	}

	[HarmonyPatch(typeof(PlayerCamera), "OpenContainer")]
	[HarmonyPostfix]
	private static void OpenContainer_Postfix(PlayerCamera __instance)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (!positionsCaptured)
		{
			textOrigPos = ((Component)__instance.containerText).GetComponent<RectTransform>().anchoredPosition;
			populateOrigPos = __instance.contPopulate.GetComponent<RectTransform>().anchoredPosition;
			Transform obj = __instance.containerMenu.transform.Find("Image");
			RectTransform val = ((obj != null) ? ((Component)obj).GetComponent<RectTransform>() : null);
			if ((Object)(object)val != (Object)null)
			{
				imageOrigPos = val.anchoredPosition;
			}
			positionsCaptured = true;
		}
		currentScrollY = 0f;
		targetScrollY = 0f;
		ApplyScroll(__instance);
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPriority(0)]
	[HarmonyPostfix]
	private static void Update_Postfix(PlayerCamera __instance)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (!__instance.containerMenu.activeSelf || (Object)(object)__instance.currentContainer == (Object)null)
		{
			return;
		}
		UIScrollInputRouter.ResetFrame();
		if (Input.mousePosition.x > (float)Screen.width * 0.5f)
		{
			float num = Input.mouseScrollDelta.y * -1f;
			if (Mathf.Abs(num) > 0.01f && !UIScrollInputRouter.IsConsumed() && UIScrollInputRouter.TryConsume())
			{
				targetScrollY += num * 64f;
			}
		}
		int itemCount = __instance.currentContainer.itemCount;
		int num2 = Mathf.CeilToInt((float)itemCount / 2f);
		float num3 = Mathf.Max(0f, (float)(num2 - 5) * 64f);
		targetScrollY = Mathf.Clamp(targetScrollY, 0f, num3);
		if (Mathf.Abs(currentScrollY - targetScrollY) > 0.01f)
		{
			currentScrollY = Mathf.Lerp(currentScrollY, targetScrollY, Time.unscaledDeltaTime * 20f);
			ApplyScroll(__instance);
		}
	}

	private static void ApplyScroll(PlayerCamera cam)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (positionsCaptured)
		{
			cam.contPopulate.GetComponent<RectTransform>().anchoredPosition = populateOrigPos + new Vector2(0f, Mathf.Round(currentScrollY));
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "RepopulateContainer")]
	[HarmonyPostfix]
	private static void RepopulateContainer_Postfix(PlayerCamera __instance)
	{
		if (!((Object)(object)__instance.currentContainer == (Object)null))
		{
			int num = Mathf.CeilToInt((float)__instance.currentContainer.itemCount / 2f);
			float num2 = Mathf.Max(0f, (float)(num - 5) * 64f);
			if (targetScrollY > num2)
			{
				targetScrollY = num2;
			}
			ApplyScroll(__instance);
		}
	}
}
