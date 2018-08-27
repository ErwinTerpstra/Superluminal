using UnityEngine;
using System;

namespace Superluminal
{
	[Serializable]
	public class TesselationSettings
	{
		public float minimumError = 0.05f;

		public bool allowRecursiveSplitting = true;

		[Range(0.0f, 1.0f)]
		public float minimumErrorFactor = 0.8f;

		public float maxVertexDensity = 10.0f;
		
		public int edgeOptimizeSteps = 20;
	}
}
