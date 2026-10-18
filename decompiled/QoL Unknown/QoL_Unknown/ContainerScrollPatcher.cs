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

	private static PlayerCamera capturedCamera;

	private static RectTransform populateRect;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.containerscroll", typeof(ContainerScrollPatcher), logger);
	}

	[HarmonyPatch(typeof(PlayerCamera), "OpenContainer")]
	[HarmonyPostfix]
	private static void OpenContainer_Postfix(PlayerCamera __instance)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.containerMenu == (Object)null || (Object)(object)__instance.containerText == (Object)null || (Object)(object)__instance.contPopulate == (Object)null)
		{
			return;
		}
		if (!positionsCaptured || (Object)(object)capturedCamera != (Object)(object)__instance || (Object)(object)populateRect == (Object)null)
		{
			RectTransform component = ((Component)__instance.containerText).GetComponent<RectTransform>();
			populateRect = __instance.contPopulate.GetComponent<RectTransform>();
			if ((Object)(object)component == (Object)null || (Object)(object)populateRect == (Object)null)
			{
				positionsCaptured = false;
				return;
			}
			textOrigPos = component.anchoredPosition;
			populateOrigPos = populateRect.anchoredPosition;
			Transform obj = __instance.containerMenu.transform.Find("Image");
			RectTransform val = ((obj != null) ? ((Component)obj).GetComponent<RectTransform>() : null);
			if ((Object)(object)val != (Object)null)
			{
				imageOrigPos = val.anchoredPosition;
			}
			positionsCaptured = true;
			capturedCamera = __instance;
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
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.containerMenu == (Object)null || !__instance.containerMenu.activeSelf || (Object)(object)__instance.currentContainer == (Object)null)
		{
			return;
		}
		if (Input.mousePosition.x > (float)Screen.width * 0.5f)
		{
			float num = Input.mouseScrollDelta.y * -1f;
			if (Mathf.Abs(num) > 0.01f && UIScrollInputRouter.TryConsume())
			{
				targetScrollY += num * 64f;
			}
		}
		float maxScroll = GetMaxScroll(__instance);
		targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxScroll);
		if (Mathf.Abs(currentScrollY - targetScrollY) > 0.01f)
		{
			currentScrollY = Mathf.Lerp(currentScrollY, targetScrollY, Time.unscaledDeltaTime * 20f);
			ApplyScroll(__instance);
		}
	}

	private static void ApplyScroll(PlayerCamera cam)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (positionsCaptured)
		{
			if ((Object)(object)cam != (Object)(object)capturedCamera || (Object)(object)populateRect == (Object)null)
			{
				positionsCaptured = false;
			}
			else
			{
				populateRect.anchoredPosition = populateOrigPos + new Vector2(0f, Mathf.Round(currentScrollY));
			}
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "RepopulateContainer")]
	[HarmonyPostfix]
	private static void RepopulateContainer_Postfix(PlayerCamera __instance)
	{
		if (!((Object)(object)__instance.currentContainer == (Object)null))
		{
			float maxScroll = GetMaxScroll(__instance);
			if (targetScrollY > maxScroll)
			{
				targetScrollY = maxScroll;
			}
			ApplyScroll(__instance);
		}
	}

	private static float GetMaxScroll(PlayerCamera camera)
	{
		if (ContainerStackingPatcher.TryGetMaxScroll(camera, out var maxScroll))
		{
			return maxScroll;
		}
		int num = Mathf.CeilToInt((float)camera.currentContainer.itemCount / 2f);
		return Mathf.Max(0f, (float)(num - 5) * 64f);
	}
}
