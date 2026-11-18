#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Yasin.EditorToolbox;

namespace Yasin.EditorToolbox.Editor
{
    /// <summary>
    /// [ReadOnlyField] alanları Inspector'da gri (düzenlenemez / disabled) olarak çizer.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
    public sealed class ReadOnlyFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
