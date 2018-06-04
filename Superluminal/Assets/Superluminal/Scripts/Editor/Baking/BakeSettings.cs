using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class BakeSettings : ScriptableObject
	{
		public BakeBackendType backendType = BakeBackendType.LIGHTMAP_CONVERTER;

		public StaticEditorFlags bakeFlags = StaticEditorFlags.BatchingStatic;

		public StaticEditorFlags occluderFlags = StaticEditorFlags.OccluderStatic;

		public bool includeDisabledObjects = true;

		public RaytracingSettings raytracingSettings = null;

		public TesselationSettings tesselationSettings = null;
	}

}