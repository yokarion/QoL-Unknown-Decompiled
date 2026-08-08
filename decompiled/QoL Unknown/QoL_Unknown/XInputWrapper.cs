using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace QoL_Unknown;

public static class XInputWrapper
{
	public struct XINPUT_STATE
	{
		public uint dwPacketNumber;

		public XINPUT_GAMEPAD Gamepad;
	}

	public struct XINPUT_GAMEPAD
	{
		public ushort wButtons;

		public byte bLeftTrigger;

		public byte bRightTrigger;

		public short sThumbLX;

		public short sThumbLY;

		public short sThumbRX;

		public short sThumbRY;
	}

	private const int ERROR_SUCCESS = 0;

	private const int ERROR_DEVICE_NOT_CONNECTED = 1167;

	public const ushort XINPUT_GAMEPAD_DPAD_UP = 1;

	public const ushort XINPUT_GAMEPAD_DPAD_DOWN = 2;

	public const ushort XINPUT_GAMEPAD_DPAD_LEFT = 4;

	public const ushort XINPUT_GAMEPAD_DPAD_RIGHT = 8;

	public const ushort XINPUT_GAMEPAD_START = 16;

	public const ushort XINPUT_GAMEPAD_BACK = 32;

	public const ushort XINPUT_GAMEPAD_LEFT_THUMB = 64;

	public const ushort XINPUT_GAMEPAD_RIGHT_THUMB = 128;

	public const ushort XINPUT_GAMEPAD_LEFT_SHOULDER = 256;

	public const ushort XINPUT_GAMEPAD_RIGHT_SHOULDER = 512;

	public const ushort XINPUT_GAMEPAD_A = 4096;

	public const ushort XINPUT_GAMEPAD_B = 8192;

	public const ushort XINPUT_GAMEPAD_X = 16384;

	public const ushort XINPUT_GAMEPAD_Y = 32768;

	private const int XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE = 7849;

	private const int XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE = 8689;

	private const int XINPUT_GAMEPAD_TRIGGER_THRESHOLD = 30;

	private static bool useXInput1_3 = false;

	private static bool xinputAvailable = true;

	[DllImport("xinput1_4.dll")]
	private static extern int XInputGetState(int dwUserIndex, ref XINPUT_STATE pState);

	[DllImport("xinput1_3.dll")]
	private static extern int XInputGetState_1_3(int dwUserIndex, ref XINPUT_STATE pState);

	public static bool GetState(int userIndex, out XINPUT_STATE state)
	{
		state = default(XINPUT_STATE);
		if (!xinputAvailable)
		{
			return false;
		}
		try
		{
			int num = ((!useXInput1_3) ? XInputGetState(userIndex, ref state) : XInputGetState_1_3(userIndex, ref state));
			return num == 0;
		}
		catch (DllNotFoundException)
		{
			if (!useXInput1_3)
			{
				useXInput1_3 = true;
				try
				{
					int num2 = XInputGetState_1_3(userIndex, ref state);
					return num2 == 0;
				}
				catch
				{
					xinputAvailable = false;
					return false;
				}
			}
			xinputAvailable = false;
			return false;
		}
		catch
		{
			return false;
		}
	}

	public static float NormalizeThumbstick(short value, int deadzone)
	{
		if (value < -deadzone)
		{
			float num = (float)(value + deadzone) / (32768f - (float)deadzone);
			return Mathf.Max(num, -1f);
		}
		if (value > deadzone)
		{
			float num2 = (float)(value - deadzone) / (32767f - (float)deadzone);
			return Mathf.Min(num2, 1f);
		}
		return 0f;
	}

	public static Vector2 GetLeftStick(XINPUT_GAMEPAD gamepad)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(NormalizeThumbstick(gamepad.sThumbLX, 7849), NormalizeThumbstick(gamepad.sThumbLY, 7849));
	}

	public static Vector2 GetRightStick(XINPUT_GAMEPAD gamepad)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(NormalizeThumbstick(gamepad.sThumbRX, 8689), NormalizeThumbstick(gamepad.sThumbRY, 8689));
	}

	public static bool IsButtonPressed(ushort buttons, ushort buttonMask)
	{
		return (buttons & buttonMask) != 0;
	}
}
