using System;
using UnityEngine;

namespace QoL_Unknown;

internal static class ControllerProfileResolver
{
	public const string NintendoLayoutPrefKey = "ControllerNintendoPhysicalLabels";

	public static ControllerProfile Resolve(string deviceName, string sourceName)
	{
		if (!string.IsNullOrEmpty(sourceName) && sourceName.Equals("XInput", StringComparison.OrdinalIgnoreCase))
		{
			return ControllerProfile.Xbox;
		}
		string text = (deviceName ?? string.Empty).ToLowerInvariant();
		if (text.Contains("dualsense") || text.Contains("dualshock") || text.Contains("wireless controller") || text.Contains("playstation") || text.Contains("ps5"))
		{
			return ControllerProfile.PS5;
		}
		if (text.Contains("nintendo") || text.Contains("switch") || text.Contains("joy-con") || text.Contains("joycon") || text.Contains("pro controller"))
		{
			return ControllerProfile.Nintendo;
		}
		return ControllerProfile.Generic;
	}

	public static void ApplyProfile(RawControllerState raw, ControllerProfile profile, ControllerState target)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		target.SetSticks(raw.LeftStick, raw.RightStick);
		bool pressed = raw.South;
		bool pressed2 = raw.East;
		bool pressed3 = raw.West;
		bool pressed4 = raw.North;
		if (profile == ControllerProfile.Nintendo && UseNintendoPhysicalLabels())
		{
			pressed = raw.East;
			pressed2 = raw.South;
			pressed3 = raw.North;
			pressed4 = raw.West;
		}
		target.SetButton(ControllerButton.A, pressed);
		target.SetButton(ControllerButton.B, pressed2);
		target.SetButton(ControllerButton.X, pressed3);
		target.SetButton(ControllerButton.Y, pressed4);
		target.SetButton(ControllerButton.Start, raw.Start);
		target.SetButton(ControllerButton.Back, raw.Back);
		target.SetButton(ControllerButton.RT, raw.RT);
		target.SetButton(ControllerButton.LT, raw.LT);
		target.SetButton(ControllerButton.RB, raw.RB);
		target.SetButton(ControllerButton.LB, raw.LB);
		target.SetButton(ControllerButton.L3, raw.L3);
		target.SetButton(ControllerButton.DpadUp, raw.DpadUp);
		target.SetButton(ControllerButton.DpadDown, raw.DpadDown);
		target.SetButton(ControllerButton.DpadLeft, raw.DpadLeft);
		target.SetButton(ControllerButton.DpadRight, raw.DpadRight);
	}

	private static bool UseNintendoPhysicalLabels()
	{
		return PlayerPrefs.GetInt("ControllerNintendoPhysicalLabels", 1) == 1;
	}
}
