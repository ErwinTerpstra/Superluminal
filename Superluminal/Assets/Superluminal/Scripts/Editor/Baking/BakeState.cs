using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	public class BakeState
	{
		public BakeStep step;

		public int bakedMeshes;

		public int totalMeshes;

		public BakeState()
		{
			step = BakeStep.INITIALIZING;
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
