using System.Globalization;
using Core.Attributes;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Editor.Attributes
{
    [CustomPropertyDrawer(typeof(RangeIntAttribute))]
    public class RangeIntAttributeDrawer : PropertyDrawer
    {
        private const int LabelWidth = 30;
        private const int IndentLevel = 5;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var range = attribute as RangeIntAttribute;

            var minLimit = range?.Min ?? 0;
            var maxLimit = range?.Max ?? 1;

            var minValueProperty = property.FindPropertyRelative("Min");
            var maxValueProperty = property.FindPropertyRelative("Max");

            var minValue = (float) math.clamp(minValueProperty.intValue, minLimit, maxLimit);
            var maxValue = (float) math.clamp(maxValueProperty.intValue, minLimit, maxLimit);

            EditorGUI.BeginProperty(position, label, minValueProperty);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var minValueRect = new Rect(position.x, position.y, LabelWidth, position.height);
            var sliderRect = new Rect(
                position.x + LabelWidth + IndentLevel, position.y, position.width - 2 * LabelWidth - 2 * IndentLevel,
                position.height
            );
            var maxValueRect = new Rect(
                position.x + position.width - LabelWidth, position.y, position.width - LabelWidth, position.height
            );

            EditorGUI.LabelField(minValueRect, minValue.ToString("G2", CultureInfo.InvariantCulture));

            EditorGUI.MinMaxSlider(
                sliderRect,
                ref minValue, ref maxValue,
                minLimit, maxLimit
            );

            EditorGUI.LabelField(maxValueRect, maxValue.ToString("G2", CultureInfo.InvariantCulture));

            minValueProperty.intValue = (int) math.round(minValue);
            maxValueProperty.intValue = (int) math.round(maxValue);

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
