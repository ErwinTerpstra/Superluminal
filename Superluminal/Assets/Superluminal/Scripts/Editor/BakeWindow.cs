using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class BakeWindow : EditorWindow
	{
		private bool drawKDTree;

		private Lightbaker baker;

		private void OnEnable()
		{
			baker = new Lightbaker();

			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();
			drawKDTree = GUILayout.Toggle(drawKDTree, "Draw KD-tree");

			if (EditorGUI.EndChangeCheck())
				SceneView.RepaintAll();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();
			bool previewEnabled = GUILayout.Toggle(baker.PreviewEnabled, "Preview baked objects");

			if (EditorGUI.EndChangeCheck())
			{
				if (previewEnabled)
					baker.EnablePreview();
				else
					baker.DisablePreview();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Bake"))
				Bake();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			if (baker.HasBakeData)
			{
				EditorGUILayout.LabelField("Baked meshes", baker.BakeTargetCount.ToString());
			}
			else
			{
				EditorGUILayout.LabelField("No bake data yet");
			}

			EditorGUILayout.EndHorizontal();
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			if (drawKDTree)
			{
				KDTree tree = baker.Context.Tree;

				if (tree.RootNode != null)
					DrawKDTreeNode(tree, tree.RootNode, tree.Bounds);
			}
		}

		private void DrawKDTreeNode(KDTree tree, KDTreeNode node, AABB bounds)
		{
			Handles.DrawWireCube(bounds.Center, bounds.Size);

			if (!node.IsLeaf)
			{
				AABB upperBounds, lowerBounds;
				KDTree.CalculateBounds(ref bounds, node.SplitAxis, node.SplitPoint, out upperBounds, out lowerBounds);

				KDTreeNode upperNode, lowerNode;
				tree.GetChildNodes(node, out upperNode, out lowerNode);

				DrawKDTreeNode(tree, upperNode, upperBounds);
				DrawKDTreeNode(tree, lowerNode, lowerBounds);
			}
		}
	

		private void Bake()
		{
			baker.Bake();

			if (drawKDTree)
				SceneView.RepaintAll();
		}

		[MenuItem("Window/Superluminal")]
		public static void OpenWindow()
		{
			EditorWindow.GetWindow(typeof(BakeWindow));
		}

	}
}