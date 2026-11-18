using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// Tip-güvenli nesne referansı seçici niteliği ([Find]).
    /// UnityEngine.Object, ScriptableObject, Prefab veya Component referansları için
    /// zengin arama penceresi (FindPickerWindow), drag & drop ve isteğe bağlı yerinde düzenleme (Inline) sunar.
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
