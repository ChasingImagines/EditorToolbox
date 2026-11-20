using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// Displays and edits <see cref="ScriptableObject"/> references in-place within the Inspector ([Inline]).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class InlineAttribute : PropertyAttribute
    {
        /// <summary>
        /// Whether the inline foldout starts expanded the first time it is shown in the Inspector.
        /// </summary>
        public bool StartExpanded { get; }

        /// <summary>
        /// Maximum nested inline expansion depth allowed before halting further recursion. Defaults to 4.
        /// </summary>
        public int MaxDepth { get; set; } = 4;

        public InlineAttribute(bool startExpanded = false)
        {
            StartExpanded = startExpanded;
        }
    }

    /// <summary>
    /// Marks fields that should be rendered as read-only (disabled) inside an [Inline] inspector view.
    /// Does not inherit from <see cref="PropertyAttribute"/> so it can combine freely with other property drawers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class InlineReadOnlyAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks fields that should be hidden inside an [Inline] inspector view.
    /// Does not inherit from <see cref="PropertyAttribute"/> so it can combine freely with other property drawers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class InlineIgnoreAttribute : Attribute
    {
    }
}
