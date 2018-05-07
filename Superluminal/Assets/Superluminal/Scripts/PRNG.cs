using System;
using System.Collections.Generic;

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

		public static unsafe uint SeedFromTime()
		{
			return (uint) DateTime.Now.Ticks;
		}

	}
}