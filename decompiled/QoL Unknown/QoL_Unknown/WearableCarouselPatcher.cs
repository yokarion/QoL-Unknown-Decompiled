using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class WearableCarouselPatcher
{
	private sealed class WearableCarouselState
	{
		public int Offset;

		public int LastWearableCount;

		public float TargetOffset;

		public float VisualOffset;
	}

	private const int ItemsPerRow = 10;

	private const int MaxVisibleRows = 2;

	private const int MaxVisibleWearables = 20;

	private const float WearableSpacing = 110f;

	private const float ScrollAnimationSpeed = 14f;

	private static readonly Dictionary<PlayerCamera, WearableCarouselState> States = new Dictionary<PlayerCamera, WearableCarouselState>();

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.wearablecarousel").PatchAll(typeof(WearableCarouselPatcher));
	}

	private static WearableCarouselState GetState(PlayerCamera camera)
	{
		if ((Object)(object)camera == (Object)null)
		{
			return null;
		}
		if (!States.TryGetValue(camera, out var value) || value == null)
		{
			value = new WearableCarouselState();
			States[camera] = value;
		}
		return value;
	}

	private static void RefreshWearables(PlayerCamera camera)
	{
		if ((Object)(object)camera == (Object)null)
		{
			return;
		}
		WearableCarouselState state = GetState(camera);
		if (state != null)
		{
			List<Item> list = (((Object)(object)camera.body != (Object)null) ? camera.body.GetAllWearables() : null);
			if ((state.LastWearableCount = list?.Count ?? 0) <= 20)
			{
				state.Offset = 0;
				state.VisualOffset = 0f;
				state.TargetOffset = 0f;
			}
			else
			{
				ApplyAnimatedWearableLayout(camera, state, list);
			}
		}
	}

	private static void ApplyAnimatedWearableLayout(PlayerCamera camera, WearableCarouselState state, List<Item> wearables)
	{
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		int num = wearables?.Count ?? 0;
		if (num <= 20)
		{
			return;
		}
		int num2 = Mathf.Min(20, camera.unwearButtons.Count);
		int num3 = Mathf.FloorToInt(state.VisualOffset / 10f) * 10;
		float num4 = state.VisualOffset - (float)num3;
		for (int i = 0; i < camera.unwearButtons.Count; i++)
		{
			GameObject val = camera.unwearButtons[i];
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			bool flag = i < num2;
			if (val.activeSelf != flag)
			{
				val.SetActive(flag);
			}
			if (flag)
			{
				int index = WrapIndex(num3 + i, num);
				InvButton component = val.GetComponent<InvButton>();
				if ((Object)(object)component != (Object)null && (Object)(object)component.refItem != (Object)(object)wearables[index])
				{
					component.refItem = wearables[index];
					component.UpdateRefs();
					component.UpdateGraphic();
				}
				RectTransform component2 = val.GetComponent<RectTransform>();
				if (!((Object)(object)component2 == (Object)null))
				{
					int num5 = i / 10;
					int num6 = Mathf.Min(10, num2 - num5 * 10);
					float num7 = ((float)(i % 10) - ((float)num6 - 1f) * 0.5f) * 110f;
					float num8 = (float)(-num5) * 110f + num4 * 11f;
					component2.anchoredPosition = new Vector2(num7, num8);
				}
			}
		}
	}

	private static bool IsPointerOverWearableRow(PlayerCamera camera)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)camera == (Object)null || camera.unwearButtons == null || camera.unwearButtons.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < camera.unwearButtons.Count; i++)
		{
			GameObject val = camera.unwearButtons[i];
			if (!((Object)(object)val == (Object)null))
			{
				RectTransform component = val.GetComponent<RectTransform>();
				if (!((Object)(object)component == (Object)null) && ((Component)component).gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(component, Vector2.op_Implicit(Input.mousePosition), (Camera)null))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static int WrapIndex(int index, int count)
	{
		if (count <= 0)
		{
			return 0;
		}
		int num = index % count;
		if (num < 0)
		{
			num += count;
		}
		return num;
	}

	[HarmonyPatch(typeof(PlayerCamera), "UpdateWearables")]
	[HarmonyPostfix]
	private static void UpdateWearables_Postfix(PlayerCamera __instance)
	{
		RefreshWearables(__instance);
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPriority(800)]
	[HarmonyPostfix]
	private static void Update_Postfix(PlayerCamera __instance)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.body == (Object)null || (Object)(object)__instance.woundView == (Object)null)
		{
			return;
		}
		WearableCarouselState state = GetState(__instance);
		if (state != null && state.LastWearableCount > 20 && !PauseHandler.paused && !__instance.woundView.activeSelf && !__instance.tradeMenu.activeSelf)
		{
			UIScrollInputRouter.ResetFrame();
			float y = Input.mouseScrollDelta.y;
			if (IsPointerOverWearableRow(__instance) && Mathf.Abs(y) > 0.01f && UIScrollInputRouter.TryConsume())
			{
				state.Offset += ((y > 0f) ? (-10) : 10);
				state.TargetOffset = state.Offset;
			}
			state.VisualOffset = Mathf.Lerp(state.VisualOffset, state.TargetOffset, Time.unscaledDeltaTime * 14f);
			if (Mathf.Abs(state.TargetOffset - state.VisualOffset) < 0.001f)
			{
				state.VisualOffset = state.TargetOffset;
			}
			List<Item> allWearables = __instance.body.GetAllWearables();
			state.LastWearableCount = allWearables?.Count ?? 0;
			ApplyAnimatedWearableLayout(__instance, state, allWearables);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "Awake")]
	[HarmonyPrefix]
	private static void Awake_Prefix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance != (Object)null)
		{
			States.Remove(__instance);
		}
	}
}
