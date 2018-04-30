using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	[System.Serializable]
	public class BakeTarget
	{
		public string guid;

		public MeshRenderer renderer;

		public Mesh originalMesh;

		public Mesh bakedMesh;

		public List<BakeTargetSubmesh> submeshes;
	}

	[System.Serializable]
	public class BakeTargetSubmesh
	{ 
		public int idx;

		public Material material;

	}
}
