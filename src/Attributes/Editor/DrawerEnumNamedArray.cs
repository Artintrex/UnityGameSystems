using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EnumNamedArrayAttribute))]
public class DrawerEnumNamedArray : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EnumNamedArrayAttribute enumNames = attribute as EnumNamedArrayAttribute;
        
        int index = System.Convert.ToInt32(property.propertyPath.Substring(property.propertyPath.IndexOf("[")).Replace("[", "").Replace("]", ""));
        //Get label name
        label.text = enumNames.names[index];
        //Draw Property
        EditorGUI.PropertyField(position, property, label, true );
        //If expanded draw child properties
        if (property.isExpanded)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            position.xMin += EditorGUIUtility.labelWidth;
        }
    }
}