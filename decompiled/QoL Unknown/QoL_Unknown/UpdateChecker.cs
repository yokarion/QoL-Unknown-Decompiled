using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoL_Unknown;

public class UpdateChecker : MonoBehaviour
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static UnityAction _003C_003E9__12_0;

		internal void _003CCreateUpdateUIDelayed_003Eb__12_0()
		{
			Application.OpenURL("https://github.com/jimmyking9999999/QoL-Unknown/releases");
		}
	}

	private const string PluginGuid = "org.bepinex.plugins.qol_unknown";

	private static ManualLogSource Logger;

	private static bool hasChecked = false;

	private static string newVersion = "";

	private static bool updateAvailable = false;

	private static string currentVersion = "";

	private static UpdateChecker instance;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		try
		{
			if (Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.qol_unknown"))
			{
				currentVersion = "v" + Chainloader.PluginInfos["org.bepinex.plugins.qol_unknown"].Metadata.Version.ToString();
			}
			SceneManager.sceneLoaded += OnSceneLoadedStatic;
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Updatecheck Initialize failed...? Huh? " + ex.Message));
		}
	}

	private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		RemoveUpdateUI();
		if ((Object)(object)instance == (Object)null)
		{
			GameObject val = new GameObject("QoL_UpdateChecker");
			Object.DontDestroyOnLoad((Object)(object)val);
			instance = val.AddComponent<UpdateChecker>();
		}
		if (!hasChecked)
		{
			hasChecked = true;
			((MonoBehaviour)instance).StartCoroutine(instance.CheckForUpdates());
		}
		if (((Scene)(ref scene)).name == "PreGen" && updateAvailable)
		{
			((MonoBehaviour)instance).StartCoroutine(instance.CreateUpdateUIDelayed());
		}
	}

	private static void RemoveUpdateUI()
	{
		TextMeshProUGUI[] array = Object.FindObjectsOfType<TextMeshProUGUI>();
		foreach (TextMeshProUGUI val in array)
		{
			if ((Object)(object)val != (Object)null && (Object)(object)((Component)val).gameObject != (Object)null && ((Object)((Component)val).gameObject).name == "QoLUpdateText")
			{
				Object.Destroy((Object)(object)((Component)val).gameObject);
			}
		}
	}

	private IEnumerator CheckForUpdates()
	{
		UnityWebRequest www = UnityWebRequest.Get("https://jimmyking9999999.github.io/Metadata-generator/nexusmods-versions.json");
		try
		{
			www.SetRequestHeader("User-Agent", "QoLUnknownMod");
			yield return www.SendWebRequest();
			if ((int)www.result != 1)
			{
				Logger.LogWarning((object)("Failed to check version, probably due to being offline: " + www.error));
				yield break;
			}
			try
			{
				Dictionary<string, string> versions = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);
				if (versions == null || !versions.TryGetValue("org.bepinex.plugins.qol_unknown", out var latestVersion))
				{
					Logger.LogWarning((object)"No Nexus version published for org.bepinex.plugins.qol_unknown yet.");
					yield break;
				}
				int versionComparison = CompareVersions(currentVersion, latestVersion);
				if (versionComparison < 0)
				{
					updateAvailable = true;
					newVersion = latestVersion;
					Logger.LogWarning((object)("QoL is out of date! " + currentVersion + " -> " + newVersion));
					Scene activeScene = SceneManager.GetActiveScene();
					string sceneName = ((Scene)(ref activeScene)).name;
					if (sceneName == "PreGen")
					{
						((MonoBehaviour)this).StartCoroutine(CreateUpdateUIDelayed());
					}
				}
				else if (versionComparison > 0)
				{
					Logger.LogInfo((object)("QoL is (more then) up to date " + currentVersion + " -> " + latestVersion + " (what?)"));
				}
				else
				{
					Logger.LogInfo((object)("QoL is up to date! " + currentVersion + " -> " + latestVersion));
				}
			}
			catch (Exception ex)
			{
				Logger.LogWarning((object)("Failed to parse Nexus version metadata: " + ex.Message));
			}
		}
		finally
		{
			((IDisposable)www)?.Dispose();
		}
	}

	private int CompareVersions(string current, string latest)
	{
		current = current.Trim().TrimStart(new char[1] { 'v' });
		latest = latest.Trim().TrimStart(new char[1] { 'v' });
		if (Version.TryParse(current, out Version result) && Version.TryParse(latest, out Version result2))
		{
			return result.CompareTo(result2);
		}
		return 0;
	}

	private IEnumerator CreateUpdateUIDelayed()
	{
		Canvas canvas = null;
		int attempts = 0;
		Scene activeScene;
		while ((Object)(object)canvas == (Object)null && attempts < 100)
		{
			activeScene = SceneManager.GetActiveScene();
			if (((Scene)(ref activeScene)).name != "PreGen")
			{
				yield break;
			}
			GameObject canvasObj = GameObject.Find("Canvas");
			if ((Object)(object)canvasObj != (Object)null)
			{
				canvas = canvasObj.GetComponent<Canvas>();
			}
			if ((Object)(object)canvas == (Object)null)
			{
				attempts++;
				yield return (object)new WaitForSeconds(0.1f);
			}
		}
		if ((Object)(object)canvas == (Object)null)
		{
			yield break;
		}
		activeScene = SceneManager.GetActiveScene();
		if (((Scene)(ref activeScene)).name != "PreGen" || (Object)(object)((Component)canvas).transform.Find("QoLUpdateText") != (Object)null)
		{
			yield break;
		}
		GameObject textObj = new GameObject("QoLUpdateText", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		textObj.transform.SetParent(((Component)canvas).transform, false);
		RectTransform rect = textObj.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(0f, 0f);
		rect.pivot = new Vector2(0f, 0f);
		rect.anchoredPosition = new Vector2(22f, 90f);
		rect.sizeDelta = new Vector2(600f, 50f);
		TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
		TMP_FontAsset font = ResourceLookupCache.FindFontContains("Retro GamingPix");
		if (Object.op_Implicit((Object)(object)font))
		{
			((TMP_Text)tmp).font = font;
		}
		((TMP_Text)tmp).fontSize = 20f;
		((TMP_Text)tmp).alignment = (TextAlignmentOptions)1025;
		((Graphic)tmp).color = new Color(1f, 0.3f, 0.3f);
		((TMP_Text)tmp).text = "QoL is out of date! (" + currentVersion + " -> " + newVersion + ")\n(Click here to open the github release)";
		Button btn = textObj.AddComponent<Button>();
		((Selectable)btn).transition = (Transition)1;
		((Selectable)btn).targetGraphic = (Graphic)(object)tmp;
		ButtonClickedEvent onClick = btn.onClick;
		object obj = _003C_003Ec._003C_003E9__12_0;
		if (obj == null)
		{
			UnityAction val = delegate
			{
				Application.OpenURL("https://github.com/jimmyking9999999/QoL-Unknown/releases");
			};
			_003C_003Ec._003C_003E9__12_0 = val;
			obj = (object)val;
		}
		((UnityEvent)onClick).AddListener((UnityAction)obj);
	}
}
