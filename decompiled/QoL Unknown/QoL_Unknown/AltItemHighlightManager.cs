using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoL_Unknown;

public sealed class AltItemHighlightManager : MonoBehaviour
{
	private struct OutlineEntry
	{
		public readonly GameObject GameObject;

		public readonly SpriteRenderer Renderer;

		public OutlineEntry(GameObject gameObject, SpriteRenderer renderer)
		{
			GameObject = gameObject;
			Renderer = renderer;
		}
	}

	private struct VisibleItemEntry
	{
		public readonly string Name;

		public readonly Vector2 Anchor;

		public readonly Vector2 Min;

		public readonly Vector2 Max;

		public readonly Vector3 WorldAnchor;

		public VisibleItemEntry(string name, Vector2 anchor, Vector2 min, Vector2 max, Vector3 worldAnchor)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			Name = name;
			Anchor = anchor;
			Min = min;
			Max = max;
			WorldAnchor = worldAnchor;
		}
	}

	private struct ClusterEntry
	{
		public readonly List<VisibleItemEntry> Items;

		public Vector2 Center;

		public float Right;

		public Vector3 WorldAnchor;

		private Vector2 min;

		private Vector2 max;

		private float worldRight;

		private float worldTop;

		private float worldBottom;

		public ClusterEntry(VisibleItemEntry item)
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			Items = new List<VisibleItemEntry>();
			Center = item.Anchor;
			Right = item.Max.x;
			WorldAnchor = item.WorldAnchor;
			min = item.Min;
			max = item.Max;
			worldRight = item.WorldAnchor.x;
			worldTop = item.WorldAnchor.y;
			worldBottom = item.WorldAnchor.y;
			Items.Add(item);
		}

		public void Add(VisibleItemEntry item)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
			Items.Add(item);
			min = new Vector2(Mathf.Min(min.x, item.Min.x), Mathf.Min(min.y, item.Min.y));
			max = new Vector2(Mathf.Max(max.x, item.Max.x), Mathf.Max(max.y, item.Max.y));
			Center = (min + max) * 0.5f;
			Right = max.x;
			worldRight = Mathf.Max(worldRight, item.WorldAnchor.x);
			worldTop = Mathf.Max(worldTop, item.WorldAnchor.y);
			worldBottom = Mathf.Min(worldBottom, item.WorldAnchor.y);
			WorldAnchor = new Vector3(worldRight, (worldTop + worldBottom) * 0.5f, item.WorldAnchor.z);
		}
	}

	private struct RowData
	{
		public readonly string Text;

		public RowData(string text)
		{
			Text = text;
		}
	}

	private struct RowVisual
	{
		public readonly GameObject GameObject;

		public readonly RectTransform Rect;

		public readonly RectTransform Background;

		public readonly TextMeshProUGUI Text;

		public RowVisual(GameObject gameObject, RectTransform rect, RectTransform background, TextMeshProUGUI text)
		{
			GameObject = gameObject;
			Rect = rect;
			Background = background;
			Text = text;
		}
	}

	private struct TableEntry
	{
		public readonly GameObject GameObject;

		public readonly RectTransform Rect;

		public readonly RectTransform Background;

		public readonly List<RowVisual> Rows;

		public Vector2 CurrentPosition;

		public Vector2 TargetPosition;

		public Vector3 WorldAnchor;

		public bool HasPosition;

		public TableEntry(GameObject gameObject, RectTransform rect, RectTransform background)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			GameObject = gameObject;
			Rect = rect;
			Background = background;
			Rows = new List<RowVisual>();
			CurrentPosition = Vector2.zero;
			TargetPosition = Vector2.zero;
			WorldAnchor = Vector3.zero;
			HasPosition = false;
		}
	}

	private const float RefreshInterval = 0.12f;

	private const float OutlineScale = 1.16f;

	private const float TableXOffset = 48f;

	private const float ClusterRadiusX = 120f;

	private const float ClusterRadiusY = 70f;

	private const float PositionSmoothSpeed = 24f;

	private const float ScreenMargin = 24f;

	private const float MouseUiRadiusScreenFraction = 0.33f;

	private const float RowPadding = 1f;

	private const float LabelFontScale = 0.78f;

	private const int MaxItemScanHits = 768;

	private static Material cachedOutlineMaterial;

	private readonly Collider2D[] itemHits = (Collider2D[])(object)new Collider2D[768];

	private readonly Dictionary<Item, OutlineEntry> outlines = new Dictionary<Item, OutlineEntry>();

	private readonly HashSet<Item> activeItems = new HashSet<Item>();

	private readonly List<VisibleItemEntry> visibleItems = new List<VisibleItemEntry>();

	private readonly List<ClusterEntry> clusters = new List<ClusterEntry>();

	private readonly List<TableEntry> tables = new List<TableEntry>();

	private int itemLayerMask;

	private float nextRefreshTime;

	private bool wasAltHeld;

	private Canvas labelCanvas;

	private RectTransform labelCanvasRect;

	private Transform labelRoot;

	public static AltItemHighlightManager Instance { get; private set; }

	public static AltItemHighlightManager EnsureInstance(string source)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if ((Object)(object)Instance != (Object)null)
		{
			return Instance;
		}
		GameObject val = new GameObject("AltItemHighlightManager");
		Object.DontDestroyOnLoad((Object)(object)val);
		AltItemHighlightManager altItemHighlightManager = (Instance = val.AddComponent<AltItemHighlightManager>());
		AltItemHighlightPatcher.DebugLog("Manager GameObject created from " + source + ".");
		altItemHighlightManager.InitializeFields();
		return altItemHighlightManager;
	}

	private void Awake()
	{
		if ((Object)(object)Instance != (Object)null && (Object)(object)Instance != (Object)(object)this)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		Instance = this;
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
		InitializeFields();
	}

	private void InitializeFields()
	{
		EnsureOutlineMaterialLoaded();
		itemLayerMask = LayerMask.GetMask(new string[1] { "Item" });
		if (itemLayerMask == 0)
		{
			itemLayerMask = -1;
		}
	}

	private void Update()
	{
	}

	public void Tick()
	{
		bool key = Input.GetKey((KeyCode)308);
		bool key2 = Input.GetKey((KeyCode)307);
		bool keyByName = GetKeyByName("left alt");
		bool keyByName2 = GetKeyByName("right alt");
		if (!(key || key2 || keyByName || keyByName2))
		{
			if (wasAltHeld)
			{
				ClearActive();
			}
			wasAltHeld = false;
			return;
		}
		Camera main = Camera.main;
		if (!CanShowHighlights(main))
		{
			ClearActive();
			wasAltHeld = true;
			return;
		}
		if (!wasAltHeld || Time.unscaledTime >= nextRefreshTime)
		{
			RefreshHighlights(main);
			nextRefreshTime = Time.unscaledTime + 0.12f;
		}
		wasAltHeld = true;
		UpdateTablePositions(main);
	}

	private static bool GetKeyByName(string keyName)
	{
		try
		{
			return Input.GetKey(keyName);
		}
		catch
		{
			return false;
		}
	}

	private static bool CanShowHighlights(Camera cam)
	{
		return (Object)(object)cam != (Object)null && Screen.width > 0 && Screen.height > 0;
	}

	private void RefreshHighlights(Camera cam)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		activeItems.Clear();
		EnsureLabelCanvas();
		GetViewportWorldBounds(cam, out var minWorld, out var maxWorld);
		int num = Physics2D.OverlapAreaNonAlloc(minWorld, maxWorld, itemHits, itemLayerMask);
		visibleItems.Clear();
		for (int i = 0; i < num; i++)
		{
			Collider2D val = itemHits[i];
			itemHits[i] = null;
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			Item item = ((Component)val).GetComponent<Item>() ?? ((Component)val).GetComponentInParent<Item>();
			if (IsVisibleLooseItem(item, cam, out var renderer))
			{
				activeItems.Add(item);
				ShowOutline(item, renderer);
				VisibleItemEntry item2 = CreateVisibleItemEntry(item, renderer, cam);
				if (IsNearMouseForUi(item2))
				{
					visibleItems.Add(item2);
				}
			}
		}
		BuildClusters();
		ShowTables();
		HideInactiveOutlines();
		HideUnusedTables(clusters.Count);
	}

	private static void GetViewportWorldBounds(Camera cam, out Vector2 minWorld, out Vector2 maxWorld)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Abs(((Component)cam).transform.position.z);
		Vector3 val = cam.ScreenToWorldPoint(new Vector3(0f, 0f, num));
		Vector3 val2 = cam.ScreenToWorldPoint(new Vector3((float)Screen.width, (float)Screen.height, num));
		minWorld = new Vector2(Mathf.Min(val.x, val2.x), Mathf.Min(val.y, val2.y));
		maxWorld = new Vector2(Mathf.Max(val.x, val2.x), Mathf.Max(val.y, val2.y));
	}

	private static bool IsVisibleLooseItem(Item item, Camera cam, out SpriteRenderer renderer)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		renderer = null;
		if ((Object)(object)item == (Object)null || !((Component)item).gameObject.activeInHierarchy || (Object)(object)((Component)item).transform.parent != (Object)null || (Object)(object)item.rb == (Object)null)
		{
			return false;
		}
		renderer = ((Component)item).GetComponent<SpriteRenderer>();
		if ((Object)(object)renderer == (Object)null || (Object)(object)renderer.sprite == (Object)null || !((Renderer)renderer).enabled)
		{
			return false;
		}
		Vector3 val = cam.WorldToScreenPoint(((Component)item).transform.position);
		if (val.z < 0f || val.x < -24f || val.x > (float)Screen.width + 24f || val.y < -24f || val.y > (float)Screen.height + 24f)
		{
			return false;
		}
		return item.rb.simulated || (Object)(object)WorldGeneration.world == (Object)null || !WorldGeneration.world.worldExists;
	}

	private void ShowOutline(Item item, SpriteRenderer itemRenderer)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		if (!outlines.TryGetValue(item, out var value) || (Object)(object)value.GameObject == (Object)null)
		{
			value = CreateOutline(item);
			outlines[item] = value;
		}
		if (!((Object)(object)value.Renderer == (Object)null))
		{
			Transform transform = value.GameObject.transform;
			if ((Object)(object)transform.parent != (Object)(object)((Component)item).transform)
			{
				transform.SetParent(((Component)item).transform);
			}
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one * 1.16f;
			value.Renderer.sprite = itemRenderer.sprite;
			((Renderer)value.Renderer).sortingLayerID = ((Renderer)itemRenderer).sortingLayerID;
			((Renderer)value.Renderer).sortingOrder = ((Renderer)itemRenderer).sortingOrder - 1;
			value.GameObject.SetActive(true);
		}
	}

	private static OutlineEntry CreateOutline(Item item)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("AltItemOutline", new Type[1] { typeof(SpriteRenderer) });
		val.transform.SetParent(((Component)item).transform);
		val.transform.localPosition = Vector3.zero;
		val.transform.localRotation = Quaternion.identity;
		val.transform.localScale = Vector3.one * 1.16f;
		SpriteRenderer component = val.GetComponent<SpriteRenderer>();
		EnsureOutlineMaterialLoaded();
		if ((Object)(object)cachedOutlineMaterial != (Object)null)
		{
			((Renderer)component).material = cachedOutlineMaterial;
		}
		else
		{
			AltItemHighlightPatcher.DebugLog("WARNING: Resources.Load<Material>(\"Special/ItemOutline\") returned null.");
		}
		component.color = new Color(1f, 1f, 1f, 0.95f);
		return new OutlineEntry(val, component);
	}

	private static void EnsureOutlineMaterialLoaded()
	{
		if ((Object)(object)cachedOutlineMaterial == (Object)null)
		{
			cachedOutlineMaterial = Resources.Load<Material>("Special/ItemOutline");
		}
	}

	private void HideInactiveOutlines()
	{
		List<Item> list = null;
		foreach (KeyValuePair<Item, OutlineEntry> outline in outlines)
		{
			if ((Object)(object)outline.Key == (Object)null || (Object)(object)outline.Value.GameObject == (Object)null)
			{
				if (list == null)
				{
					list = new List<Item>();
				}
				list.Add(outline.Key);
			}
			else if (!activeItems.Contains(outline.Key))
			{
				outline.Value.GameObject.SetActive(false);
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				outlines.Remove(list[i]);
			}
		}
	}

	private VisibleItemEntry CreateVisibleItemEntry(Item item, SpriteRenderer renderer, Camera cam)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		Bounds bounds = ((Renderer)renderer).bounds;
		Vector3 val = cam.WorldToScreenPoint(((Bounds)(ref bounds)).min);
		Vector3 val2 = cam.WorldToScreenPoint(((Bounds)(ref bounds)).max);
		Vector2 val3 = default(Vector2);
		((Vector2)(ref val3))._002Ector(Mathf.Min(val.x, val2.x), Mathf.Min(val.y, val2.y));
		Vector2 val4 = default(Vector2);
		((Vector2)(ref val4))._002Ector(Mathf.Max(val.x, val2.x), Mathf.Max(val.y, val2.y));
		Vector2 val5 = (val3 + val4) * 0.5f;
		Vector2 anchor = default(Vector2);
		((Vector2)(ref anchor))._002Ector(val4.x, val5.y);
		Vector3 worldAnchor = default(Vector3);
		((Vector3)(ref worldAnchor))._002Ector(((Bounds)(ref bounds)).max.x, ((Bounds)(ref bounds)).center.y, ((Component)item).transform.position.z);
		return new VisibleItemEntry(GetDisplayName(item), anchor, val3, val4, worldAnchor);
	}

	private void BuildClusters()
	{
		clusters.Clear();
		visibleItems.Sort(CompareVisibleItems);
		for (int i = 0; i < visibleItems.Count; i++)
		{
			VisibleItemEntry item = visibleItems[i];
			int num = FindClusterFor(item);
			if (num < 0)
			{
				clusters.Add(new ClusterEntry(item));
				continue;
			}
			ClusterEntry value = clusters[num];
			value.Add(item);
			clusters[num] = value;
		}
	}

	private static int CompareVisibleItems(VisibleItemEntry left, VisibleItemEntry right)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		float x = left.Anchor.x;
		int num = x.CompareTo(right.Anchor.x);
		if (num != 0)
		{
			return num;
		}
		x = left.Anchor.y;
		return x.CompareTo(right.Anchor.y);
	}

	private static bool IsNearMouseForUi(VisibleItemEntry item)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(Mathf.Clamp(Input.mousePosition.x, item.Min.x, item.Max.x), Mathf.Clamp(Input.mousePosition.y, item.Min.y, item.Max.y));
		float num = (float)Mathf.Min(Screen.width, Screen.height) * 0.33f;
		Vector2 val2 = Vector2.op_Implicit(Input.mousePosition) - val;
		return ((Vector2)(ref val2)).sqrMagnitude <= num * num;
	}

	private int FindClusterFor(VisibleItemEntry item)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < clusters.Count; i++)
		{
			ClusterEntry clusterEntry = clusters[i];
			if (Mathf.Abs(item.Anchor.x - clusterEntry.Center.x) <= 120f && Mathf.Abs(item.Anchor.y - clusterEntry.Center.y) <= 70f)
			{
				return i;
			}
		}
		return -1;
	}

	private void ShowTables()
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)labelRoot == (Object)null || (Object)(object)labelCanvasRect == (Object)null)
		{
			HideAllTables();
			return;
		}
		for (int i = 0; i < clusters.Count; i++)
		{
			TableEntry orCreateTable = GetOrCreateTable(i);
			List<RowData> rows = BuildRows(clusters[i]);
			ConfigureTable(orCreateTable, rows);
			orCreateTable.WorldAnchor = clusters[i].WorldAnchor;
			Vector2 currentPosition = (orCreateTable.TargetPosition = GetTableTargetPosition(orCreateTable.WorldAnchor, orCreateTable.Rect.sizeDelta, Camera.main));
			if (!orCreateTable.HasPosition || !wasAltHeld)
			{
				orCreateTable.CurrentPosition = currentPosition;
				orCreateTable.HasPosition = true;
			}
			orCreateTable.Rect.anchoredPosition = orCreateTable.CurrentPosition;
			orCreateTable.GameObject.SetActive(true);
			tables[i] = orCreateTable;
		}
	}

	private List<RowData> BuildRows(ClusterEntry cluster)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < cluster.Items.Count; i++)
		{
			string name = cluster.Items[i].Name;
			dictionary.TryGetValue(name, out var value);
			dictionary[name] = value + 1;
		}
		List<string> list = new List<string>(dictionary.Keys);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		List<RowData> list2 = new List<RowData>(list.Count);
		for (int j = 0; j < list.Count; j++)
		{
			string text = list[j];
			int num = dictionary[text];
			list2.Add(new RowData((num > 1) ? (text + " (" + num + ")") : text));
		}
		return list2;
	}

	private void ConfigureTable(TableEntry table, List<RowData> rows)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		float num = 36f;
		float fallbackRowHeight = GetFallbackRowHeight();
		for (int i = 0; i < rows.Count; i++)
		{
			RowVisual orCreateRow = GetOrCreateRow(table, i);
			((TMP_Text)orCreateRow.Text).text = rows[i].Text;
			((TMP_Text)orCreateRow.Text).rectTransform.sizeDelta = new Vector2(0f, fallbackRowHeight);
			((TMP_Text)orCreateRow.Text).ForceMeshUpdate(false, true);
			num = Mathf.Max(num, Mathf.Ceil(GetTextPreferredWidth(orCreateRow.Text, rows[i].Text)) + 2f);
			orCreateRow.GameObject.SetActive(true);
			table.Rows[i] = orCreateRow;
		}
		for (int j = 0; j < table.Rows.Count; j++)
		{
			RowVisual value = table.Rows[j];
			if (j >= rows.Count)
			{
				value.GameObject.SetActive(false);
				table.Rows[j] = value;
				continue;
			}
			value.Rect.anchoredPosition = new Vector2(0f, (float)(-j) * fallbackRowHeight);
			SetRectSize(value.Rect, num, fallbackRowHeight);
			SetRectSize(value.Background, num, fallbackRowHeight);
			SetRectSize(((TMP_Text)value.Text).rectTransform, num - 2f, fallbackRowHeight);
			((TMP_Text)value.Text).maxVisibleLines = 1;
			((TMP_Text)value.Text).ForceMeshUpdate(false, true);
			table.Rows[j] = value;
		}
		SetRectSize(table.Rect, num, fallbackRowHeight * (float)rows.Count);
		SetRectSize(table.Background, num, fallbackRowHeight * (float)rows.Count);
	}

	private static float GetTextPreferredWidth(TextMeshProUGUI text, string value)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return Mathf.Max(0f, ((TMP_Text)text).GetPreferredValues(value ?? string.Empty, 0f, 0f).x);
	}

	private static void SetRectSize(RectTransform rect, float width, float height)
	{
		rect.SetSizeWithCurrentAnchors((Axis)0, width);
		rect.SetSizeWithCurrentAnchors((Axis)1, height);
	}

	private static Vector2 GetTableTargetPosition(Vector3 worldAnchor, Vector2 tableSize, Camera cam)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = cam.WorldToScreenPoint(worldAnchor);
		float num = val.x + 48f;
		float num2 = val.y + tableSize.y * 0.5f;
		return new Vector2(Mathf.Clamp(num, 24f, (float)Screen.width - 24f - tableSize.x), Mathf.Clamp(num2, 24f + tableSize.y, (float)Screen.height - 24f));
	}

	private static string GetDisplayName(Item item)
	{
		if (item.Stats.rec.recognizable)
		{
			return item.Stats.fullName;
		}
		switch (item.Stats.category)
		{
		case "medical":
		case "drug":
		case "tool":
		case "container":
		case "utility":
		case "water":
		case "food":
			return Locale.GetOther("unknown" + item.Stats.category);
		default:
			return Locale.GetOther("unknownobject");
		}
	}

	private void EnsureLabelCanvas()
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		Canvas val = null;
		if ((Object)(object)PlayerCamera.main != (Object)null && (Object)(object)PlayerCamera.main.mainCanvas != (Object)null)
		{
			val = ((Component)PlayerCamera.main.mainCanvas).GetComponent<Canvas>();
		}
		if ((Object)(object)val == (Object)null && (Object)(object)GlobalDark.main != (Object)null && (Object)(object)GlobalDark.main.canvas != (Object)null)
		{
			val = GlobalDark.main.canvas;
		}
		if ((Object)(object)val == (Object)null)
		{
			labelCanvas = null;
			labelCanvasRect = null;
			labelRoot = null;
		}
		else if (!((Object)(object)val == (Object)(object)labelCanvas) || !((Object)(object)labelRoot != (Object)null))
		{
			labelCanvas = val;
			labelCanvasRect = ((Component)labelCanvas).GetComponent<RectTransform>();
			GameObject val2 = new GameObject("AltItemHighlightLabels", new Type[1] { typeof(RectTransform) });
			labelRoot = val2.transform;
			labelRoot.SetParent(((Component)labelCanvas).transform, false);
			labelRoot.SetAsLastSibling();
			RectTransform component = val2.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			for (int i = 0; i < tables.Count; i++)
			{
				((Transform)tables[i].Rect).SetParent(labelRoot, false);
			}
		}
	}

	private TableEntry GetOrCreateTable(int index)
	{
		while (tables.Count <= index)
		{
			tables.Add(CreateTable());
		}
		return tables[index];
	}

	private TableEntry CreateTable()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("AltItemHighlightTable", new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(labelRoot, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 0f);
		component.anchorMax = new Vector2(0f, 0f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(180f, GetFallbackRowHeight());
		GameObject val2 = new GameObject("TableBackground", new Type[3]
		{
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(Image)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform component2 = val2.GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(0f, 0f);
		component2.anchorMax = new Vector2(1f, 1f);
		component2.offsetMin = Vector2.zero;
		component2.offsetMax = Vector2.zero;
		Image component3 = val2.GetComponent<Image>();
		((Graphic)component3).color = Color.clear;
		((Graphic)component3).raycastTarget = false;
		val.SetActive(false);
		return new TableEntry(val, component, component2);
	}

	private RowVisual CreateRow(Transform parent)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Expected O, but got Unknown
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Row", new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.sizeDelta = new Vector2(180f, GetFallbackRowHeight());
		GameObject val2 = new GameObject("NameBackground", new Type[3]
		{
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(Image)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform component2 = val2.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.offsetMin = Vector2.zero;
		component2.offsetMax = Vector2.zero;
		CopyTooltipBackground(val2.GetComponent<Image>());
		GameObject val3 = new GameObject("Name", new Type[3]
		{
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(TextMeshProUGUI)
		});
		val3.transform.SetParent(val.transform, false);
		RectTransform component3 = val3.GetComponent<RectTransform>();
		component3.anchorMin = new Vector2(0f, 0f);
		component3.anchorMax = new Vector2(1f, 1f);
		component3.offsetMin = new Vector2(1f, 0f);
		component3.offsetMax = new Vector2(-1f, 0f);
		TextMeshProUGUI component4 = val3.GetComponent<TextMeshProUGUI>();
		CopyTooltipTextStyle(component4);
		((Graphic)component4).raycastTarget = false;
		((TMP_Text)component4).margin = Vector4.zero;
		((TMP_Text)component4).enableAutoSizing = false;
		((TMP_Text)component4).enableWordWrapping = false;
		((TMP_Text)component4).overflowMode = (TextOverflowModes)0;
		((TMP_Text)component4).maxVisibleCharacters = int.MaxValue;
		((TMP_Text)component4).maxVisibleWords = int.MaxValue;
		((TMP_Text)component4).maxVisibleLines = 1;
		val.SetActive(false);
		return new RowVisual(val, component, component2, component4);
	}

	private RowVisual GetOrCreateRow(TableEntry table, int index)
	{
		while (table.Rows.Count <= index)
		{
			table.Rows.Add(CreateRow((Transform)(object)table.Rect));
		}
		return table.Rows[index];
	}

	private static float GetFallbackRowHeight()
	{
		if ((Object)(object)GlobalDark.main != (Object)null && (Object)(object)GlobalDark.main.tooltipNameObj != (Object)null)
		{
			return Mathf.Max(10f, Mathf.Ceil(((TMP_Text)GlobalDark.main.tooltipNameObj).fontSize * 0.78f + 2f));
		}
		return 14f;
	}

	private static void CopyTooltipBackground(Image image)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		image.sprite = null;
		((Graphic)image).material = null;
		image.type = (Type)0;
		((Graphic)image).color = new Color(0f, 0f, 0f, 0.5f);
		((Graphic)image).raycastTarget = false;
	}

	private static void CopyTooltipTextStyle(TextMeshProUGUI text)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)GlobalDark.main != (Object)null && (Object)(object)GlobalDark.main.tooltipNameObj != (Object)null)
		{
			TextMeshProUGUI tooltipNameObj = GlobalDark.main.tooltipNameObj;
			((TMP_Text)text).font = ((TMP_Text)tooltipNameObj).font;
			((TMP_Text)text).fontMaterial = ((TMP_Text)tooltipNameObj).fontMaterial;
			((TMP_Text)text).fontSize = ((TMP_Text)tooltipNameObj).fontSize * 0.78f;
			((TMP_Text)text).fontStyle = ((TMP_Text)tooltipNameObj).fontStyle;
			((Graphic)text).color = ((Graphic)tooltipNameObj).color;
			((TMP_Text)text).alignment = ((TMP_Text)tooltipNameObj).alignment;
		}
		else
		{
			((TMP_Text)text).alignment = (TextAlignmentOptions)513;
			((Graphic)text).color = Color.white;
			((TMP_Text)text).fontSize = 12.5f;
			((TMP_Text)text).fontStyle = (FontStyles)1;
		}
	}

	private void HideUnusedTables(int usedCount)
	{
		for (int i = usedCount; i < tables.Count; i++)
		{
			TableEntry value = tables[i];
			value.GameObject.SetActive(false);
			value.HasPosition = false;
			tables[i] = value;
		}
	}

	private void UpdateTablePositions(Camera cam)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		float num = 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime);
		for (int i = 0; i < tables.Count; i++)
		{
			TableEntry value = tables[i];
			if (value.GameObject.activeSelf)
			{
				value.TargetPosition = GetTableTargetPosition(value.WorldAnchor, value.Rect.sizeDelta, cam);
				value.CurrentPosition = Vector2.Lerp(value.CurrentPosition, value.TargetPosition, num);
				value.Rect.anchoredPosition = value.CurrentPosition;
				tables[i] = value;
			}
		}
	}

	private void HideAllTables()
	{
		for (int i = 0; i < tables.Count; i++)
		{
			TableEntry value = tables[i];
			value.GameObject.SetActive(false);
			value.HasPosition = false;
			tables[i] = value;
		}
	}

	private void ClearActive()
	{
		activeItems.Clear();
		foreach (KeyValuePair<Item, OutlineEntry> outline in outlines)
		{
			if ((Object)(object)outline.Value.GameObject != (Object)null)
			{
				outline.Value.GameObject.SetActive(false);
			}
		}
		HideAllTables();
	}

	private void OnDestroy()
	{
		if ((Object)(object)Instance == (Object)(object)this)
		{
			Instance = null;
		}
		foreach (KeyValuePair<Item, OutlineEntry> outline in outlines)
		{
			if ((Object)(object)outline.Value.GameObject != (Object)null)
			{
				Object.Destroy((Object)(object)outline.Value.GameObject);
			}
		}
		outlines.Clear();
		if ((Object)(object)labelRoot != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)labelRoot).gameObject);
		}
	}
}
