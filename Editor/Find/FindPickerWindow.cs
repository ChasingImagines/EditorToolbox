#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// [Find] niteliği için zengin arama ve seçim penceresi (FindPickerWindow).
    /// Canlı filtreleme, sekmeler (Sahne, Prefablar, Assetler), Türkçe karakter normalizasyonu ve klavye desteği sunar.
    /// </summary>
    public sealed class FindPickerWindow : EditorWindow
    {
        private SerializedObject _serializedObject;
        private string _propertyPath;
        private Type _fieldType;
        private FindAttribute _attr;

        private Vector2 _scroll;
        private string _search = "";
        private int _tab; // 0 Scene, 1 Prefabs, 2 Assets
        private int _selectedIndex = -1;

        private readonly List<UnityEngine.Object> _scene = new List<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> _prefabs = new List<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> _filtered = new List<UnityEngine.Object>();

        public static void Open(SerializedProperty property, Type fieldType, FindAttribute attr)
        {
            var w = CreateInstance<FindPickerWindow>();
            w.titleContent = new GUIContent("Varlık / Referans Seçici", EditorGUIUtility.IconContent("Search Icon")?.image);
            w._serializedObject = property.serializedObject;
            w._propertyPath = property.propertyPath;
            w._fieldType = fieldType;
            w._attr = attr;
            w.minSize = new Vector2(440, 420);
            w.Refresh();
            w.ShowUtility();
        }

        private void OnGUI()
        {
            if (_serializedObject == null || _serializedObject.targetObject == null)
            {
                Close();
                return;
            }

            HandleKeyboardEvents();

            DrawHeader();
            DrawTabs();
            DrawList();
        }

        private void HandleKeyboardEvents()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Escape)
            {
                Close();
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.DownArrow)
            {
                if (_filtered.Count > 0)
                {
                    _selectedIndex = Mathf.Clamp(_selectedIndex + 1, 0, _filtered.Count - 1);
                    Repaint();
                }
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.UpArrow)
            {
                if (_filtered.Count > 0)
                {
                    _selectedIndex = Mathf.Clamp(_selectedIndex - 1, 0, _filtered.Count - 1);
                    Repaint();
                }
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _filtered.Count)
                {
                    Assign(_filtered[_selectedIndex]);
                    Close();
                    e.Use();
                    return;
                }
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(22), GUILayout.Height(20));
                GUI.SetNextControlName("SearchField");
                string newSearch = EditorGUILayout.TextField(_search, GUILayout.ExpandWidth(true));
                if (newSearch != _search)
                {
                    _search = newSearch;
                    _selectedIndex = 0;
                }

                if (GUILayout.Button("Yenile", GUILayout.Width(70)))
                {
                    Refresh();
                }
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"Gereken Tip: {_attr.RequiredType?.Name ?? _fieldType.Name}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Kaynak: {_attr.Source}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);
        }

        private void DrawTabs()
        {
            var tabs = new List<string>();
            if (Allow(FindSource.Scene)) tabs.Add($"Sahne ({_scene.Count})");
            if (Allow(FindSource.Prefabs)) tabs.Add($"Prefablar ({_prefabs.Count})");
            if (Allow(FindSource.Assets)) tabs.Add($"Assetler ({_assets.Count})");

            if (tabs.Count == 0)
            {
                EditorGUILayout.HelpBox("FindAttribute.Source içinde hiçbir kaynak etkinleştirilmemiş.", MessageType.Warning);
                return;
            }

            int prevTab = _tab;
            _tab = GUILayout.Toolbar(Mathf.Clamp(_tab, 0, tabs.Count - 1), tabs.ToArray());
            if (_tab != prevTab)
            {
                _selectedIndex = 0;
            }

            EditorGUILayout.Space(4);
        }

        private void DrawList()
        {
            var rawList = GetCurrentList();
            if (rawList == null) return;

            _filtered.Clear();
            for (int i = 0; i < rawList.Count; i++)
            {
                var obj = rawList[i];
                if (obj != null && PassSearch(obj))
                    _filtered.Add(obj);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUIStyle rowStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 26,
                margin = new RectOffset(4, 4, 2, 2),
                padding = new RectOffset(8, 8, 0, 0)
            };

            // Referansı Temizle Butonu
            GUI.backgroundColor = new Color(0.95f, 0.4f, 0.4f, 0.35f);
            if (GUILayout.Button("∅ Seçimi Temizle (None)", rowStyle))
            {
                Assign(null);
                Close();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(2);

            for (int i = 0; i < _filtered.Count; i++)
            {
                var obj = _filtered[i];
                if (obj == null) continue;

                bool isSelected = i == _selectedIndex;
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.2f, 0.6f, 1f, 0.5f);
                }

                GUIContent content = EditorGUIUtility.ObjectContent(obj, obj.GetType());
                if (GUILayout.Button(content, rowStyle))
                {
                    Assign(obj);
                    Close();
                    break;
                }

                if (isSelected)
                {
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void Assign(UnityEngine.Object picked)
        {
            _serializedObject.Update();
            var prop = _serializedObject.FindProperty(_propertyPath);
            if (prop == null) return;

            if (picked == null)
            {
                prop.objectReferenceValue = null;
            }
            else
            {
                var normalized = FindAttributeDrawer.NormalizeForField(picked, _fieldType, _attr.RequiredType);
                prop.objectReferenceValue = normalized;
            }

            _serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_serializedObject.targetObject);
        }

        private void Refresh()
        {
            _scene.Clear();
            _prefabs.Clear();
            _assets.Clear();

            if (Allow(FindSource.Scene)) CollectSceneObjects();
            if (Allow(FindSource.Prefabs)) CollectPrefabs();
            if (Allow(FindSource.Assets)) CollectAssets();
        }

        private bool Allow(FindSource s) => _attr.Source == FindSource.Auto || (_attr.Source & s) != 0;

        private List<UnityEngine.Object> GetCurrentList()
        {
            var enabled = new List<List<UnityEngine.Object>>();
            if (Allow(FindSource.Scene)) enabled.Add(_scene);
            if (Allow(FindSource.Prefabs)) enabled.Add(_prefabs);
            if (Allow(FindSource.Assets)) enabled.Add(_assets);
            if (enabled.Count == 0) return null;
            return enabled[Mathf.Clamp(_tab, 0, enabled.Count - 1)];
        }

        private bool PassSearch(UnityEngine.Object obj)
        {
            if (obj == null) return false;
            if (string.IsNullOrWhiteSpace(_search)) return true;

            string q = Normalize(_search);
            string name = Normalize(obj.name);
            string type = Normalize(obj.GetType().Name);

            if (name.Contains(q) || type.Contains(q))
                return true;

            var tokens = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool allTokensMatch = true;
            foreach (var t in tokens)
            {
                if (!(name.Contains(t) || type.Contains(t) || FuzzyContains(name, t) || FuzzyContains(type, t)))
                {
                    allTokensMatch = false;
                    break;
                }
            }
            if (allTokensMatch) return true;

            int dn = LevenshteinDistance(name, q);
            int dt = LevenshteinDistance(type, q);
            int threshold = q.Length <= 4 ? 1 : q.Length <= 8 ? 2 : 3;

            return dn <= threshold || dt <= threshold;
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = s.ToLowerInvariant().Trim();
            s = s.Replace("_", " ").Replace("-", " ").Replace(".", " ");

            s = s
                .Replace('ç', 'c')
                .Replace('ğ', 'g')
                .Replace('ı', 'i')
                .Replace('ö', 'o')
                .Replace('ş', 's')
                .Replace('ü', 'u');

            return s;
        }

        private static bool FuzzyContains(string text, string token)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return false;
            if (text.Contains(token)) return true;

            int len = token.Length;
            if (text.Length < len) return LevenshteinDistance(text, token) <= 1;

            int allowed = len <= 4 ? 1 : 2;
            for (int i = 0; i <= text.Length - len; i++)
            {
                var part = text.Substring(i, len);
                if (LevenshteinDistance(part, token) <= allowed)
                    return true;
            }
            return false;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[a.Length, b.Length];
        }

        private void CollectSceneObjects()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var trs = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in trs)
                    {
                        var candidate = CandidateFromGameObject(t.gameObject, _fieldType, _attr.RequiredType);
                        if (candidate != null) _scene.Add(candidate);
                    }
                }
            }
        }

        private void CollectPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var candidate = CandidateFromGameObject(prefab, _fieldType, _attr.RequiredType);
                if (candidate != null) _prefabs.Add(candidate);
            }
        }

        private void CollectAssets()
        {
            Type targetType = _attr.RequiredType ?? _fieldType;
            string filter = "t:ScriptableObject";

            if (targetType != null && targetType != typeof(UnityEngine.Object))
            {
                if (typeof(ScriptableObject).IsAssignableFrom(targetType))
                    filter = $"t:{targetType.Name}";
                else if (typeof(AudioClip).IsAssignableFrom(targetType))
                    filter = "t:AudioClip";
                else if (typeof(Texture).IsAssignableFrom(targetType))
                    filter = "t:Texture";
                else if (typeof(Material).IsAssignableFrom(targetType))
                    filter = "t:Material";
            }

            var guids = AssetDatabase.FindAssets(filter);
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null) continue;

                if (IsCompatible(asset, _fieldType, _attr.RequiredType))
                    _assets.Add(asset);
            }
        }

        private static UnityEngine.Object CandidateFromGameObject(GameObject go, Type fieldType, Type requiredType)
        {
            return FindAttributeDrawer.NormalizeForField(go, fieldType, requiredType);
        }

        private static bool IsCompatible(UnityEngine.Object obj, Type fieldType, Type requiredType)
        {
            if (obj == null) return false;
            if (!fieldType.IsAssignableFrom(obj.GetType()) && fieldType != typeof(UnityEngine.Object)) return false;
            if (requiredType == null) return true;
            return requiredType.IsAssignableFrom(obj.GetType());
        }
    }
}
#endif
