using System.Collections;
using UnityEngine;

namespace QoL_Unknown;

public class SharedCoroutineStarter : MonoBehaviour
{
	private static SharedCoroutineStarter _instance;

	public static void StartCoroutine(IEnumerator routine)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		if ((Object)(object)_instance == (Object)null)
		{
			GameObject val = new GameObject("QoL_CoroutineRunner");
			Object.DontDestroyOnLoad((Object)val);
			_instance = val.AddComponent<SharedCoroutineStarter>();
		}
		((MonoBehaviour)_instance).StartCoroutine(routine);
	}
}
