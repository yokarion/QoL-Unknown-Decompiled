using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QoL_Unknown;

public static class LocaleDownloader
{
	public struct RemoteLocaleInfo
	{
		public string Name;

		public string DownloadUrl;

		public string FileName;
	}

	private const string API_URL = "https://api.github.com/repos/Orsoniks/scavgame-locale/contents/demo";

	public static List<RemoteLocaleInfo> AvailableLocales = new List<RemoteLocaleInfo>();

	public static IEnumerator FetchLocaleList(Action<bool> onComplete)
	{
		UnityWebRequest www = UnityWebRequest.Get("https://api.github.com/repos/Orsoniks/scavgame-locale/contents/demo");
		try
		{
			www.SetRequestHeader("User-Agent", "CasualtiesUnknown-Mod");
			yield return www.SendWebRequest();
			if ((int)www.result != 1)
			{
				Debug.LogError((object)("[LocaleDownloader] Failed to fetch list: " + www.error));
				onComplete?.Invoke(obj: false);
				yield break;
			}
			try
			{
				string json = www.downloadHandler.text;
				JArray data = JArray.Parse(json);
				AvailableLocales.Clear();
				foreach (JObject item2 in data)
				{
					JObject item = item2;
					string name = (string)item["name"];
					string type = (string)item["type"];
					if (type == "file" && name.EndsWith(".json"))
					{
						AvailableLocales.Add(new RemoteLocaleInfo
						{
							Name = Path.GetFileNameWithoutExtension(name),
							FileName = name,
							DownloadUrl = (string)item["download_url"]
						});
					}
				}
				onComplete?.Invoke(obj: true);
			}
			catch (Exception ex)
			{
				Debug.LogError((object)("[LocaleDownloader] Parse error: " + ex.Message));
				onComplete?.Invoke(obj: false);
			}
		}
		finally
		{
			((IDisposable)www)?.Dispose();
		}
	}

	public static IEnumerator DownloadAndInstall(RemoteLocaleInfo info, Action<string> onSuccess)
	{
		UnityWebRequest www = UnityWebRequest.Get(info.DownloadUrl);
		try
		{
			yield return www.SendWebRequest();
			if ((int)www.result != 1)
			{
				Debug.LogError((object)("[LocaleDownloader] Failed download: " + www.error));
				yield break;
			}
			string path = Path.Combine(Application.dataPath, "Lang", info.FileName);
			if (!Directory.Exists(Path.GetDirectoryName(path)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			}
			try
			{
				File.WriteAllText(path, www.downloadHandler.text);
				onSuccess?.Invoke(info.Name);
			}
			catch (Exception ex)
			{
				Debug.LogError((object)("[LocaleDownloader] Write error: " + ex.Message));
			}
		}
		finally
		{
			((IDisposable)www)?.Dispose();
		}
	}
}
