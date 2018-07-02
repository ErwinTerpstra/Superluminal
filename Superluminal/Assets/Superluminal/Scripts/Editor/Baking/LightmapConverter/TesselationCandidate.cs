using UnityEngine;
using System.Collections;

namespace Superluminal
{

	public class TesselationCandidate
	{
		public readonly Edge edge;

		public float t;

		public float error;

		public TesselationCandidate(Edge edge)
		{
			this.edge = edge;
		}
	}
}
