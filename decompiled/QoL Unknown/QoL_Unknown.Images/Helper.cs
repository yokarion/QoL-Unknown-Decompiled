using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QoL_Unknown.Images;

internal class Helper
{
	public static Sprite LoadEmbeddedSprite(string filename)
	{
		return LoadEmbeddedSprite(filename, 100f, (FilterMode)0, 1, 1);
	}

	public static Sprite LoadEmbeddedSprite(string filename, float ppu, FilterMode filterMode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return LoadEmbeddedSprite(filename, ppu, filterMode, 1, 1);
	}

	public static Sprite LoadEmbeddedSprite(string filename, float ppu, FilterMode filterMode, int widthMultiplier, int heightMultiplier)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string text = executingAssembly.GetManifestResourceNames().FirstOrDefault((string n) => n.EndsWith(filename + ".png"));
		if (text == null)
		{
			return null;
		}
		Stream? manifestResourceStream = executingAssembly.GetManifestResourceStream(text);
		byte[] array = new byte[manifestResourceStream.Length];
		manifestResourceStream.Read(array, 0, array.Length);
		Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false)
		{
			wrapMode = (TextureWrapMode)1,
			filterMode = filterMode,
			name = text
		};
		ImageConversion.LoadImage(val, array);
		val.Apply();
		Sprite obj = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), ppu);
		((Object)obj).name = text;
		return obj;
	}
}
