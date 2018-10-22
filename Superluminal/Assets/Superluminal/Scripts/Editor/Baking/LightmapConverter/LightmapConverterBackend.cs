using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class LightmapConverterBackend : BakeBackend
	{

		private Sampler[] lightmapSamplers;

		public LightmapConverterBackend(BakeSettings settings) : base(settings)
		{
			LightmapData[] lightmaps = LightmapSettings.lightmaps;

			lightmapSamplers = new Sampler[lightmaps.Length];

			for (int lightmapIdx = 0; lightmapIdx < lightmaps.Length; ++lightmapIdx)
			{
				LightmapData lightmap = lightmaps[lightmapIdx];

				EnsureTextureIsReadable(lightmap.lightmapColor);

				lightmapSamplers[lightmapIdx] = new Sampler(lightmap.lightmapColor);
			}
		}

		private void EnsureTextureIsReadable(Texture2D texture)
		{
			// Retrieve the texture importer instance
			string assetPath = AssetDatabase.GetAssetPath(texture);
			TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

			// Make sure the texture is marked as readable
			if (!importer.isReadable)
			{
				importer.isReadable = true;
				AssetDatabase.ImportAsset(assetPath);
				AssetDatabase.Refresh();
			}
		}
		
		public override IEnumerator<BakeCommand> Bake(BakeTarget target)
		{
			// Copy vertex attributes from the original mesh
			Vector3[] vertices = target.originalMesh.vertices;
			Vector3[] normals = target.originalMesh.normals;
			Vector4[] tangents = target.originalMesh.tangents;
			Vector2[] uv = target.originalMesh.uv;
			Vector2[] uv2 = target.originalMesh.uv2;

			// Create a new mesh with the same vertex attributes
			Mesh bakedMesh = new Mesh();
			bakedMesh.vertices = vertices;
			bakedMesh.normals = normals;
			bakedMesh.tangents = tangents;
			bakedMesh.uv = uv;
			bakedMesh.uv2 = uv2;

			CopyIndices(target.originalMesh, bakedMesh);

			target.bakedMesh = bakedMesh;

			if (bakedMesh.uv.Length == 0 && bakedMesh.uv2.Length == 0)
			{
				Debug.LogError("Skipping mesh because it has no lightmap UVs", target.originalMesh);
				yield break;
			}

			// Check if the target renderer has a valid lightmap
			if (target.renderer.lightmapIndex == -1 || target.renderer.lightmapIndex >= 0xFFFE)
			{
				Debug.LogWarning("Attempt to bake renderer with invalid lightmap index!", target.renderer);
				yield break;
			}
			
			Sampler lightmapSampler = lightmapSamplers[target.renderer.lightmapIndex];

			Tesselator tesselator = new Tesselator(target, lightmapSampler, settings.tesselationSettings);
			tesselator.BakeVertexColors();

			if (settings.tesselationSettings.tesselate)
			{
				var tesselationRoutine = tesselator.Tesselate();

				while (tesselationRoutine.MoveNext())
					yield return tesselationRoutine.Current;
			}

			tesselator.StoreMesh();
		}
		
	}

}