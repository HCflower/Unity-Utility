using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public partial class ResourceCompressionTool : EditorWindow
{
    private enum CompressionPage
    {
        Texture = 0,
        Audio = 1,
        Model = 2,
    }

    private CompressionPage currentPage = CompressionPage.Texture;
    private Vector2 scrollPosition;

    // 纹理数据 - 使用缓存
    private List<TextureCacheData> textureCacheList = new List<TextureCacheData>();
    private TextureCompressionSettings textureSettings = new TextureCompressionSettings();
    private TextureSettings texturePreSettings = new TextureSettings();
    private long cachedTextureTotalMemory = 0;

    // 音频数据 - 使用缓存
    private List<AudioCacheData> audioCacheList = new List<AudioCacheData>();
    private AudioCompressionSettings audioSettings = new AudioCompressionSettings();
    private long cachedAudioTotalMemory = 0;

    // 统计信息
    private CompressionResult lastResult;

    // GUI状态
    private bool showTextureList = true;
    private bool showCompressionSettings = true;
    private bool showTextureSettings = true;
    private bool showAudioList = true;
    private bool showAudioSettings = true;
    private bool showAudioCompressionSettings = true;

    [MenuItem("FFramework/资源压缩工具", priority = 4)]
    public static void ShowWindow()
    {
        ResourceCompressionTool window = GetWindow<ResourceCompressionTool>("资源压缩工具");
        window.minSize = new Vector2(400, 610);
        window.maxSize = new Vector2(500, 1000);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width));
        DrawHeader();
        DrawNavigation();
        DrawCurrentPage();
        DrawFooter();
        EditorGUILayout.EndVertical();
    }
}

// 资源缓存数据类
[System.Serializable]
public class TextureCacheData
{
    public Texture2D texture;
    public string path;
    public long memoryUsage;
    public Texture2D preview;
    public bool foldout;
    public int width;
    public int height;
    public TextureFormat format;
    public int mipmapCount;
}

[System.Serializable]
public class AudioCacheData
{
    public AudioClip audio;
    public string path;
    public long memoryUsage;
    public bool foldout;
    public float length;
    public int frequency;
    public int channels;
}