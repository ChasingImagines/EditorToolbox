#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// Helper API for rendering and measuring in-place <see cref="ScriptableObject"/> fields marked with [Inline].
    /// Caches <see cref="SerializedObject"/> instances safely and separates cycle detection from nesting depth limits.
    /// </summary>
    [InitializeOnLoad]
    public static class InlineEditorApi
    {
        public const int DefaultMaxDepth = 4;

        internal const float BoxPadding = 4f;
        internal const float WarningBoxHeight = 24f;

        internal enum InlineGuardState
        {
            Allowed,
            CircularReference,
            MaxDepthReached
        }

        private static readonly Dictionary<int, SerializedObject> SerializedObjectCache = new Dictionary<int, SerializedObject>();
        private static readonly Dictionary<(Type, string), FieldInfo> FieldInfoCache = new Dictionary<(Type, string), FieldInfo>();
        private static readonly HashSet<int> ActiveChain = new HashSet<int>();

        private static int _activeDepth;
        private static bool _pendingCacheClear;

        static InlineEditorApi()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
            Selection.selectionChanged += ClearCache;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange _) => ClearCache();

        /// <summary>
        /// Disposes all cached <see cref="SerializedObject"/> instances. Deferred automatically if called mid-draw.
        /// </summary>
        public static void ClearCache()
        {
            if (_activeDepth > 0)
            {
                _pendingCacheClear = true;
                return;
            }

            FlushCache();
        }

        private static void FlushCache()
        {
            _pendingCacheClear = false;
            foreach (var kvp in SerializedObjectCache)
            {
                kvp.Value?.Dispose();
            }
            SerializedObjectCache.Clear();
        }

        internal static int CachedObjectCount => SerializedObjectCache.Count;

        internal static SerializedObject GetOrCreateSerializedObject(ScriptableObject target)
        {
            if (target == null) return null;

            int id = target.GetInstanceID();
            if (SerializedObjectCache.TryGetValue(id, out var cached))
            {
                if (cached != null && cached.targetObject != null && ReferenceEquals(cached.targetObject, target))
                    return cached;

                cached?.Dispose();
                SerializedObjectCache.Remove(id);
            }

            var created = new SerializedObject(target);
            SerializedObjectCache[id] = created;
            return created;
        }

        /// <summary>
        /// Extracts a non-null, single-valued <see cref="ScriptableObject"/> target suitable for inline editing.
        /// Rejects non-ScriptableObjects and multi-object mixed values.
        /// </summary>
        public static bool TryGetInlineTarget(SerializedProperty objectRefProperty, out ScriptableObject target)
        {
            target = null;

            if (objectRefProperty == null) return false;
            if (objectRefProperty.propertyType != SerializedPropertyType.ObjectReference) return false;
            if (objectRefProperty.hasMultipleDifferentValues) return false;
            if (!(objectRefProperty.objectReferenceValue is ScriptableObject so) || so == null) return false;

            target = so;
            return true;
        }

        public static bool IsIgnored(SerializedProperty property) => HasAttribute<InlineIgnoreAttribute>(property);
        public static bool IsReadOnly(SerializedProperty property) => HasAttribute<InlineReadOnlyAttribute>(property);

        public static bool HasAttribute<T>(SerializedProperty property) where T : Attribute
        {
            if (property == null) return false;
            var fieldInfo = GetFieldInfoFromProperty(property);
            return fieldInfo != null && Attribute.IsDefined(fieldInfo, typeof(T), true);
        }

        public static float CalculateInlineHeight(SerializedProperty hostProperty, ScriptableObject target, int maxDepth = DefaultMaxDepth)
        {
            if (target == null) return 0f;

            var guard = EnterGuard(hostProperty, target, maxDepth, out int hostId, out bool addedHost, out int targetId, out bool addedTarget);
            try
            {
                if (guard != InlineGuardState.Allowed)
                    return WarningBoxHeight;

                var so = GetOrCreateSerializedObject(target);
                if (so == null) return 0f;

                so.UpdateIfRequiredOrScript();

                float total = 0f;
                int visibleCount = 0;
                var iterator = so.GetIterator();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.name == "m_Script") continue;
                    if (IsIgnored(iterator)) continue;

                    if (visibleCount > 0)
                        total += EditorGUIUtility.standardVerticalSpacing;

                    total += EditorGUI.GetPropertyHeight(iterator, true);
                    visibleCount++;
                }

                if (visibleCount == 0)
                    return BoxPadding * 2f + EditorGUIUtility.singleLineHeight;

                return BoxPadding * 2f + total;
            }
            finally
            {
                ExitGuard(hostId, addedHost, targetId, addedTarget);
            }
        }

        public static void DrawInlineFields(Rect rect, SerializedProperty hostProperty, ScriptableObject target, int maxDepth = DefaultMaxDepth)
        {
            if (target == null) return;

            int effectiveMaxDepth = Mathf.Max(1, maxDepth);
            var guard = EnterGuard(hostProperty, target, effectiveMaxDepth, out int hostId, out bool addedHost, out int targetId, out bool addedTarget);
            try
            {
                if (guard != InlineGuardState.Allowed)
                {
                    string message = guard == InlineGuardState.CircularReference
                        ? "Circular reference detected."
                        : $"Max inline nesting depth ({effectiveMaxDepth}) reached.";

                    Rect indentedWarn = EditorGUI.IndentedRect(new Rect(rect.x, rect.y, rect.width, WarningBoxHeight));
                    EditorGUI.HelpBox(indentedWarn, message, MessageType.Warning);
                    return;
                }

                var so = GetOrCreateSerializedObject(target);
                if (so == null) return;

                so.UpdateIfRequiredOrScript();

                Rect boxRect = EditorGUI.IndentedRect(rect);
                GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

                float currentY = rect.y + BoxPadding;
                int visibleCount = 0;
                var iterator = so.GetIterator();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.name == "m_Script") continue;
                    if (IsIgnored(iterator)) continue;

                    float height = EditorGUI.GetPropertyHeight(iterator, true);
                    var propertyRect = new Rect(
                        rect.x + BoxPadding,
                        currentY,
                        Mathf.Max(0f, rect.width - BoxPadding * 2f),
                        height
                    );

                    bool readOnly = IsReadOnly(iterator);
                    using (new EditorGUI.DisabledScope(readOnly))
                    {
                        EditorGUI.PropertyField(propertyRect, iterator, true);
                    }

                    currentY += height + EditorGUIUtility.standardVerticalSpacing;
                    visibleCount++;
                }

                if (visibleCount == 0)
                {
                    var emptyRect = new Rect(
                        boxRect.x + BoxPadding,
                        rect.y + BoxPadding,
                        Mathf.Max(0f, boxRect.width - BoxPadding * 2f),
                        EditorGUIUtility.singleLineHeight
                    );
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUI.Label(emptyRect, "(No serialized fields)", EditorStyles.miniLabel);
                    }
                }

                if (target != null)
                    so.ApplyModifiedProperties();
            }
            finally
            {
                ExitGuard(hostId, addedHost, targetId, addedTarget);
            }
        }

        internal static InlineGuardState EvaluateGuardForTest(SerializedProperty hostProperty, ScriptableObject target, int maxDepth, Action whileInside = null)
        {
            var state = EnterGuard(hostProperty, target, maxDepth, out int hostId, out bool addedHost, out int targetId, out bool addedTarget);
            try
            {
                if (state == InlineGuardState.Allowed)
                    whileInside?.Invoke();

                return state;
            }
            finally
            {
                ExitGuard(hostId, addedHost, targetId, addedTarget);
            }
        }

        private static InlineGuardState EnterGuard(
            SerializedProperty hostProperty,
            ScriptableObject target,
            int maxDepth,
            out int hostId,
            out bool addedHost,
            out int targetId,
            out bool addedTarget)
        {
            int effectiveMaxDepth = Mathf.Max(1, maxDepth);

            hostId = 0;
            addedHost = false;
            var hostObj = hostProperty?.serializedObject?.targetObject;
            if (hostObj != null)
            {
                hostId = hostObj.GetInstanceID();
                addedHost = ActiveChain.Add(hostId);
            }

            targetId = target.GetInstanceID();
            addedTarget = false;

            if (ActiveChain.Contains(targetId))
                return InlineGuardState.CircularReference;

            if (_activeDepth >= effectiveMaxDepth)
                return InlineGuardState.MaxDepthReached;

            addedTarget = ActiveChain.Add(targetId);
            _activeDepth++;
            return InlineGuardState.Allowed;
        }

        private static void ExitGuard(int hostId, bool addedHost, int targetId, bool addedTarget)
        {
            if (addedTarget)
            {
                _activeDepth = Mathf.Max(0, _activeDepth - 1);
                ActiveChain.Remove(targetId);
            }

            if (addedHost)
                ActiveChain.Remove(hostId);

            if (_pendingCacheClear && _activeDepth == 0)
                FlushCache();
        }

        public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property)
        {
            if (property == null || property.serializedObject == null || property.serializedObject.targetObject == null)
                return null;

            Type parentType = property.serializedObject.targetObject.GetType();
            string normalizedPath = NormalizePropertyPath(property.propertyPath);
            if (string.IsNullOrEmpty(normalizedPath))
                return null;

            var cacheKey = (parentType, normalizedPath);
            if (FieldInfoCache.TryGetValue(cacheKey, out var cached))
                return cached;

            FieldInfo resolved = ResolveFieldInfo(parentType, normalizedPath);
            FieldInfoCache[cacheKey] = resolved;
            return resolved;
        }

        internal static string NormalizePropertyPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath)) return string.Empty;

            const string arrayToken = ".Array.data[";
            int firstToken = propertyPath.IndexOf(arrayToken, StringComparison.Ordinal);
            if (firstToken < 0) return propertyPath;

            var sb = new StringBuilder(propertyPath.Length);
            int i = 0;
            while (i < propertyPath.Length)
            {
                int idx = propertyPath.IndexOf(arrayToken, i, StringComparison.Ordinal);
                if (idx < 0)
                {
                    sb.Append(propertyPath, i, propertyPath.Length - i);
                    break;
                }

                sb.Append(propertyPath, i, idx - i);
                sb.Append("[]");

                int closeIdx = propertyPath.IndexOf(']', idx + arrayToken.Length);
                if (closeIdx < 0)
                    break;

                i = closeIdx + 1;
            }

            return sb.ToString();
        }

        private static FieldInfo ResolveFieldInfo(Type parentType, string normalizedPath)
        {
            string[] parts = normalizedPath.Split('.');
            FieldInfo currentField = null;
            Type currentType = parentType;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                int bracketIdx = part.IndexOf('[');
                if (bracketIdx >= 0)
                {
                    string fieldName = part.Substring(0, bracketIdx);
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
            if (type == null) return null;
            if (type.IsArray) return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var args = type.GetGenericArguments();
                if (args.Length > 0) return args[0];
            }

            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var iface = interfaces[i];
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var args = iface.GetGenericArguments();
                    if (args.Length > 0) return args[0];
                }
            }

            return type;
        }
    }
}
#endif
