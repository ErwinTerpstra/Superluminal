using UnityEngine;

using System;

namespace Superluminal
{
	public struct Edge : IEquatable<Edge>
	{
		public readonly int index0, index1;

		public Edge(int index0, int index1)
		{
			this.index0 = index0;
			this.index1 = index1;
		}

		public override int GetHashCode()
		{
			return index0 << 13 + index1;
		}

		public override bool Equals(object obj)
		{
			if (obj is Edge)
				return Equals((Edge)obj);

			return base.Equals(obj);
		}

		public bool Equals(Edge other)
		{
			return (other.index0 == index0 && other.index1 == index1) || (other.index0 == index1 && other.index1 == index0);
		}

		public static bool operator==(Edge a, Edge b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Edge a, Edge b)
		{
			return !(a == b);
		}
	}

}