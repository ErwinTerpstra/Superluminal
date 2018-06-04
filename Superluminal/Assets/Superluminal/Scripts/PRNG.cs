using System.Collections.Generic;

using UnityEngine;

using DateTime = System.DateTime;

namespace Superluminal
{
	public class PRNG
	{
		private uint state;

		public PRNG(uint seed)
		{
			state = seed;
		}

		public PRNG()
		{
			state = SeedFromTime();
		}

		public unsafe uint Next()
		{
			uint x = state;
			x ^= x << 13;
			x ^= x >> 17;
			x ^= x << 15;
			state = x;

			return x;
		}

		public float NextFloat()
		{
			return Next() / (float) uint.MaxValue;
		}

		public Vector2 NextVector2()
		{
			return new Vector2(NextFloat(), NextFloat());
		}

		public static unsafe uint SeedFromTime()
		{
			return (uint) DateTime.Now.Ticks;
		}

	}
}