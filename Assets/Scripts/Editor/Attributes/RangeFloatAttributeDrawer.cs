using System.Globalization;
using Core.Attributes;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Editor.Attributes
{
    [CustomPropertyDrawer(typeof(RangeFloatAttribute))]
    public class RangeFloatAttributeDrawer : PropertyDrawer
    {
        private const int FloatLabelWidth = 30;
        private const int IndentLevel = 5;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var range = attribute as RangeFloatAttribute;

            var minLimit = range?.Min ?? 0f;
            var maxLimit = range?.Max ?? 1f;

            var minValueProperty = property.FindPropertyRelative("Min");
            var maxValueProperty = property.FindPropertyRelative("Max");

            var minValue = math.clamp(minValueProperty.floatValue, minLimit, maxLimit);
            var maxValue = math.clamp(maxValueProperty.floatValue, minLimit, maxLimit);

            EditorGUI.BeginProperty(position, label, minValueProperty);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var minValueRect = new Rect(position.x, position.y, FloatLabelWidth, position.height);
            var sliderRect = new Rect(
                position.x + FloatLabelWidth + IndentLevel, position.y, position.width - 2 * FloatLabelWidth - 2 * IndentLevel,
                position.height
            );
            var maxValueRect = new Rect(
                position.x + position.width - FloatLabelWidth, position.y, position.width - FloatLabelWidth, position.height
            );

            EditorGUI.LabelField(minValueRect, minValue.ToString("G2", CultureInfo.InvariantCulture));

            EditorGUI.MinMaxSlider(
                sliderRect,
                ref minValue, ref maxValue,
                minLimit, maxLimit
            );

            EditorGUI.LabelField(maxValueRect, maxValue.ToString("G2", CultureInfo.InvariantCulture));

            minValueProperty.floatValue = minValue;
            maxValueProperty.floatValue = maxValue;

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
