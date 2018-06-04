using UnityEngine;
using System.Collections;

namespace Superluminal
{
	public struct TesselationCandidate
	{
		public int submeshIndex;

		public int indexOffset;

		public Vector3 barycentricCoords;

		public float error;
	}
}
