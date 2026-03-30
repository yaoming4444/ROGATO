using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteSheetBuilder : EditorWindow
{
    public Texture2D[] inputFrames;
    public int columns = 7;
    public string outputPath = "Assets/BakedFrames/BatWalk_Sheet.png";

    [MenuItem("Tools/Build Sprite Sheet")]
    public static void ShowWindow()
    {
        GetWindow<SpriteSheetBuilder>("Sprite Sheet Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Build Horizontal Sprite Sheet", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(this);
        SerializedProperty framesProp = so.FindProperty("inputFrames");

        EditorGUILayout.PropertyField(framesProp, true);
        so.ApplyModifiedProperties();

        columns = EditorGUILayout.IntField("Columns", columns);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        if (GUILayout.Button("Build Sheet"))
        {
            BuildSheet();
        }
    }

    private void BuildSheet()
    {
        if (inputFrames == null || inputFrames.Length == 0)
        {
            Debug.LogError("No frames assigned.");
            return;
        }

        int frameWidth = inputFrames[0].width;
        int frameHeight = inputFrames[0].height;

        int rows = Mathf.CeilToInt((float)inputFrames.Length / columns);

        Texture2D sheet = new Texture2D(frameWidth * columns, frameHeight * rows, TextureFormat.ARGB32, false);

        for (int i = 0; i < inputFrames.Length; i++)
        {
            int x = i % columns;
            int y = i / columns;

            Color[] pixels = inputFrames[i].GetPixels();
            sheet.SetPixels(x * frameWidth, (rows - 1 - y) * frameHeight, frameWidth, frameHeight, pixels);
        }

        sheet.Apply();

        byte[] png = sheet.EncodeToPNG();
        File.WriteAllBytes(outputPath, png);

        AssetDatabase.Refresh();

        Debug.Log("Sprite sheet created: " + outputPath);
    }
}