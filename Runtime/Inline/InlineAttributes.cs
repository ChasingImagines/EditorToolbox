using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// Displays and edits ScriptableObject references in-place within the Inspector ([Inline]).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InlineAttribute : PropertyAttribute
    {
        public bool StartExpanded { get; }

        public InlineAttribute(bool startExpanded = false)
        {
            StartExpanded = startExpanded;
        }
    }

    /// <summary>
    /// Marks fields that should be rendered as read-only (disabled) inside [Inline] view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InlineReadOnlyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Marks fields that should be hidden inside [Inline] view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InlineIgnoreAttribute : PropertyAttribute
    {
    }
}
