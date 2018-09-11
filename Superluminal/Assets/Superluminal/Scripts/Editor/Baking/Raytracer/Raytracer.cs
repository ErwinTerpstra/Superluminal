using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	public class Raytracer
	{
		private RaytracerContext context;

		private PRNG rng;

		private long rays;

		public Raytracer(RaytracerContext context)
		{
			this.context = context;

			rng = new PRNG();
		}

		public Color Integrate(Vector3 position, Vector3 normal, int bounces, int indirectSamples)
		{
			Color ambient = SampleAmbient(normal);
			Color direct = SampleDirectLight(position, normal);
			Color indirect = Color.black;

			if (bounces > 0)
			{
				for (int sample = 0; sample < indirectSamples; ++sample)
					indirect += SampleIndirectLight(position, normal, bounces - 1);

				indirect /= indirectSamples;
			}

			return ambient + direct + indirect;
		}

		public void Sample(ref Ray ray, ref ShadingInfo shadingInfo, int bounces)
		{
			RaycastHit hitInfo;
			if (!context.Raycast(ref ray, out hitInfo))
			{
				shadingInfo.color = SampleSkybox(ray.Direction);
				return;
			}

			Submesh submesh;
			if (!context.RetrieveTriangleData(hitInfo.element, out submesh))
			{
				shadingInfo.color = Color.magenta;
				Debug.LogError("Could not find submesh for triangle!");
				return;
			}

			Color diffuse = submesh.material.GetColor("_Color");

			Color ambient = SampleAmbient(hitInfo.element.Normal);
			Color direct = SampleDirectLight(hitInfo.position, hitInfo.element.Normal);
			Color indirect;

			if (bounces > 0)
				indirect = SampleIndirectLight(hitInfo.position, hitInfo.element.Normal, bounces - 1);
			else
				indirect = Color.black;

			shadingInfo.color = (ambient + direct + indirect) * diffuse / FastMath.PI;
		}

		public Color SampleSkybox(Vector3 direction)
		{
			Material skybox = RenderSettings.skybox;

			if (skybox == null)
				return Color.black;

			return Color.black;
		}

		public Color SampleAmbient(Vector3 normal)
		{
			switch (RenderSettings.ambientMode)
			{
				case UnityEngine.Rendering.AmbientMode.Flat:
					return RenderSettings.ambientLight * FastMath.INV_PI;

				default:
					return Color.black;
			}
		}

		public Color SampleIndirectLight(Vector3 position, Vector3 normal, int bounces)
		{
			Vector3 sampleVector;
			float pdf;

			SampleUtil.CosineWeightedHemisphere(new Vector2(rng.NextFloat(), rng.NextFloat()), out sampleVector, out pdf);
			SampleUtil.TransformSampleVector(ref normal, ref sampleVector);

			float NdotL = Vector3.Dot(normal, sampleVector);

			Ray ray = new Ray(position + normal * 1e-4f, sampleVector);
			ShadingInfo shadingInfo = new ShadingInfo();
			Sample(ref ray, ref shadingInfo, bounces);

			return (shadingInfo.color / pdf) * NdotL;
		}

		public Color SampleDirectLight(Vector3 position, Vector3 normal)
		{
			Color irradiance = Color.black;
			
			foreach (Light light in context.Lights)
			{
				if (light.type != LightType.Directional)
					continue;

				float NdotL = Vector3.Dot(normal, -light.transform.forward);

				if (NdotL > 0.0f)
				{
					Color radiance = SampleLight(position + normal * 1e-4f, light);
					irradiance += radiance * NdotL;
				}
			}

			return irradiance;
		}

		public Color SampleLight(Vector3 position, Light light)
		{
			Vector3 l = -light.transform.forward;
			
			Ray shadowRay = new Ray(position, l);
			RaycastHit shadowHitInfo;
			if (context.Raycast(ref shadowRay, out shadowHitInfo))
				return Color.black;
			
			return light.color;
		}

	}
}