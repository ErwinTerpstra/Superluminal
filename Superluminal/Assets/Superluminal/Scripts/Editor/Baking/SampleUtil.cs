using UnityEngine;

namespace Superluminal
{
	public static class SampleUtil
	{

		public static void UniformBarycentric(Vector2 r, out Vector3 v)
		{
			float sqrtX = FastMath.Sqrt(r.x);

			v = new Vector3(1 - sqrtX, sqrtX * (1 - r.y), sqrtX * r.y);
		}

		public static void UniformHemisphere(Vector2 r, out float phi, out float theta, out float pdf)
		{
			phi = 2 * FastMath.PI * r.y;
			theta = FastMath.Acos(FastMath.Sqrt(FastMath.Max(0.0f, 1 - r.x * r.x)));

			pdf = FastMath.INV_TWO_PI;
		}

		public static void CosineWeightedHemisphere(Vector2 r, out float phi, out float theta, out float pdf)
		{
			float cosTheta = FastMath.Sqrt(FastMath.Max(0.0f, 1 - FastMath.Sqrt(r.x)));

			phi = 2 * FastMath.PI * r.y;
			theta = FastMath.Acos(cosTheta);

			pdf = cosTheta * FastMath.INV_PI;
		}

		public static void CosineWeightedHemisphere(Vector2 r, out Vector3 v, out float pdf)
		{
			ConcentricSampleDisk(r, out v.x, out v.z);
			v.y = FastMath.Sqrt(FastMath.Max(0.0f, 1.0f - v.x * v.x - v.z * v.z));

			pdf = v.y * FastMath.INV_PI;
		}

		public static void ConcentricSampleDisk(Vector2 r, out float x, out float y)
		{
			if (r.x == 0.0f && r.y == 0.0f)
			{
				x = 0.0f;
				y = 0.0f;
				return;
			}

			float phi, radius;

			float a = 2 * r.x - 1;
			float b = 2 * r.y - 1;

			if (a * a > b * b)
			{
				radius = a;
				phi = FastMath.PI_OVER_4 * (b / a);
			}
			else
			{
				radius = b;
				phi = FastMath.PI_OVER_2 - FastMath.PI_OVER_4 * (a / b);
			}

			x = radius * FastMath.Cos(phi);
			y = radius * FastMath.Sin(phi);
		}

		public static void OrthoNormalize(ref Vector3 up, out Vector3 right, out Vector3 forward)
		{
			if (FastMath.Abs(up[0]) > FastMath.Abs(up[1]))
				right = new Vector3(up[2], 0, -up[0]) / FastMath.Sqrt(up[0] * up[0] + up[2] * up[2]);
			else
				right = new Vector3(0, -up[2], up[1]) / FastMath.Sqrt(up[1] * up[1] + up[2] * up[2]);

			forward = Vector3.Cross(up, right);
			forward.Normalize();
		}

		public static void TransformSampleVector(ref Vector3 normal, ref Vector3 vector)
		{
			Vector3 right, forward;
			OrthoNormalize(ref normal, out right, out forward);

			Matrix4x4 m = new Matrix4x4();
			m.SetRow(0, right);
			m.SetRow(1, normal);
			m.SetRow(2, forward);
			m.SetRow(3, new Vector4(0, 0, 0, 1));

			vector = m.MultiplyVector(vector);
		}
	}

}