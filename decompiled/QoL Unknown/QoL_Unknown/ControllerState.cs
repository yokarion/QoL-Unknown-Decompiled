using UnityEngine;

namespace QoL_Unknown;

internal sealed class ControllerState
{
	private ulong currentMask;

	private ulong previousMask;

	public Vector2 LeftStick { get; private set; }

	public Vector2 RightStick { get; private set; }

	public void BeginFrame()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		previousMask = currentMask;
		currentMask = 0uL;
		LeftStick = Vector2.zero;
		RightStick = Vector2.zero;
	}

	public void SetButton(ControllerButton button, bool pressed)
	{
		if (pressed)
		{
			if (button >= ControllerButton.A && button < (ControllerButton)63)
			{
				currentMask |= (ulong)(1L << (int)button);
			}
		}
	}

	public void SetSticks(Vector2 left, Vector2 right)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		LeftStick = left;
		RightStick = right;
	}

	public bool IsHeld(ControllerButton button)
	{
		return (currentMask & (ulong)(1L << (int)button)) != 0;
	}

	public bool IsDown(ControllerButton button)
	{
		ulong num = (ulong)(1L << (int)button);
		return (currentMask & num) != 0L && (previousMask & num) == 0;
	}

	public bool IsUp(ControllerButton button)
	{
		ulong num = (ulong)(1L << (int)button);
		return (currentMask & num) == 0L && (previousMask & num) != 0;
	}

	public void ClearAll()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		currentMask = 0uL;
		previousMask = 0uL;
		LeftStick = Vector2.zero;
		RightStick = Vector2.zero;
	}
}
