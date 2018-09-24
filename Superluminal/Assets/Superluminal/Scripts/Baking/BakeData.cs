using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	[ExecuteInEditMode]
	public class BakeData : MonoBehaviour
	{
		public delegate void BakeDataEvent(BakeData data);

		public static event BakeDataEvent Loaded;
		public static event BakeDataEvent Unloaded;

		public bool applyWhenBuilding = true;

		public BakeTarget[] targets = new BakeTarget[0];

		private void OnEnable()
		{
			if (Loaded != null)
				Loaded(this);
		}

		private void OnDisable()
		{
			if (Unloaded != null)
				Unloaded(this);
		}
	}

}