using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	public struct Ray
	{
		private Vector3 origin;

		private Vector3 direction;

		private Vector3 invDirection;

		public Ray(Vector3 origin, Vector3 direction)
		{
			this.origin = origin;
			this.direction = direction;

			invDirection = new Vector3(1.0f / direction.x, 1.0f / direction.y, 1.0f / direction.z);
		}

		public Vector3 Origin
		{
			get { return origin; }
		}

		public Vector3 Direction
		{
			get { return direction; }
		}

		public Vector3 InvDirection
		{
			get { return invDirection; }
		}
	}
}
