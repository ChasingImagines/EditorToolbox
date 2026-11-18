#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// Custom property drawer for [Inline] attribute.
    /// Draws a foldout arrow next to ScriptableObject fields and displays their serialized properties in-place.
    /// </summary>
    [CustomPropertyDrawer(typeof(InlineAttribute))]
    public sealed class InlinePropertyDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> Foldouts = new Dictionary<string, bool>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return height;

            if (!InlineEditorApi.TryGetSerializedObject(property, out var so, out var target))
                return height;

            bool expanded = GetFoldoutState(property, ((InlineAttribute)attribute).StartExpanded);
            if (!expanded) return height;

            height += EditorGUIUtility.standardVerticalSpacing;
            height += InlineEditorApi.CalculateInlineHeight(so, target);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                !InlineEditorApi.TryGetSerializedObject(property, out var so, out var target))
            {
                EditorGUI.PropertyField(line, property, label);
                EditorGUI.EndProperty();
                return;
            }

            float foldoutWidth = 15f;
            var foldRect = new Rect(line.x, line.y, foldoutWidth, line.height);
            var propertyRect = new Rect(line.x + foldoutWidth, line.y, line.width - foldoutWidth, line.height);

            bool expanded = GetFoldoutState(property, ((InlineAttribute)attribute).StartExpanded);

            expanded = EditorGUI.Foldout(foldRect, expanded, GUIContent.none, toggleOnLabelClick: false);
            SetFoldoutState(property, expanded);

            EditorGUI.PropertyField(propertyRect, property, label);

            if (expanded)
            {
                EditorGUI.indentLevel++;

                float contentY = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                float contentHeight = position.height - line.height - EditorGUIUtility.standardVerticalSpacing;

                var contentRect = new Rect(
                    position.x,
                    contentY,
                    position.width,
                    contentHeight
                );

                InlineEditorApi.DrawInlineFields(contentRect, so, target);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static string Key(SerializedProperty property)
            => $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";

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
