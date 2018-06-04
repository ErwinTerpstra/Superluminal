using UnityEngine;
using System;

namespace Superluminal
{
	[Serializable]
	public class RaytracingSettings
	{
		public int bounces = 2;

		public int indirectSamples = 10;
	}
}
