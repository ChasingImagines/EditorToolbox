#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// [Find] niteliği için CustomPropertyDrawer.
    /// Tip-güvenli nesne seçici, drag & drop, ping/focus ve opsiyonel inline düzenleme desteği sunar.
    /// </summary>
    [CustomPropertyDrawer(typeof(FindAttribute))]
    public sealed class FindAttributeDrawer : PropertyDrawer
    {
        private const float BtnW = 22f;
        private const float Gap = 2f;

        private static readonly Dictionary<string, bool> InlineFoldouts = new Dictionary<string, bool>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (FindAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(position, "[Find] niteliği yalnızca UnityEngine.Object referans alanlarını destekler.", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // Ana satır
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var content = EditorGUI.PrefixLabel(line, label);

            var fieldRect = new Rect(content.x, content.y, content.width - BtnW - Gap, content.height);
            var btnRect = new Rect(fieldRect.xMax + Gap, content.y, BtnW, content.height);

            DrawReadOnlyObjectLikeField(fieldRect, property.objectReferenceValue);

            // Drag & Drop
            Type resolvedFieldType = GetTargetFieldType(property);
            HandleDragAndDrop(fieldRect, property, resolvedFieldType, attr);

            // Focus / Ping
            HandleFocusClick(fieldRect, property.objectReferenceValue);

            // Seçici (Picker Window)
            GUIContent searchIcon = EditorGUIUtility.IconContent("d_Search Icon");
            if (searchIcon == null || searchIcon.image == null)
                searchIcon = EditorGUIUtility.IconContent("Search Icon");

            if (GUI.Button(btnRect, searchIcon ?? new GUIContent("🔍"), EditorStyles.iconButton))
            {
                FindPickerWindow.Open(property, resolvedFieldType, attr);
            }

            float y = line.yMax;

            // Tip uyumsuzluğu uyarısı
            var assigned = property.objectReferenceValue;
            bool invalidAssigned = assigned != null && !IsValid(assigned, attr);
            if (invalidAssigned && attr.WarnIfAssignedValueInvalid)
            {
                var warnRect = new Rect(
                    position.x,
                    y + 2f,
                    position.width,
                    EditorGUIUtility.singleLineHeight * 1.5f
                );
                EditorGUI.HelpBox(
                    warnRect,
                    $"Uyumsuz referans: Gereken '{attr.RequiredType?.Name ?? resolvedFieldType.Name}', Kaynak: {attr.Source}",
                    MessageType.Warning
                );
                y = warnRect.yMax;
            }

            // Inline foldout
            if (attr.Inline && InlineEditorApi.TryGetSerializedObject(property, out var so, out var target))
            {
                var foldRect = new Rect(
                    position.x + 16f,
                    y,
                    position.width - 16f,
                    EditorGUIUtility.singleLineHeight
                );

                bool expanded = GetInlineExpanded(property);
                expanded = EditorGUI.Foldout(foldRect, expanded, "İçerik (Inline)", true);
                SetInlineExpanded(property, expanded);

                y += EditorGUIUtility.singleLineHeight;

                if (expanded)
                {
                    float inlineHeight = InlineEditorApi.CalculateInlineHeight(so, target);

                    var inlineRect = new Rect(
                        position.x,
                        y + EditorGUIUtility.standardVerticalSpacing,
                        position.width,
                        inlineHeight
                    );

                    EditorGUI.indentLevel++;
                    InlineEditorApi.DrawInlineFields(inlineRect, so, target);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return h;

            var attr = (FindAttribute)attribute;

            var assigned = property.objectReferenceValue;
            bool invalidAssigned = assigned != null && !IsValid(assigned, attr);
            if (invalidAssigned && attr.WarnIfAssignedValueInvalid)
                h += 2f + EditorGUIUtility.singleLineHeight * 1.5f;

            if (attr.Inline && InlineEditorApi.TryGetSerializedObject(property, out var so, out var target))
            {
                h += EditorGUIUtility.singleLineHeight;

                if (GetInlineExpanded(property))
                {
                    h += EditorGUIUtility.standardVerticalSpacing;
                    h += InlineEditorApi.CalculateInlineHeight(so, target);
                }
            }

            return h;
        }

        private Type GetTargetFieldType(SerializedProperty property)
        {
            var fi = fieldInfo != null ? fieldInfo : InlineEditorApi.GetFieldInfoFromProperty(property);
            if (fi != null)
            {
                if (fi.FieldType.IsArray)
                    return fi.FieldType.GetElementType();
                if (fi.FieldType.IsGenericType && fi.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    return fi.FieldType.GetGenericArguments()[0];
                return fi.FieldType;
            }
            return typeof(UnityEngine.Object);
        }

        private static string InlineKey(SerializedProperty p)
            => $"{p.serializedObject.targetObject.GetInstanceID()}:{p.propertyPath}:find_inline";

        private static bool GetInlineExpanded(SerializedProperty p)
        {
            string key = InlineKey(p);
            if (InlineFoldouts.TryGetValue(key, out bool v)) return v;

            InlineFoldouts[key] = false;
            return false;
        }

        private static void SetInlineExpanded(SerializedProperty p, bool value)
            => InlineFoldouts[InlineKey(p)] = value;

        private static void DrawReadOnlyObjectLikeField(Rect rect, UnityEngine.Object value)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.objectField);

            var iconRect = new Rect(rect.x + 2f, rect.y + 1f, rect.height - 2f, rect.height - 2f);
            var textRect = new Rect(iconRect.xMax + 2f, rect.y, rect.width - iconRect.width - 6f, rect.height);

            Texture icon = null;
            string text;

            if (value == null)
            {
                text = "Yok (Seçilmedi)";
            }
            else
            {
                icon = AssetPreview.GetMiniThumbnail(value);
                text = $"{value.name} ({value.GetType().Name})";
            }

            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            EditorGUI.LabelField(textRect, text, EditorStyles.label);
        }

        private static void HandleFocusClick(Rect fieldRect, UnityEngine.Object current)
        {
            if (current == null) return;

            var e = Event.current;
            if (e.type != EventType.MouseDown) return;
            if (e.button != 0) return;
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
                }
                else if (current is GameObject go)
                {
                    Selection.activeObject = go;
                    EditorGUIUtility.PingObject(go);
                    SceneView.lastActiveSceneView?.FrameSelected();
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

        private void HandleDragAndDrop(Rect dropRect, SerializedProperty property, Type fieldType, FindAttribute attr)
        {
            var e = Event.current;
            if (!dropRect.Contains(e.mousePosition)) return;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length == 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            UnityEngine.Object accepted = null;
            foreach (var dragged in refs)
            {
                var normalized = NormalizeForField(dragged, fieldType, attr.RequiredType);
                if (normalized != null && IsValid(normalized, attr))
                {
                    accepted = normalized;
                    break;
                }
            }

            DragAndDrop.visualMode = accepted != null
                ? DragAndDropVisualMode.Link
                : DragAndDropVisualMode.Rejected;

            if (e.type == EventType.DragPerform)
            {
                if (accepted != null)
                {
                    DragAndDrop.AcceptDrag();
                    property.serializedObject.Update();
                    property.objectReferenceValue = accepted;
                    property.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }

                e.Use();
            }
            else
            {
                e.Use();
            }
        }

        private static bool IsValid(UnityEngine.Object obj, FindAttribute attr)
        {
            if (obj == null) return false;
            return PassesSourceFilter(obj, attr.Source) && MatchesRequiredType(obj, attr.RequiredType);
        }

        public static UnityEngine.Object NormalizeForField(UnityEngine.Object picked, Type fieldType, Type requiredType)
        {
            if (picked == null) return null;

            GameObject go = picked as GameObject ?? (picked as Component)?.gameObject;

            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                if (go == null) return null;
                var comp = go.GetComponent(fieldType);
                if (comp == null) return null;

                if (requiredType == null || requiredType.IsAssignableFrom(comp.GetType()))
                    return comp;

                return null;
            }

            if (fieldType == typeof(GameObject))
            {
                if (go == null) return null;

                if (requiredType == null || requiredType.IsAssignableFrom(typeof(GameObject)))
                    return go;

                return go.GetComponent(requiredType) != null ? go : null;
            }

            if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
            {
                if (picked is ScriptableObject so && fieldType.IsAssignableFrom(so.GetType()))
                {
                    if (requiredType == null || requiredType.IsAssignableFrom(so.GetType()))
                        return so;
                }
                return null;
            }

            if (fieldType == typeof(UnityEngine.Object))
            {
                if (requiredType != null && (typeof(Component).IsAssignableFrom(requiredType) || requiredType.IsInterface))
                {
                    if (go != null)
                    {
                        var comp = go.GetComponent(requiredType);
                        if (comp != null) return comp;
                    }
                }

                if (requiredType == null || requiredType.IsAssignableFrom(picked.GetType()))
                    return picked;
            }

            return null;
        }

        private static bool MatchesRequiredType(UnityEngine.Object obj, Type requiredType)
        {
            if (obj == null) return false;
            if (requiredType == null) return true;

            if (obj is GameObject go)
            {
                if (requiredType.IsAssignableFrom(typeof(GameObject))) return true;
                return go.GetComponent(requiredType) != null;
            }

            return requiredType.IsAssignableFrom(obj.GetType());
        }

        private static bool PassesSourceFilter(UnityEngine.Object obj, FindSource source)
        {
            if (source == FindSource.Auto) return true;
            if (source == FindSource.None) return false;

            bool allowScene = (source & FindSource.Scene) != 0;
            bool allowPrefabs = (source & FindSource.Prefabs) != 0;
            bool allowAssets = (source & FindSource.Assets) != 0;

            bool isPersistent = EditorUtility.IsPersistent(obj);
            if (!isPersistent) return allowScene;

            string path = AssetDatabase.GetAssetPath(obj);
            var prefabType = PrefabUtility.GetPrefabAssetType(obj);
            bool isPrefabAsset = prefabType != PrefabAssetType.NotAPrefab ||
                                 path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

            if (isPrefabAsset) return allowPrefabs;
            return allowAssets;
        }
    }
}
#endif
