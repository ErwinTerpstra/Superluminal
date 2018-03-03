using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Superluminal
{
	public class BakeWindow : EditorWindow
	{
		private bool drawKDTree;

		private Lightbaker baker;

		private void OnEnable()
		{
			Scene activeScene = EditorSceneManager.GetActiveScene();

			if (!string.IsNullOrEmpty(activeScene.name))
				baker = new Lightbaker(activeScene);

			SceneView.onSceneGUIDelegate += OnSceneGUI;

			EditorSceneManager.newSceneCreated += OnNewSceneCreated;
			EditorSceneManager.sceneOpened += OnSceneOpened;
			EditorSceneManager.sceneClosed += OnSceneClosed;
		}

		private void OnDisable()
		{
			EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
			EditorSceneManager.sceneOpened -= OnSceneOpened;
			EditorSceneManager.sceneClosed -= OnSceneClosed;
		}

		private void OnGUI()
		{
			if (baker == null)
			{
				EditorGUILayout.LabelField("Lightbaking can only be performed in saved scenes.");
				return;
			}

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


		private void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
		{
			if (baker != null)
			{
				baker.Destroy();
				baker = null;

				Repaint();
			}
		}

		private void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			if (!string.IsNullOrEmpty(scene.name))
			{
				baker = new Lightbaker(scene);
				Repaint();
			}
		}

		private void OnSceneClosed(Scene scene)
		{
			if (baker != null && scene == baker.Scene)
			{
				baker.Destroy();
				baker = null;

				Repaint();
			}
		}

		[MenuItem("Window/Superluminal")]
		public static void OpenWindow()
		{
			EditorWindow window = EditorWindow.GetWindow(typeof(BakeWindow));
			window.titleContent = new GUIContent("Superluminal");
		}

	}
}