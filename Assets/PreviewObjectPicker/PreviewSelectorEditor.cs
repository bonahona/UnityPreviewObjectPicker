using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Fyrvall.PreviewObjectPicker
{
    public class PreviewSelectorEditor : EditorWindow
    {
        const int DefaultListViewWidth = 250;
        const int DefaultBottomRow = 38;

        public static void ShowAuxWindow(System.Type type, SerializedProperty serializedProperty)
        {
            var window = EditorWindow.CreateInstance<PreviewSelectorEditor>();
            window.ChangeSelectedType(type);
            window.SerializedProperty = serializedProperty;
            window.OriginalValue = serializedProperty.objectReferenceValue;
            window.titleContent = new GUIContent(type.Name);
            window.ShowAuxWindow();
        }

        public System.Type SelectedType;

        public string FilterString;
        public Object SelectedObject;
        public Editor SelectedObjectEditor;
        public SerializedProperty SerializedProperty;
        public Object OriginalValue;

        public List<Object> FoundObjects = new List<Object>();
        public List<Object> FilteredObjects = new List<Object>();

        public GUIStyle SelectedStyle;
        public GUIStyle UnselectedStyle;

        public Vector2 ListScrollViewOffset;
        public float ListViewWidth = DefaultListViewWidth;
        public Vector2 InspectorScrollViewOffset;

        private SearchField ObjectSearchField;

        public void SetupStyles()
        {
            SelectedStyle = new GUIStyle(GUI.skin.label);
            SelectedStyle.normal.textColor = Color.white;
            SelectedStyle.normal.background = CreateTexture(300, 20, new Color(0.24f, 0.48f, 0.9f));
            UnselectedStyle = new GUIStyle(GUI.skin.label);
        }

        private Texture2D CreateTexture(int width, int height, Color color)
        {
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(Enumerable.Repeat(color, width * height).ToArray());
            result.Apply();

            return result;
        }

        public void OnEnable()
        {
            ObjectSearchField = new SearchField();
        }

        public void OnGUI()
        {
            SetupStyles();
            EditorGUILayout.Space();

            if (Event.current.type == EventType.KeyDown) {
                if (Event.current.keyCode == KeyCode.DownArrow) {
                    UpdateSelectedObjectIndex(delta: 1);
                    Event.current.Use();
                } else if (Event.current.keyCode == KeyCode.UpArrow) {
                    UpdateSelectedObjectIndex(delta: -1);
                    Event.current.Use();
                }
            }

            var previewWidth = this.position.width - (ListViewWidth + 70);
            var previewHeight = this.position.height - (DefaultBottomRow);

            using (new EditorGUILayout.VerticalScope()) {
                using (new EditorGUILayout.VerticalScope(GUILayout.Height(previewHeight))) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(ListViewWidth))) {
                            DisplayObjects();
                        }

                        if (SelectedObject != null) {
                            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                                DisplaySelectedObject(previewWidth, previewHeight);
                            }
                        }
                    }
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.Height(DefaultBottomRow))) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        if (GUILayout.Button("Ok")) {
                            this.Close();
                        }

                        if (GUILayout.Button("Cancel")) {
                            RevertValue();
                            this.Close();
                        }
                    }
                }
            }
        }

        public void RevertValue()
        {
            SerializedProperty.objectReferenceValue = OriginalValue;
            SerializedProperty.serializedObject.ApplyModifiedProperties();
        }

        public void UpdateSelectedObjectIndex(int delta)
        {
            if (FilteredObjects.Count == 0) {
                return;
            }

            var currentIndex = FilteredObjects.IndexOf(SelectedObject);
            currentIndex = Mathf.Clamp(currentIndex + delta, 0, FilteredObjects.Count - 1);
            ChangeSelectedObject(FilteredObjects[currentIndex]);
        }

        public void DisplayObjects()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Found " + FilteredObjects.Count());
            }

            DisplaySearchField();

            if (FoundObjects == null) {
                return;
            }

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(ListScrollViewOffset)) {
                ListScrollViewOffset = scrollScope.scrollPosition;
                foreach (var foundObject in FilteredObjects.ToList()) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        DrawPreviewTexture(GetPreviewTexture(foundObject));
                        if (GUILayout.Button(GetObjectName(foundObject), GetGuIStyle(foundObject))) {
                            ChangeSelectedObject(foundObject);
                        }
                    }
                }
            }
        }

        public void DrawPreviewTexture(Texture texture)
        {
            var rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            if(texture == null) {
                return;
            }

            GUI.DrawTexture(rect, texture);
        }
        
        public string GetObjectName(Object o)
        {
            if(o == null) {
                return "None";
            } else {
                return o.name;
            }
        }

        public Texture GetPreviewTexture(Object asset)
        {
            Texture result = AssetPreview.GetAssetPreview(asset);

            if (result == null) {
                result = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(asset));
            }

            if (result == null) {
                result = AssetPreview.GetMiniThumbnail(asset);
            }

            return result;
        }

        public void DisplaySearchField()
        {
            var searchRect = GUILayoutUtility.GetRect(100, 32);
            var tmpFilterString = ObjectSearchField.OnGUI(searchRect, FilterString);

            if (tmpFilterString != FilterString) {
                UpdateFilter(tmpFilterString);
                FilterString = tmpFilterString;
            }
        }

        public void UpdateFilter(string filterString)
        {
            FilteredObjects = FilterObjects(FoundObjects, filterString);
        }

        public GUIStyle GetGuIStyle(UnityEngine.Object o)
        {
            if (SelectedObject == o) {
                return SelectedStyle;
            } else {
                return UnselectedStyle;
            }
        }

        public void DisplaySelectedObject(float previewWidth, float previewHeight)
        {
            if(SelectedObjectEditor == null) {
                return;
            }

            SelectedObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(previewWidth, previewHeight - 6), GUIStyle.none);
            Repaint();
        }

        public float GetLabelWidth(float totalWidth, float minWidth)
        {
            if (totalWidth <= minWidth) {
                return totalWidth;
            }

            return Mathf.Min(minWidth, totalWidth);
        }

        public void DrawUILine(Color color, int thickness = 1, int padding = 0)
        {
            Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            lineRect.height = thickness;
            lineRect.y += padding / 2;
            lineRect.x -= 20;
            lineRect.width += 20;
            EditorGUI.DrawRect(lineRect, color);
        }

        public void DrawComponentPreview(UnityEngine.Object unityObject)
        {
            var drawRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            var previewTexture = AssetPreview.GetMiniThumbnail(unityObject);

            if (previewTexture != null) {
                GUI.DrawTexture(drawRect, previewTexture);
            }
        }

        public List<UnityEngine.Object> FindAssetsOfType(System.Type type)
        {
            if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                return FindScriptableObjectOfType(type);
            } else if (typeof(Component).IsAssignableFrom(type)) {
                return FindPrefabsWithComponentType(type);
            } else {
                return new List<Object>();
            }
        }

        public List<UnityEngine.Object> FindScriptableObjectOfType(System.Type type)
        {
            return AssetDatabase.FindAssets(string.Format("t:{0}", type))
                .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(o => o.name)
                .ToList();
        }

        private List<UnityEngine.Object> FindPrefabsWithComponentType(System.Type type)
        {
            return AssetDatabase.GetAllAssetPaths()
                .Where(p => p.Contains(".prefab"))
                .Select(p => AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p))
                .Where(a => HasComponent(a, type))
                .Select(a => (UnityEngine.Object)a)
                .ToList();
        }

        private bool HasComponent(GameObject gameObject, System.Type type)
        {
            return gameObject.GetComponents<Component>()
                .Where(t => type.IsInstanceOfType(t))
                .Any();
        }

        public List<UnityEngine.Object> FilterObjects(List<UnityEngine.Object> startCollection, string filter)
        {
            var result = startCollection.ToList();

            if (filter != string.Empty) {
                result = result.Where(o => o.name.ToLower().Contains(filter.ToLower())).ToList();
            }

            result.Insert(0, null);
            return result;
        }

        public void ChangeSelectedObject(UnityEngine.Object selectedObject)
        {
            if (selectedObject == SelectedObject) {
                return;
            }

            SelectedObject = selectedObject;

            if(SelectedObjectEditor != null) {
                GameObject.DestroyImmediate(SelectedObjectEditor);
            }

            if (SelectedObject != null) {
                SelectedObjectEditor = Editor.CreateEditor(SelectedObject);
            }

            SerializedProperty.objectReferenceValue = SelectedObject;
            SerializedProperty.serializedObject.ApplyModifiedProperties();

            GUI.FocusControl(null);
        }

        public void ChangeSelectedType(System.Type type)
        {
            SelectedType = type;
            FoundObjects = FindAssetsOfType(type).OrderBy(a => a.name).ToList();

            FilteredObjects = FilterObjects(FoundObjects, "");
            FilterString = string.Empty;
            SelectedObject = null;
        }
    }
}