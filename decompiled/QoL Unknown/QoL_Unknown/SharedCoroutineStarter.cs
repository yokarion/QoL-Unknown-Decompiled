using System.Collections;
using UnityEngine;

namespace QoL_Unknown;

public class SharedCoroutineStarter : MonoBehaviour
{
	private static SharedCoroutineStarter _instance;

	public static void StartCoroutine(IEnumerator routine)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		if ((Object)(object)_instance == (Object)null)
		{
			GameObject val = new GameObject("QoL_CoroutineRunner");
			Object.DontDestroyOnLoad((Object)(object)val);
			_instance = val.AddComponent<SharedCoroutineStarter>();
		}
		((MonoBehaviour)_instance).StartCoroutine(routine);
	}
}
