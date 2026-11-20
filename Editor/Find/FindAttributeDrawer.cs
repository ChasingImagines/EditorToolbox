#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// Custom property drawer for [Find] attribute.
    /// Provides type-safe object/component picking, drag-and-drop extraction, ping/focus, and optional inline editing.
    /// </summary>
    [InitializeOnLoad]
    [CustomPropertyDrawer(typeof(FindAttribute))]
    public sealed class FindAttributeDrawer : PropertyDrawer
    {
        private const float ClearBtnW = 18f;
        private const float SearchBtnW = 22f;
        private const float Gap = 2f;
        private const float HelpBoxHeight = 32f;

        private static readonly Dictionary<string, bool> InlineFoldouts = new Dictionary<string, bool>();

        private static GUIStyle _buttonStyle;
        private static GUIContent _searchIconContent;
        private static GUIContent _clearIconContent;

        static FindAttributeDrawer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += InlineFoldouts.Clear;
        }

        private static GUIStyle ButtonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
#if UNITY_2022_1_OR_NEWER
                    _buttonStyle = EditorStyles.iconButton ?? EditorStyles.miniButton;
#else
                    _buttonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        padding = new RectOffset(2, 2, 2, 2),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
#endif
                }
                return _buttonStyle;
            }
        }

        private static GUIContent SearchIconContent
        {
            get
            {
                if (_searchIconContent == null)
                {
                    var icon = EditorGUIUtility.IconContent("d_Search Icon");
                    if (icon == null || icon.image == null)
                        icon = EditorGUIUtility.IconContent("Search Icon");

                    _searchIconContent = (icon != null && icon.image != null)
                        ? new GUIContent(icon.image, "Open Find Picker")
                        : new GUIContent("?", "Open Find Picker");
                }
                return _searchIconContent;
            }
        }

        private static GUIContent ClearIconContent
            => _clearIconContent ?? (_clearIconContent = new GUIContent("\u00D7", "Clear reference"));

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(
                    new Rect(position.x, position.y, position.width, HelpBoxHeight),
                    "[Find] attribute only supports UnityEngine.Object reference fields.",
                    MessageType.Error
                );
                return;
            }

            var attr = (FindAttribute)attribute;
            Type resolvedFieldType = GetTargetFieldType(property);

            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var content = EditorGUI.PrefixLabel(line, label);

            float buttonsTotalW = ClearBtnW + SearchBtnW + Gap * 2f;
            float fieldW = Mathf.Max(40f, content.width - buttonsTotalW);
            var fieldRect = new Rect(content.x, content.y, fieldW, content.height);
            var clearRect = new Rect(fieldRect.xMax + Gap, content.y, ClearBtnW, content.height);
            var searchRect = new Rect(clearRect.xMax + Gap, content.y, SearchBtnW, content.height);

            var assigned = property.objectReferenceValue;
            DrawReadOnlyObjectLikeField(fieldRect, property, assigned, resolvedFieldType, attr);

            if (GUI.enabled && IsValidRequiredType(attr.RequiredType))
                HandleDragAndDrop(fieldRect, property, resolvedFieldType, attr);

            if (!property.hasMultipleDifferentValues)
                HandleFocusClick(fieldRect, assigned);

            bool canClear = GUI.enabled && (property.hasMultipleDifferentValues || assigned != null);
            using (new EditorGUI.DisabledScope(!canClear))
            {
                if (GUI.Button(clearRect, ClearIconContent, ButtonStyle))
                {
                    property.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                    GUI.changed = true;
                }
            }

            if (GUI.Button(searchRect, SearchIconContent, ButtonStyle))
                FindPickerWindow.Open(property, resolvedFieldType, attr);

            float y = line.yMax;

            string diagMessage = GetDiagnosticMessage(property, resolvedFieldType, attr, out MessageType diagType);
            if (diagMessage != null)
            {
                var diagRect = EditorGUI.IndentedRect(new Rect(position.x, y + 2f, position.width, HelpBoxHeight));
                EditorGUI.HelpBox(diagRect, diagMessage, diagType);
                y = diagRect.yMax;
            }

            if (attr.Inline && InlineEditorApi.TryGetInlineTarget(property, out var inlineTarget))
            {
                var foldRect = new Rect(position.x, y + 2f, position.width, EditorGUIUtility.singleLineHeight);
                bool expanded = GetInlineExpanded(property, attr.StartInlineExpanded);
                expanded = EditorGUI.Foldout(foldRect, expanded, "Content (Inline)", true);
                SetInlineExpanded(property, expanded);

                y = foldRect.yMax;

                if (expanded)
                {
                    float inlineHeight = InlineEditorApi.CalculateInlineHeight(property, inlineTarget);
                    var inlineRect = new Rect(
                        position.x,
                        y + EditorGUIUtility.standardVerticalSpacing,
                        position.width,
                        inlineHeight
                    );

                    EditorGUI.indentLevel++;
                    InlineEditorApi.DrawInlineFields(inlineRect, property, inlineTarget);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return HelpBoxHeight;

            float height = EditorGUIUtility.singleLineHeight;
            var attr = (FindAttribute)attribute;
            Type resolvedFieldType = GetTargetFieldType(property);

            if (GetDiagnosticMessage(property, resolvedFieldType, attr, out _) != null)
                height += 2f + HelpBoxHeight;

            if (attr.Inline && InlineEditorApi.TryGetInlineTarget(property, out var inlineTarget))
            {
                height += 2f + EditorGUIUtility.singleLineHeight;

                if (GetInlineExpanded(property, attr.StartInlineExpanded))
                {
                    height += EditorGUIUtility.standardVerticalSpacing;
                    height += InlineEditorApi.CalculateInlineHeight(property, inlineTarget);
                }
            }

            return height;
        }

        private static string GetDiagnosticMessage(SerializedProperty property, Type resolvedFieldType, FindAttribute attr, out MessageType messageType)
        {
            if (!IsValidRequiredType(attr.RequiredType))
            {
                messageType = MessageType.Error;
                return $"Invalid [Find] RequiredType '{attr.RequiredType.Name}': must be an interface or assignable to UnityEngine.Object.";
            }

            if (attr.Source == FindSource.None)
            {
                messageType = MessageType.Warning;
                return "[Find] Source is set to None. No objects or assets can be picked.";
            }

            if (!property.hasMultipleDifferentValues && attr.WarnIfAssignedValueInvalid)
            {
                var assigned = property.objectReferenceValue;
                if (assigned != null && !IsValid(assigned, resolvedFieldType, attr))
                {
                    messageType = MessageType.Warning;
                    return $"Incompatible reference: expected '{(attr.RequiredType ?? resolvedFieldType).Name}', source '{attr.Source}'.";
                }
            }

            messageType = MessageType.None;
            return null;
        }

        private Type GetTargetFieldType(SerializedProperty property)
        {
            var fi = fieldInfo ?? InlineEditorApi.GetFieldInfoFromProperty(property);
            if (fi != null)
            {
                Type t = fi.FieldType;
                if (t.IsArray)
                    return t.GetElementType();
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                    return t.GetGenericArguments()[0];
                return t;
            }
            return typeof(UnityEngine.Object);
        }

        private static string InlineKey(SerializedProperty p)
        {
            var obj = p.serializedObject?.targetObject;
            int id = obj != null ? obj.GetInstanceID() : 0;
            return $"{id}:{p.propertyPath}:find_inline";
        }

        private static bool GetInlineExpanded(SerializedProperty p, bool defaultExpanded)
        {
            string key = InlineKey(p);
            if (InlineFoldouts.TryGetValue(key, out bool v)) return v;

            InlineFoldouts[key] = defaultExpanded;
            return defaultExpanded;
        }

        private static void SetInlineExpanded(SerializedProperty p, bool value)
            => InlineFoldouts[InlineKey(p)] = value;

        private static void DrawReadOnlyObjectLikeField(Rect rect, SerializedProperty property, UnityEngine.Object value, Type fallbackType, FindAttribute attr)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.objectField);

            var iconRect = new Rect(rect.x + 2f, rect.y + 1f, rect.height - 2f, rect.height - 2f);
            var textRect = new Rect(iconRect.xMax + 2f, rect.y, Mathf.Max(0f, rect.width - iconRect.width - 6f), rect.height);

            Texture icon = null;
            string text;

            if (property.hasMultipleDifferentValues)
            {
                text = "\u2014 (Mixed values)";
            }
            else if (value == null)
            {
                string expectedName = (attr.RequiredType ?? fallbackType)?.Name ?? nameof(UnityEngine.Object);
                text = $"None ({expectedName})";
            }
            else
            {
                icon = AssetPreview.GetMiniThumbnail(value);
                text = $"{value.name} ({value.GetType().Name})";
            }

            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            GUI.Label(textRect, text, EditorStyles.label);
        }

        private static void HandleFocusClick(Rect fieldRect, UnityEngine.Object current)
        {
            if (current == null) return;

            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return;
            if (!fieldRect.Contains(e.mousePosition)) return;

            if (e.clickCount == 1)
            {
                EditorGUIUtility.PingObject(current);
                e.Use();
                return;
            }

            if (e.clickCount >= 2)
            {
                if (current is Component c)
                {
                    Selection.activeObject = c.gameObject;
                    EditorGUIUtility.PingObject(c.gameObject);
                    if (!EditorUtility.IsPersistent(c.gameObject))
                        SceneView.lastActiveSceneView?.FrameSelected();
                }
                else if (current is GameObject go)
                {
                    Selection.activeObject = go;
                    EditorGUIUtility.PingObject(go);
                    if (!EditorUtility.IsPersistent(go))
                        SceneView.lastActiveSceneView?.FrameSelected();
                    else
                        AssetDatabase.OpenAsset(go);
                }
                else
                {
                    Selection.activeObject = current;
                    EditorGUIUtility.PingObject(current);
                    AssetDatabase.OpenAsset(current);
                }

                e.Use();
            }
        }

        private static void HandleDragAndDrop(Rect dropRect, SerializedProperty property, Type fieldType, FindAttribute attr)
        {
            var e = Event.current;
            if (!dropRect.Contains(e.mousePosition)) return;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length == 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                e.Use();
                return;
            }

            UnityEngine.Object accepted = null;
            for (int i = 0; i < refs.Length; i++)
            {
                var normalized = NormalizeForField(refs[i], fieldType, attr.RequiredType);
                if (normalized != null && IsValid(normalized, fieldType, attr))
                {
                    accepted = normalized;
                    break;
                }
            }

            DragAndDrop.visualMode = accepted != null
                ? DragAndDropVisualMode.Link
                : DragAndDropVisualMode.Rejected;

            if (e.type == EventType.DragPerform && accepted != null)
            {
                DragAndDrop.AcceptDrag();
                property.objectReferenceValue = accepted;
                property.serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
            }

            e.Use();
        }

        public static bool IsValidRequiredType(Type requiredType)
        {
            return requiredType == null
                || requiredType.IsInterface
                || typeof(UnityEngine.Object).IsAssignableFrom(requiredType);
        }

        public static bool CanMatchGameObjectOrComponent(Type fieldType, Type requiredType)
        {
            if (fieldType == null || !IsValidRequiredType(requiredType))
                return false;

            bool fieldAcceptsGoOrComp = fieldType == typeof(UnityEngine.Object)
                || fieldType == typeof(GameObject)
                || typeof(Component).IsAssignableFrom(fieldType);

            if (!fieldAcceptsGoOrComp)
                return false;

            if (requiredType == null)
                return true;

            return requiredType.IsInterface
                || requiredType == typeof(UnityEngine.Object)
                || requiredType == typeof(GameObject)
                || typeof(Component).IsAssignableFrom(requiredType);
        }

        public static bool CanMatchAsset(Type fieldType, Type requiredType)
        {
            if (fieldType == null || !IsValidRequiredType(requiredType))
                return false;

            if (fieldType == typeof(GameObject) || typeof(Component).IsAssignableFrom(fieldType))
                return false;

            if (requiredType == null)
                return true;

            if (requiredType == typeof(GameObject) || typeof(Component).IsAssignableFrom(requiredType))
                return false;

            if (!requiredType.IsInterface && !fieldType.IsInterface
                && !fieldType.IsAssignableFrom(requiredType)
                && !requiredType.IsAssignableFrom(fieldType))
            {
                return false;
            }

            return true;
        }

        public static void CollectCandidatesFromGameObject(GameObject go, Type fieldType, Type requiredType, List<UnityEngine.Object> results)
        {
            if (go == null || results == null || fieldType == null || !IsValidRequiredType(requiredType))
                return;

            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                var components = go.GetComponents(fieldType);
                for (int i = 0; i < components.Length; i++)
                {
                    var comp = components[i];
                    if (comp != null && SatisfiesRequired(comp, requiredType))
                        results.Add(comp);
                }
                return;
            }

            if (fieldType == typeof(GameObject))
            {
                if (requiredType == null || requiredType.IsAssignableFrom(typeof(GameObject)))
                {
                    results.Add(go);
                    return;
                }

                if (typeof(Component).IsAssignableFrom(requiredType) || requiredType.IsInterface)
                {
                    var components = go.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        var comp = components[i];
                        if (comp != null && requiredType.IsAssignableFrom(comp.GetType()))
                        {
                            results.Add(go);
                            return;
                        }
                    }
                }
                return;
            }

            if (fieldType == typeof(UnityEngine.Object))
            {
                if (requiredType == null || requiredType.IsAssignableFrom(typeof(GameObject)))
                {
                    results.Add(go);
                    return;
                }

                if (typeof(Component).IsAssignableFrom(requiredType) || requiredType.IsInterface)
                {
                    var components = go.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        var comp = components[i];
                        if (comp != null && requiredType.IsAssignableFrom(comp.GetType()))
                            results.Add(comp);
                    }
                }
            }
        }

        public static UnityEngine.Object NormalizeForField(UnityEngine.Object picked, Type fieldType, Type requiredType)
        {
            if (picked == null || fieldType == null || !IsValidRequiredType(requiredType))
                return null;

            GameObject go = picked as GameObject ?? (picked as Component)?.gameObject;

            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                if (picked is Component directComp
                    && fieldType.IsAssignableFrom(directComp.GetType())
                    && SatisfiesRequired(directComp, requiredType))
                {
                    return directComp;
                }

                if (go == null) return null;

                var components = go.GetComponents(fieldType);
                for (int i = 0; i < components.Length; i++)
                {
                    var comp = components[i];
                    if (comp != null && SatisfiesRequired(comp, requiredType))
                        return comp;
                }
                return null;
            }

            if (fieldType == typeof(GameObject))
            {
                if (go == null) return null;

                if (requiredType == null || requiredType.IsAssignableFrom(typeof(GameObject)))
                    return go;

                if (typeof(Component).IsAssignableFrom(requiredType) || requiredType.IsInterface)
                {
                    var components = go.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        var comp = components[i];
                        if (comp != null && requiredType.IsAssignableFrom(comp.GetType()))
                            return go;
                    }
                }
                return null;
            }

            if (fieldType == typeof(UnityEngine.Object))
            {
                if (SatisfiesRequired(picked, requiredType))
                    return picked;

                if (go != null && requiredType != null && (typeof(Component).IsAssignableFrom(requiredType) || requiredType.IsInterface))
                {
                    var components = go.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        var comp = components[i];
                        if (comp != null && requiredType.IsAssignableFrom(comp.GetType()))
                            return comp;
                    }
                }
                return null;
            }

            // Non-Component/GameObject assets: ScriptableObject, AudioClip, Material, Texture2D, Sprite, Mesh, etc.
            if (fieldType.IsAssignableFrom(picked.GetType()) && SatisfiesRequired(picked, requiredType))
                return picked;

            if (fieldType == typeof(Sprite) && picked is Texture2D)
            {
                string assetPath = AssetDatabase.GetAssetPath(picked);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite != null && SatisfiesRequired(sprite, requiredType))
                        return sprite;
                }
            }

            return null;
        }

        internal static bool IsValid(UnityEngine.Object obj, Type fieldType, FindAttribute attr)
        {
            if (obj == null || attr == null) return false;
            if (!IsValidRequiredType(attr.RequiredType)) return false;
            if (!PassesSourceFilter(obj, attr.Source)) return false;

            if (fieldType != null && fieldType != typeof(UnityEngine.Object) && !fieldType.IsAssignableFrom(obj.GetType()))
                return false;

            return MatchesRequiredType(obj, attr.RequiredType);
        }

        internal static bool SatisfiesRequired(UnityEngine.Object obj, Type requiredType)
        {
            if (obj == null) return false;
            if (requiredType == null) return true;
            return requiredType.IsAssignableFrom(obj.GetType());
        }

        internal static bool MatchesRequiredType(UnityEngine.Object obj, Type requiredType)
        {
            if (obj == null) return false;
            if (requiredType == null) return true;

            if (requiredType.IsAssignableFrom(obj.GetType()))
                return true;

            if (obj is GameObject go && (typeof(Component).IsAssignableFrom(requiredType) || requiredType.IsInterface))
            {
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    var comp = components[i];
                    if (comp != null && requiredType.IsAssignableFrom(comp.GetType()))
                        return true;
                }
            }

            return false;
        }

        private static bool PassesSourceFilter(UnityEngine.Object obj, FindSource source)
        {
            if (source == FindSource.None) return false;
            if (source == FindSource.Auto) return true;

            bool allowScene = (source & FindSource.Scene) != 0;
            bool allowPrefabs = (source & FindSource.Prefabs) != 0;
            bool allowAssets = (source & FindSource.Assets) != 0;

            if (!EditorUtility.IsPersistent(obj))
                return allowScene;

            var prefabType = PrefabUtility.GetPrefabAssetType(obj);
            if (prefabType != PrefabAssetType.NotAPrefab)
                return allowPrefabs;

            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                return allowPrefabs;

            return allowAssets;
        }
    }
}
#endif
