using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class BakeWindow : EditorWindow
	{
		private Lightbaker baker;

		private void OnEnable()
		{
			baker = new Lightbaker();

			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Setup"))
				Setup();

			if (GUILayout.Button("Bake"))
				Bake();

			EditorGUILayout.EndHorizontal();
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			KDTree tree = baker.Scene.Tree;

		}

		private void DrawKDTreeNode(KDTreeNode node, AABB bounds)
		{

		}

		private void Setup()
		{
			baker.SetupScene();
		}

		private void Bake()
		{
		}

		[MenuItem("Window/Superluminal")]
		public static void OpenWindow()
		{
			EditorWindow.GetWindow(typeof(BakeWindow));
		}

	}
}