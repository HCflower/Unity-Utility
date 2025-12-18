using UnityEngine;
using UnityEditor;

[System.Serializable]
public class TextureCompressionSettings
{
    public int maxTextureSize = 1024;
    public float compressionQuality = 50f;
    public bool generateMipMaps = false;
    public bool forceCompression = true;
    public TextureImporterCompression compressionMode = TextureImporterCompression.Compressed;
    public bool overridePlatformSettings = true;
}

[System.Serializable]
public class TextureSettings
{
    public TextureImporterType textureType = TextureImporterType.Default;
    public TextureImporterShape textureShape = TextureImporterShape.Texture2D;
    public bool sRGBTexture = true;
    public TextureImporterAlphaSource alphaSource = TextureImporterAlphaSource.FromInput;
    public bool alphaIsTransparency; // 新增
#if UNITY_2020_1_OR_NEWER
    public bool alphaPremultiply;    // 新增
#endif
    public TextureImporterNPOTScale nonPowerOf2 = TextureImporterNPOTScale.ToNearest;
    public bool readable = false;
    public bool streamingMipMaps = false;
    public FilterMode filterMode = FilterMode.Bilinear;
    public int anisoLevel = 1;
    public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
}

[System.Serializable]
public class AudioCompressionSettings
{
    public AudioClipLoadType loadType = AudioClipLoadType.DecompressOnLoad;
    public bool preloadAudioData = true;
    public AudioCompressionFormat compressionFormat = AudioCompressionFormat.Vorbis;
    public float quality = 0.5f;
    public AudioSampleRateSetting sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
    public int sampleRateOverride = 44100;
    public bool overridePlatformSettings = false;
    public AudioPlatform targetPlatform = AudioPlatform.Standalone;
    public bool forceToMono = false;
    public bool ambisonic = false;
}

public enum AudioPlatform
{
    Standalone,
    Android,
    iPhone, // Unity内部名称为iPhone
    WebGL
}

[System.Serializable]
public class CompressionResult
{
    public float originalSize;
    public float compressedSize;
    public int processedCount;
}