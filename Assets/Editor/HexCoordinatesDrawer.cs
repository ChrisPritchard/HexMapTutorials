
using UnityEngine;
using UnityEditor;
using DarkDomains;

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer: PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position = EditorGUI.PrefixLabel(position, label);
        var cell = (HexCell)property.serializedObject.targetObject;
        GUI.Label(position, cell.Coordinates.ToString());
    }
}