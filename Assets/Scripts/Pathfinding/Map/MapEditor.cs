#if UNITY_EDITOR 
using UnityEngine;
using System.Collections;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(Map))]
public class MapEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		
		EditorGUILayout.LabelField ("Show Grid");
		EditorGUILayout.LabelField ("Show Open Tiles");
		EditorGUILayout.LabelField ("Show Closed Tiles");
		
		EditorGUILayout.LabelField ("Tile Size");
		EditorGUILayout.LabelField ("Grid Width");
		EditorGUILayout.LabelField ("Grid Length");
		EditorGUILayout.LabelField ("Width Offset");
		EditorGUILayout.LabelField ("Length Offset");
		EditorGUILayout.LabelField ("Max Steepness");
		EditorGUILayout.LabelField ("Block Indent");
		EditorGUILayout.LabelField ("Passable Height");
		
		EditorGUILayout.EndVertical();
		
		EditorGUILayout.BeginVertical();
		
		Map.ShowGrid = EditorGUILayout.Toggle(Map.ShowGrid);
		Map.ShowOpenTiles = EditorGUILayout.Toggle(Map.ShowOpenTiles);
		Map.ShowClosedTiles = EditorGUILayout.Toggle(Map.ShowClosedTiles);
		
		Map.TileSize = EditorGUILayout.FloatField (Map.TileSize);
		Map.Width = EditorGUILayout.IntField(Map.Width);
		Map.Length = EditorGUILayout.IntField (Map.Length);
		Map.WidthOffset = EditorGUILayout.FloatField (Map.WidthOffset);
		Map.LengthOffset = EditorGUILayout.FloatField (Map.LengthOffset);
		Map.MaxSteepness = EditorGUILayout.FloatField (Map.MaxSteepness);
		Map.BlockIndent = EditorGUILayout.IntField (Map.BlockIndent);
		Map.PassableHeight = EditorGUILayout.FloatField (Map.PassableHeight);
		
		
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal ();
		
		if(GUILayout.Button("Evaluate Terrain"))
		{
			Map.Initialise ();
		}
	}
}
#endif