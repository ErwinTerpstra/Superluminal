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
			min.x = FloatMath.Min(min.x, point.x);
			min.y = FloatMath.Min(min.y, point.y);
			min.z = FloatMath.Min(min.z, point.z);

			max.x = FloatMath.Max(max.x, point.x);
			max.y = FloatMath.Max(max.y, point.y);
			max.z = FloatMath.Max(max.z, point.z);
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
			tMin = -float.MaxValue;
			tMax = float.MaxValue;

			for (int axis = 0; axis < 3; ++axis)
			{
				if (ray.Direction[axis] == 0.0f)
					continue;

				float cMin = (min[axis] - ray.Origin[axis]) * ray.InvDirection[axis];
				float cMax = (max[axis] - ray.Origin[axis]) * ray.InvDirection[axis];
				
				tMin = FloatMath.Max(tMin, FloatMath.Min(cMin, cMax));
				tMax = FloatMath.Min(tMax, FloatMath.Max(cMin, cMax));
			}

			return tMax >= FloatMath.Max(tMin, 0.0f);
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