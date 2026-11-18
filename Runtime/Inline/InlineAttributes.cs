using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// ScriptableObject referansını Inspector'da ayrı pencereye gitmeden
    /// yerinde (in-place) katlanabilir alan olarak düzenlemek için kullanılır ([Inline]).
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
    /// [Inline] çiziminde sadece okunur (disabled) gösterilecek alanları işaretler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InlineReadOnlyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// [Inline] çiziminde Inspector'da gizlenecek alanları işaretler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InlineIgnoreAttribute : PropertyAttribute
    {
    }
}
