using System;
using UnityEngine;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// Inspector'da yalnızca okunabilir (salt okunur / disabled) alan işareti.
    /// Sınıf adı bilerek `ReadOnlyFieldAttribute` olarak adlandırılmıştır
    /// (Unity.Collections.ReadOnlyAttribute ile isim çakışmasını engellemek için).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ReadOnlyFieldAttribute : PropertyAttribute { }
}
