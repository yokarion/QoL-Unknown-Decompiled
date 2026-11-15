using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class CameraZoomPatcher
{
	private static bool enabled;

	private static Camera zoomCamera;

	private static PixelPerfectCamera pixelPerfectCamera;

	private static GameObject sightLimiter;

	private static float originalSize;

	private static float targetScale = 1f;

	private static float currentScale = 1f;

	private static float zoomVelocity;

	public static void Initialize(ManualLogSource logger)
	{
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.camerazoom", typeof(CameraZoomPatcher), logger);
	}

	internal static void SetEnabled(bool value)
	{
		enabled = value;
		if (!enabled)
		{
			RestoreCamera();
		}
	}

	private static void RestoreCamera()
	{
		if ((Object)(object)zoomCamera != (Object)null)
		{
			zoomCamera.orthographicSize = originalSize;
		}
		if ((Object)(object)pixelPerfectCamera != (Object)null)
		{
			((Behaviour)pixelPerfectCamera).enabled = Settings.Get<SettingBool>("pixelate").value;
		}
		if ((Object)(object)sightLimiter != (Object)null)
		{
			sightLimiter.SetActive(true);
		}
		zoomCamera = null;
		pixelPerfectCamera = null;
		sightLimiter = null;
		targetScale = (currentScale = 1f);
		zoomVelocity = 0f;
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleCameraPosition")]
	[HarmonyPrefix]
	private static void HandleCameraPositionPrefix(PlayerCamera __instance)
	{
		if (!enabled || (Object)(object)__instance != (Object)(object)PlayerCamera.main || (Object)(object)__instance.body == (Object)null || (Object)(object)WorldGeneration.world == (Object)null)
		{
			return;
		}
		Camera component = ((Component)__instance).GetComponent<Camera>();
		if ((Object)(object)component == (Object)null || !component.orthographic)
		{
			return;
		}
		if ((Object)(object)zoomCamera != (Object)(object)component)
		{
			RestoreCamera();
			zoomCamera = component;
			pixelPerfectCamera = ((Component)__instance).GetComponent<PixelPerfectCamera>();
			originalSize = component.orthographicSize;
			Transform obj = ((Component)component).transform.Find("SightLimiter");
			sightLimiter = ((obj != null) ? ((Component)obj).gameObject : null);
			if ((Object)(object)sightLimiter != (Object)null)
			{
				sightLimiter.SetActive(false);
			}
		}
		if ((Object)(object)pixelPerfectCamera != (Object)null)
		{
			((Behaviour)pixelPerfectCamera).enabled = false;
		}
		float num = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
		if (CanReadInput())
		{
			bool flag = Input.GetKey((KeyCode)61) || Input.GetKey((KeyCode)43) || Input.GetKey((KeyCode)270);
			bool flag2 = Input.GetKey((KeyCode)45) || Input.GetKey((KeyCode)269);
			targetScale = Mathf.Clamp(targetScale + ((flag2 ? 1f : 0f) - (flag ? 1f : 0f)) * num, 0.5f, 2f);
		}
		currentScale = Mathf.SmoothDamp(currentScale, targetScale, ref zoomVelocity, 0.12f, float.PositiveInfinity, num);
		float num2 = Mathf.Min((float)WorldGeneration.world.height * 0.5f, (float)WorldGeneration.world.width * 0.5f / component.aspect);
		component.orthographicSize = Mathf.Min(originalSize * currentScale, num2);
	}

	private static bool CanReadInput()
	{
		if (!Application.isFocused || PauseHandler.paused || ((Object)(object)ConsoleScript.instance != (Object)null && ConsoleScript.instance.active))
		{
			return false;
		}
		GameObject val = (((Object)(object)EventSystem.current != (Object)null) ? EventSystem.current.currentSelectedGameObject : null);
		if ((Object)(object)val != (Object)null)
		{
			TMP_InputField componentInParent = val.GetComponentInParent<TMP_InputField>();
			InputField componentInParent2 = val.GetComponentInParent<InputField>();
			if (((Object)(object)componentInParent != (Object)null && componentInParent.isFocused) || ((Object)(object)componentInParent2 != (Object)null && componentInParent2.isFocused))
			{
				return false;
			}
		}
		return true;
	}
}
