using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace Superluminal
{
	public class Tesselator
	{
		private const float ldrMultiplier = 1.0f;
		private const float gammaToLinear = 2.2f;
		private const float linearToGamma = 1.0f / gammaToLinear;

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
			AddCandidatesForAllEdges();

			int maxVertexCount = (int) (meshEditor.vertices.Count * settings.maxTesselationFactor);

			while (meshEditor.vertices.Count < maxVertexCount)
			{
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
		/// Performs tesselation by adding the candidate vertex somewhere on the candidate edge. This removes each triangle that uses this edge and adds two new ones.
		/// </summary>
		/// <param name="candidate"></param>
		private void PerformTesselation(TesselationCandidate candidate)
		{
			// Add the new vertex
			int i2 = SplitEdge(candidate.edge, candidate.t);

			// Retrieve the indices for this candidate
			int i0 = candidate.edge.index0;
			int i1 = candidate.edge.index1;
			
			// Iterate through all submeshes and find triangles that use this edge
			for (int submeshIdx = 0; submeshIdx < meshEditor.indices.Count; ++submeshIdx)
			{
				List<int> indices = meshEditor.indices[submeshIdx];

				for (int indexOffset = 0; indexOffset < indices.Count; indexOffset += 3)
				{
					// Check this triangle twice, the second time with the edge indices reversed
					for (int i = 0; i < 2; ++i)
					{
						SplitEdgeIfIndicesMatch(submeshIdx, indexOffset + 0, indexOffset + 1, indexOffset + 2, i0, i1, i2);
						SplitEdgeIfIndicesMatch(submeshIdx, indexOffset + 1, indexOffset + 2, indexOffset + 0, i0, i1, i2);
						SplitEdgeIfIndicesMatch(submeshIdx, indexOffset + 2, indexOffset + 0, indexOffset + 1, i0, i1, i2);

						Swap(ref i0, ref i1);
					}
				}

			}

			SortCandidates();
		}
		
		private void SplitEdgeIfIndicesMatch(int submeshIdx, int indexOffset0, int indexOffset1, int indexOffset2, int edgeIndex0, int edgeIndex1, int newIndex)
		{
			List<int> indices = meshEditor.indices[submeshIdx];

			int t0 = indices[indexOffset0];
			int t1 = indices[indexOffset1];
			int t2 = indices[indexOffset2];

			if (t0 == edgeIndex0 && t1 == edgeIndex1)
			{
				indices[indexOffset1] = newIndex;

				indices.Add(newIndex);
				indices.Add(t1);
				indices.Add(t2);

				AddCandidate(t0, newIndex);
				AddCandidate(newIndex, t1);
				AddCandidate(t2, newIndex);
			}
			

		}
		
		/// <summary>
		/// Adds a vertex to the mesh is at the given edge
		/// </summary>
		/// <param name="edge"></param>
		/// <returns>The index of the added vertex</returns>
		private int SplitEdge(Edge edge, float t)
		{
			int newIndex = meshEditor.vertices.Count;

			// Retrieve the indices for this candidate
			int i0 = edge.index0;
			int i1 = edge.index1;

			// Add the new vertex position
			Vector3 v0 = meshEditor.vertices[i0];
			Vector3 v1 = meshEditor.vertices[i1];

			meshEditor.vertices.Add(Vector3.Lerp(v0, v1, t));

			// Add the new vertex's normal, if neccesary
			if (meshEditor.normals.Count > 0)
			{
				Vector3 n0 = meshEditor.normals[i0];
				Vector3 n1 = meshEditor.normals[i1];

				Vector3 normal = Vector3.Lerp(n0, n1, t);
				normal.Normalize();
				meshEditor.normals.Add(normal);
			}

			// Add the new vertex's tangent, if neccesary
			if (meshEditor.tangents.Count > 0)
			{
				Vector3 t0 = meshEditor.tangents[i0];
				Vector3 t1 = meshEditor.tangents[i1];

				Vector3 tangent = Vector3.Lerp(t0, t1, t);
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
				uvs.Add(Vector3.Lerp(uv0, uv1, t));
			}

			// Add the color the lightmap has at the candidate position as the vertex's color
			Vector2 luv0 = meshEditor.LightmapUV[i0];
			Vector2 luv1 = meshEditor.LightmapUV[i1];
			meshEditor.colors.Add(EncodeVertexColor(SampleLightmap(Vector2.Lerp(luv0, luv1, t))));

			return newIndex;
		}

		private void AddCandidatesForAllEdges()
		{
			for (int submeshIdx = 0; submeshIdx < meshEditor.indices.Count; ++submeshIdx)
			{
				List<int> indices = meshEditor.indices[submeshIdx];

				for (int indexOffset = 0; indexOffset < indices.Count; indexOffset += 3)
				{
					AddCandidate(indices[indexOffset + 0], indices[indexOffset + 1]);
					AddCandidate(indices[indexOffset + 1], indices[indexOffset + 2]);
					AddCandidate(indices[indexOffset + 2], indices[indexOffset + 0]);
				}
			}

			SortCandidates();			
		}

		private void AddCandidate(int index0, int index1)
		{
			Edge edge = new Edge(index0, index1);
			TesselationCandidate candidate = FindCandidate(edge);

			if (candidate != null)
				return;

			candidate = new TesselationCandidate(edge);
			candidates.Add(candidate);

			UpdateCandidate(candidate);
		}

		private void UpdateCandidate(int index0, int index1)
		{
			TesselationCandidate candidate = FindCandidate(new Edge(index0, index1));

			if (candidate != null)
				UpdateCandidate(candidate);
		}

		private void UpdateCandidate(TesselationCandidate candidate)
		{			
			for (int step = 0; step < settings.edgeOptimizeSteps; ++step)
			{
				float t = step / (float) (settings.edgeOptimizeSteps - 1);
				float error = CalculateError(candidate.edge, t);

				if (error > candidate.error)
				{
					candidate.t = t;
					candidate.error = error;
				}
			}
			
		}

		private TesselationCandidate FindCandidate(Edge edge)
		{
			foreach (TesselationCandidate candidate in candidates)
			{
				if (candidate.edge == edge)
					return candidate;
			}

			return null;
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
			color.r = Mathf.Min(color.r / ldrMultiplier, 1.0f);
			color.g = Mathf.Min(color.g / ldrMultiplier, 1.0f);
			color.b = Mathf.Min(color.b / ldrMultiplier, 1.0f);

			// When rendering in linear mode, Unity expects vertex colors in gamma space and automatically converts them to linear colors
			if (PlayerSettings.colorSpace == ColorSpace.Linear)
				color = color.gamma;

			return color;
		}

		private Color DecodeVertexColor(Color color)
		{
			if (PlayerSettings.colorSpace == ColorSpace.Linear)
				color = color.linear;
			
			color.r *= ldrMultiplier;
			color.g *= ldrMultiplier;
			color.b *= ldrMultiplier;

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

			string lightmapAssetPath = AssetDatabase.GetAssetPath(lightmapSampler.Texture);
			TextureImporter lightmapImporter = AssetImporter.GetAtPath(lightmapAssetPath) as TextureImporter;

			if (lightmapImporter.sRGBTexture)
				lightmapColor = lightmapColor.linear;

			// Decode RGBMA lightmap
			// TODO: automatically detect from player settings if this should be performed ("normal quality" lightmaps)
			//lightmapColor.r = lightmapColor.r * 8.0f * lightmapColor.a;
			//lightmapColor.g = lightmapColor.g * 8.0f * lightmapColor.a;
			//lightmapColor.b = lightmapColor.b * 8.0f * lightmapColor.a;

			return lightmapColor;
		}

		private float CalculateError(Edge edge, float t)
		{
			// Retrieve the indices for this candidate
			int i0 = edge.index0;
			int i1 = edge.index1;

			// Retrieve the lightmap UVs of this candidate
			Vector2 uv0 = meshEditor.LightmapUV[i0];
			Vector2 uv1 = meshEditor.LightmapUV[i1];

			// Retrieve the vertex colors of this candidate
			Color c0 = DecodeVertexColor(meshEditor.colors[i0]);
			Color c1 = DecodeVertexColor(meshEditor.colors[i1]);

			// Interpolate the lightmap UV at the candidate position
			Vector2 interpolatedUV = Vector2.Lerp(uv0, uv1, t);

			// Sample the lightmap to get the desired color at the candidate
			Color desiredColor = SampleLightmap(interpolatedUV);

			// Calculate the error as the difference between the interpolated color and the desired color
			float currentError = CalculateError(c0, c1, uv0, uv1, t);

			// Retrieve the vertices of this candidate
			Vector3 v0 = meshEditor.vertices[i0];
			Vector3 v1 = meshEditor.vertices[i1];

			// Transform the vertices to world space
			v0 = target.renderer.transform.TransformPoint(v0);
			v1 = target.renderer.transform.TransformPoint(v1);

			// Calculate the edge length
			// TODO: use the area of all adjacent triangles
			float length = Vector3.Distance(v0, v1);
			currentError *= length;

			return currentError;
		}

		private float CalculateError(Color c0, Color c1, Vector2 uv0, Vector2 uv1, float t)
		{
			// Interpolate the lightmap UV at the given position
			Vector2 interpolatedUV = Vector2.Lerp(uv0, uv1, t);

			// Sample the lightmap to get the desired color at the candidate
			Color desiredColor = SampleLightmap(interpolatedUV);

			// Interpolate the current vertex colors to calculate what would be rendered with the current vertex colors
			Color interpolatedColor = Color.Lerp(c0, c1, t);

			// Calculate the error as the difference between the interpolated color and the desired color
			return FloatMath.Abs(CalculateColorIntensity(desiredColor) - CalculateColorIntensity(interpolatedColor));
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

			if (a == 0.0f || b == 0.0f || c == 0.0f)
				return 0.0f;

			float s = 0.5f * (a + b + c);

			return FloatMath.Sqrt(s * (s - a) * (s - b) * (s - c));
		}

		private void Sort(ref int a, ref int b)
		{
			if (b > a)
				Swap(ref a, ref b);
		}

		private void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
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