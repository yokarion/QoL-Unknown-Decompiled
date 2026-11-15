using System.Runtime.InteropServices;
using UnityEngine;

namespace QoL_Unknown;

internal static class ControllerCursorDriver
{
	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int x, int y);

	public static Vector3 Update(Vector3 currentVirtualMousePos, ControllerState state)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		float num = 1200f * Time.unscaledDeltaTime;
		if (state.IsHeld(ControllerButton.RB))
		{
			num *= 0.3f;
		}
		Vector2 rightStick = state.RightStick;
		if (((Vector2)(ref rightStick)).sqrMagnitude > 0.05f)
		{
			currentVirtualMousePos += new Vector3(rightStick.x * num, rightStick.y * num, 0f);
		}
		currentVirtualMousePos.x = Mathf.Clamp(currentVirtualMousePos.x, 0f, (float)Screen.width);
		currentVirtualMousePos.y = Mathf.Clamp(currentVirtualMousePos.y, 0f, (float)Screen.height);
		SetCursorPos((int)currentVirtualMousePos.x, (int)((float)Screen.height - currentVirtualMousePos.y));
		Cursor.visible = true;
		return currentVirtualMousePos;
	}
}
