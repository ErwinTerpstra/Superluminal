
namespace Superluminal
{
	public static class FloatMath
	{
		public const float PI = (float)System.Math.PI;

		public const float PI_OVER_2 = PI / 2;
		public const float PI_OVER_4 = PI / 4;

		public const float INV_PI = 1.0f / PI;
		public const float INV_TWO_PI = 1.0f / (2 * PI);

		public static float Abs(float x) { return System.Math.Abs(x); }
		public static float Sqrt(float x) { return (float)System.Math.Sqrt(x); }

		public static float Sin(float x) { return (float)System.Math.Sin(x); }
		public static float Cos(float x) { return (float)System.Math.Cos(x); }
		public static float Asin(float x) { return (float)System.Math.Asin(x); }
		public static float Acos(float x) { return (float)System.Math.Acos(x); }
		
		public static float Max(float a, float b) { return System.Math.Max(a, b); }
		public static float Min(float a, float b) { return System.Math.Min(a, b); }

		public static int Sign(float x) { return System.Math.Sign(x); }
		
		public static int RoundToInt(float x) { return (int)System.Math.Round(x); }
	}
}
