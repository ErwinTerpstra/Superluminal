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
		private enum PreviewMode
		{
			SHADED,
			WIREFRAME,
			SHADED_WIREFRAME,
		}

		[SerializeField]
		private bool drawKDTree;

		[SerializeField]
		private bool previewEnabled;

		[SerializeField]
		private PreviewMode previewMode;

		[SerializeField]
		private BakeSettings bakeSettings;

		private Lightbaker baker;

		private BakeDispatcher dispatcher;

		private WireframeRenderer wireframeRenderer;

		private bool showSettings;

		private void OnEnable()
		{
			bakeSettings = CreateInstance<BakeSettings>();

			dispatcher = new BakeDispatcher();

			Scene activeScene = EditorSceneManager.GetActiveScene();
			BakeData bakeData = FindObjectOfType<BakeData>();

			wireframeRenderer = new WireframeRenderer();

			if (bakeData != null && !string.IsNullOrEmpty(activeScene.name))
				baker = new Lightbaker(activeScene, bakeData, bakeSettings);

			BakeData.Loaded += OnBakeDataLoaded;
			BakeData.Unloaded += OnBakeDataUnloaded;

			SceneView.onSceneGUIDelegate += OnSceneGUI;

			Camera.onPostRender += OnCameraPostRender;

			EditorApplication.update += Update;

#if UNITY_2017_1_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
		}

		private void OnDisable()
		{
			BakeData.Loaded -= OnBakeDataLoaded;
			BakeData.Unloaded -= OnBakeDataUnloaded;

			SceneView.onSceneGUIDelegate -= OnSceneGUI;

			Camera.onPostRender -= OnCameraPostRender;

			EditorApplication.update -= Update;

#if UNITY_2017_1_OR_NEWER
			EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif

			wireframeRenderer.Dispose();

			DestroyImmediate(bakeSettings);
			bakeSettings = null;
		}

		private void Update()
		{
			if (dispatcher.UpdateForeground())
			{
				SceneView.RepaintAll();
				Repaint();
			}
		}

		private void OnGUI()
		{
			bool wasEnabled;

			if (baker == null)
			{
				Scene activeScene = EditorSceneManager.GetActiveScene();
				if (!string.IsNullOrEmpty(activeScene.name))
				{
					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField("Baking is not setup for this scene");

					if (GUILayout.Button("Setup"))
						Setup();

					EditorGUILayout.EndHorizontal();
				}
				else
					EditorGUILayout.LabelField("Baking is only supported for saved scenes.");

				return;
			}

			if (EditorApplication.isPlayingOrWillChangePlaymode || baker.IsBaking)
				GUI.enabled = false;

			// Draw KD-tree
			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();
			drawKDTree = GUILayout.Toggle(drawKDTree, "Draw KD-tree");

			if (EditorGUI.EndChangeCheck())
				SceneView.RepaintAll();

			EditorGUILayout.EndHorizontal();
			
			// Preview
			{
				wasEnabled = GUI.enabled;
				GUI.enabled &= baker.HasBakeData;

				EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck();

				bool shouldEnablePreview = GUILayout.Toggle(previewEnabled, "Preview baked meshes");
				
				if (EditorGUI.EndChangeCheck())
				{
					if (shouldEnablePreview)
						EnablePreview();
					else
						DisablePreview();

					UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
				}

				EditorGUILayout.EndHorizontal();
				
				if (previewEnabled)
				{
					EditorGUILayout.BeginHorizontal();

					EditorGUI.BeginChangeCheck();

					previewMode = (PreviewMode) EditorGUILayout.EnumPopup("Preview mode", previewMode);

					if (EditorGUI.EndChangeCheck())
						UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

					EditorGUILayout.EndHorizontal();
				}

				GUI.enabled = wasEnabled;
			}

			// Settings
			{
				showSettings = EditorGUILayout.Foldout(showSettings, "Settings");

				if (showSettings && bakeSettings != null)
				{
					++EditorGUI.indentLevel;
					
					Editor editor = Editor.CreateEditor(bakeSettings);
					editor.DrawDefaultInspector();

					--EditorGUI.indentLevel;
				}
			}

			// Progress
			BakeState state = baker.State;
			if (state != null)
			{
				{
					EditorGUILayout.BeginHorizontal();

					Rect rect = EditorGUILayout.GetControlRect();

					float progress = 0.0f;
					string label = state.step.ToString();

					switch (state.step)
					{
						case BakeStep.BAKING:
							progress = state.bakedMeshes / (float)state.totalMeshes;
							label += string.Format(" {0}/{1}", state.bakedMeshes, state.totalMeshes);
							break;

						case BakeStep.STORING_BAKE_DATA:
							progress = 1.0f;
							break;

						case BakeStep.CANCELLED:
						case BakeStep.FINISHED:
							progress = 1.0f;
							break;
					}

					EditorGUI.ProgressBar(rect, progress, label);

					EditorGUILayout.EndHorizontal();
				}

				if (baker.Backend is RaytracerBackend)
				{
					RaytracerContext context = (baker.Backend as RaytracerBackend).Context;

					double raysPerSecond;
					if (context.CastedRayCount > 0)
					{
						DateTime startTime = state.bakingStart.Value;
						DateTime endTime = state.bakingEnd.GetValueOrDefault(DateTime.Now);

						raysPerSecond = context.CastedRayCount / (endTime - startTime).TotalSeconds;
					}
					else
						raysPerSecond = 0.0;

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Rays", string.Format("{0}", context.CastedRayCount));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Speed", string.Format("{0:0.0}KRays/s", raysPerSecond / 1e3));
					EditorGUILayout.EndHorizontal();
				}
			}

			{
				EditorGUILayout.BeginHorizontal();

				// Bake button
				if (GUILayout.Button("Bake"))
				{
					dispatcher.StartBake(baker);
				}

				// Cancel button
				wasEnabled = GUI.enabled;
				GUI.enabled = baker.IsBaking;

				if (GUILayout.Button("Cancel"))
					dispatcher.CancelBake();

				GUI.enabled = wasEnabled;

				EditorGUILayout.EndHorizontal();
			}

			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Active scene", baker.Scene.name);
				EditorGUILayout.EndHorizontal();
			}

			{
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

			GUI.enabled = true;
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

		private void Setup()
		{
			if (baker != null)
				return;
			
			// Create a bake data object
			GameObject bakeDataObject = new GameObject("BakeData");
			bakeDataObject.AddComponent<BakeData>();

			// Make sure the scene will be saved
			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
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
			if (camera.name == "Preview Scene Camera")
				return;

			bool isSceneCamera = camera.name == "SceneCamera";

			foreach (BakeTarget target in baker.BakeTargets)
			{
				if (target.bakedMesh == null || target.renderer == null)
					continue;

				foreach (BakeTargetSubmesh submesh in target.submeshes)
				{
					Material material = submesh.bakedMaterial != null ? submesh.bakedMaterial : submesh.originalMaterial;
					for (int passIdx = 0; passIdx < material.passCount; ++passIdx)
					{
						if (previewMode == PreviewMode.SHADED || previewMode == PreviewMode.SHADED_WIREFRAME || !isSceneCamera)
						{
							material.SetPass(passIdx);
							Graphics.DrawMeshNow(target.bakedMesh, target.renderer.transform.localToWorldMatrix, submesh.idx);
						}

						if (isSceneCamera)
						{
							if (previewMode == PreviewMode.SHADED_WIREFRAME || previewMode == PreviewMode.WIREFRAME)
								wireframeRenderer.DrawWireframe(target.bakedMesh, submesh.idx, target.renderer.transform.localToWorldMatrix, Color.black);
						}
					}
				}
			}
		}
		
		private void OnSceneGUI(SceneView sceneView)
		{
			if (baker != null && drawKDTree && baker.Backend is RaytracerBackend)
			{
				KDTree tree = (baker.Backend as RaytracerBackend).Context.Tree;

				if (tree.RootNode != null)
					DrawKDTreeNode(tree, tree.RootNode, tree.Bounds);
			}
		}

		private void OnCameraPostRender(Camera camera)
		{
			if (baker != null && baker.HasBakeData && previewEnabled)
				DrawPreview(camera);
		}
		
		private void OnBakeDataLoaded(BakeData data)
		{
			// TODO: check if we don't have other bake data active yet
			Scene activeScene = EditorSceneManager.GetActiveScene();
			baker = new Lightbaker(activeScene, data, bakeSettings);

			DisablePreview();
			Repaint();
		}

		private void OnBakeDataUnloaded(BakeData data)
		{
			// TODO: check if the unloaded data is the one the baker uses
			baker = null;

			Repaint();
		}

#if UNITY_2017_1_OR_NEWER
		private void OnPlayModeChanged(PlayModeStateChange stateChange)
		{
			Repaint();
		}
#endif

		[MenuItem("Window/Superluminal")]
		public static void OpenWindow()
		{
			EditorWindow window = EditorWindow.GetWindow(typeof(BakeWindow));
			window.titleContent = new GUIContent("Superluminal");
		}

	}
}