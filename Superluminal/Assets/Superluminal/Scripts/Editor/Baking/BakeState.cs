using System;
using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	public class BakeState
	{
		public BakeStep step;

		public int bakedMeshes;

		public int totalMeshes;

		public long rays;

		public DateTime? bakingStart;

		public DateTime? bakingEnd;

		public BakeState()
		{
			step = BakeStep.INITIALIZING;
		}

		public TimeSpan Duration
		{
			get { return bakingEnd.Value - bakingStart.Value; }
		}
	}

	public enum BakeStep
	{
		INITIALIZING,
		PREPARING_SCENE,
		BAKING,
		STORING_BAKE_DATA,
		FINISHED,
		CANCELLED
	}
}
