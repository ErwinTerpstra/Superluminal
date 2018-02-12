using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Superluminal
{ 
	public struct KDStackNode
	{
		public KDTreeNode node;
		public float tMin, tMax;
	}

	public struct KDTraversalStack
	{
		private KDStackNode[] stack;

		private int count;
		
		public KDTraversalStack(int maxDepth)
		{
			stack = new KDStackNode[maxDepth];
			count = 0;
		}

		public void Push(KDTreeNode node, float tMin, float tMax)
		{
			stack[count] = new KDStackNode()
			{
				node = node,
				tMin = tMin,
				tMax = tMax
			};
	
			++count;
		}

		public KDStackNode Pop()
		{
			--count;

			return stack[count];
		}

		public bool IsEmpty
		{
			get { return count == 0; }
		}
	}
}