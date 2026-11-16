using UnityEditor;
using UnityEngine;

// 반드시 Editor 폴더 안에 둘 것 (예: Assets/Scripts/AP/Ground/Editor/TileGridEditor.cs)
[CustomEditor(typeof(TileGrid))]
public class TileGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // tiles, layout, gap 등 기본 필드

        EditorGUILayout.Space();

        var grid = (TileGrid)target;

        if (GUILayout.Button("Build Tiles", GUILayout.Height(30))) grid.Build();
        if (GUILayout.Button("Clear Tiles")) grid.ClearTiles();
    }
}