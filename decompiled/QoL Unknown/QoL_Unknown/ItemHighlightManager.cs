using System;
using UnityEngine;

namespace QoL_Unknown;

public class ItemHighlightManager : MonoBehaviour
{
	public static ItemHighlightManager Instance;

	private GameObject outlineObj;

	private SpriteRenderer outlineRenderer;

	private static Material cachedOutlineMaterial;

	private const float HIGHLIGHT_RADIUS = 150f;

	private const float HIGHLIGHT_RADIUS_SQR = 22500f;

	private const int MaxItemScanHits = 128;

	private readonly Collider2D[] nearbyItemHits = (Collider2D[])(object)new Collider2D[128];

	private int itemLayerMask;

	public Item HighlightedItem { get; private set; }

	private void Awake()
	{
		Instance = this;
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
		EnsureOutlineMaterialLoaded();
		itemLayerMask = LayerMask.GetMask(new string[1] { "Item" });
		if (itemLayerMask == 0)
		{
			itemLayerMask = -1;
		}
	}

	private void Update()
	{
		ControllerInputManager instance = ControllerInputManager.Instance;
		if ((Object)(object)instance == (Object)null || !instance.IsControllerActive)
		{
			ClearHighlight();
		}
		else
		{
			UpdateHighlight(instance);
		}
	}

	private void UpdateHighlight(ControllerInputManager controller)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null)
		{
			ClearHighlight();
			return;
		}
		Vector3 virtualMousePos = controller.VirtualMousePos;
		Vector3 val = main.ScreenToWorldPoint(virtualMousePos);
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))._002Ector(val.x, val.y);
		float num = 150f * (main.orthographicSize * 2f / (float)Mathf.Max(1, Screen.height));
		if (num <= 0f)
		{
			num = 1f;
		}
		int num2 = Physics2D.OverlapCircleNonAlloc(val2, num, nearbyItemHits, itemLayerMask);
		if (num2 == 0)
		{
			ClearHighlight();
			return;
		}
		Item val3 = null;
		float num3 = float.MaxValue;
		for (int i = 0; i < num2; i++)
		{
			Collider2D val4 = nearbyItemHits[i];
			nearbyItemHits[i] = null;
			if ((Object)(object)val4 == (Object)null)
			{
				continue;
			}
			Item val5 = ((Component)val4).GetComponent<Item>() ?? ((Component)val4).GetComponentInParent<Item>();
			if (!IsDroppedItemCandidate(val5))
			{
				continue;
			}
			Vector3 val6 = main.WorldToScreenPoint(((Component)val5).transform.position);
			if (!(val6.z < 0f))
			{
				float num4 = val6.x - virtualMousePos.x;
				float num5 = val6.y - virtualMousePos.y;
				float num6 = num4 * num4 + num5 * num5;
				if (num6 < 22500f && num6 < num3)
				{
					num3 = num6;
					val3 = val5;
				}
			}
		}
		if ((Object)(object)val3 != (Object)(object)HighlightedItem)
		{
			SetHighlight(val3);
		}
	}

	private static bool IsDroppedItemCandidate(Item item)
	{
		return (Object)(object)item != (Object)null && ((Component)item).gameObject.activeInHierarchy && (Object)(object)((Component)item).transform.parent == (Object)null && (Object)(object)item.rb != (Object)null;
	}

	private static void EnsureOutlineMaterialLoaded()
	{
		if ((Object)(object)cachedOutlineMaterial == (Object)null)
		{
			cachedOutlineMaterial = Resources.Load<Material>("Special/ItemOutline");
		}
	}

	private void SetHighlight(Item item)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		ClearHighlight();
		if ((Object)(object)item == (Object)null)
		{
			return;
		}
		HighlightedItem = item;
		outlineObj = new GameObject("ControllerItemOutline", new Type[1] { typeof(SpriteRenderer) });
		outlineObj.transform.SetParent(((Component)item).transform);
		outlineObj.transform.localPosition = Vector3.zero;
		outlineObj.transform.localRotation = Quaternion.identity;
		outlineObj.transform.localScale = Vector3.one * 1.125f;
		outlineRenderer = outlineObj.GetComponent<SpriteRenderer>();
		SpriteRenderer component = ((Component)item).GetComponent<SpriteRenderer>();
		if ((Object)(object)component != (Object)null)
		{
			((Renderer)outlineRenderer).sortingOrder = ((Renderer)component).sortingOrder - 1;
			outlineRenderer.sprite = component.sprite;
			EnsureOutlineMaterialLoaded();
			if ((Object)(object)cachedOutlineMaterial != (Object)null)
			{
				((Renderer)outlineRenderer).material = cachedOutlineMaterial;
			}
			outlineRenderer.color = new Color(1f, 1f, 0f, 0.8f);
		}
	}

	private void ClearHighlight()
	{
		if ((Object)(object)outlineObj != (Object)null)
		{
			Object.Destroy((Object)(object)outlineObj);
			outlineObj = null;
			outlineRenderer = null;
		}
		HighlightedItem = null;
	}

	private void OnDestroy()
	{
		ClearHighlight();
	}
}
