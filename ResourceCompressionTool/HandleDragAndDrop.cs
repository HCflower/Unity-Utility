using UnityEngine;
using UnityEditor;
using System.IO;

public partial class ResourceCompressionTool
{
    #region Drag & Drop

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                ProcessDroppedObjects(DragAndDrop.objectReferences);
                evt.Use();
            }
        }
    }

    private void ProcessDroppedObjects(UnityEngine.Object[] droppedObjects)
    {
        int addedCount = 0;
        foreach (var obj in droppedObjects)
        {
            if (obj == null) continue;

            if (obj is Texture2D texture)
            {
                if (AddTextureToCache(texture)) addedCount++;
            }
            else if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)))
            {
                addedCount += AddTexturesFromFolderPath(AssetDatabase.GetAssetPath(obj));
            }
        }
        if (addedCount > 0)
        {
            Debug.Log($"成功添加 {addedCount} 个图片");
            Repaint();
        }
    }

    private void HandleAudioDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                ProcessDroppedAudioObjects(DragAndDrop.objectReferences);
                evt.Use();
            }
        }
    }

    private void ProcessDroppedAudioObjects(UnityEngine.Object[] droppedObjects)
    {
        int addedCount = 0;
        foreach (var obj in droppedObjects)
        {
            if (obj == null) continue;

            if (obj is AudioClip audio)
            {
                if (AddAudioToCache(audio)) addedCount++;
            }
            else if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)))
            {
                addedCount += AddAudioFromFolderPath(AssetDatabase.GetAssetPath(obj));
            }
        }
        if (addedCount > 0)
        {
            Debug.Log($"成功添加 {addedCount} 个音频");
            Repaint();
        }
    }

    #endregion

    #region Folder & Cache Helpers

    private bool AddTextureToCache(Texture2D texture)
    {
        if (texture == null) return false;
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path)) return false;

        // 已存在就跳过
        if (textureCacheList.Exists(t => t.path == path)) return false;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        var cache = new TextureCacheData
        {
            texture = texture,
            path = path,
            width = texture.width,
            height = texture.height,
            format = texture.format,
            mipmapCount = texture.mipmapCount,
            memoryUsage = CalculateTextureMemoryUsage(texture, importer),
            foldout = false
        };

        // 预览图（简单同步获取；如果想更省性能可以做 delayCall）
        cache.preview = AssetPreview.GetAssetPreview(texture) ??
                        (EditorGUIUtility.IconContent("Texture Icon").image as Texture2D);

        textureCacheList.Add(cache);
        cachedTextureTotalMemory += cache.memoryUsage;
        return true;
    }

    private int AddTexturesFromFolderPath(string folderPath)
    {
        int addedCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (AddTextureToCache(texture)) addedCount++;
        }
        return addedCount;
    }

    private bool AddAudioToCache(AudioClip audio)
    {
        if (audio == null) return false;
        string path = AssetDatabase.GetAssetPath(audio);
        if (string.IsNullOrEmpty(path)) return false;

        if (audioCacheList.Exists(a => a.path == path)) return false;

        var cache = new AudioCacheData
        {
            audio = audio,
            path = path,
            length = audio.length,
            frequency = audio.frequency,
            channels = audio.channels,
            memoryUsage = CalculateAudioMemoryUsage(audio),
            foldout = false
        };

        audioCacheList.Add(cache);
        cachedAudioTotalMemory += cache.memoryUsage;
        return true;
    }

    private int AddAudioFromFolderPath(string folderPath)
    {
        int addedCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (AddAudioToCache(audio)) addedCount++;
        }
        return addedCount;
    }

    private void ClearTextureCache()
    {
        textureCacheList.Clear();
        cachedTextureTotalMemory = 0;
    }

    private void ClearAudioCache()
    {
        audioCacheList.Clear();
        cachedAudioTotalMemory = 0;
    }

    #endregion

    #region Texture Logic

    private void ApplyTextureSettings()
    {
        if (textureCacheList.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "没有选择任何图片", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认应用设置",
                $"确定要将设置应用到 {textureCacheList.Count} 张图片吗？", "确定", "取消"))
        {
            return;
        }

        int processedCount = 0;
        try
        {
            EditorUtility.DisplayProgressBar("应用设置", "正在处理图片设置...", 0f);
            for (int i = 0; i < textureCacheList.Count; i++)
            {
                var cache = textureCacheList[i];
                var texture = cache.texture;
                if (texture == null) continue;

                string path = cache.path;
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.textureType = texturePreSettings.textureType;
                    importer.textureShape = texturePreSettings.textureShape;
                    importer.sRGBTexture = texturePreSettings.sRGBTexture;
                    importer.alphaSource = texturePreSettings.alphaSource;
                    importer.npotScale = texturePreSettings.nonPowerOf2;
                    importer.isReadable = texturePreSettings.readable;
                    importer.streamingMipmaps = texturePreSettings.streamingMipMaps;
                    importer.filterMode = texturePreSettings.filterMode;
                    importer.anisoLevel = texturePreSettings.anisoLevel;
                    importer.wrapMode = texturePreSettings.wrapMode;
                    importer.SaveAndReimport();
                    processedCount++;
                }

                EditorUtility.DisplayProgressBar(
                    "应用设置",
                    $"正在处理 {Path.GetFileName(path)}...",
                    (float)(i + 1) / textureCacheList.Count);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 重新统计内存
        RecalculateTextureCacheMemory();
        AssetDatabase.Refresh();
        Repaint();
        EditorUtility.DisplayDialog("完成", $"成功应用设置到 {processedCount} 张图片", "确定");
    }

    private void ResetTextureSettings()
    {
        texturePreSettings = new TextureSettings();
        Repaint();
    }

    private void LoadSettingsFromFirstTexture()
    {
        if (textureCacheList.Count == 0) return;
        var first = textureCacheList[0];
        if (first.texture == null) return;

        string path = first.path;
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            texturePreSettings.textureType = importer.textureType;
            texturePreSettings.textureShape = importer.textureShape;
            texturePreSettings.sRGBTexture = importer.sRGBTexture;
            texturePreSettings.alphaSource = importer.alphaSource;
            texturePreSettings.nonPowerOf2 = importer.npotScale;
            texturePreSettings.readable = importer.isReadable;
            texturePreSettings.streamingMipMaps = importer.streamingMipmaps;
            texturePreSettings.filterMode = importer.filterMode;
            texturePreSettings.anisoLevel = importer.anisoLevel;
            texturePreSettings.wrapMode = importer.wrapMode;
            Repaint();
        }
    }

    private long CalculateTextureMemoryUsage(Texture2D texture, TextureImporter importer)
    {
        if (texture == null) return 0;
        return texture.GetRawTextureData().Length;
    }

    private void RecalculateTextureCacheMemory()
    {
        long total = 0;
        foreach (var cache in textureCacheList)
        {
            if (cache.texture == null) continue;
            var importer = AssetImporter.GetAtPath(cache.path) as TextureImporter;
            cache.memoryUsage = CalculateTextureMemoryUsage(cache.texture, importer);
            total += cache.memoryUsage;
        }
        cachedTextureTotalMemory = total;
    }

    #endregion

    #region Audio Logic

    private void ApplyAudioSettings()
    {
        if (audioCacheList.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "没有选择任何音频", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认应用设置",
                $"确定要将设置应用到 {audioCacheList.Count} 个音频吗？", "确定", "取消"))
        {
            return;
        }

        int processedCount = 0;
        try
        {
            EditorUtility.DisplayProgressBar("应用设置", "正在处理音频设置...", 0f);
            for (int i = 0; i < audioCacheList.Count; i++)
            {
                var cache = audioCacheList[i];
                var audio = cache.audio;
                if (audio == null) continue;

                string path = cache.path;
                if (AssetImporter.GetAtPath(path) is AudioImporter importer)
                {
                    importer.forceToMono = audioSettings.forceToMono;
                    importer.ambisonic = audioSettings.ambisonic;

                    var sampleSettings = importer.defaultSampleSettings;
                    sampleSettings.preloadAudioData = audioSettings.preloadAudioData;
                    sampleSettings.loadType = audioSettings.loadType;
                    sampleSettings.compressionFormat = audioSettings.compressionFormat;
                    sampleSettings.quality = audioSettings.quality;
                    sampleSettings.sampleRateSetting = audioSettings.sampleRateSetting;
                    if (audioSettings.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
                    {
                        sampleSettings.sampleRateOverride = (uint)audioSettings.sampleRateOverride;
                    }
                    importer.defaultSampleSettings = sampleSettings;

                    if (audioSettings.overridePlatformSettings)
                    {
                        string platformName = GetPlatformName(audioSettings.targetPlatform);
                        importer.SetOverrideSampleSettings(platformName, sampleSettings);
                    }
                    else
                    {
                        string platformName = GetPlatformName(audioSettings.targetPlatform);
                        var defaultSettings = importer.defaultSampleSettings;
                        importer.SetOverrideSampleSettings(platformName, defaultSettings);
                    }

                    importer.SaveAndReimport();
                    processedCount++;
                }

                EditorUtility.DisplayProgressBar(
                    "应用设置",
                    $"正在处理 {Path.GetFileName(path)}...",
                    (float)(i + 1) / audioCacheList.Count);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        RecalculateAudioCacheMemory();
        AssetDatabase.Refresh();
        Repaint();
        EditorUtility.DisplayDialog("完成", $"成功应用设置到 {processedCount} 个音频", "确定");
    }

    private void ResetAudioSettings()
    {
        audioSettings = new AudioCompressionSettings();
        Repaint();
    }

    private void LoadSettingsFromFirstAudio()
    {
        if (audioCacheList.Count == 0) return;
        var first = audioCacheList[0];
        if (first.audio == null) return;

        string path = first.path;
        if (AssetImporter.GetAtPath(path) is AudioImporter importer)
        {
            var sampleSettings = importer.defaultSampleSettings;
            audioSettings.loadType = sampleSettings.loadType;
            audioSettings.compressionFormat = sampleSettings.compressionFormat;
            audioSettings.quality = sampleSettings.quality;
            audioSettings.sampleRateSetting = sampleSettings.sampleRateSetting;
            audioSettings.sampleRateOverride = (int)sampleSettings.sampleRateOverride;
            audioSettings.preloadAudioData = sampleSettings.preloadAudioData;
            audioSettings.forceToMono = importer.forceToMono;
            audioSettings.ambisonic = importer.ambisonic;
            Repaint();
        }
    }

    private long CalculateAudioMemoryUsage(AudioClip audio)
    {
        if (audio == null) return 0;
        return audio.samples * audio.channels * 2; // 16bit
    }

    private void RecalculateAudioCacheMemory()
    {
        long total = 0;
        foreach (var cache in audioCacheList)
        {
            if (cache.audio == null) continue;
            cache.memoryUsage = CalculateAudioMemoryUsage(cache.audio);
            total += cache.memoryUsage;
        }
        cachedAudioTotalMemory = total;
    }

    #endregion

    #region Compression Core

    private void StartCompression()
    {
        switch (currentPage)
        {
            case CompressionPage.Texture:
                CompressSelectedTextures();
                break;
            case CompressionPage.Audio:
                CompressSelectedAudios();
                break;
            case CompressionPage.Model:
                EditorUtility.DisplayDialog("提示", "模型压缩尚未实现", "确定");
                break;
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("提示", "所有资源已保存！", "确定");
    }

    private void CompressSelectedTextures()
    {
        if (textureCacheList.Count == 0) return;

        long originalTotal = 0;
        long compressedTotal = 0;
        int processed = 0;

        try
        {
            EditorUtility.DisplayProgressBar("纹理压缩", "处理中...", 0f);
            for (int i = 0; i < textureCacheList.Count; i++)
            {
                var cache = textureCacheList[i];
                var tex = cache.texture;
                if (tex == null) continue;

                string path = cache.path;
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    originalTotal += CalculateTextureMemoryUsage(tex, importer);

                    importer.maxTextureSize = textureSettings.maxTextureSize;
                    importer.compressionQuality = Mathf.RoundToInt(textureSettings.compressionQuality);
                    importer.mipmapEnabled = textureSettings.generateMipMaps;
                    importer.textureCompression = textureSettings.compressionMode;

                    if (textureSettings.forceCompression &&
                        importer.textureCompression == TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Compressed;
                    }

                    if (textureSettings.overridePlatformSettings)
                    {
                        ApplyPlatformTextureSetting(importer, "Standalone");
                        ApplyPlatformTextureSetting(importer, "Android");
                        ApplyPlatformTextureSetting(importer, "iPhone");
                    }
                    else
                    {
                        importer.ClearPlatformTextureSettings("Standalone");
                        importer.ClearPlatformTextureSettings("Android");
                        importer.ClearPlatformTextureSettings("iPhone");
                    }

                    importer.SaveAndReimport();

                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    var newTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    compressedTotal += CalculateTextureMemoryUsage(newTex, importer);

                    // 更新缓存里的内存数据
                    cache.texture = newTex;
                    cache.memoryUsage = CalculateTextureMemoryUsage(newTex, importer);

                    processed++;
                }

                EditorUtility.DisplayProgressBar(
                    "纹理压缩",
                    $"正在处理 {Path.GetFileName(path)}",
                    (float)(i + 1) / textureCacheList.Count);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        RecalculateTextureCacheMemory();

        lastResult = new CompressionResult
        {
            originalSize = originalTotal / (1024f * 1024f),
            compressedSize = compressedTotal / (1024f * 1024f),
            processedCount = processed
        };

        ShowResultDialog();
        Repaint();
    }

    private void ApplyPlatformTextureSetting(TextureImporter importer, string platformName)
    {
        var ps = importer.GetPlatformTextureSettings(platformName);
        ps.overridden = true;
        ps.maxTextureSize = textureSettings.maxTextureSize;
        ps.compressionQuality = Mathf.RoundToInt(textureSettings.compressionQuality);
        ps.format = TextureImporterFormat.Automatic;
        importer.SetPlatformTextureSettings(ps);
    }

    private void CompressSelectedAudios()
    {
        // 直接复用 ApplyAudioSettings 逻辑
        ApplyAudioSettings();
        RecalculateAudioCacheMemory();
    }

    private void ShowResultDialog()
    {
        if (lastResult == null) return;
        string message = $"压缩完成！\n\n" +
                         $"处理资源: {lastResult.processedCount} 个\n" +
                         $"压缩前内存: {lastResult.originalSize:0.00} MB\n" +
                         $"压缩后内存: {lastResult.compressedSize:0.00} MB\n" +
                         $"节省内存: {lastResult.originalSize - lastResult.compressedSize:0.00} MB";
        EditorUtility.DisplayDialog("压缩结果", message, "确定");
    }

    private string GetPlatformName(AudioPlatform p)
    {
        return p.ToString();
    }

    #endregion
}
