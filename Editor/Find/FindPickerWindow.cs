#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// Searchable picker window for [Find] attribute.
    /// Features cached search/filtering, smart source tabs, diacritic normalization, and multi-object assignment.
    /// </summary>
    public sealed class FindPickerWindow : EditorWindow
    {
        private struct PickerEntry
        {
            public UnityEngine.Object Object;
            public string DisplayName;
            public string Tooltip;
            public string NormalizedName;
            public string NormalizedType;
        }

        private struct ScoredEntry : IComparable<ScoredEntry>
        {
            public PickerEntry Entry;
            public int Score;

            public int CompareTo(ScoredEntry other)
            {
                int cmp = Score.CompareTo(other.Score);
                if (cmp != 0) return cmp;
                return string.Compare(Entry.DisplayName, other.Entry.DisplayName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private const string SearchControlName = "FindPickerSearchField";
        private static readonly char[] SearchSeparators = { ' ' };
        private static FindPickerWindow _activeWindow;

        private static int[] _levPrev = Array.Empty<int>();
        private static int[] _levCurr = Array.Empty<int>();

        private UnityEngine.Object[] _targets;
        private string _propertyPath;
        private Type _fieldType;
        private FindAttribute _attr;

        private Vector2 _scroll;
        private string _search = string.Empty;
        private int _tab;
        private int _selectedIndex = -1;
        private bool _focusSearchOnNextGui;

        private readonly List<PickerEntry> _scene = new List<PickerEntry>();
        private readonly List<PickerEntry> _prefabs = new List<PickerEntry>();
        private readonly List<PickerEntry> _assets = new List<PickerEntry>();
        private readonly List<FindSource> _enabledSources = new List<FindSource>(3);
        private string[] _tabLabels = Array.Empty<string>();

        private readonly List<PickerEntry> _filtered = new List<PickerEntry>();
        private readonly List<ScoredEntry> _scoredScratch = new List<ScoredEntry>();
        private readonly List<UnityEngine.Object> _candidateScratch = new List<UnityEngine.Object>(4);
        private readonly HashSet<int> _seenIds = new HashSet<int>();

        private GUIStyle _rowStyle;
        private readonly GUIContent _rowContentScratch = new GUIContent();

        public static void Open(SerializedProperty property, Type fieldType, FindAttribute attr)
        {
            if (property == null || property.serializedObject == null)
                return;

            var targets = property.serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
                return;

            if (_activeWindow != null)
                _activeWindow.Close();

            var w = CreateInstance<FindPickerWindow>();
            _activeWindow = w;

            var searchIcon = EditorGUIUtility.IconContent("Search Icon");
            w.titleContent = new GUIContent("Asset / Reference Picker", searchIcon != null ? searchIcon.image : null);
            w._targets = (UnityEngine.Object[])targets.Clone();
            w._propertyPath = property.propertyPath;
            w._fieldType = fieldType ?? typeof(UnityEngine.Object);
            w._attr = attr ?? new FindAttribute();
            w._search = string.Empty;
            w._tab = 0;
            w._selectedIndex = -1;
            w._focusSearchOnNextGui = true;

            w.minSize = new Vector2(360f, 420f);
            w.Refresh(selectFirstNonEmptyTab: true);
            w.ShowAuxWindow();
        }

        private void OnDestroy()
        {
            if (_activeWindow == this)
                _activeWindow = null;
        }

        private void OnGUI()
        {
            if (!HasValidTargets())
            {
                Close();
                return;
            }

            HandleKeyboard();

            DrawHeader();
            DrawTabs();
            DrawList();
        }

        private bool HasValidTargets()
        {
            if (_targets == null) return false;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null) return true;
            }
            return false;
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
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
                    Assign(_filtered[_selectedIndex].Object);
                    Close();
                    e.Use();
                }
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(22f), GUILayout.Height(20f));
                GUI.SetNextControlName(SearchControlName);
                string newSearch = EditorGUILayout.TextField(_search, GUILayout.ExpandWidth(true));

                if (_focusSearchOnNextGui)
                {
                    EditorGUI.FocusTextInControl(SearchControlName);
                    _focusSearchOnNextGui = false;
                }

                if (newSearch != _search)
                {
                    _search = newSearch;
                    RebuildFilter();
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
                    Refresh(selectFirstNonEmptyTab: false);
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                string reqName = _attr.RequiredType?.Name ?? _fieldType.Name;
                GUILayout.Label($"Required Type: {reqName}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Source: {_attr.Source}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);
        }

        private void DrawTabs()
        {
            if (_enabledSources.Count == 0)
            {
                EditorGUILayout.HelpBox("No compatible search sources enabled for this field type.", MessageType.Warning);
                return;
            }

            int prevTab = _tab;
            _tab = GUILayout.Toolbar(Mathf.Clamp(_tab, 0, _enabledSources.Count - 1), _tabLabels);
            if (_tab != prevTab)
                RebuildFilter();

            EditorGUILayout.Space(4);
        }

        private void DrawList()
        {
            if (_enabledSources.Count == 0)
                return;

            if (_rowStyle == null)
            {
                _rowStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fixedHeight = 26f,
                    margin = new RectOffset(4, 4, 2, 2),
                    padding = new RectOffset(8, 8, 0, 0)
                };
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.95f, 0.4f, 0.4f, 0.35f);
            if (GUILayout.Button("\u2205 Clear Selection (None)", _rowStyle))
            {
                GUI.backgroundColor = prevBg;
                Assign(null);
                Close();
                EditorGUILayout.EndScrollView();
                return;
            }
            GUI.backgroundColor = prevBg;

            EditorGUILayout.Space(2);

            for (int i = 0; i < _filtered.Count; i++)
            {
                var entry = _filtered[i];
                if (entry.Object == null) continue;

                bool isSelected = i == _selectedIndex;
                if (isSelected)
                    GUI.backgroundColor = new Color(0.2f, 0.6f, 1f, 0.5f);

                var objContent = EditorGUIUtility.ObjectContent(entry.Object, entry.Object.GetType());
                _rowContentScratch.text = entry.DisplayName;
                _rowContentScratch.image = objContent.image;
                _rowContentScratch.tooltip = entry.Tooltip;

                if (GUILayout.Button(_rowContentScratch, _rowStyle))
                {
                    GUI.backgroundColor = prevBg;
                    Assign(entry.Object);
                    Close();
                    break;
                }

                if (isSelected)
                    GUI.backgroundColor = prevBg;
            }

            EditorGUILayout.EndScrollView();
        }

        private void Assign(UnityEngine.Object picked)
        {
            if (_targets == null || _targets.Length == 0 || string.IsNullOrEmpty(_propertyPath))
                return;

            var validTargets = new List<UnityEngine.Object>(_targets.Length);
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null)
                    validTargets.Add(_targets[i]);
            }

            if (validTargets.Count == 0)
                return;

            using (var so = new SerializedObject(validTargets.ToArray()))
            {
                so.Update();
                var prop = so.FindProperty(_propertyPath);
                if (prop == null) return;

                if (picked == null)
                {
                    prop.objectReferenceValue = null;
                }
                else
                {
                    var normalized = FindAttributeDrawer.NormalizeForField(picked, _fieldType, _attr.RequiredType);
                    if (normalized == null) return;
                    prop.objectReferenceValue = normalized;
                }

                so.ApplyModifiedProperties();
            }
        }

        private void Refresh(bool selectFirstNonEmptyTab)
        {
            _scene.Clear();
            _prefabs.Clear();
            _assets.Clear();
            _enabledSources.Clear();

            if (IsSourceEnabled(FindSource.Scene))
            {
                _enabledSources.Add(FindSource.Scene);
                CollectSceneObjects();
            }

            if (IsSourceEnabled(FindSource.Prefabs))
            {
                _enabledSources.Add(FindSource.Prefabs);
                CollectPrefabs();
            }

            if (IsSourceEnabled(FindSource.Assets))
            {
                _enabledSources.Add(FindSource.Assets);
                CollectAssets();
            }

            _tabLabels = new string[_enabledSources.Count];
            for (int i = 0; i < _enabledSources.Count; i++)
            {
                switch (_enabledSources[i])
                {
                    case FindSource.Scene:
                        _tabLabels[i] = $"Scene ({_scene.Count})";
                        break;
                    case FindSource.Prefabs:
                        _tabLabels[i] = $"Prefabs ({_prefabs.Count})";
                        break;
                    case FindSource.Assets:
                        _tabLabels[i] = $"Assets ({_assets.Count})";
                        break;
                }
            }

            if (selectFirstNonEmptyTab && _enabledSources.Count > 0)
            {
                _tab = 0;
                for (int i = 0; i < _enabledSources.Count; i++)
                {
                    if (GetListForSource(_enabledSources[i]).Count > 0)
                    {
                        _tab = i;
                        break;
                    }
                }
            }
            else if (_enabledSources.Count > 0)
            {
                _tab = Mathf.Clamp(_tab, 0, _enabledSources.Count - 1);
            }
            else
            {
                _tab = 0;
            }

            RebuildFilter();
        }

        private void RebuildFilter()
        {
            _filtered.Clear();
            _scoredScratch.Clear();

            var rawList = GetCurrentList();
            if (rawList == null)
            {
                _selectedIndex = -1;
                return;
            }

            if (string.IsNullOrWhiteSpace(_search))
            {
                for (int i = 0; i < rawList.Count; i++)
                {
                    if (rawList[i].Object != null)
                        _filtered.Add(rawList[i]);
                }

                _filtered.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                _selectedIndex = _filtered.Count > 0 ? 0 : -1;
                return;
            }

            string query = NormalizeSearchText(_search);
            string[] tokens = query.Split(SearchSeparators, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < rawList.Count; i++)
            {
                var entry = rawList[i];
                if (entry.Object == null) continue;

                if (TryScoreEntry(in entry, query, tokens, out int score))
                    _scoredScratch.Add(new ScoredEntry { Entry = entry, Score = score });
            }

            _scoredScratch.Sort();

            for (int i = 0; i < _scoredScratch.Count; i++)
                _filtered.Add(_scoredScratch[i].Entry);

            _selectedIndex = _filtered.Count > 0 ? 0 : -1;
        }

        private bool IsSourceEnabled(FindSource source)
        {
            if (!FindAttributeDrawer.IsValidRequiredType(_attr.RequiredType))
                return false;

            if ((_attr.Source & source) == 0)
                return false;

            switch (source)
            {
                case FindSource.Scene:
                case FindSource.Prefabs:
                    return FindAttributeDrawer.CanMatchGameObjectOrComponent(_fieldType, _attr.RequiredType);
                case FindSource.Assets:
                    return FindAttributeDrawer.CanMatchAsset(_fieldType, _attr.RequiredType);
                default:
                    return false;
            }
        }

        private List<PickerEntry> GetListForSource(FindSource source)
        {
            switch (source)
            {
                case FindSource.Scene: return _scene;
                case FindSource.Prefabs: return _prefabs;
                case FindSource.Assets: return _assets;
                default: return null;
            }
        }

        private List<PickerEntry> GetCurrentList()
        {
            if (_enabledSources.Count == 0) return null;
            return GetListForSource(_enabledSources[Mathf.Clamp(_tab, 0, _enabledSources.Count - 1)]);
        }

        private static bool TryScoreEntry(in PickerEntry entry, string query, string[] tokens, out int score)
        {
            string name = entry.NormalizedName;
            string type = entry.NormalizedType;

            if (string.Equals(name, query, StringComparison.Ordinal))
            {
                score = 0;
                return true;
            }

            if (name.StartsWith(query, StringComparison.Ordinal))
            {
                score = 10;
                return true;
            }

            if (name.Contains(query))
            {
                score = 20;
                return true;
            }

            if (type.Contains(query))
            {
                score = 30;
                return true;
            }

            if (tokens.Length > 0)
            {
                bool allExact = true;
                bool allFuzzy = true;

                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    bool exactToken = name.Contains(token) || type.Contains(token);
                    if (!exactToken)
                    {
                        allExact = false;
                        if (!(FuzzyContains(name, token) || FuzzyContains(type, token)))
                        {
                            allFuzzy = false;
                            break;
                        }
                    }
                }

                if (allExact)
                {
                    score = 40;
                    return true;
                }

                if (allFuzzy)
                {
                    score = 50;
                    return true;
                }
            }

            int dn = ComputeLevenshteinDistance(name, query);
            int dt = ComputeLevenshteinDistance(type, query);
            int minDist = dn < dt ? dn : dt;
            int threshold = query.Length <= 4 ? 1 : query.Length <= 8 ? 2 : 3;

            if (minDist <= threshold)
            {
                score = 60 + minDist;
                return true;
            }

            score = int.MaxValue;
            return false;
        }

        internal static string NormalizeSearchText(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // Map Turkish dotless/dotted I explicitly before invariant lowercasing and FormD stripping.
            string preMapped = s.Replace('\u0130', 'i').Replace('\u0131', 'i').ToLowerInvariant();
            string decomposed = preMapped.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder(decomposed.Length);
            bool lastWasSpace = true;

            for (int i = 0; i < decomposed.Length; i++)
            {
                char c = decomposed[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (c == '_' || c == '-' || c == '.' || char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                    continue;
                }

                sb.Append(c);
                lastWasSpace = false;
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Length--;

            return sb.ToString();
        }

        private static bool FuzzyContains(string text, string token)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return false;
            if (text.Contains(token)) return true;

            int len = token.Length;
            if (text.Length < len) return ComputeLevenshteinDistance(text, token) <= 1;

            int allowed = len <= 4 ? 1 : 2;
            int maxOffset = text.Length - len;
            for (int i = 0; i <= maxOffset; i++)
            {
                string part = text.Substring(i, len);
                if (ComputeLevenshteinDistance(part, token) <= allowed)
                    return true;
            }
            return false;
        }

        internal static int ComputeLevenshteinDistance(string a, string b)
        {
            if (a == b) return 0;
            if (string.IsNullOrEmpty(a)) return b == null ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            if (a.Length < b.Length)
            {
                string tmp = a;
                a = b;
                b = tmp;
            }

            int needed = b.Length + 1;
            if (_levPrev.Length < needed)
            {
                _levPrev = new int[needed];
                _levCurr = new int[needed];
            }

            for (int j = 0; j <= b.Length; j++)
                _levPrev[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                _levCurr[0] = i;
                char ca = a[i - 1];

                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = ca == b[j - 1] ? 0 : 1;
                    int del = _levPrev[j] + 1;
                    int ins = _levCurr[j - 1] + 1;
                    int sub = _levPrev[j - 1] + cost;
                    int best = del < ins ? del : ins;
                    _levCurr[j] = best < sub ? best : sub;
                }

                int[] swap = _levPrev;
                _levPrev = _levCurr;
                _levCurr = swap;
            }

            return _levPrev[b.Length];
        }

        private void CollectSceneObjects()
        {
            _seenIds.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    var transforms = roots[r].GetComponentsInChildren<Transform>(true);
                    for (int t = 0; t < transforms.Length; t++)
                    {
                        var go = transforms[t].gameObject;
                        _candidateScratch.Clear();
                        FindAttributeDrawer.CollectCandidatesFromGameObject(go, _fieldType, _attr.RequiredType, _candidateScratch);

                        for (int c = 0; c < _candidateScratch.Count; c++)
                        {
                            var candidate = _candidateScratch[c];
                            if (candidate == null || !_seenIds.Add(candidate.GetInstanceID()))
                                continue;

                            string hierarchyPath = BuildHierarchyPath(transforms[t]);
                            _scene.Add(CreateEntry(candidate, $"{scene.name}: {hierarchyPath}"));
                        }
                    }
                }
            }
        }

        private void CollectPrefabs()
        {
            _seenIds.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                _candidateScratch.Clear();
                FindAttributeDrawer.CollectCandidatesFromGameObject(prefab, _fieldType, _attr.RequiredType, _candidateScratch);

                for (int c = 0; c < _candidateScratch.Count; c++)
                {
                    var candidate = _candidateScratch[c];
                    if (candidate == null || !_seenIds.Add(candidate.GetInstanceID()))
                        continue;

                    _prefabs.Add(CreateEntry(candidate, path));
                }
            }
        }

        private void CollectAssets()
        {
            _seenIds.Clear();

            Type searchType = typeof(UnityEngine.Object);
            if (_attr.RequiredType != null
                && typeof(UnityEngine.Object).IsAssignableFrom(_attr.RequiredType)
                && _attr.RequiredType != typeof(UnityEngine.Object))
            {
                searchType = _attr.RequiredType;
            }
            else if (_fieldType != null
                     && typeof(UnityEngine.Object).IsAssignableFrom(_fieldType)
                     && _fieldType != typeof(UnityEngine.Object))
            {
                searchType = _fieldType;
            }

            string filter;
            string[] searchFolders = null;

            if (searchType == typeof(UnityEngine.Object))
            {
                if (_attr.RequiredType != null && _attr.RequiredType.IsInterface)
                {
                    filter = "t:ScriptableObject";
                }
                else
                {
                    filter = "t:Object";
                    searchFolders = new[] { "Assets" };
                }
            }
            else
            {
                filter = $"t:{searchType.Name}";
            }

            bool expectSubAssets = typeof(Sprite).IsAssignableFrom(searchType)
                || typeof(Mesh).IsAssignableFrom(searchType)
                || typeof(AnimationClip).IsAssignableFrom(searchType)
                || typeof(Avatar).IsAssignableFrom(searchType);

            string[] guids = searchFolders != null
                ? AssetDatabase.FindAssets(filter, searchFolders)
                : AssetDatabase.FindAssets(filter);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)
                    || AssetDatabase.IsValidFolder(path)
                    || path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (expectSubAssets)
                {
                    AddCompatibleAssets(AssetDatabase.LoadAllAssetsAtPath(path), path);
                    continue;
                }

                var mainAsset = AssetDatabase.LoadAssetAtPath(path, searchType);
                if (mainAsset != null)
                {
                    AddAssetIfCompatible(mainAsset, path);
                }
                else
                {
                    AddCompatibleAssets(AssetDatabase.LoadAllAssetsAtPath(path), path);
                }
            }
        }

        private void AddCompatibleAssets(UnityEngine.Object[] loaded, string path)
        {
            if (loaded == null) return;
            for (int i = 0; i < loaded.Length; i++)
                AddAssetIfCompatible(loaded[i], path);
        }

        private void AddAssetIfCompatible(UnityEngine.Object asset, string path)
        {
            if (asset == null) return;
            if (!_seenIds.Add(asset.GetInstanceID())) return;
            if (!IsAssetCompatible(asset, _fieldType, _attr.RequiredType)) return;

            _assets.Add(CreateEntry(asset, path));
        }

        private static PickerEntry CreateEntry(UnityEngine.Object obj, string locationPath)
        {
            string typeName = obj.GetType().Name;
            string displayName = $"{obj.name} ({typeName})";
            string tooltip = string.IsNullOrEmpty(locationPath)
                ? displayName
                : $"{locationPath} [{typeName}]";

            return new PickerEntry
            {
                Object = obj,
                DisplayName = displayName,
                Tooltip = tooltip,
                NormalizedName = NormalizeSearchText(obj.name),
                NormalizedType = NormalizeSearchText(typeName)
            };
        }

        private static string BuildHierarchyPath(Transform target)
        {
            if (target == null) return string.Empty;
            if (target.parent == null) return target.name;

            var parts = new List<string>(4);
            var current = target;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static bool IsAssetCompatible(UnityEngine.Object obj, Type fieldType, Type requiredType)
        {
            if (obj == null) return false;
            if (fieldType != null && fieldType != typeof(UnityEngine.Object) && !fieldType.IsAssignableFrom(obj.GetType()))
                return false;

            return requiredType == null || requiredType.IsAssignableFrom(obj.GetType());
        }
    }
}
#endif
