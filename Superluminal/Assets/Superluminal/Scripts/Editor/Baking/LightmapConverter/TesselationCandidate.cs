using UnityEngine;
using System.Collections;

namespace Superluminal
{

	public class TesselationCandidate
	{
		public readonly Edge edge;

		public float t;

		public float error;

		public int vertexIndex;

		public TesselationCandidate(Edge edge)
		{
			this.edge = edge;

			vertexIndex = -1;
		}

		public bool IsSplit
		{
			get { return vertexIndex >= 0; }
		}
	}
}
