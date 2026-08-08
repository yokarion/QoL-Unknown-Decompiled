using UnityEngine;

namespace QoL_Unknown;

internal sealed class XInputControllerSource : IControllerInputSource
{
	private const float TriggerThreshold = 30f;

	private const float DisconnectedScanInterval = 0.5f;

	private int activeIndex = 0;

	private float nextDisconnectedScanTime;

	public string SourceName => "XInput";

	public bool TryRead(out RawControllerState rawState, out string deviceName)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		rawState = default(RawControllerState);
		deviceName = "Xbox Controller";
		if (!TryGetConnectedState(activeIndex, out var state))
		{
			if (Time.unscaledTime < nextDisconnectedScanTime)
			{
				return false;
			}
			nextDisconnectedScanTime = Time.unscaledTime + 0.5f;
			bool flag = false;
			for (int i = 0; i < 4; i++)
			{
				if (TryGetConnectedState(i, out state))
				{
					activeIndex = i;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		else
		{
			nextDisconnectedScanTime = 0f;
		}
		rawState.LeftStick = XInputWrapper.GetLeftStick(state.Gamepad);
		rawState.RightStick = XInputWrapper.GetRightStick(state.Gamepad);
		rawState.South = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 4096);
		rawState.East = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 8192);
		rawState.West = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 16384);
		rawState.North = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 32768);
		rawState.Start = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 16);
		rawState.Back = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 32);
		rawState.L3 = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 64);
		rawState.LB = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 256);
		rawState.RB = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 512);
		rawState.DpadUp = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 1);
		rawState.DpadDown = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 2);
		rawState.DpadLeft = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 4);
		rawState.DpadRight = XInputWrapper.IsButtonPressed(state.Gamepad.wButtons, 8);
		rawState.RT = (float)(int)state.Gamepad.bRightTrigger > 30f;
		rawState.LT = (float)(int)state.Gamepad.bLeftTrigger > 30f;
		return true;
	}

	private static bool TryGetConnectedState(int index, out XInputWrapper.XINPUT_STATE state)
	{
		return XInputWrapper.GetState(index, out state);
	}
}
