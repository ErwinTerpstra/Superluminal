using System;
using UnityEngine;

public static class SampleUtil
{
	public const float INV_PI = (float) (1.0 / Math.PI);
	public const float INV_TWO_PI = (float) (1.0 / (2 * Math.PI));

	public static void UniformHemisphere(Vector2 r, out float phi, out float theta, out float pdf)
	{
		phi = (float) (2 * Math.PI * r.y);
		theta = (float) Math.Acos(Math.Sqrt(Math.Max(0.0f, 1 - r.x * r.x)));
		pdf = INV_TWO_PI;
	}

	public static void CosineWeightedHemisphere(Vector2 r, out float phi, out float theta, out float pdf)
	{
		double cosTheta = Math.Sqrt(Math.Max(0.0f, 1 - Math.Sqrt(r.x)));

		phi = (float)(2 * Math.PI * r.y);
		theta = (float)Math.Acos(cosTheta);
		pdf = (float) (cosTheta * INV_PI);
	}
}
