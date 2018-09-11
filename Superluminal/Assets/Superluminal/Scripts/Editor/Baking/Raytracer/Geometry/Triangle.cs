using System;
using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	/// <summary>
	/// Representation of a single triangle
	/// </summary>
	public class Triangle
	{
		private Vector3 v0, v1, v2;

		private Vector3 v01, v02;

		private float dot00, dot01, dot11;

		private float invDenom;

		private Vector3 normal;

		public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
		{
			this.v0 = v0;
			this.v1 = v1;
			this.v2 = v2;

			normal = CalculateNormal();

			PreCalculateBarycentric();
		}

		private Vector3 CalculateNormal()
		{
			// Calculate normal as a vector perpendicular to ab and ac
			Vector3 ab = v1 - v0;
			Vector3 ac = v2 - v0;

			ab.Normalize();
			ac.Normalize();

			normal = Vector3.Cross(ac, ab);
			normal.Normalize();

			return normal;
		}

		private void PreCalculateBarycentric()
		{
			// Compute vectors from A -> C and A -> B
			v01 = v1 - v0;
			v02 = v2 - v0;

			dot00 = Vector3.Dot(v01, v01);
			dot01 = Vector3.Dot(v01, v02);
			dot11 = Vector3.Dot(v02, v02);

			float denom = dot00 * dot11 - dot01 * dot01;

			if (denom < 1e-15f)
				invDenom = 1.0f;
			else
				invDenom = 1.0f / denom;
		}

		public bool IntersectRay(ref Ray ray, ref RaycastHit hitInfo, float maxDistance)
		{
			float dot = Vector3.Dot(normal, ray.Direction);

			// Ray is parallel or hits the triangle plane from behind
			if (dot >= 0.0f)
				return false;

			float distanceToPlane = Vector3.Dot(normal, ray.Origin - v0);

			// Intersection point is behind the ray
			if (distanceToPlane < 0.0f)
				return false;

			float t = distanceToPlane / -dot;

			if (t > maxDistance)
				return false;

			Vector3 pointOnPlane = ray.Origin + t * ray.Direction;

			// Calculate barycentric coords to check if the point is within triangle boundaries
			Vector3 v0p = pointOnPlane - v0;

			// Compute dot products
			float dot02 = Vector3.Dot(v01, v0p);
			float dot12 = Vector3.Dot(v02, v0p);

			// Compute barycentric coordinates
			float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
			float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

			if (u < 0.0f || v < 0.0f || u + v > 1.0f)
				return false;

			// Fill the hit info struct with gathered data
			hitInfo.position = pointOnPlane;
			hitInfo.distance = t;
			hitInfo.barycentricCoords = new Vector3(1.0f - u - v, u, v);

			return true;
		}

		/// <summary>
		/// Check which side of the axis-aligned plane this triangle resides.
		/// </summary>
		/// <param name="axis">The axis of the plane to check, 0 for the X axis, 1 for Y and 2 for Z</param>
		/// <param name="position"></param>
		/// <returns>1 if the triangle is on the positive side, -1 if it's on the negative side. 0 if it intersects the plane.</returns>
		public int SideOfAAPlane(int axis, float position)
		{
			int a = FastMath.Sign(v0[axis] - position);
			int b = FastMath.Sign(v1[axis] - position);
			int c = FastMath.Sign(v2[axis] - position);

			// If all vertices are on the same side, return that side
			// Otherwise the triangle intersects the plane
			return (a == b && a == c) ? a : 0;
		}

		public Vector3 V0
		{
			get { return v0; }
		}

		public Vector3 V1
		{
			get { return v1; }
		}

		public Vector3 V2
		{
			get { return v2; }
		}

		public Vector3 Normal
		{
			get { return normal; }
		}
	}

}