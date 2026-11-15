using UnityEngine;

namespace QoL_Unknown;

internal sealed class SeedInputLayoutFollower : MonoBehaviour
{
	public RectTransform sourceRect;

	public Vector2 originalOffsetMin;

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
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rect == (Object)null) && !((Object)(object)sourceRect == (Object)null) && !((Object)(object)((Transform)sourceRect).parent == (Object)null))
		{
			SaveMenuPatcher.ConfigureSeedInputRect(rect, sourceRect, originalOffsetMin, widthFactor, maxWidth, spacingFactor);
		}
	}
}
