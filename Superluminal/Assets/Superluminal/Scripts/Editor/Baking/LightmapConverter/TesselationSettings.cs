using UnityEngine;
using System;

namespace Superluminal
{
	[Serializable]
	public class TesselationSettings
	{
		public float minimumError = 0.0f;

		public float maxTesselationFactor = 5.0f;

		public int edgeOptimizeSteps = 20;
	}
}
