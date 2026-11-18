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
    /// [Inline] niteliğiyle işaretlenen ScriptableObject alanlarını yerinde (in-place)
    /// çizmek ve yüksekliğini hesaplamak için yardımcı API.
    /// Döngüsel referans (recursion) korumalıdır.
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
                return EditorGUIUtility.singleLineHeight; // Döngüsel koruma uyarısı satırı
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
                EditorGUI.HelpBox(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "[Döngüsel Referans Engellendi]", MessageType.Info);
                return;
            }

            VisitedObjects.Add(targetId);
            try
            {
                so.Update();

                var iterator = so.GetIterator();
                bool enterChildren = true;
                float y = rect.y;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.name == "m_Script") continue;
                    if (IsIgnored(iterator)) continue;

                    if (HasHeaderAttribute(iterator, out string headerText))
                    {
                        var headerRect = new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.LabelField(headerRect, headerText, EditorStyles.boldLabel);
                        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    var row = new Rect(rect.x, y, rect.width, h);

                    bool readOnly = IsReadOnly(iterator);
                    using (new EditorGUI.DisabledScope(readOnly))
                    {
                        EditorGUI.PropertyField(row, iterator, new GUIContent(iterator.displayName), true);
                    }

                    y += h + EditorGUIUtility.standardVerticalSpacing;
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
            var fieldInfo = GetFieldInfoFromProperty(property);
            if (fieldInfo == null) return false;

            var headerAttr = (HeaderAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(HeaderAttribute));
            if (headerAttr != null)
            {
                headerText = headerAttr.header;
                return true;
            }
            return false;
        }

        public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property)
        {
            if (property == null) return null;

            object currentObject = property.serializedObject.targetObject;
            if (currentObject == null) return null;

            Type currentType = currentObject.GetType();
            FieldInfo fieldInfo = null;

            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf('['));
                    fieldInfo = GetFieldInTypeHierarchy(currentType, elementName);
                    if (fieldInfo == null) return null;

                    if (currentObject != null)
                    {
                        currentObject = fieldInfo.GetValue(currentObject);
                        int index = GetArrayIndex(element);
                        if (currentObject is System.Collections.IList list && index >= 0 && index < list.Count)
                        {
                            currentObject = list[index];
                            currentType = currentObject != null ? currentObject.GetType() : GetElementType(fieldInfo.FieldType);
                        }
                        else
                        {
                            currentType = GetElementType(fieldInfo.FieldType);
                            currentObject = null;
                        }
                    }
                    else
                    {
                        currentType = GetElementType(fieldInfo.FieldType);
                    }
                }
                else
                {
                    fieldInfo = GetFieldInTypeHierarchy(currentType, element);
                    if (fieldInfo == null) return null;

                    if (currentObject != null)
                    {
                        currentObject = fieldInfo.GetValue(currentObject);
                        currentType = currentObject != null ? currentObject.GetType() : fieldInfo.FieldType;
                    }
                    else
                    {
                        currentType = fieldInfo.FieldType;
                    }
                }
            }

            return fieldInfo;
        }

        private static FieldInfo GetFieldInTypeHierarchy(Type type, string fieldName)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            while (type != null && type != typeof(object))
            {
                var field = type.GetField(fieldName, flags);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

        private static int GetArrayIndex(string element)
        {
            int start = element.IndexOf('[');
            int end = element.IndexOf(']');
            if (start != -1 && end != -1 && int.TryParse(element.Substring(start + 1, end - start - 1), out int index))
                return index;
            return -1;
        }

        private static Type GetElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                return type.GetGenericArguments()[0];
            return type;
        }
    }
}
#endif
