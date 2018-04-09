using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	public struct RaycastHit
	{
		public Vector3 position;
		
		public float distance;

		public Vector3 barycentricCoords;

		public Triangle element;
	}
}