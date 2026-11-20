using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// Type-safe object reference selector attribute ([Find]).
    /// Provides a searchable picker window, drag-and-drop extraction, ping/focus, and optional in-place editing
    /// for <see cref="UnityEngine.Object"/>, <see cref="ScriptableObject"/>, Prefab, or <see cref="Component"/> references.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class FindAttribute : PropertyAttribute
    {
        public Type RequiredType { get; }
        public FindSource Source { get; set; } = FindSource.Auto;
        public bool WarnIfAssignedValueInvalid { get; set; } = true;
        public bool Inline { get; set; } = false;
        public bool StartInlineExpanded { get; set; } = false;

        public FindAttribute(Type requiredType = null)
        {
            RequiredType = requiredType;
        }
    }
}
