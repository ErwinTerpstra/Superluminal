using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

using DateTime = System.DateTime;

namespace Superluminal
{
	/// <summary>
	/// The main lightbaking class. Will collect meshes from the scene and store newly generated meshes
	/// </summary>
	public class Lightbaker
	{
		
		private BakeBackend backend;

		private Dictionary<string, BakeTarget> targets;
		
		private BakedAssetRepository bakedAssetRepository;

		private Scene scene;

		private BakeData bakeData;

		private BakeSettings settings;

		private BakeState state;

		private Shader shader;

		public Lightbaker(Scene scene, BakeData bakeData, BakeSettings settings)
		{
			this.scene = scene;
			this.bakeData = bakeData;
			this.settings = settings;

			shader = Shader.Find("Superluminal/VertexBaked");

			targets = new Dictionary<string, BakeTarget>();
		}

		public void Destroy()
		{

		}

		public IEnumerator<BakeCommand> BakeRoutine()
		{
			state = new BakeState();
			state.totalMeshes = targets.Count;

			state.step = BakeStep.PREPARING_SCENE;
			yield return null;

			PreBake();

			bakedAssetRepository = BakedAssetRepository.Create(scene);

			state.step = BakeStep.BAKING;
			yield return null;

			state.bakingStart = DateTime.Now;
			
			Dictionary<Material, Material> bakedMaterialMap = new Dictionary<Material, Material>();

			// Iterate through all bake targets
			foreach (KeyValuePair<string, BakeTarget> pair in targets)
			{
				// Assign baked materials for each submesh
				foreach (BakeTargetSubmesh submesh in pair.Value.submeshes)
				{
					Material bakedMaterial;

					if (!bakedMaterialMap.TryGetValue(submesh.originalMaterial, out bakedMaterial))
					{
						bakedMaterial = new Material(submesh.originalMaterial);
						bakedMaterial.shader = shader;

						bakedMaterialMap.Add(submesh.originalMaterial, bakedMaterial);
					}

					submesh.bakedMaterial = bakedMaterial;
				}

				// Bake the new mesh
				var bakeEnumerator = backend.Bake(pair.Value);
				while (bakeEnumerator.MoveNext())
					yield return bakeEnumerator.Current;

				// Store the new baked mesh
				bakedAssetRepository.StoreMesh(pair.Value.bakedMesh, pair.Value.guid);

				++state.bakedMeshes;

				yield return null;
			}

			// Store all materials
			foreach (var pair in bakedMaterialMap)
				bakedAssetRepository.StoreMaterial(pair.Value, GUID.Generate().ToString());

			state.bakingEnd = DateTime.Now;

			state.step = BakeStep.STORING_BAKE_DATA;
			yield return null;

			StoreBakeData();

			state.step = BakeStep.FINISHED;
		}

		public void CancelBake()
		{
			if (state != null)
				state.step = BakeStep.CANCELLED;
		}

		private void PreBake()
		{
			switch (settings.backendType)
			{
				case BakeBackendType.RAYTRACER:
					backend = new RaytracerBackend(settings);
					break;

				case BakeBackendType.LIGHTMAP_CONVERTER:
					backend = new LightmapConverterBackend(settings);
					break;
			}
			
			CollectTargets();
		}

		private void StoreBakeData()
		{
			// Store the list of baked meshes in the bake data
			bakeData.targets = targets.Values.ToArray();

			// Write the meshes to disk
			bakedAssetRepository.Flush();

			// Make sure the scene will be saved
			EditorSceneManager.MarkSceneDirty(scene);
		}
		
		private void CollectTargets()
		{
			targets.Clear();

			List<BakeTargetSubmesh> targetSubmeshes = new List<BakeTargetSubmesh>();

			MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();

			foreach (MeshRenderer renderer in renderers)
			{
				if (!settings.includeDisabledObjects && !renderer.gameObject.activeInHierarchy)
					continue;

				// Retrieve the linked mesh
				MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

				if (meshFilter == null)
					continue;

				Mesh mesh = meshFilter.sharedMesh;

				if (mesh == null)
					continue;

				StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
				Material[] materials = renderer.sharedMaterials;

				targetSubmeshes.Clear();

				// Iterate through all submeshes in the meshss
				for (int submeshIdx = 0; submeshIdx < mesh.subMeshCount; ++submeshIdx)
				{
					Material material = materials[submeshIdx % materials.Length];

					// Check if this is a mesh that should be baked
					if ((staticFlags & settings.bakeFlags) != 0)/// && material.shader.name == "Superluminal/VertexBaked")
					{
						// Create a submesh for the binding
						targetSubmeshes.Add(new BakeTargetSubmesh()
						{
							idx = submeshIdx,
							originalMaterial = material,
						});
					}
					
				}

				if (targetSubmeshes.Count > 0)
				{
					BakeTarget target = new BakeTarget()
					{
						guid = GUID.Generate().ToString(),
						renderer = renderer,
						originalMesh = mesh,
						bakedMesh = null,

						submeshes = targetSubmeshes.ToArray(),
					};

					targets.Add(target.guid, target);
				}
			}
		}

		public Scene Scene
		{
			get { return scene; }
		}
		
		public bool IsBaking
		{
			get { return state != null && state.step != BakeStep.FINISHED && state.step != BakeStep.CANCELLED; }
		}

		public BakeBackend Backend
		{
			get { return backend; }
		}

		public BakeState State
		{
			get { return state; }
		}

		public bool HasBakeData
		{
			get { return bakeData != null && bakeData.targets.Length > 0; }
		}

		public BakeTarget[] BakeTargets
		{
			get { return bakeData.targets; }
		}
	}
}
