using System;
using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	/// <summary>
	/// A KD-tree implementation to optimize ray-triangle intersection
	/// </summary>
	public class KDTree
	{
		private int maxDepth;


		private AABB bounds;

		private List<KDTreeNode> nodes;

		public KDTree()
		{
			nodes = new List<KDTreeNode>();
			maxDepth = 25;
		}

		/// <summary>
		/// Clears the current tree structure
		/// </summary>
		public void Clear()
		{
			nodes.Clear();
		}

		/// <summary>
		/// Generates a tree with the given list of elements.
		/// </summary>
		/// <param name="elements"></param>
		public void Generate(List<Triangle> elements)
		{
			if (elements.Count == 0)
				throw new InvalidOperationException("A KD tree needs at least a single element.");

			// Calculate the collective bounds of the triangle list
			bounds = new AABB(elements[0].V0, elements[0].V0);
			
			foreach (Triangle triangle in elements)
			{
				bounds.Encapsulate(triangle.V0);
				bounds.Encapsulate(triangle.V1);
				bounds.Encapsulate(triangle.V2);
			}

			// Create the root node and recursively split it as long as neccesary
			nodes.Add(null);
			nodes[0] = CreateNode(ref bounds, elements, 0);
		}

		/// <summary>
		/// Create a new node and add it to the nodes list. This will recursively split nodes until the stopping conditions are met
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="elements"></param>
		/// <param name="depth"></param>
		private KDTreeNode CreateNode(ref AABB bounds, List<Triangle> elements, int depth)
		{
			// If we are at the maximum tree depth, this will always be a leaf node
			if (depth >= maxDepth)
				return new KDTreeNode(elements);

			/// Attempt to find the most optimal split point
			int splitAxis = depth % 3;
			float splitPoint;

			if (!FindSplitPoint(elements, splitAxis, ref bounds, out splitPoint))
				return new KDTreeNode(elements); // If the splitting algorithm deems the node unfit to be split, create a leaf node

			List<Triangle> upperElements = new List<Triangle>();
			List<Triangle> lowerElements = new List<Triangle>();

			// Relocate all elements in this leaf to the child nodes they intersect
			foreach (Triangle triangle in elements)
			{
				int side = triangle.SideOfAAPlane(splitAxis, splitPoint);

				if (side >= 0)
					upperElements.Add(triangle);

				if (side <= 0)
					lowerElements.Add(triangle);
			}

			// Create an internal node
			KDTreeNode node = new KDTreeNode(splitAxis, splitPoint, nodes.Count);

			// Reserve space for child nodes
			nodes.Add(null);
			nodes.Add(null);

			// Create the child nodes
			AABB upperBounds, lowerBounds;
			CalculateBounds(ref bounds, splitAxis, splitPoint, out upperBounds, out lowerBounds);

			nodes[node.UpperNodeIdx] = CreateNode(ref upperBounds, upperElements, depth + 1);
			nodes[node.LowerNodeIdx] = CreateNode(ref lowerBounds, lowerElements, depth + 1);

			return node;
		}

		/// <summary>
		/// Intersects the given ray with the KD tree
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="hitInfo"></param>
		/// <param name="maxDistance"></param>
		/// <returns>True if an intersection is found, false otherwise</returns>
		public bool IntersectRay(ref Ray ray, ref RaycastHit hitInfo, float maxDistance = float.MaxValue)
		{
			float tMax, tMin;
			if (!bounds.IntersectRay(ref ray, out tMin, out tMax))
				return false;
			
			if (tMin > maxDistance)
				return false;

			// Somehow, if we use std::min(maxDistance, tMax) as max distance, this sometimes misses intersection.
			// TODO: Test why this is.;
			return IntersectRay(ref ray, ref hitInfo, Math.Max(0.0f, tMin), maxDistance);
		}

		private bool IntersectRayRec(ref Ray ray, ref RaycastHit hitInfo, KDTreeNode node)
		{
			if (node.IsLeaf)
			{
				bool hit = false;
				
				foreach (Triangle element in node.Elements)
				{
					// Perform the ray-triangle intersection
					if (element.IntersectRay(ref ray, ref hitInfo, hitInfo.distance))
					{
						hitInfo.element = element;
						hit = true;
					}
				}

				return hit;
			}
			else
			{
				return IntersectRayRec(ref ray, ref hitInfo, nodes[node.UpperNodeIdx]) || IntersectRayRec(ref ray, ref hitInfo, nodes[node.LowerNodeIdx]);
			}
		}

		private bool IntersectRay(ref Ray ray, ref RaycastHit hitInfo, float tMin, float tMax)
		{
			// Setup a traversal stack and add the root node
			KDTraversalStack stack = new KDTraversalStack(maxDepth * 2);
			stack.Push(nodes[0], tMin, tMax);

			while (!stack.IsEmpty)
			{
				KDStackNode stackNode = stack.Pop();
				KDTreeNode node = stackNode.node;

				tMin = stackNode.tMin;
				tMax = stackNode.tMax;
				
				while (!node.IsLeaf)
				{
					int axis = node.SplitAxis;
					float splitPoint = node.SplitPoint;

					KDTreeNode nearNode;
					KDTreeNode farNode;

					// Check which node is the nearest to the ray
					if (ray.Origin[axis] < splitPoint)
					{
						farNode = nodes[node.UpperNodeIdx];
						nearNode = nodes[node.LowerNodeIdx];
					}
					else
					{
						nearNode = nodes[node.UpperNodeIdx];
						farNode = nodes[node.LowerNodeIdx];
					}

					if (ray.Direction[axis] != 0.0f)
					{
						float tSplit = (splitPoint - ray.Origin[axis]) * ray.InvDirection[axis];

						if (tSplit >= tMax || tSplit < 0)
							node = nearNode; // Node leaves the bounds before entering far node or will never enter it
						else if (tSplit <= tMin)
							node = farNode; // Node only enters the bounds when it is in the far node
						else
						{
							// The ray will enter both child node. Store the far node and continue with the near node first
							stack.Push(farNode, tSplit, tMax);

							node = nearNode;
							tMax = tSplit;
						}

						stack.Push(farNode, tSplit, tMax);

						node = nearNode;
						tMax = tSplit;
					}
					else
					{
						node = nearNode;
					}
				}

				// The current node is a leaf node, this means we can check its contents
				float closestDistance = tMax;
				bool hit = false;

				foreach (Triangle element in node.Elements)
				{
					// Perform the ray-triangle intersection
					if (element.IntersectRay(ref ray, ref hitInfo, closestDistance))
					{
						hitInfo.element = element;

						closestDistance = hitInfo.distance;
						hit = true;
					}
				}

				if (hit)
					return true;
			}

			return false;
		}

		public void GetChildNodes(KDTreeNode node, out KDTreeNode upper, out KDTreeNode lower)
		{
			upper = nodes[node.UpperNodeIdx];
			lower = nodes[node.LowerNodeIdx];
		}

		public KDTreeNode RootNode
		{
			get { return nodes.Count > 0 ? nodes[0] : null; }
		}

		public AABB Bounds
		{
			get { return bounds; }
		}


		/// <summary>
		/// Calculate the two separate bounds when splitting the bounds at the given point on the given plane
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="splitAxis"></param>
		/// <param name="splitPoint"></param>
		/// <param name="upper"></param>
		/// <param name="lower"></param>
		public static void CalculateBounds(ref AABB bounds, int splitAxis, float splitPoint, out AABB upper, out AABB lower)
		{
			Vector3 minLower = bounds.min;
			Vector3 maxUpper = bounds.max;

			Vector3 maxLower = maxUpper;
			Vector3 minUpper = minLower;

			maxLower[splitAxis] = splitPoint;
			minUpper[splitAxis] = splitPoint;

			lower = new AABB(minLower, maxLower);
			upper = new AABB(minUpper, maxUpper);
		}

		public static bool FindSplitPoint(List<Triangle> elements, int axis, ref AABB bounds, out float splitPoint)
		{
			if (elements.Count < 20)
			{
				splitPoint = 0.0f;
				return false;
			}

			splitPoint = (bounds.min[axis] + bounds.max[axis]) * 0.5f;

			return true;
		}
	}

}