using UnityEngine;

namespace QoL_Unknown;

internal sealed class SeedInputLayoutFollower : MonoBehaviour
{
	public RectTransform sourceRect;

	public float widthFactor;

	public float maxWidth;

	public float spacingFactor;

	private RectTransform rect;

	private void Awake()
	{
		rect = ((Component)this).GetComponent<RectTransform>();
	}

	private void LateUpdate()
	{
		if (!((Object)(object)rect == (Object)null) && !((Object)(object)sourceRect == (Object)null) && !((Object)(object)((Transform)sourceRect).parent == (Object)null))
		{
			SaveMenuPatcher.ConfigureSeedInputRect(rect, sourceRect, widthFactor, maxWidth, spacingFactor);
		}
	}
}
