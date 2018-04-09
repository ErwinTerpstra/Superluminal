using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class BakeSettings : ScriptableObject
	{
		public StaticEditorFlags bakeFlags = StaticEditorFlags.BatchingStatic;

		public StaticEditorFlags occluderFlags = StaticEditorFlags.OccluderStatic;
		
		public int bounces = 2;

		public int indirectSamples = 10;
	}

}