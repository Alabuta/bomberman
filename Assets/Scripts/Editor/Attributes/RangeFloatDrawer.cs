using Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace Editor.Attributes
{
    [CustomPropertyDrawer(typeof(RangeFloat))]
    public class RangeFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // var indent = EditorGUI.indentLevel;
            // EditorGUI.indentLevel = 0;

            // var range = attribute as RangeAttribute;

            var minValue = property.FindPropertyRelative("Min").floatValue;
            var maxValue = property.FindPropertyRelative("Max").floatValue;

            EditorGUI.MinMaxSlider(
                position,
                // new Rect(position.x, position.y, position.width, position.height),
                // label,
                ref minValue, ref maxValue,
                0f, 100f
            );

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
