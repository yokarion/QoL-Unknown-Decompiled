using TMPro;
using UnityEngine;

namespace QoL_Unknown;

public class SaveSlotButtonTextEnforcer : MonoBehaviour
{
	public string textToEnforce;

	private TextMeshProUGUI tmp;

	private void Awake()
	{
		tmp = ((Component)this).GetComponent<TextMeshProUGUI>();
	}

	private void OnEnable()
	{
		Enforce();
	}

	private void LateUpdate()
	{
		Enforce();
	}

	private void Enforce()
	{
		if ((Object)(object)tmp != (Object)null && !string.IsNullOrEmpty(textToEnforce) && ((TMP_Text)tmp).text != textToEnforce)
		{
			((TMP_Text)tmp).text = textToEnforce;
			if (((TMP_Text)tmp).fontSize != 24f)
			{
				((TMP_Text)tmp).fontSize = 24f;
			}
			if (((TMP_Text)tmp).enableAutoSizing)
			{
				((TMP_Text)tmp).enableAutoSizing = false;
			}
		}
	}
}
