using UnityEngine;

namespace QoL_Unknown;

internal static class ControllerActionBridge
{
	public static bool GetMouseButton(ControllerState state, int button)
	{
		if (state == null)
		{
			return false;
		}
		return button switch
		{
			0 => state.IsHeld(ControllerButton.RT), 
			1 => state.IsHeld(ControllerButton.LT), 
			2 => state.IsHeld(ControllerButton.X), 
			_ => false, 
		};
	}

	public static bool GetMouseButtonDown(ControllerState state, int button)
	{
		if (state == null)
		{
			return false;
		}
		return button switch
		{
			0 => state.IsDown(ControllerButton.RT), 
			1 => state.IsDown(ControllerButton.LT), 
			2 => state.IsDown(ControllerButton.X), 
			_ => false, 
		};
	}

	public static bool GetMouseButtonUp(ControllerState state, int button)
	{
		if (state == null)
		{
			return false;
		}
		return button switch
		{
			0 => state.IsUp(ControllerButton.RT), 
			1 => state.IsUp(ControllerButton.LT), 
			2 => state.IsUp(ControllerButton.X), 
			_ => false, 
		};
	}

	public static bool GetKeyDown(ControllerState state, KeyCode key)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if (state == null)
		{
			return false;
		}
		return MatchesBoundAction(state, key, down: true) || ((int)key == 304 && state.IsDown(ControllerButton.LB)) || ((int)key == 120 && state.IsDown(ControllerButton.L3));
	}

	public static bool GetKey(ControllerState state, KeyCode key)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if (state == null)
		{
			return false;
		}
		return MatchesBoundAction(state, key, down: false) || ((int)key == 304 && state.IsHeld(ControllerButton.LB)) || ((int)key == 120 && state.IsHeld(ControllerButton.L3));
	}

	public static bool GetKeyUp(ControllerState state, KeyCode key)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if (state == null)
		{
			return false;
		}
		return MatchesBoundAction(state, key, down: false, up: true) || ((int)key == 304 && state.IsUp(ControllerButton.LB)) || ((int)key == 120 && state.IsUp(ControllerButton.L3));
	}

	private static bool MatchesBoundAction(ControllerState state, KeyCode key, bool down, bool up = false)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		if (MatchBinding(state, key, "jump", ControllerButton.A, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "throw", ControllerButton.Y, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "toggleinventory", ControllerButton.DpadUp, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "woundview", ControllerButton.DpadDown, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "pause", ControllerButton.Start, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "itemuse", ControllerButton.B, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "craft", ControllerButton.DpadLeft, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "switchhands", ControllerButton.DpadRight, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "attack", ControllerButton.RT, down, up))
		{
			return true;
		}
		if (MatchBinding(state, key, "iteminteract", ControllerButton.LT, down, up))
		{
			return true;
		}
		return false;
	}

	private static bool MatchBinding(ControllerState state, KeyCode key, string action, ControllerButton button, bool down, bool up)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		if (key != KeyBinds.GetBind(action))
		{
			return false;
		}
		if (!ControllerBindings.ShouldTrigger(button, action))
		{
			return false;
		}
		if (up)
		{
			return state.IsUp(button);
		}
		return down ? state.IsDown(button) : state.IsHeld(button);
	}
}
