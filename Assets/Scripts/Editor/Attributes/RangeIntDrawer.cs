using UnityEditor;
using UnityEngine;

namespace Editor.Attributes
{
    [CustomPropertyDrawer(typeof(RangeInt))]
    public class RangeIntDrawer : PropertyDrawer
    {
        private readonly GUIContent[] _subLabels = {new("Min"), new("Max")};
        private readonly int[] _range = {0, 1};

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var minValueProperty = property.FindPropertyRelative("Min");
            var maxValueProperty = property.FindPropertyRelative("Max");

            _range[0] = minValueProperty.intValue;
            _range[1] = maxValueProperty.intValue;

            EditorGUI.BeginChangeCheck();

            EditorGUI.MultiIntField(position, _subLabels, _range);

            if (EditorGUI.EndChangeCheck())
            {
                minValueProperty.intValue = _range[0];
                maxValueProperty.intValue = _range[1];
            }

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
