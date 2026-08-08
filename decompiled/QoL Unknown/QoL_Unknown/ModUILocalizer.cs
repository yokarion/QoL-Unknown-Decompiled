using TMPro;
using UnityEngine;

namespace QoL_Unknown;

internal sealed class ModUILocalizer : MonoBehaviour
{
	public string Key = string.Empty;

	public string Fallback = string.Empty;

	public bool Upper = false;

	private TextMeshProUGUI text;

	private void Awake()
	{
		text = ((Component)this).GetComponent<TextMeshProUGUI>();
	}

	private void OnEnable()
	{
		Refresh();
	}

	public void Refresh()
	{
		if ((Object)(object)this.text == (Object)null)
		{
			this.text = ((Component)this).GetComponent<TextMeshProUGUI>();
		}
		if (!((Object)(object)this.text == (Object)null))
		{
			string text = ModLocale.Get(Key, Fallback);
			((TMP_Text)this.text).text = (Upper ? text.ToUpperInvariant() : text);
		}
	}
}
