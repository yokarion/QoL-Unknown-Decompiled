using System;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace QoL_Unknown;

internal static class CuCoreLibTransitionCompat
{
	internal static void CaptureBodyState(Body body, JObject save)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		Type type = AccessTools.TypeByName("CUCoreLib.Saving.SaveCoordinator");
		if (type == null)
		{
			return;
		}
		object? obj = AccessTools.Method(type, "CaptureBodyState", (Type[])null, (Type[])null)?.Invoke(null, new object[1] { body });
		JObject val = (JObject)((obj is JObject) ? obj : null);
		if (val == null)
		{
			return;
		}
		JToken obj2 = save["CUCoreLib"];
		JObject val2 = (JObject)(((object)((obj2 is JObject) ? obj2 : null)) ?? ((object)new JObject()));
		foreach (JProperty item in val.Properties())
		{
			val2[item.Name] = item.Value.DeepClone();
		}
		save["CUCoreLib"] = (JToken)(object)val2;
	}

	internal static void RestoreBodyState(Body body, JObject save)
	{
		JToken obj = save["CUCoreLib"];
		JObject val = (JObject)(object)((obj is JObject) ? obj : null);
		if (val != null)
		{
			Type type = AccessTools.TypeByName("CUCoreLib.Saving.SaveCoordinator");
			if (!(type == null))
			{
				AccessTools.Method(type, "RestoreBodyState", (Type[])null, (Type[])null)?.Invoke(null, new object[2] { body, val });
			}
		}
	}
}
