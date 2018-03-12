using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	public class Raytracer
	{
		private BakeContext context;

		public Raytracer(BakeContext context)
		{
			this.context = context;
		}

		
		public void CalculateShading(ref Ray ray, ref ShadingInfo shadingInfo)
		{
			RaycastHit hitInfo;
			if (!context.Raycast(ref ray, out hitInfo))
			{
				shadingInfo.color = Color.magenta;
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
			Color radiance = SampleDirectLight(hitInfo.position, hitInfo.element.Normal);

			shadingInfo.color = radiance * diffuse;
		}

		public Color Sample(Vector3 position, Vector3 normal, Material material)
		{
			return SampleDirectLight(position, normal);
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
					Color radiance = SampleLight(position, normal, light);
					irradiance += radiance * NdotL;
				}
			}

			return irradiance;
		}

		public Color SampleLight(Vector3 position, Vector3 normal, Light light)
		{
			Vector3 l = -light.transform.forward;
			
			Ray shadowRay = new Ray(position + normal * 1e-4f, l);
			RaycastHit shadowHitInfo;
			if (context.Raycast(ref shadowRay, out shadowHitInfo))
			{
				Debug.DrawLine(shadowRay.Origin, shadowHitInfo.position, Color.red, 1.0f);
				return Color.black;
			}
			
			Debug.DrawLine(shadowRay.Origin, shadowRay.Origin + shadowRay.Direction, Color.green, 1.0f);
			return light.color;
		}

	}
}