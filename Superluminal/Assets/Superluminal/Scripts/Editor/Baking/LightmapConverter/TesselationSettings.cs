using UnityEngine;
using System;

namespace Superluminal
{
	[Serializable]
	public class TesselationSettings
	{
		public float minimumError = 0.01f;

		public bool allowRecursiveSplitting = true;

		[Range(0.0f, 1.0f)]
		public float minimumErrorFactor = 0.8f;

		[Range(1.0f, 10.0f)]
		public float maxTesselationFactor = 2.0f;
		
		public int edgeOptimizeSteps = 20;
	}
}
