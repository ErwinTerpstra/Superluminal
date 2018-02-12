using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	[System.Serializable]
	public class MeshBinding
	{
		public MeshRenderer renderer;

		public Mesh originalMesh;

		public Mesh bakedMesh;

		public List<Submesh> submeshes;
	}

	[System.Serializable]
	public class Submesh
	{ 
		public int idx;

		public Material material;

	}
}
