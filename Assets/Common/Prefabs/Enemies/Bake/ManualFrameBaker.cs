//using System.IO;
//using UnityEditor;
//using UnityEngine;

//public class ManualFrameBaker : EditorWindow
//{
//    public Camera bakeCamera;
//    public RenderTexture renderTexture;

//    public string outputFolder = "Assets/BakedFrames/BatWalk";
//    public string filePrefix = "Bat_Walk";
//    public int nextFrameIndex = 0;

//    public bool padTo3Digits = true;

//    [MenuItem("Tools/Manual Frame Baker")]
//    public static void ShowWindow()
//    {
//        GetWindow<ManualFrameBaker>("Manual Frame Baker");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("Manual Frame Baker", EditorStyles.boldLabel);
//        EditorGUILayout.HelpBox(
//            "1. Move the Animation Preview to the pose you want.\n" +
//            "2. Click Save Current Frame.\n" +
//            "3. Repeat for each frame.",
//            MessageType.Info);

//        bakeCamera = (Camera)EditorGUILayout.ObjectField("Bake Camera", bakeCamera, typeof(Camera), true);
//        renderTexture = (RenderTexture)EditorGUILayout.ObjectField("Render Texture", renderTexture, typeof(RenderTexture), false);

//        EditorGUILayout.Space();

//        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
//        filePrefix = EditorGUILayout.TextField("File Prefix", filePrefix);
//        nextFrameIndex = EditorGUILayout.IntField("Next Frame Index", nextFrameIndex);
//        padTo3Digits = EditorGUILayout.Toggle("Pad To 3 Digits", padTo3Digits);

//        EditorGUILayout.Space();

//        if (GUILayout.Button("Save Current Frame", GUILayout.Height(30)))
//        {
//            SaveCurrentFrame();
//        }

//        EditorGUILayout.BeginHorizontal();

//        if (GUILayout.Button("Save And +1", GUILayout.Height(24)))
//        {
//            SaveCurrentFrame();
//            nextFrameIndex++;
//        }

//        if (GUILayout.Button("Reset Index To 0", GUILayout.Height(24)))
//        {
//            nextFrameIndex = 0;
//        }

//        EditorGUILayout.EndHorizontal();
//    }

//    private void SaveCurrentFrame()
//    {
//        if (bakeCamera == null)
//        {
//            Debug.LogError("Bake Camera is not assigned.");
//            return;
//        }

//        if (renderTexture == null)
//        {
//            Debug.LogError("Render Texture is not assigned.");
//            return;
//        }

//        if (!Directory.Exists(outputFolder))
//            Directory.CreateDirectory(outputFolder);

//        bakeCamera.targetTexture = renderTexture;
//        bakeCamera.Render();

//        RenderTexture prev = RenderTexture.active;
//        RenderTexture.active = renderTexture;

//        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
//        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
//        tex.Apply();

//        string indexText = padTo3Digits ? nextFrameIndex.ToString("D3") : nextFrameIndex.ToString();
//        string filePath = Path.Combine(outputFolder, $"{filePrefix}_{indexText}.png");

//        byte[] png = tex.EncodeToPNG();
//        File.WriteAllBytes(filePath, png);

//        RenderTexture.active = prev;
//        DestroyImmediate(tex);

//        AssetDatabase.Refresh();
//        Debug.Log("Saved frame: " + filePath);
//    }
//}