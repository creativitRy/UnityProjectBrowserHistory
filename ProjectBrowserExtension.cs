using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CloudCanards.Core.Algorithms;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CloudCanards.Util.Editor
{
	public class ProjectBrowserExtension : EditorWindow
	{
		private static readonly LimitedDeque<FolderEntry> UndoStack = new LimitedDeque<FolderEntry>(256);
		private static readonly LimitedDeque<FolderEntry> RedoStack = new LimitedDeque<FolderEntry>(256);

		private static readonly List<TreeViewState> Browsers = new List<TreeViewState>();

		private void Update()
		{
			UpdateHistory();
			Repaint();
		}

		private void OnGUI()
		{
			GUILayout.BeginHorizontal();
			try
			{
				var prevEnabled = GUI.enabled;

				GUI.enabled = UndoStack.Count > 1;
				if (GUILayout.Button("<-"))
				{
					Undo();
				}

				GUI.enabled = RedoStack.Count > 0;
				if (GUILayout.Button("->"))
				{
					Redo();
				}

				GUI.enabled = prevEnabled;
			}
			finally
			{
				GUILayout.EndHorizontal();
			}
		}

		public void Undo()
		{
			if (UndoStack.Count <= 1)
				return;

			var folder = UndoStack.RemoveLast();
			RedoStack.AddLast(folder);
			TryOpenFolder(UndoStack.GetLast());
		}

		public void Redo()
		{
			if (RedoStack.Count <= 0)
				return;

			var folder = RedoStack.RemoveLast();
			UndoStack.AddLast(folder);
			TryOpenFolder(folder);
		}

		private void UpdateHistory()
		{
			if (Browsers.Count <= 0)
				CacheFolders();

			if (Browsers.Count <= 0)
				return;

			var browser = Browsers[0];

			if (browser.selectedIDs == null || browser.selectedIDs.Count <= 0)
				return;

			var folderId = browser.selectedIDs[0];
			if (folderId == int.MaxValue)
				return;

			if (UndoStack.Count > 0)
			{
				var prev = UndoStack.GetLast();
				if (prev.InstanceId == folderId)
					return;
			}

			RedoStack.Clear();

			UndoStack.AddLast(new FolderEntry(folderId));
		}

		private void TryOpenFolder(FolderEntry folder)
		{
			var (type, instances) = GetBrowsers();

			if (instances == null || instances.Count <= 0)
				return;

			var method = type.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
			method.Invoke(instances[0], new object[] {folder.InstanceId, true});
		}

		private void CacheFolders()
		{
			var (type, instances) = GetBrowsers();

			Browsers.Clear();

			if (instances == null)
				return;

			foreach (var instance in instances)
			{
				try
				{
					// get folder tree
					var folderTreeField = type.GetField("m_FolderTree", BindingFlags.NonPublic | BindingFlags.Instance);
					var folderTree = folderTreeField.GetValue(instance);

					// get tree state
					var folderTreeType = folderTree.GetType();
					var stateField = folderTreeType.GetProperty("state", BindingFlags.Public | BindingFlags.Instance);
					var treeState = (TreeViewState) stateField.GetValue(folderTree);

					Browsers.Add(treeState);
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}

		private static (Type type, IList instances) GetBrowsers()
		{
			// get type
			var assembly = typeof(Selection).Assembly;
			var type = assembly.GetType("UnityEditor.ProjectBrowser");

			// get instance
			var browsersField = type.GetMethod("GetAllProjectBrowsers", BindingFlags.Public | BindingFlags.Static);
			var instances = (IList) browsersField.Invoke(null, null);
			return (type, instances);
		}

		[MenuItem("Window/Utility/Project Browser Extension", priority = 5000)]
		public static void ShowWindow()
		{
			var editorWindow = GetWindow<ProjectBrowserExtension>();
			editorWindow.Show();
		}

		private void Awake()
		{
			minSize = new Vector2(100f, 20f);
			maxSize = minSize;
			titleContent = new GUIContent("Browser");
		}

		private void OnEnable()
		{
			Selection.selectionChanged += Update;
		}

		private void OnDisable()
		{
			Selection.selectionChanged -= Update;
		}

		private void OnDestroy()
		{
			UndoStack.Clear();
			RedoStack.Clear();
			Browsers.Clear();
		}

		private struct FolderEntry
		{
			public readonly Object FolderInstance;
			public readonly int InstanceId;
			public readonly string Path;

			public FolderEntry(Object folderInstance, int folderId, string path)
			{
				FolderInstance = folderInstance;
				InstanceId = folderId;
				Path = path;
			}

			public FolderEntry(int folderId) : this(EditorUtility.InstanceIDToObject(folderId), folderId,
				AssetDatabase.GetAssetPath(folderId))
			{
			}
		}
	}
}