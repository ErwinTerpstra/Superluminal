using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Callbacks;

namespace Superluminal
{
	public static class ScenePostProcessor
	{

		[PostProcessSceneAttribute(2)]
		public static void OnPostprocessScene()
		{
			BakeData bakeData = Object.FindObjectOfType<BakeData>();

			if (bakeData == null || !bakeData.applyWhenBuilding)
				return;

			HashSet<GameObject> rootObjects = new HashSet<GameObject>();

			foreach (BakeTarget target in bakeData.targets)
			{
				if (target.bakedMesh == null || target.renderer == null)
					continue;

				// Apply the mesh
				MeshFilter meshFilter = target.renderer.GetComponent<MeshFilter>();
				meshFilter.sharedMesh = target.bakedMesh;
				
				// Apply the materials
				Material[] materials = target.renderer.sharedMaterials;
				foreach (BakeTargetSubmesh submesh in target.submeshes)
					materials[submesh.idx] = submesh.bakedMaterial;

				target.renderer.sharedMaterials = materials;

				// Make sure the renderer is enabled
				target.renderer.enabled = true;
				
				rootObjects.Add(target.renderer.transform.root.gameObject);
			}

			// Apply static batching on all the root objects
			// TODO: check static flags? 
			foreach (GameObject root in rootObjects)
				StaticBatchingUtility.Combine(root);
			
		}
	}

}
