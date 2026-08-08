using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

internal sealed class UnityJoystickControllerSource : IControllerInputSource
{
	private const float TriggerThreshold = 0.5f;

	private const float DpadThreshold = 0.5f;

	private const float DeviceScanInterval = 0.5f;

	private static readonly HashSet<string> MissingAxes = new HashSet<string>(StringComparer.Ordinal);

	private int activeJoystick = 1;

	private float nextDeviceScanTime;

	private string cachedDeviceName = string.Empty;

	public string SourceName => "UnityLegacyInput";

	public bool TryRead(out RawControllerState rawState, out string deviceName)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		rawState = default(RawControllerState);
		deviceName = string.Empty;
		string deviceName2;
		int activeJoystickIndexCached = GetActiveJoystickIndexCached(out deviceName2);
		if (activeJoystickIndexCached <= 0)
		{
			return false;
		}
		activeJoystick = activeJoystickIndexCached;
		deviceName = deviceName2;
		rawState.LeftStick = new Vector2(GetAxisOrDefault("Horizontal"), GetAxisOrDefault("Vertical"));
		rawState.RightStick = ReadRightStick();
		rawState.South = GetJoystickButton(activeJoystick, 0);
		rawState.East = GetJoystickButton(activeJoystick, 1);
		rawState.West = GetJoystickButton(activeJoystick, 2);
		rawState.North = GetJoystickButton(activeJoystick, 3);
		rawState.LB = GetJoystickButton(activeJoystick, 4);
		rawState.RB = GetJoystickButton(activeJoystick, 5);
		rawState.Back = GetJoystickButton(activeJoystick, 6);
		rawState.Start = GetJoystickButton(activeJoystick, 7);
		rawState.L3 = GetJoystickButton(activeJoystick, 8);
		float num = ReadDpadAxisX();
		float num2 = ReadDpadAxisY();
		rawState.DpadUp = num2 > 0.5f;
		rawState.DpadDown = num2 < -0.5f;
		rawState.DpadRight = num > 0.5f;
		rawState.DpadLeft = num < -0.5f;
		ReadTriggers(activeJoystick, out var lt, out var rt);
		rawState.LT = lt;
		rawState.RT = rt;
		return true;
	}

	private int GetActiveJoystickIndexCached(out string deviceName)
	{
		if (Time.unscaledTime < nextDeviceScanTime)
		{
			deviceName = cachedDeviceName;
			return activeJoystick;
		}
		deviceName = string.Empty;
		nextDeviceScanTime = Time.unscaledTime + 0.5f;
		string[] joystickNames = Input.GetJoystickNames();
		if (joystickNames == null || joystickNames.Length == 0)
		{
			activeJoystick = 0;
			cachedDeviceName = string.Empty;
			return 0;
		}
		for (int i = 0; i < joystickNames.Length; i++)
		{
			if (!string.IsNullOrEmpty(joystickNames[i]))
			{
				activeJoystick = i + 1;
				cachedDeviceName = joystickNames[i];
				deviceName = cachedDeviceName;
				return activeJoystick;
			}
		}
		activeJoystick = 0;
		cachedDeviceName = string.Empty;
		return 0;
	}

	private static bool GetJoystickButton(int joystickIndex, int button)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		int num = (joystickIndex - 1) * 20 + button;
		KeyCode val = (KeyCode)(350 + num);
		return Input.GetKey(val);
	}

	private static Vector2 ReadRightStick()
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		float axisAny = GetAxisAny("4th axis", "Axis 4", "3rd axis", "Axis 3", "Joystick X", "RightStickX");
		float axisAny2 = GetAxisAny("5th axis", "Axis 5", "4th axis", "Axis 4", "Joystick Y", "RightStickY");
		return new Vector2(axisAny, axisAny2);
	}

	private static float ReadDpadAxisX()
	{
		return GetAxisAny("6th axis", "Axis 6", "8th axis", "Axis 8");
	}

	private static float ReadDpadAxisY()
	{
		return GetAxisAny("7th axis", "Axis 7", "9th axis", "Axis 9");
	}

	private static void ReadTriggers(int joystickIndex, out bool lt, out bool rt)
	{
		float axisAny = GetAxisAny("3rd axis", "Axis 3");
		float axisAny2 = GetAxisAny("9th axis", "Axis 9", "LeftTrigger", "LT");
		float axisAny3 = GetAxisAny("10th axis", "Axis 10", "RightTrigger", "RT");
		lt = axisAny2 > 0.5f;
		rt = axisAny3 > 0.5f;
		if (!lt && !rt)
		{
			lt = axisAny < -0.5f;
			rt = axisAny > 0.5f;
		}
		if (!lt)
		{
			lt = GetJoystickButton(joystickIndex, 14) || GetJoystickButton(joystickIndex, 10);
		}
		if (!rt)
		{
			rt = GetJoystickButton(joystickIndex, 15) || GetJoystickButton(joystickIndex, 11);
		}
	}

	private static float GetAxisAny(params string[] names)
	{
		for (int i = 0; i < names.Length; i++)
		{
			float axisOrDefault = GetAxisOrDefault(names[i], float.NaN);
			if (!float.IsNaN(axisOrDefault))
			{
				return axisOrDefault;
			}
		}
		return 0f;
	}

	private static float GetAxisOrDefault(string axisName, float fallback = 0f)
	{
		if (string.IsNullOrEmpty(axisName))
		{
			return fallback;
		}
		if (MissingAxes.Contains(axisName))
		{
			return fallback;
		}
		try
		{
			return Input.GetAxisRaw(axisName);
		}
		catch
		{
			MissingAxes.Add(axisName);
			return fallback;
		}
	}
}
