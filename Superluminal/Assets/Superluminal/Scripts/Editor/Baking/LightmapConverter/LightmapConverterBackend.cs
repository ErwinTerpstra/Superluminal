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
		
		public override void Bake(BakeTarget target)
		{
			// Copy vertex attributes from the original mesh
			Vector3[] vertices = target.originalMesh.vertices;
			Vector3[] normals = target.originalMesh.normals;
			Vector2[] texcoords = target.originalMesh.uv;
			
			// Create a new mesh with the same vertex attributes
			Mesh bakedMesh = new Mesh();
			bakedMesh.vertices = vertices;
			bakedMesh.normals = normals;
			bakedMesh.uv = texcoords;

			CopyIndices(target.originalMesh, bakedMesh);

			target.bakedMesh = bakedMesh;
			
			// Check if the target renderer has a valid lightmap
			if (target.renderer.lightmapIndex == -1 || target.renderer.lightmapIndex >= 0xFFFE)
			{
				Debug.LogWarning("Attempt to bake renderer with invalid lightmap index!", target.renderer);
				return;
			}

			// Check if the mesh has lightmap uvs
			Vector2[] lightmapCoords = target.originalMesh.uv2;
			if (lightmapCoords.Length == 0)
				lightmapCoords = texcoords;

			Sampler lightmapSampler = lightmapSamplers[target.renderer.lightmapIndex];

			// Bake vertex colors
			Color[] colors = new Color[target.originalMesh.vertexCount];

			for (int vertexIdx = 0; vertexIdx < vertices.Length; ++vertexIdx)
			{
				// Calculate the lightmap UV coordinates according to the renderer's scale and offset
				Vector4 scaleOffset = target.renderer.lightmapScaleOffset;
				Vector2 lightmapUV = lightmapCoords[vertexIdx];
				lightmapUV.x = (scaleOffset.x * lightmapUV.x + scaleOffset.z);
				lightmapUV.y = (scaleOffset.y * lightmapUV.y + scaleOffset.w);
				
				Color lightmapColor = lightmapSampler.SamplePoint(lightmapUV);
				colors[vertexIdx] = lightmapColor;
			}

			// Store the generated colors
			bakedMesh.colors = colors;			
		}
	}

}