using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fyrvall.PreviewObjectPicker
{
    public class PreviewPickerBasePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = GetType(property);
            position = EditorGUI.PrefixLabel(position, label);
            if (GUI.Button(position, GetPropertyValueName(property), GUI.skin.textField)) {
                PreviewSelectorEditor.ShowAuxWindow(type, property);
            }
        }

        private GUIContent GetPropertyValueName(SerializedProperty serializedProperty)
        {
            if (serializedProperty.objectReferenceValue == null) {
                return new GUIContent("None");
            } else {
                return new GUIContent(serializedProperty.objectReferenceValue.name);
            }
        }

        private System.Type GetType(SerializedProperty property)
        {
            System.Type parentType = property.serializedObject.targetObject.GetType();
            System.Reflection.FieldInfo fi = parentType.GetField(property.propertyPath);
            return fi.FieldType;
        }
    }
}