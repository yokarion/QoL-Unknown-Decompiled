using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

public class QoLMenuButton : MonoBehaviour
{
	public Image overlay;

	public TextMeshProUGUI label;

	private RectTransform rect;

	private Vector3 lastMousePosition;

	private float lastDistanceToCursor = 99999f;

	private bool overlayActive
	{
		get
		{
			if ((Object)(object)PreRunScript.instance != (Object)null && !PreRunScript.instance.runSettingsScreen.activeSelf && !PreRunScript.instance.contentWarning.activeSelf && !PreRunScript.instance.tutorialWarning.activeSelf && !ScrollableText.textExists && !Object.op_Implicit((Object)(object)SettingsMenu.instance))
			{
				return PreRunScript.instance.creditsScreen.activeSelf;
			}
			return true;
		}
	}

	private void Start()
	{
		rect = ((Component)this).GetComponent<RectTransform>();
	}

	private void Update()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		QoLSettingsMenu.CloseGeofruitMenuIfRunSettingsActive();
		Vector3 mousePosition = Input.mousePosition;
		if (mousePosition != lastMousePosition)
		{
			lastMousePosition = mousePosition;
			lastDistanceToCursor = GetDistanceToCursor(mousePosition);
		}
		float num = lastDistanceToCursor;
		if (overlayActive)
		{
			num = 99999f;
		}
		float num2 = Mathf.Max(1f - num * 0.005f, 0.025f);
		if ((Object)(object)overlay != (Object)null)
		{
			((Graphic)overlay).color = Color.Lerp(((Graphic)overlay).color, new Color(1f, 1f, 1f, num2), Time.unscaledDeltaTime * 3f);
		}
		num2 = ((num <= 0f) ? 1f : 0f);
		if ((Object)(object)label != (Object)null)
		{
			((Graphic)label).color = Color.Lerp(((Graphic)label).color, new Color(1f, 1f, 1f, num2), Time.unscaledDeltaTime * 3f);
		}
		if (Input.GetMouseButtonDown(0) && num <= 0f && !MultiplayerCompat.IsMultiplayerMenuOpen)
		{
			UnityAction qoLToggleAction = QoLSettingsMenu.QoLToggleAction;
			if (qoLToggleAction != null)
			{
				qoLToggleAction.Invoke();
			}
		}
	}

	public float GetDistanceToCursor()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return GetDistanceToCursor(Input.mousePosition);
	}

	private float GetDistanceToCursor(Vector3 mousePosition)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)rect == (Object)null)
		{
			return 99999f;
		}
		Vector2 val = default(Vector2);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Vector2.op_Implicit(mousePosition), (Camera)null, ref val);
		Rect val2 = rect.rect;
		if (((Rect)(ref val2)).Contains(val))
		{
			return 0f;
		}
		float num = Mathf.Clamp(val.x, ((Rect)(ref val2)).xMin, ((Rect)(ref val2)).xMax);
		float num2 = Mathf.Clamp(val.y, ((Rect)(ref val2)).yMin, ((Rect)(ref val2)).yMax);
		return Vector2.Distance(val, new Vector2(num, num2));
	}
}
