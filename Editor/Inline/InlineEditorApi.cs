#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// Helper API for rendering and calculating dimensions of in-place ScriptableObject fields marked with [Inline].
    /// Features circular reference (recursion) protection.
    /// </summary>
    public static class InlineEditorApi
    {
        private static readonly HashSet<int> VisitedObjects = new HashSet<int>();

        public static bool TryGetSerializedObject(SerializedProperty objectRefProperty, out SerializedObject serializedObject, out UnityEngine.Object target)
        {
            serializedObject = null;
            target = null;

            if (objectRefProperty == null) return false;
            if (objectRefProperty.propertyType != SerializedPropertyType.ObjectReference) return false;
            if (objectRefProperty.objectReferenceValue == null) return false;

            target = objectRefProperty.objectReferenceValue;
            serializedObject = new SerializedObject(target);
            return true;
        }

        public static bool IsIgnored(SerializedProperty property) => HasAttribute<InlineIgnoreAttribute>(property);
        public static bool IsReadOnly(SerializedProperty property) => HasAttribute<InlineReadOnlyAttribute>(property);

        public static bool HasAttribute<T>(SerializedProperty property) where T : Attribute
        {
            if (property == null) return false;
            var fieldInfo = GetFieldInfoFromProperty(property);
            if (fieldInfo == null) return false;

            return Attribute.IsDefined(fieldInfo, typeof(T), true);
        }

        public static float CalculateInlineHeight(SerializedObject so, UnityEngine.Object target)
        {
            if (so == null || target == null) return 0f;

            int targetId = target.GetInstanceID();
            if (VisitedObjects.Contains(targetId) || VisitedObjects.Count >= 2)
            {
                return EditorGUIUtility.singleLineHeight; // Circular reference warning height
            }

            VisitedObjects.Add(targetId);
            try
            {
                float total = 0f;
                var iterator = so.GetIterator();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.name == "m_Script") continue;
                    if (IsIgnored(iterator)) continue;

                    if (HasHeaderAttribute(iterator, out _))
                    {
                        total += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    total += EditorGUI.GetPropertyHeight(iterator, true);
                    total += EditorGUIUtility.standardVerticalSpacing;
                }

                return total;
            }
            finally
            {
                VisitedObjects.Remove(targetId);
            }
        }

        public static void DrawInlineFields(Rect rect, SerializedObject so, UnityEngine.Object target)
        {
            if (so == null || target == null) return;

            int targetId = target.GetInstanceID();
            if (VisitedObjects.Contains(targetId) || VisitedObjects.Count >= 2)
            {
                EditorGUI.HelpBox(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "[Circular Reference Prevented]", MessageType.Info);
                return;
            }

            VisitedObjects.Add(targetId);
            try
            {
                so.Update();

                var iterator = so.GetIterator();
                bool enterChildren = true;
                float currentY = rect.y;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.name == "m_Script") continue;
                    if (IsIgnored(iterator)) continue;

                    if (HasHeaderAttribute(iterator, out string headerText))
                    {
                        var headerRect = new Rect(rect.x, currentY, rect.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.LabelField(headerRect, headerText, EditorStyles.boldLabel);
                        currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    float height = EditorGUI.GetPropertyHeight(iterator, true);
                    var propertyRect = new Rect(rect.x, currentY, rect.width, height);

                    bool readOnly = IsReadOnly(iterator);
                    if (readOnly)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.PropertyField(propertyRect, iterator, true);
                        }
                    }
                    else
                    {
                        EditorGUI.PropertyField(propertyRect, iterator, true);
                    }

                    currentY += height + EditorGUIUtility.standardVerticalSpacing;
                }

                so.ApplyModifiedProperties();
            }
            finally
            {
                VisitedObjects.Remove(targetId);
            }
        }

        private static bool HasHeaderAttribute(SerializedProperty property, out string headerText)
        {
            headerText = string.Empty;
            if (property == null) return false;

            var fieldInfo = GetFieldInfoFromProperty(property);
            if (fieldInfo == null) return false;

            var attr = fieldInfo.GetCustomAttribute<HeaderAttribute>(true);
            if (attr != null)
            {
                headerText = attr.header;
                return true;
            }

            return false;
        }

        public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property)
        {
            if (property == null) return null;

            Type parentType = property.serializedObject.targetObject.GetType();
            string[] parts = property.propertyPath.Replace(".Array.data[", "[").Split('.');

            FieldInfo currentField = null;
            Type currentType = parentType;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Contains("["))
                {
                    string fieldName = part.Substring(0, part.IndexOf('['));
                    currentField = GetFieldFromTypeHierarchy(currentType, fieldName);
                    if (currentField == null) return null;

                    currentType = GetElementOrGenericType(currentField.FieldType);
                }
                else
                {
                    currentField = GetFieldFromTypeHierarchy(currentType, part);
                    if (currentField == null) return null;

                    currentType = currentField.FieldType;
                }
            }

            return currentField;
        }

        private static FieldInfo GetFieldFromTypeHierarchy(Type type, string fieldName)
        {
            while (type != null && type != typeof(object))
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

        private static Type GetElementOrGenericType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                var args = type.GetGenericArguments();
                if (args.Length > 0) return args[0];
            }
            return type;
        }
    }
}
#endif
