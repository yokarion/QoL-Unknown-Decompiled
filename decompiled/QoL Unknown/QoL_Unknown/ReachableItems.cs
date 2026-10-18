using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

internal static class ReachableItems
{
	private const float Reach = 10f;

	private static readonly List<Item> Ground = new List<Item>();

	private static readonly HashSet<Item> Seen = new HashSet<Item>();

	private static int groundFrame = -1;

	private static Body groundBody;

	private static Vector2 groundOrigin;

	private static int itemLayerMask = -1;

	public static List<Item> Gather(Body body)
	{
		List<Item> list = new List<Item>();
		Seen.Clear();
		if ((Object)(object)body == (Object)null)
		{
			return list;
		}
		RefreshGround(body);
		AddRange(list, body.GetAllItemsThorough());
		for (int i = 0; i < Ground.Count; i++)
		{
			Add(list, Ground[i]);
		}
		return list;
	}

	public static List<Item> MatchIngredients(Recipe recipe, Body body, bool tolerateMissing)
	{
		List<Item> list = Gather(body);
		List<Item> list2 = new List<Item>();
		List<Item> list3 = new List<Item>();
		for (int i = 0; i < recipe.items.Count; i++)
		{
			RecipeItem val = recipe.items[i];
			Item matchingItem = val.GetMatchingItem(list, list3);
			if ((Object)(object)matchingItem != (Object)null)
			{
				list2.Add(matchingItem);
				if (val.destroyItem && !val.isLiquid)
				{
					list3.Add(matchingItem);
				}
			}
			else
			{
				if (!tolerateMissing)
				{
					return null;
				}
				list2.Add(null);
			}
		}
		return list2;
	}

	public static void InvalidateGround()
	{
		groundFrame = -1;
	}

	private static void RefreshGround(Body body)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = Vector2.op_Implicit(((Component)body).transform.position);
		if (groundFrame == Time.frameCount && (Object)(object)groundBody == (Object)(object)body && groundOrigin == val)
		{
			return;
		}
		if (itemLayerMask < 0)
		{
			itemLayerMask = LayerMask.GetMask(new string[1] { "Item" });
		}
		Ground.Clear();
		Seen.Clear();
		Collider2D[] array = Physics2D.OverlapCircleAll(val, 10f, itemLayerMask);
		Item val2 = default(Item);
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null && ((Component)array[i]).TryGetComponent<Item>(ref val2) && (Object)(object)val2 != (Object)null && Seen.Add(val2) && body.DoPickupCheck(val2, true))
			{
				Ground.Add(val2);
			}
		}
		Seen.Clear();
		groundFrame = Time.frameCount;
		groundBody = body;
		groundOrigin = val;
	}

	private static void AddRange(List<Item> pool, List<Item> source)
	{
		for (int i = 0; i < source.Count; i++)
		{
			Add(pool, source[i]);
		}
	}

	private static void Add(List<Item> pool, Item item)
	{
		if ((Object)(object)item != (Object)null && Seen.Add(item))
		{
			pool.Add(item);
		}
	}
}
