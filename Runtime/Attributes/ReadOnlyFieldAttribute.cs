using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// Displays the field as read-only (disabled) in the Inspector while keeping it serialized.
    /// Named `ReadOnlyFieldAttribute` to prevent naming collisions with `Unity.Collections.ReadOnlyAttribute`.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ReadOnlyFieldAttribute : PropertyAttribute { }
}
