using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	[System.Serializable]
	public class Submesh
	{
		public Mesh mesh;

		public int submeshIdx;

		public Transform transform;

		public Material material;
	}
}