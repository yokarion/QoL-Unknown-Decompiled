using UnityEngine;

namespace QoL_Unknown;

public class ControllerInputManager : MonoBehaviour
{
	public static ControllerInputManager Instance;

	private readonly ControllerState state = new ControllerState();

	private readonly IControllerInputSource xinputSource = new XInputControllerSource();

	private readonly IControllerInputSource unitySource = new UnityJoystickControllerSource();

	private string activeSourceName = "None";

	private string activeDeviceName = "None";

	private ControllerProfile activeProfile = ControllerProfile.Generic;

	public Vector3 VirtualMousePos;

	internal ControllerState State => state;

	public bool IsControllerActive { get; private set; }

	public Vector2 LeftStick => state.LeftStick;

	public Vector2 RightStick => state.RightStick;

	public bool ButtonA => state.IsHeld(ControllerButton.A);

	public bool ButtonB => state.IsHeld(ControllerButton.B);

	public bool ButtonX => state.IsHeld(ControllerButton.X);

	public bool ButtonY => state.IsHeld(ControllerButton.Y);

	public bool ButtonStart => state.IsHeld(ControllerButton.Start);

	public bool ButtonBack => state.IsHeld(ControllerButton.Back);

	public bool ButtonADown => state.IsDown(ControllerButton.A);

	public bool ButtonBDown => state.IsDown(ControllerButton.B);

	public bool ButtonXDown => state.IsDown(ControllerButton.X);

	public bool ButtonYDown => state.IsDown(ControllerButton.Y);

	public bool ButtonStartDown => state.IsDown(ControllerButton.Start);

	public bool ButtonBackDown => state.IsDown(ControllerButton.Back);

	public bool ButtonAUp => state.IsUp(ControllerButton.A);

	public bool ButtonBUp => state.IsUp(ControllerButton.B);

	public bool ButtonXUp => state.IsUp(ControllerButton.X);

	public bool ButtonYUp => state.IsUp(ControllerButton.Y);

	public bool ButtonStartUp => state.IsUp(ControllerButton.Start);

	public bool ButtonBackUp => state.IsUp(ControllerButton.Back);

	public bool RightTrigger => state.IsHeld(ControllerButton.RT);

	public bool RightTriggerDown => state.IsDown(ControllerButton.RT);

	public bool RightTriggerUp => state.IsUp(ControllerButton.RT);

	public bool LeftTrigger => state.IsHeld(ControllerButton.LT);

	public bool LeftTriggerDown => state.IsDown(ControllerButton.LT);

	public bool LeftTriggerUp => state.IsUp(ControllerButton.LT);

	public bool RightBumper => state.IsHeld(ControllerButton.RB);

	public bool RightBumperDown => state.IsDown(ControllerButton.RB);

	public bool RightBumperUp => state.IsUp(ControllerButton.RB);

	public bool LeftBumper => state.IsHeld(ControllerButton.LB);

	public bool LeftBumperDown => state.IsDown(ControllerButton.LB);

	public bool LeftBumperUp => state.IsUp(ControllerButton.LB);

	public bool LeftStickPress => state.IsHeld(ControllerButton.L3);

	public bool LeftStickPressDown => state.IsDown(ControllerButton.L3);

	public bool LeftStickPressUp => state.IsUp(ControllerButton.L3);

	public bool DpadUp => state.IsHeld(ControllerButton.DpadUp);

	public bool DpadDown => state.IsHeld(ControllerButton.DpadDown);

	public bool DpadLeft => state.IsHeld(ControllerButton.DpadLeft);

	public bool DpadRight => state.IsHeld(ControllerButton.DpadRight);

	public bool DpadUpDown => state.IsDown(ControllerButton.DpadUp);

	public bool DpadDownDown => state.IsDown(ControllerButton.DpadDown);

	public bool DpadLeftDown => state.IsDown(ControllerButton.DpadLeft);

	public bool DpadRightDown => state.IsDown(ControllerButton.DpadRight);

	internal string ActiveSourceName => activeSourceName;

	internal string ActiveDeviceName => activeDeviceName;

	internal ControllerProfile ActiveProfile => activeProfile;

	private void Awake()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		Instance = this;
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
		VirtualMousePos = new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f, 0f);
		state.ClearAll();
	}

	private void OnDestroy()
	{
		if ((Object)(object)Instance == (Object)(object)this)
		{
			Instance = null;
		}
	}

	private void Update()
	{
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		state.BeginFrame();
		if (TryReadFromSources(out var raw, out var sourceName, out var deviceName))
		{
			activeSourceName = sourceName;
			activeDeviceName = (string.IsNullOrEmpty(deviceName) ? "Unknown" : deviceName);
			activeProfile = ControllerProfileResolver.Resolve(activeDeviceName, activeSourceName);
			ControllerProfileResolver.ApplyProfile(raw, activeProfile, state);
		}
		else
		{
			activeSourceName = "None";
			activeDeviceName = "None";
			activeProfile = ControllerProfile.Generic;
		}
		UpdateActiveMode();
		if (IsControllerActive)
		{
			if (state.IsDown(ControllerButton.Back) && (Object)(object)ConsoleScript.instance != (Object)null)
			{
				ConsoleScript.instance.active = !ConsoleScript.instance.active;
			}
			VirtualMousePos = ControllerCursorDriver.Update(VirtualMousePos, state);
		}
	}

	private bool TryReadFromSources(out RawControllerState raw, out string sourceName, out string deviceName)
	{
		if (xinputSource.TryRead(out raw, out deviceName))
		{
			sourceName = xinputSource.SourceName;
			return true;
		}
		if (unitySource.TryRead(out raw, out deviceName))
		{
			sourceName = unitySource.SourceName;
			return true;
		}
		sourceName = "None";
		raw = default(RawControllerState);
		deviceName = string.Empty;
		return false;
	}

	private void UpdateActiveMode()
	{
		if (HasControllerActivity())
		{
			IsControllerActive = true;
			Cursor.visible = true;
			Cursor.lockState = (CursorLockMode)0;
		}
		else if (Input.anyKeyDown && !IsJoystickEvent())
		{
			IsControllerActive = false;
		}
	}

	private bool HasControllerActivity()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = state.LeftStick;
		if (!(((Vector2)(ref val)).sqrMagnitude > 0.01f))
		{
			val = state.RightStick;
			if (!(((Vector2)(ref val)).sqrMagnitude > 0.01f))
			{
				for (int i = 0; i <= 14; i++)
				{
					if (state.IsDown((ControllerButton)i))
					{
						return true;
					}
				}
				return false;
			}
		}
		return true;
	}

	private static bool IsJoystickEvent()
	{
		for (int i = 0; i < 20; i++)
		{
			if (Input.GetKeyDown((KeyCode)(330 + i)))
			{
				return true;
			}
		}
		return false;
	}

	public string GetDebugInfo()
	{
		return $"Controller source: {activeSourceName} | Device: {activeDeviceName} | Profile: {activeProfile} | Active: {IsControllerActive}";
	}
}
