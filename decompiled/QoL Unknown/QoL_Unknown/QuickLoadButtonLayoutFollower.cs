using UnityEngine;

namespace QoL_Unknown;

internal sealed class QuickLoadButtonLayoutFollower : MonoBehaviour
{
	public RectTransform sourceRect;

	public int index;

	public float width;

	public float height;

	public float spacing;

	private void LateUpdate()
	{
		SaveMenuPatcher.ConfigureQuickLoadRect(((Component)this).gameObject, sourceRect, index, width, height, spacing);
	}
}
