using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// Type-safe object reference selector attribute ([Find]).
    /// Provides rich search picker window (FindPickerWindow), drag &amp; drop, component/interface extraction,
    /// and optional in-place editing (Inline) for UnityEngine.Object, ScriptableObject, Prefab, or Component references.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class FindAttribute : PropertyAttribute
    {
        public Type RequiredType { get; }
        public FindSource Source { get; set; } = FindSource.Auto;
        public bool WarnIfAssignedValueInvalid { get; set; } = true;
        public bool Inline { get; set; } = false;

        public FindAttribute(Type requiredType = null)
        {
            RequiredType = requiredType;
        }
    }
}
