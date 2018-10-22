using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	public abstract class BakeBackend
	{
		protected BakeSettings settings;

		public BakeBackend(BakeSettings settings)
		{
			this.settings = settings;
		}
		
		protected void CopyIndices(Mesh src, Mesh dst)
		{
			dst.subMeshCount = src.subMeshCount;

			// Copy submesh indices to the new mesh
			List<int> indices = new List<int>();
			for (int submeshIdx = 0; submeshIdx < src.subMeshCount; ++submeshIdx)
			{
				indices.Clear();
				src.GetIndices(indices, submeshIdx);
				dst.SetTriangles(indices, submeshIdx);
			}
		}

		public abstract IEnumerator<BakeCommand> Bake(BakeTarget target);
	}
}
