using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(MapConfiguration)), CanEditMultipleObjects]
public class ConfigEditor : Editor {

    private SerializedProperty _paper;
    private SerializedProperty _dim_x; 
    private SerializedProperty _dim_y; 
    private SerializedProperty _map;
    private void OnEnable()
    {
        _paper = serializedObject.FindProperty("paper");    
        _dim_x = serializedObject.FindProperty("REAL_MAP_DIMENSION_X");
        _dim_y = serializedObject.FindProperty("REAL_MAP_DIMENSION_Y");
        _map = serializedObject.FindProperty("map");
        
    }
    public override void OnInspectorGUI()
    {
        MapConfiguration script = (MapConfiguration) target; 
        serializedObject.Update();
        EditorGUILayout.PropertyField(_map);
        EditorGUILayout.PropertyField(_paper);
        
        if(_paper.enumValueIndex == (int) PaperSize.Custom)
        {
            EditorGUILayout.PropertyField(_dim_x);
            EditorGUILayout.PropertyField(_dim_y);
        }
        if(GUILayout.Button("Update"))
        {     
            script.UpdateParameters();
        }
        serializedObject.ApplyModifiedProperties();
    }
}