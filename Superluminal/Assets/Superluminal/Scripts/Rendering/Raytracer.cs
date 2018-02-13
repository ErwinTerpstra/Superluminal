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
			
			shadingInfo.color = SampleDirectLight(hitInfo.position, hitInfo.element.Normal, submesh.material);
		}

		public Color SampleDirectLight(Vector3 position, Vector3 normal, Material material)
		{
			Color radiance = Color.black;
			Color diffuse = material.GetColor("_Color");
			
			foreach (Light light in context.Lights)
			{
				if (light.type != LightType.Directional)
					continue;

				float NdotL = Vector3.Dot(normal, -light.transform.forward);

				Color irradiance = SampleLight(position, normal, light);
				radiance += diffuse * irradiance * NdotL;
			}

			return radiance;
		}

		public Color SampleLight(Vector3 position, Vector3 normal, Light light)
		{
			Vector3 l = -light.transform.forward;

			Ray shadowRay = new Ray(position + normal * 1e-4f, l);
			RaycastHit shadowHitInfo;
			if (context.Raycast(ref shadowRay, out shadowHitInfo))
				return Color.black;

			return light.color;
		}

	}
}