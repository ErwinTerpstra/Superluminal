using System;
using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	/// <summary>
	/// An axis-aligned bounding box structure, kept by the minimum and maximum coordinates that fit inside the AABB
	/// </summary>
	public struct AABB
	{
		public Vector3 min;
		public Vector3 max;

		public AABB(Vector3 min, Vector3 max)
		{
			this.min = min;
			this.max = max;
		}

		/// <summary>
		/// Grows the AABB to encapsulate the given point
		/// </summary>
		/// <param name="point"></param>
		public void Encapsulate(Vector3 point)
		{
			min.x = Math.Min(min.x, point.x);
			min.y = Math.Min(min.y, point.y);
			min.z = Math.Min(min.z, point.z);

			max.x = Math.Max(max.x, point.x);
			max.y = Math.Max(max.y, point.y);
			max.z = Math.Max(max.z, point.z);
		}

		/// <summary>
		/// Intersects the given ray with this AABB. Returns the minimum and maximum time at which it intersects
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="tMin"></param>
		/// <param name="tMax"></param>
		/// <returns></returns>
		public bool IntersectRay(ref Ray ray, out float tMin, out float tMax)
		{
			// Find the shortest intersection between the ray and an axis of the box
			float cMin = (min.x - ray.Origin.x) * ray.InvDirection.x;
			float cMax = (max.x - ray.Origin.x) * ray.InvDirection.x;

			tMin = Math.Min(cMin, cMax);
			tMax = Math.Max(cMin, cMax);

			for (int axis = 1; axis < 3; ++axis)
			{
				cMin = (min[axis] - ray.Origin[axis]) * ray.InvDirection[axis];
				cMax = (max[axis] - ray.Origin[axis]) * ray.InvDirection[axis];
				
				tMin = Math.Max(tMin, Math.Min(cMin, cMax));
				tMax = Math.Min(tMax, Math.Max(cMin, cMax));
			}

			return tMax >= Math.Max(tMin, 0.0f);
		}

		public Vector3 Center
		{
			get { return (min + max) * 0.5f; }
		}

		public Vector3 Size
		{
			get { return (max - min); }
		}
	}

}