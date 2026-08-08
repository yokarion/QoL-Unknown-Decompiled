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
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return LoadEmbeddedSprite(filename, ppu, filterMode, 1, 1);
	}

	public static Sprite LoadEmbeddedSprite(string filename, float ppu, FilterMode filterMode, int widthMultiplier, int heightMultiplier)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string text = executingAssembly.GetManifestResourceNames().FirstOrDefault((string n) => n.EndsWith(filename + ".png"));
		if (text == null)
		{
			return null;
		}
		Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(text);
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
		Sprite val2 = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), ppu);
		((Object)val2).name = text;
		return val2;
	}
}
