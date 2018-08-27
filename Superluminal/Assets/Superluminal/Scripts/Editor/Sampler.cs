using System;
using UnityEngine;

namespace Superluminal
{
	public class Sampler
	{
		private readonly Texture2D texture;

		private Color[] colors;

		private int width, height;

		public Sampler(Texture2D texture)
		{
			this.texture = texture;

			width = texture.width;
			height = texture.height;

			colors = texture.GetPixels(0);
		}

		public void Write()
		{
			texture.SetPixels(colors, 0);
			texture.Apply();
		}

		public int GetOffset(int x, int y)
		{
			x = Math.Max(Math.Min(x, width - 1), 0);
			y = Math.Max(Math.Min(y, height - 1), 0);

			return y * width + x;
		}

		public Color GetPixel(int x, int y)
		{
			return colors[GetOffset(x, y)];
		}

		public void SetPixel(int x, int y, Color color)
		{
			colors[GetOffset(x, y)] = color;
		}

		public Color SamplePoint(Vector2 uv)
		{
			float x = uv.x * (width - 1);
			float y = uv.y * (height - 1);

			return GetPixel(FloatMath.RoundToInt(x), FloatMath.RoundToInt(y));
		}

		public Color SampleBilinear(Vector2 uv)
		{
			float x = uv.x * (width - 1);
			float y = uv.y * (height - 1);

			int intX = (int)x;
			int intY = (int)y;
			float fractX = x - intX;
			float fractY = y - intY;

			Color tl = GetPixel(intX, intY);
			Color tr = GetPixel(intX + 1, intY);
			Color bl = GetPixel(intX, intY + 1);
			Color br = GetPixel(intX + 1, intY + 1);

			Color t = Color.Lerp(tl, tr, fractX);
			Color b = Color.Lerp(bl, br, fractX);

			return Color.Lerp(t, b, fractY);
		}


		public Texture2D Texture
		{
			get { return texture; }
		}

		public int Width
		{
			get { return width; }
		}

		public int Height
		{
			get { return height; }
		}

	}

}