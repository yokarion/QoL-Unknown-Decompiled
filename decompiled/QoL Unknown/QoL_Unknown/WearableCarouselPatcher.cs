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

	private const int MaxVisibleWearables = 7;

	private const float WearableSpacing = 110f;

	private const float ScrollAnimationSpeed = 14f;

	private const float WearableWidthPadding = 80f;

	private static readonly Dictionary<PlayerCamera, WearableCarouselState> States = new Dictionary<PlayerCamera, WearableCarouselState>();

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.wearablecarousel");
		val.PatchAll(typeof(WearableCarouselPatcher));
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

	private static void RefreshWearables(PlayerCamera camera, bool preserveOffset)
	{
		if ((Object)(object)camera == (Object)null)
		{
			return;
		}
		WearableCarouselState state = GetState(camera);
		if (state == null)
		{
			return;
		}
		List<Item> list = (((Object)(object)camera.body != (Object)null) ? camera.body.GetAllWearables() : null);
		int num = (state.LastWearableCount = list?.Count ?? 0);
		if (num <= 7)
		{
			state.Offset = 0;
			state.VisualOffset = 0f;
			state.TargetOffset = 0f;
			PositionVanillaButtons(camera);
			return;
		}
		if (!preserveOffset || state.Offset >= num)
		{
			state.Offset = 0;
			state.TargetOffset = 0f;
			state.VisualOffset = 0f;
		}
		state.Offset = WrapIndex(state.Offset, num);
		state.TargetOffset = WrapOffsetNearVisual(state.TargetOffset, state.Offset, num);
		state.VisualOffset = WrapOffsetNearVisual(state.VisualOffset, state.TargetOffset, num);
		ApplyAnimatedWearableLayout(camera, state, list);
	}

	private static void PositionVanillaButtons(PlayerCamera camera)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		int count = camera.unwearButtons.Count;
		for (int i = 0; i < count; i++)
		{
			GameObject val = camera.unwearButtons[i];
			if (!((Object)(object)val == (Object)null))
			{
				val.SetActive(true);
				RectTransform component = val.GetComponent<RectTransform>();
				if ((Object)(object)component != (Object)null)
				{
					component.anchoredPosition = GetWearableButtonPosition(i, count);
				}
			}
		}
	}

	private static Vector2 GetWearableButtonPosition(int index, int totalVisible)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2((float)index * 110f - ((float)totalVisible - 1f) * 55f, 0f);
	}

	private static float GetCenteredWearablePosition(float slotPosition, int totalVisible, float spacing)
	{
		return (slotPosition - ((float)totalVisible - 1f) * 0.5f) * spacing;
	}

	private static float GetAnimatedWearableSpacing(PlayerCamera camera, int visibleCount)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (visibleCount <= 1)
		{
			return 110f;
		}
		RectTransform val = (((Object)(object)camera != (Object)null && (Object)(object)camera.mainCanvas != (Object)null) ? ((Component)camera.mainCanvas).GetComponent<RectTransform>() : null);
		float num;
		if (!((Object)(object)val != (Object)null))
		{
			num = 770f;
		}
		else
		{
			Rect rect = val.rect;
			num = Mathf.Max(110f, ((Rect)(ref rect)).width - 80f);
		}
		float num2 = num;
		return Mathf.Min(110f, num2 / ((float)visibleCount - 1f));
	}

	private static void ApplyAnimatedWearableLayout(PlayerCamera camera, WearableCarouselState state, List<Item> wearables)
	{
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		int num = wearables?.Count ?? 0;
		if (num <= 7)
		{
			return;
		}
		int num2 = Mathf.Min(num, camera.unwearButtons.Count);
		float animatedWearableSpacing = GetAnimatedWearableSpacing(camera, num2);
		int num3 = Mathf.FloorToInt(state.VisualOffset);
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
			if (!flag)
			{
				continue;
			}
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
				float num5 = (float)i - num4;
				if (num5 < -0.5f)
				{
					num5 += (float)num2;
				}
				component2.anchoredPosition = new Vector2(GetCenteredWearablePosition(num5, num2, animatedWearableSpacing), 0f);
			}
		}
	}

	private static bool IsPointerOverWearableRow(PlayerCamera camera)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
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

	private static float WrapOffsetNearVisual(float currentValue, float targetValue, int count)
	{
		if (count <= 0)
		{
			return 0f;
		}
		float result = targetValue;
		float num = Mathf.Abs(targetValue - currentValue);
		float num2 = targetValue + (float)count;
		float num3 = Mathf.Abs(num2 - currentValue);
		if (num3 < num)
		{
			result = num2;
			num = num3;
		}
		float num4 = targetValue - (float)count;
		float num5 = Mathf.Abs(num4 - currentValue);
		if (num5 < num)
		{
			result = num4;
		}
		return result;
	}

	[HarmonyPatch(typeof(PlayerCamera), "UpdateWearables")]
	[HarmonyPostfix]
	private static void UpdateWearables_Postfix(PlayerCamera __instance)
	{
		RefreshWearables(__instance, preserveOffset: true);
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPriority(800)]
	[HarmonyPostfix]
	private static void Update_Postfix(PlayerCamera __instance)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.body == (Object)null || (Object)(object)__instance.woundView == (Object)null)
		{
			return;
		}
		WearableCarouselState state = GetState(__instance);
		if (state != null && state.LastWearableCount > 7 && !PauseHandler.paused && !__instance.woundView.activeSelf && !__instance.tradeMenu.activeSelf)
		{
			UIScrollInputRouter.ResetFrame();
			float y = Input.mouseScrollDelta.y;
			if (IsPointerOverWearableRow(__instance) && Mathf.Abs(y) > 0.01f && UIScrollInputRouter.TryConsume())
			{
				state.Offset = WrapIndex(state.Offset + ((!(y > 0f)) ? 1 : (-1)), state.LastWearableCount);
				state.TargetOffset = WrapOffsetNearVisual(state.VisualOffset, state.Offset, state.LastWearableCount);
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
