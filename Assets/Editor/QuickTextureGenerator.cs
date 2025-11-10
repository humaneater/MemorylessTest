using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

public class QuickTextureGenerator
{
    private const int TEXTURE_COUNT = 10000;
    private const int RESOLUTION = 32;
    private const string FOLDER_NAME = "RandomTextures";
    
    [MenuItem("Assets/GenerateSomeTextures")]
    private static void GenerateTexturesQuick()
    {
        if (EditorUtility.DisplayDialog("confirm", 
            $"will generate {TEXTURE_COUNT}  {RESOLUTION}x{RESOLUTION} Random textures at Path: Resources/{FOLDER_NAME} \n\n that will cost sometime，sure to continue？", 
            "generate", "cancel"))
        {
            EditorCoroutine.Start(GenerateAllTexturesCoroutine());
        }
    }
    
    private static IEnumerator GenerateAllTexturesCoroutine()
    {
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        string folderPath = Path.Combine(resourcesPath, FOLDER_NAME);
        
        // 创建目录
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        int completed = 0;
        
        for (int i = 0; i < TEXTURE_COUNT; i++)
        {
            // 生成纹理
            Texture2D texture = new Texture2D(RESOLUTION, RESOLUTION, TextureFormat.RGBA32, true);
            
            Color[] pixels = new Color[RESOLUTION * RESOLUTION];
            System.Random random = new System.Random(i);
            
            for (int j = 0; j < pixels.Length; j++)
            {
                pixels[j] = new Color(
                    (float)random.NextDouble(),
                    (float)random.NextDouble(), 
                    (float)random.NextDouble(),
                    1.0f
                );
            }
            
            texture.SetPixels(pixels);
            texture.Apply(true);
            
            // 保存文件
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(folderPath, $"{i}.png"), pngData);
            
            GameObject.DestroyImmediate(texture);
            
            completed++;
            
            // 每生成10张更新一次进度
            if (completed % 10 == 0)
            {
                float progress = (float)completed / TEXTURE_COUNT;
                if (EditorUtility.DisplayCancelableProgressBar("Generate Random Textures", 
                    $"Creating... {completed}/{TEXTURE_COUNT}", progress))
                {
                    // 用户取消了生成
                    break;
                }
                
                yield return null; // 等待一帧
            }
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("finish", 
            $"success Create {completed} Textures at Resources/{FOLDER_NAME} ！", "confirm");
    }
}

// 简单的编辑器协程辅助类
public static class EditorCoroutine
{
    public static void Start(IEnumerator routine)
    {
        EditorApplication.CallbackFunction updateCallback = null;
        updateCallback = () =>
        {
            if (!routine.MoveNext())
            {
                EditorApplication.update -= updateCallback;
            }
        };
        EditorApplication.update += updateCallback;
    }
}