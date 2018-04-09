using System;
using System.Collections.Generic;
using UnityEngine;

namespace Superluminal
{
	/// <summary>
	/// A single node in a KD tree, can either be a leaf or an internal node
	/// </summary>
	public class KDTreeNode
	{
		private int splitAxis;

		private float splitPoint;

		private int upperNodeIdx;

		private List<Triangle> elements;

		/// <summary>
		/// Create a new internal node
		/// </summary>
		/// <param name="splitAxis"></param>
		/// <param name="splitPoint"></param>
		/// <param name="upperNodeIdx"></param>
		public KDTreeNode(int splitAxis, float splitPoint, int upperNodeIdx)
		{
			this.splitAxis = splitAxis;
			this.splitPoint = splitPoint;
			this.upperNodeIdx = upperNodeIdx;
		}

		/// <summary>
		/// Create a new leaf node
		/// </summary>
		/// <param name="elements"></param>
		public KDTreeNode(List<Triangle> elements)
		{
			this.elements = elements;
		}

		public bool IsLeaf
		{
			get { return elements != null; }
		}

		public int SplitAxis
		{
			get { return splitAxis; }
		}

		public float SplitPoint
		{
			get { return splitPoint; }
		}

		public int UpperNodeIdx
		{
			get { return upperNodeIdx; }
		}

		public int LowerNodeIdx
		{
			get { return upperNodeIdx + 1; }
		}

		public List<Triangle> Elements
		{
			get { return elements; }
		}

	}
}