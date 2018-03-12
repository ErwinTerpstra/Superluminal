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

		private bool previewEnabled;

		private Lightbaker baker;

		private void OnEnable()
		{
			Scene activeScene = EditorSceneManager.GetActiveScene();

			if (!string.IsNullOrEmpty(activeScene.name))
				baker = new Lightbaker(activeScene);

			SceneView.onSceneGUIDelegate += OnSceneGUI;

			Camera.onPostRender += OnCameraPostRender;

			EditorSceneManager.sceneOpened += OnSceneOpened;
			EditorSceneManager.newSceneCreated += OnNewSceneCreated;
		}

		private void OnDisable()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;

			Camera.onPostRender -= OnCameraPostRender;

			EditorSceneManager.sceneOpened -= OnSceneOpened;
			EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
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

			{
				GUI.enabled = baker.HasBakeData;

				EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck();

				bool shouldEnablePreview = GUILayout.Toggle(previewEnabled, "Preview baked meshes");

				if (EditorGUI.EndChangeCheck())
				{
					if (shouldEnablePreview)
						EnablePreview();
					else
						DisablePreview();

					SceneView.RepaintAll();
				}

				EditorGUILayout.EndHorizontal();

				GUI.enabled = true;
			}

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Bake"))
				Bake();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Active scene", baker.Scene.name);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			if (baker.HasBakeData)
			{
				EditorGUILayout.LabelField("Baked meshes", baker.BakeTargets.Length.ToString());
			}
			else
			{
				EditorGUILayout.LabelField("No bake data yet");
			}

			EditorGUILayout.EndHorizontal();
		}

		private void EnablePreview()
		{
			foreach (BakeTarget target in baker.BakeTargets)
				target.renderer.enabled = false;

			previewEnabled = true;
		}

		private void DisablePreview()
		{
			foreach (BakeTarget target in baker.BakeTargets)
				target.renderer.enabled = true;

			previewEnabled = false;
		}


		private void Bake()
		{
			baker.Bake();

			if (drawKDTree)
				SceneView.RepaintAll();
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

		private void DrawPreview(Camera camera)
		{
			foreach (BakeTarget target in baker.BakeTargets)
			{
				if (target.bakedMesh == null || target.renderer == null)
					continue;

				foreach (BakeTargetSubmesh submesh in target.submeshes)
				{
					//Graphics.DrawMesh(target.bakedMesh, target.renderer.transform.localToWorldMatrix, submesh.material, 0, camera, submesh.idx);

					for (int passIdx = 0; passIdx < submesh.material.passCount; ++passIdx)
					{
						submesh.material.SetPass(passIdx);
						Graphics.DrawMeshNow(target.bakedMesh, target.renderer.transform.localToWorldMatrix, submesh.idx);
					}
				}
			}
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

		private void OnCameraPostRender(Camera camera)
		{
			if (baker != null && baker.HasBakeData && previewEnabled)
				DrawPreview(camera);
		}

		private void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			if (!string.IsNullOrEmpty(scene.name))
			{
				baker = new Lightbaker(scene);

				if (baker.HasBakeData)
					DisablePreview();

				Repaint();
			}
		}

		private void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
		{
			if (mode == NewSceneMode.Single)
				baker = null;
		}


		[MenuItem("Window/Superluminal")]
		public static void OpenWindow()
		{
			EditorWindow window = EditorWindow.GetWindow(typeof(BakeWindow));
			window.titleContent = new GUIContent("Superluminal");
		}

	}
}