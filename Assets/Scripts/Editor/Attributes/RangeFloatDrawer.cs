using Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace Editor.Attributes
{
    [CustomPropertyDrawer(typeof(RangeFloat))]
    public class RangeFloatDrawer : PropertyDrawer
    {
        private readonly GUIContent[] _subLabels = {new GUIContent("Min"), new GUIContent("Max")};
        private readonly float[] _range = {0f, 1f};

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var minValueProperty = property.FindPropertyRelative("Min");
            var maxValueProperty = property.FindPropertyRelative("Max");

            _range[0] = minValueProperty.floatValue;
            _range[1] = maxValueProperty.floatValue;

            EditorGUI.BeginChangeCheck();

            EditorGUI.MultiFloatField(position, _subLabels, _range);

            if (EditorGUI.EndChangeCheck())
            {
                minValueProperty.floatValue = _range[0];
                maxValueProperty.floatValue = _range[1];
            }

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
