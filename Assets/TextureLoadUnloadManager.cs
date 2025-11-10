using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class TextureLoadUnloadManager : MonoBehaviour
{
    [Header("纹理设置")] public string textureFolder = "RandomTextures";
    public int totalTextureCount = 200;
    public Vector2Int loadCountRange = new Vector2Int(20, 50); // 增加同时加载数量

    [Header("RenderTexture设置")] public int renderTextureWidth = 512;
    public int renderTextureHeight = 512;
    public int renderTextureCount = 10;

    [Header("显示设置")] public Material targetMaterial; // 目标材质球
    public string texturePropertyName = "_BaseMap"; // 纹理属性名


    [Header("调试信息")] [SerializeField] private int currentLoadedTextures = 0;
    [SerializeField] private int currentRenderTextures = 0;
    [SerializeField] private float loadUnloadTime = 0f;
    [SerializeField] private string currentMaterialTexture = "None";

    // 内部状态
    private List<Texture2D> loadedTextures = new List<Texture2D>();
    private List<RenderTexture> renderTextures = new List<RenderTexture>();
    private bool isRunning = false;
    private Coroutine textureSwitchCoroutine;
    private Queue<Texture2D> textureSwitchQueue = new Queue<Texture2D>();


    [Header("立方体矩阵设置")] public int cubeCount = 100;
    public float spacing = 5f;
    public Material cubeMaterial; // 可以设置材质

    [Header("调试信息")] [SerializeField] private List<GameObject> allCubes = new List<GameObject>();
    [SerializeField] private List<Renderer> allRenderers = new List<Renderer>();
    [SerializeField] private int rows;
    [SerializeField] private int columns;

    // 公开属性，方便其他脚本访问
    public List<GameObject> AllCubes => allCubes;
    public List<Renderer> AllRenderers => allRenderers;
    public int CubeCount => allCubes.Count;
    public int Rows => rows;
    public int Columns => columns;

    void Start()
    {
        // 如果没有指定材质，创建一个默认材质
        if (targetMaterial == null)
        {
            CreateDefaultMaterial();
        }


        GenerateCubeMatrix();
    }

    /// <summary>
    /// 创建默认材质
    /// </summary>
    private void CreateDefaultMaterial()
    {
        Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        if (defaultShader == null)
            defaultShader = Shader.Find("Standard");

        if (defaultShader != null)
        {
            targetMaterial = new Material(defaultShader);
            targetMaterial.name = "DynamicTextureMaterial";
            Debug.Log("创建了默认材质: " + targetMaterial.name);
        }
        else
        {
            Debug.LogError("找不到合适的Shader!");
        }
    }


    /// <summary>
    /// 主要的加载卸载循环协程
    /// </summary>
    private void Update()
    {
        if (Time.frameCount % 60 == 0)
        {
            LoadRandomTextures();
            UnloadRandomTextures();
            LoadRandomTextures();
        }
        SetMaterials();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
    }


    /// <summary>
    /// 设置材质纹理
    /// </summary>
    private void SetMaterialTexture(Material material, Texture2D texture)
    {
        if (material != null && texture != null)
        {
            // 尝试不同的纹理属性名
            if (material.HasProperty(texturePropertyName))
            {
                material.SetTexture(texturePropertyName, texture);
            }
            else if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                texturePropertyName = "_MainTex";
            }

            currentMaterialTexture = texture.name;
        }
    }

    private void SetMaterials()
    {
        if (loadedTextures.Count > allRenderers.Count)
        {
            for (int i = 0; i < allRenderers.Count; i++)
            {
                SetMaterialTexture(allRenderers[i].sharedMaterial, loadedTextures[i]);
            }
        }
    }

    /// <summary>
    /// 卸载随机数量的纹理
    /// </summary>
    private void UnloadRandomTextures()
    {
        if (loadedTextures.Count == 0) return;
        textureSwitchQueue.Clear();
        int unloadCount = loadedTextures.Count; //Random.Range((int)(loadedTextures.Count * 0.2f), loadedTextures.Count); // 增加卸载数量

        List<Texture2D> texturesToRemove = new List<Texture2D>();

        for (int i = 0; i < unloadCount; i++)
        {
            Texture2D textureToUnload = loadedTextures[i];

            if (textureToUnload != null)
            {
                Resources.UnloadAsset(textureToUnload);
                texturesToRemove.Add(textureToUnload);
            }
        }

        // 从列表中移除
        foreach (Texture2D texture in texturesToRemove)
        {
            loadedTextures.Remove(texture);
        }

        currentLoadedTextures = loadedTextures.Count;
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    /// <summary>
    /// 加载随机数量的纹理
    /// </summary>
    private void LoadRandomTextures()
    {
        int loadCount = Random.Range(loadCountRange.x, loadCountRange.y + 1);
        List<Texture2D> newlyLoaded = new List<Texture2D>();

        for (int i = 0; i < loadCount; i++)
        {
            int textureId = Random.Range(0, totalTextureCount);
            string texturePath = $"{textureFolder}/{textureId}";
            ResourceRequest request = Resources.LoadAsync<Texture2D>(texturePath);
            request.completed += (o) =>
            {
                if (request.asset != null && !loadedTextures.Contains(request.asset))
                {
                    loadedTextures.Add(request.asset as Texture2D);
                    currentLoadedTextures = loadedTextures.Count;
                }
            };
        }
    }

    /// <summary>
    /// 在屏幕上显示纹理
    /// </summary>
    private void OnGUI()
    {
        //if (!showTexturesOnScreen || loadedTextures.Count == 0) return;

        // 显示调试信息
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("纹理加载卸载管理器", GUI.skin.box);
        GUILayout.Space(5);
        GUILayout.Label($"加载纹理: {currentLoadedTextures}");
        GUILayout.Label($"RenderTextures: {currentRenderTextures}");
        GUILayout.Label($"当前材质纹理: {currentMaterialTexture}");
        GUILayout.Space(5);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void GenerateCubeMatrix()
    {
        // 清理之前生成的立方体
        ClearExistingCubes();

        // 计算行列数 (10x10 方阵)
        rows = Mathf.CeilToInt(Mathf.Sqrt(cubeCount));
        columns = Mathf.CeilToInt((float)cubeCount / rows);

        Debug.Log($"生成立方体矩阵: {rows}行 x {columns}列, 总共{rows * columns}个位置");

        int createdCount = 0;

        // 生成立方体矩阵
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (createdCount >= cubeCount)
                    break;

                CreateCubeAtPosition(row, col);
                createdCount++;
            }
        }

        Debug.Log($"立方体生成完成！总共创建了 {allCubes.Count} 个立方体，{allRenderers.Count} 个Renderer");
    }

    void CreateCubeAtPosition(int row, int col)
    {
        // 计算位置
        Vector3 position = new Vector3(
            col * spacing,
            0,
            row * spacing
        );

        // 创建立方体
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"Cube_{row}_{col}";
        cube.transform.position = position;
        cube.transform.SetParent(this.transform); // 设置为当前物体的子物体

        // 获取Renderer并设置材质
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.sharedMaterial = new Material(cubeMaterial);
        
        // 保存Renderer引用
        allRenderers.Add(renderer);

        // 保存GameObject引用
        allCubes.Add(cube);
    }

    void ClearExistingCubes()
    {
        foreach (GameObject cube in allCubes)
        {
            if (cube != null)
                DestroyImmediate(cube);
        }

        allCubes.Clear();
        allRenderers.Clear();
    }

    // 公共方法：获取所有Renderer
    public List<Renderer> GetAllRenderers()
    {
        return allRenderers;
    }

    // 公共方法：获取特定位置的Renderer
    public Renderer GetRendererAt(int row, int col)
    {
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return null;

        int index = row * columns + col;
        if (index < allRenderers.Count)
            return allRenderers[index];

        return null;
    }

    // 公共方法：批量修改所有立方体的材质
    public void SetAllCubesMaterial(Material newMaterial)
    {
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null)
                renderer.material = newMaterial;
        }
    }

    //公共方法：批量修改所有立方体的颜色
    public void SetAllCubesColor(Color color)
    {
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null)
                renderer.material.color = color;
        }
    }

    /// <summary>
    /// 清理所有资源
    /// </summary>
    private void CleanupAllResources()
    {
        // 卸载所有纹理
        foreach (Texture2D texture in loadedTextures)
        {
            if (texture != null)
            {
                Resources.UnloadAsset(texture);
            }
        }

        loadedTextures.Clear();
        textureSwitchQueue.Clear();

        // 强制垃圾回收
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        currentLoadedTextures = 0;
        currentRenderTextures = 0;
        currentMaterialTexture = "None";

        Debug.Log("清理所有资源完成");
    }


    /// <summary>
    /// 获取当前状态信息
    /// </summary>
    public string GetStatusInfo()
    {
        return $"加载纹理: {currentLoadedTextures} | RenderTextures: {currentRenderTextures} | 周期耗时: {loadUnloadTime:F4}s | 材质纹理: {currentMaterialTexture}";
    }

    private void OnDisable()
    {
        CleanupAllResources();
        // 清理默认创建的材质
        if (targetMaterial != null && targetMaterial.name == "DynamicTextureMaterial")
        {
            DestroyImmediate(targetMaterial);
        }
    }

    void OnDestroy()
    {

        CleanupAllResources();
        if (targetMaterial != null && targetMaterial.name == "DynamicTextureMaterial")
        {
            DestroyImmediate(targetMaterial);
        }
    }

    void OnApplicationQuit()
    {
    }
}