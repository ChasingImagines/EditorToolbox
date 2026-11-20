#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// Custom property drawer for [Inline] attribute.
    /// Draws a foldout arrow next to <see cref="ScriptableObject"/> fields and displays their serialized properties in-place.
    /// </summary>
    [InitializeOnLoad]
    [CustomPropertyDrawer(typeof(InlineAttribute))]
    public sealed class InlinePropertyDrawer : PropertyDrawer
    {
        private const float FoldoutWidth = 14f;
        private static readonly Dictionary<string, bool> Foldouts = new Dictionary<string, bool>();

        static InlinePropertyDrawer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Foldouts.Clear;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return EditorGUI.GetPropertyHeight(property, label, true);

            float height = EditorGUIUtility.singleLineHeight;

            if (!InlineEditorApi.TryGetInlineTarget(property, out var target))
                return height;

            var attr = (InlineAttribute)attribute;
            if (!GetFoldoutState(property, attr.StartExpanded))
                return height;

            height += EditorGUIUtility.standardVerticalSpacing;
            height += InlineEditorApi.CalculateInlineHeight(property, target, attr.MaxDepth);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.BeginProperty(position, label, property);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (!InlineEditorApi.TryGetInlineTarget(property, out var target))
            {
                EditorGUI.PropertyField(line, property, label);
                EditorGUI.EndProperty();
                return;
            }

            var attr = (InlineAttribute)attribute;
            bool expanded = GetFoldoutState(property, attr.StartExpanded);

            if (line.x >= FoldoutWidth)
            {
                var foldRect = new Rect(line.x - FoldoutWidth, line.y, FoldoutWidth, line.height);
                expanded = EditorGUI.Foldout(foldRect, expanded, GUIContent.none, toggleOnLabelClick: false);
                SetFoldoutState(property, expanded);
                EditorGUI.PropertyField(line, property, label);
            }
            else
            {
                var foldRect = new Rect(line.x, line.y, FoldoutWidth, line.height);
                var propertyRect = new Rect(line.x + FoldoutWidth, line.y, Mathf.Max(0f, line.width - FoldoutWidth), line.height);
                expanded = EditorGUI.Foldout(foldRect, expanded, GUIContent.none, toggleOnLabelClick: false);
                SetFoldoutState(property, expanded);
                EditorGUI.PropertyField(propertyRect, property, label);
            }

            if (expanded)
            {
                EditorGUI.indentLevel++;

                float contentY = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                float contentHeight = Mathf.Max(0f, position.yMax - contentY);
                var contentRect = new Rect(position.x, contentY, position.width, contentHeight);

                InlineEditorApi.DrawInlineFields(contentRect, property, target, attr.MaxDepth);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static string Key(SerializedProperty property)
        {
            var obj = property.serializedObject?.targetObject;
            int id = obj != null ? obj.GetInstanceID() : 0;
            return $"{id}:{property.propertyPath}";
        }

        private static bool GetFoldoutState(SerializedProperty property, bool defaultValue)
        {
            string key = Key(property);
            if (Foldouts.TryGetValue(key, out bool value)) return value;

            Foldouts[key] = defaultValue;
            return defaultValue;
        }

        private static void SetFoldoutState(SerializedProperty property, bool value)
        {
            Foldouts[Key(property)] = value;
        }
    }
}
#endif
