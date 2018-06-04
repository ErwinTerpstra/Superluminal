using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace Superluminal
{
	public class Tesselator
	{
		private static readonly Vector3 BARYCENTER = new Vector3(1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f);

		private BakeTarget target;

		private Sampler lightmapSampler;

		private TesselationSettings settings;

		private MeshEditor meshEditor;

		private List<TesselationCandidate> candidates;

		private PRNG rng;

		public Tesselator(BakeTarget target, Sampler lightmapSampler, TesselationSettings settings)
		{
			this.target = target;
			this.lightmapSampler = lightmapSampler;
			this.settings = settings;

			//Unwrapping.GenerateSecondaryUVSet(target.bakedMesh);

			meshEditor = new MeshEditor(target.bakedMesh);

			candidates = new List<TesselationCandidate>();

			rng = new PRNG();
		}

		public void BakeVertexColors()
		{
			meshEditor.colors.Clear();

			// Sample lightmap for each vertex
			for (int vertexIdx = 0; vertexIdx < meshEditor.vertices.Count; ++vertexIdx)
			{
				Color lightmapColor = SampleLightmap(meshEditor.LightmapUV[vertexIdx]);
				meshEditor.colors.Add(EncodeVertexColor(lightmapColor));
			}
		}

		public void Tesselate()
		{
			int maxVertexCount = (int) (meshEditor.vertices.Count * settings.maxTesselationFactor);

			while (meshEditor.vertices.Count < maxVertexCount)
			{
				if (candidates.Count == 0)
					FindCandidates();

				// Select the best candidate for tesselation
				int candidateIdx = candidates.Count - 1;
				TesselationCandidate candidate = candidates[candidateIdx];
				candidates.RemoveAt(candidateIdx);

				// If the error of this candidate is below the threshold, we are finished
				if (candidate.error < settings.minimumError)
					break;

				// Otherwise, tesselate the mesh with this candidate
				PerformTesselation(candidate);
			}

			// Store the changes to the mesh
			meshEditor.Store();
		}

		/// <summary>
		/// Performs tesselation by adding the candidate vertex. This removes one triangle and adds three new ones.
		/// </summary>
		/// <param name="candidate"></param>
		private void PerformTesselation(TesselationCandidate candidate)
		{
			// Add the new vertex
			int i3 = AddVertex(candidate);

			List<int> indices = meshEditor.indices[candidate.submeshIndex];

			// Retrieve the indices for this candidate
			int i0 = indices[candidate.indexOffset + 0];
			int i1 = indices[candidate.indexOffset + 1];
			int i2 = indices[candidate.indexOffset + 2];
			
			// For the first triangle, the last index is replaced
			indices[candidate.indexOffset + 2] = i3;

			// The other two are added as new triangles
			indices.Add(i1);
			indices.Add(i2);
			indices.Add(i3);

			indices.Add(i2);
			indices.Add(i0);
			indices.Add(i3);

			// Remove candidates for this same triangle
			for (int candidateIdx = candidates.Count - 1; candidateIdx >= 0; --candidateIdx)
			{
				TesselationCandidate otherCandidate = candidates[candidateIdx];

				if (otherCandidate.submeshIndex == candidate.submeshIndex && otherCandidate.indexOffset == candidate.indexOffset)
					candidates.RemoveAt(candidateIdx);
			}

			// Add new candidates for the new triangles
			GenerateCandidates(candidate.submeshIndex, candidate.indexOffset);
			GenerateCandidates(candidate.submeshIndex, indices.Count - 6);
			GenerateCandidates(candidate.submeshIndex, indices.Count - 3);

			SortCandidates();
		}

		/// <summary>
		/// Adds a vertex to the mesh that matches the given candidate
		/// </summary>
		/// <param name="candidate"></param>
		/// <returns>The index of the added vertex</returns>
		private int AddVertex(TesselationCandidate candidate)
		{
			int newIndex = meshEditor.vertices.Count;

			// Retrieve the indices for this candidate
			int i0 = meshEditor.indices[candidate.submeshIndex][candidate.indexOffset + 0];
			int i1 = meshEditor.indices[candidate.submeshIndex][candidate.indexOffset + 1];
			int i2 = meshEditor.indices[candidate.submeshIndex][candidate.indexOffset + 2];

			// Add the new vertex position
			Vector3 v0 = meshEditor.vertices[i0];
			Vector3 v1 = meshEditor.vertices[i1];
			Vector3 v2 = meshEditor.vertices[i2];

			meshEditor.vertices.Add(Interpolate(v0, v1, v2, candidate.barycentricCoords));
			
			// Add the new vertex's normal, if neccesary
			if (meshEditor.normals.Count > 0)
			{
				Vector3 n0 = meshEditor.normals[i0];
				Vector3 n1 = meshEditor.normals[i1];
				Vector3 n2 = meshEditor.normals[i2];

				Vector3 normal = Interpolate(n0, n1, n2, candidate.barycentricCoords);
				normal.Normalize();
				meshEditor.normals.Add(normal);
			}

			// Add the new vertex's tangent, if neccesary
			if (meshEditor.tangents.Count > 0)
			{
				Vector3 t0 = meshEditor.tangents[i0];
				Vector3 t1 = meshEditor.tangents[i1];
				Vector3 t2 = meshEditor.tangents[i2];

				Vector3 tangent = Interpolate(t0, t1, t2, candidate.barycentricCoords);
				tangent.Normalize();
				meshEditor.tangents.Add(tangent);
			}

			// Add the new vertex's UV, for each existing channel
			for (int uvChannel = 0; uvChannel < meshEditor.uvs.Count; ++uvChannel)
			{
				List<Vector2> uvs = meshEditor.uvs[uvChannel];

				if (uvs.Count == 0)
					continue;

				Vector2 uv0 = uvs[i0];
				Vector2 uv1 = uvs[i1];
				Vector2 uv2 = uvs[i2];
				uvs.Add(Interpolate(uv0, uv1, uv2, candidate.barycentricCoords));
			}

			// Add the color the lightmap has at the candidate position as the vertex's color
			Vector2 luv0 = meshEditor.LightmapUV[i0];
			Vector2 luv1 = meshEditor.LightmapUV[i1];
			Vector2 luv2 = meshEditor.LightmapUV[i2];
			meshEditor.colors.Add(EncodeVertexColor(SampleLightmap(Interpolate(luv0, luv1, luv2, candidate.barycentricCoords))));

			return newIndex;
		}
		
		private void FindCandidates()
		{
			for (int submeshIdx = 0; submeshIdx < meshEditor.indices.Count; ++submeshIdx)
			{
				List<int> indices = meshEditor.indices[submeshIdx];

				for (int indexOffset = 0; indexOffset < indices.Count; indexOffset += 3)
					GenerateCandidates(submeshIdx, indexOffset);
			}

			SortCandidates();
		}

		private void GenerateCandidates(int submeshIdx, int indexOffset)
		{
			for (int candidateIdx = 0; candidateIdx < settings.candidatesPerTriangle; ++candidateIdx)
			{
				TesselationCandidate candidate = new TesselationCandidate()
				{
					submeshIndex = submeshIdx,
					indexOffset = indexOffset
				};

				// Generate a random point on this triangle to use as a candidate
				SampleUtil.UniformBarycentric(rng.NextVector2(), out candidate.barycentricCoords);

				// Calculate the error that is currently present at this candidate
				CalculateError(ref candidate);

				candidates.Add(candidate);
			}
		}

		private void SortCandidates()
		{
			candidates.Sort((a, b) =>
			{
				return FloatMath.Sign(a.error - b.error);
			});
		}

		private Color EncodeVertexColor(Color color)
		{
			// Convert color to LDR range
			color.r = Mathf.Min(color.r / 2.0f, 1.0f);
			color.g = Mathf.Min(color.g / 2.0f, 1.0f);
			color.b = Mathf.Min(color.b / 2.0f, 1.0f);

			// Apply gamma curve, if neccesary
			if (PlayerSettings.colorSpace == ColorSpace.Gamma)
				color = color.gamma;

			return color;
		}

		private Color DecodeVertexColor(Color color)
		{
			if (PlayerSettings.colorSpace == ColorSpace.Gamma)
				color = color.linear;

			color.r *= 2.0f;
			color.g *= 2.0f;
			color.b *= 2.0f;

			return color;
		}

		private Color SampleLightmap(Vector2 uv)
		{
			// Calculate the lightmap UV coordinates according to the renderer's scale and offset
			Vector4 scaleOffset = target.renderer.lightmapScaleOffset;
			
			Vector2 lightmapUV = uv;
			lightmapUV.x = (scaleOffset.x * lightmapUV.x + scaleOffset.z);
			lightmapUV.y = (scaleOffset.y * lightmapUV.y + scaleOffset.w);
			
			Color lightmapColor = lightmapSampler.SamplePoint(lightmapUV);

			//lightmapColor.r = lightmapColor.r * 8.0f * lightmapColor.a;
			//lightmapColor.g = lightmapColor.g * 8.0f * lightmapColor.a;
			//lightmapColor.b = lightmapColor.b * 8.0f * lightmapColor.a;

			string lightmapAssetPath = AssetDatabase.GetAssetPath(lightmapSampler.Texture);
			TextureImporter lightmapImporter = AssetImporter.GetAtPath(lightmapAssetPath) as TextureImporter;

			if (lightmapImporter.sRGBTexture)
				lightmapColor = lightmapColor.linear;

			return lightmapColor;
		}

		private void CalculateError(ref TesselationCandidate candidate)
		{
			// Retrieve the indices for this candidate
			int i0 = meshEditor.indices[candidate.submeshIndex][candidate.indexOffset + 0];
			int i1 = meshEditor.indices[candidate.submeshIndex][candidate.indexOffset + 1];
			int i2 = meshEditor.indices[candidate.submeshIndex][candidate.indexOffset + 2];

			// Retrieve the lightmap UVs of this candidate
			Vector2 uv0 = meshEditor.LightmapUV[i0];
			Vector2 uv1 = meshEditor.LightmapUV[i1];
			Vector2 uv2 = meshEditor.LightmapUV[i2];

			// Retrieve the vertex colors of this candidate
			Color c0 = DecodeVertexColor(meshEditor.colors[i0]);
			Color c1 = DecodeVertexColor(meshEditor.colors[i1]);
			Color c2 = DecodeVertexColor(meshEditor.colors[i2]);

			// Interpolate the lightmap UV at the candidate position
			Vector2 interpolatedUV = Interpolate(uv0, uv1, uv2, candidate.barycentricCoords);

			// Sample the lightmap to get the desired color at the candidate
			Color desiredColor = SampleLightmap(interpolatedUV);

			// Interpolate the current vertex colors to calculate what would be rendered with the current vertex colors
			Color interpolatedColor = Interpolate(c0, c1, c2, candidate.barycentricCoords);

			// Calculate the error as the difference between the interpolated color and the desired color
			float currentError = CalculateError(c0, c1, c2, uv0, uv1, uv2, candidate.barycentricCoords);

			float currentError0 = CalculateError(c0, c1, interpolatedColor, uv0, uv1, interpolatedUV, BARYCENTER);
			float currentError1 = CalculateError(c1, c2, interpolatedColor, uv1, uv2, interpolatedUV, BARYCENTER);
			float currentError2 = CalculateError(c2, c0, interpolatedColor, uv2, uv0, interpolatedUV, BARYCENTER);

			float newError0 = CalculateError(c0, c1, desiredColor, uv0, uv1, interpolatedUV, BARYCENTER);
			float newError1 = CalculateError(c1, c2, desiredColor, uv1, uv2, interpolatedUV, BARYCENTER);
			float newError2 = CalculateError(c2, c0, desiredColor, uv2, uv0, interpolatedUV, BARYCENTER);

			float deltaError = ((currentError0 + currentError1 + currentError2) / 3.0f) - ((newError0 + newError1 + newError2) / 3.0f);

			/*
			// Retrieve the vertices of this candidate
			Vector3 v0 = meshEditor.vertices[i0];
			Vector3 v1 = meshEditor.vertices[i1];
			Vector3 v2 = meshEditor.vertices[i2];

			// Transform the vertices to world space
			v0 = target.renderer.transform.TransformPoint(v0);
			v1 = target.renderer.transform.TransformPoint(v1);
			v2 = target.renderer.transform.TransformPoint(v2);

			// Calculate the triangle area
			float area = CalculateArea(v0, v1, v2);
			deltaError *= area;
			*/

			candidate.error = deltaError;
		}

		private float CalculateError(Color c0, Color c1, Color c2, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector3 barycentricCoords)
		{
			// Interpolate the lightmap UV at the given position
			Vector2 interpolatedUV = Interpolate(uv0, uv1, uv2, barycentricCoords);

			// Sample the lightmap to get the desired color at the candidate
			Color desiredColor = SampleLightmap(interpolatedUV);

			// Interpolate the current vertex colors to calculate what would be rendered with the current vertex colors
			Color interpolatedColor = Interpolate(c0, c1, c2, barycentricCoords);
			
			// Calculate the error as the difference between the interpolated color and the desired color
			return FloatMath.Abs(CalculateColorIntensity(desiredColor) - CalculateColorIntensity(interpolatedColor));
		}

		private float CalculateColorIntensity(Color c)
		{
			return c.r + c.g + c.b;
		}

		private float CalculateArea(Vector3 v0, Vector3 v1, Vector3 v2)
		{
			float a = (v1 - v0).magnitude;
			float b = (v2 - v0).magnitude;
			float c = (v2 - v1).magnitude;

			float s = 0.5f * (a + b + c);

			return FloatMath.Sqrt(s * (s - a) * (s - b) * (s - c));
		}

		private Vector2 Interpolate(Vector2 a, Vector2 b, Vector2 c, Vector3 barycentric)
		{
			return a * barycentric.x + b * barycentric.y + c * barycentric.z;
		}

		private Vector3 Interpolate(Vector3 a, Vector3 b, Vector3 c, Vector3 barycentric)
		{
			return a * barycentric.x + b * barycentric.y + c * barycentric.z;
		}

		private Vector4 Interpolate(Vector4 a, Vector4 b, Vector4 c, Vector3 barycentric)
		{
			return a * barycentric.x + b * barycentric.y + c * barycentric.z;
		}

		private Color Interpolate(Color a, Color b, Color c, Vector3 barycentric)
		{
			return a * barycentric.x + b * barycentric.y + c * barycentric.z;
		}
	}

}