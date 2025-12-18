using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public partial class ResourceCompressionTool
{
    // 在类顶部添加预览图缓存字典
    private Dictionary<string, Texture2D> previewCache = new Dictionary<string, Texture2D>();

    private void DrawHeader()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("资源压缩工具", titleStyle, GUILayout.Height(32));
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawNavigation()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("功能模块:", GUILayout.Width(60));
            string[] pageNames = { "图片压缩", "音频压缩", "模型压缩" };
            currentPage = (CompressionPage)EditorGUILayout.Popup((int)currentPage, pageNames, GUILayout.ExpandWidth(true));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawCurrentPage()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        switch (currentPage)
        {
            case CompressionPage.Texture:
                DrawTextureCompressionPage();
                break;
            case CompressionPage.Audio:
                DrawAudioCompressionPage();
                break;
            case CompressionPage.Model:
                DrawModelCompressionPage();
                break;
        }
        EditorGUILayout.EndScrollView();
    }

    #region Texture GUI

    private void DrawTextureCompressionPage()
    {
        DrawDragAndDropArea();
        DrawTextureSettings();
        DrawCompressionSettings();
        DrawTextureListSection();
    }

    private void DrawDragAndDropArea()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.LabelField("拖拽区域", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("将图片文件或文件夹拖拽到下方区域", MessageType.Info);

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "", EditorStyles.helpBox);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            GUI.Label(dropArea, "拖拽图片或文件夹到这里", labelStyle);

            HandleDragAndDrop(dropArea);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            GUILayout.Space(2);
            // 折叠按钮和应用按钮在同一行，且都在TipBox内
            EditorGUILayout.BeginHorizontal();
            showTextureSettings = EditorGUILayout.Foldout(showTextureSettings, "图片设置", true);
            GUILayout.FlexibleSpace();
            // 原来使用 selectedTextures，这里改为使用缓存列表
            if (textureCacheList.Count > 0 && GUILayout.Button("应用设置", GUILayout.Height(20), GUILayout.Width(80)))
            {
                ApplyTextureSettings();
            }
            EditorGUILayout.EndHorizontal();

            if (showTextureSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("基础设置", EditorStyles.miniBoldLabel);
                    texturePreSettings.textureType = (TextureImporterType)EditorGUILayout.EnumPopup("纹理类型:", texturePreSettings.textureType);
                    texturePreSettings.textureShape = (TextureImporterShape)EditorGUILayout.EnumPopup("纹理形状:", texturePreSettings.textureShape);
                    texturePreSettings.sRGBTexture = EditorGUILayout.Toggle("sRGB (颜色纹理)", texturePreSettings.sRGBTexture);
                    texturePreSettings.alphaSource = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup("Alpha 来源", texturePreSettings.alphaSource);
                    texturePreSettings.alphaIsTransparency = EditorGUILayout.Toggle("Alpha是否透明", texturePreSettings.alphaIsTransparency);
#if UNITY_2020_1_OR_NEWER
                    texturePreSettings.alphaPremultiply = EditorGUILayout.Toggle("Alpha预乘", texturePreSettings.alphaPremultiply);
#endif

                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("高级设置", EditorStyles.miniBoldLabel);
                    texturePreSettings.nonPowerOf2 = (TextureImporterNPOTScale)EditorGUILayout.EnumPopup("非2次幂", texturePreSettings.nonPowerOf2);
                    texturePreSettings.readable = EditorGUILayout.Toggle("可读写", texturePreSettings.readable);
                    texturePreSettings.streamingMipMaps = EditorGUILayout.Toggle("流式MipMaps", texturePreSettings.streamingMipMaps);
                    texturePreSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup("过滤模式:", texturePreSettings.filterMode);
                    texturePreSettings.anisoLevel = EditorGUILayout.IntSlider("各向异性:", texturePreSettings.anisoLevel, 0, 16);

                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("包装设置", EditorStyles.miniBoldLabel);
                    texturePreSettings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("包装模式:", texturePreSettings.wrapMode);

                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("重置设置", GUILayout.Height(25))) ResetTextureSettings();
                    if (GUILayout.Button("从第一张读取", GUILayout.Height(25))) LoadSettingsFromFirstTexture();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCompressionSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            showCompressionSettings = EditorGUILayout.Foldout(showCompressionSettings, "压缩设置", true);

            if (showCompressionSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("基础设置", EditorStyles.miniBoldLabel);
                    textureSettings.maxTextureSize = EditorGUILayout.IntField("最大尺寸:", textureSettings.maxTextureSize);
                    textureSettings.compressionQuality = EditorGUILayout.Slider("压缩质量:", textureSettings.compressionQuality, 0f, 100f);
                    textureSettings.generateMipMaps = EditorGUILayout.Toggle("生成MipMaps", textureSettings.generateMipMaps);

                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("高级设置", EditorStyles.miniBoldLabel);
                    textureSettings.forceCompression = EditorGUILayout.Toggle("强制压缩", textureSettings.forceCompression);
                    textureSettings.compressionMode = (TextureImporterCompression)EditorGUILayout.EnumPopup("压缩模式:", textureSettings.compressionMode);
                    textureSettings.overridePlatformSettings = EditorGUILayout.Toggle("覆盖平台设置", textureSettings.overridePlatformSettings);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureListSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.BeginHorizontal();
            {
                showTextureList = EditorGUILayout.Foldout(showTextureList, "已选择的图片", true);
                GUILayout.FlexibleSpace();

                if (textureCacheList.Count > 0)
                {
                    EditorGUILayout.LabelField($"({textureCacheList.Count}个)", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } }, GUILayout.Width(40));

                    // 使用缓存的总内存
                    float totalMemoryMB = cachedTextureTotalMemory / (1024f * 1024f);
                    GUIStyle memoryStyle = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = totalMemoryMB > 50 ? Color.red : totalMemoryMB > 20 ? Color.yellow : Color.green } };
                    EditorGUILayout.LabelField($"{totalMemoryMB:0.0}MB", memoryStyle, GUILayout.Width(60));

                    if (GUILayout.Button("清空", GUILayout.Height(20), GUILayout.Width(50)))
                    {
                        if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有图片吗?", "确定", "取消"))
                        {
                            ClearTextureCache();
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (showTextureList)
            {
                if (textureCacheList.Count == 0)
                    EditorGUILayout.HelpBox("请添加图片文件", MessageType.Info);
                else
                    DrawTextureList();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureList()
    {
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < textureCacheList.Count; i++)
        {
            var cache = textureCacheList[i];
            if (cache.texture == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                Rect mainRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                Rect buttonRect = new Rect(mainRect.x + 1, mainRect.y + 1, mainRect.width, mainRect.height - 2);

                float memoryMB = cache.memoryUsage / (1024f * 1024f);

                string arrowText = cache.foldout ? "▼" : "▶";
                GUIContent buttonContent = new GUIContent($"{arrowText}  {cache.texture.name}", cache.preview);
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    imagePosition = ImagePosition.ImageLeft,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(8, 60, 4, 4)
                };

                if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                {
                    cache.foldout = !cache.foldout;
                }

                Rect memoryRect = new Rect(buttonRect.xMax - 55, buttonRect.y + 6, 50, 16);
                GUIStyle memoryStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = memoryMB > 10 ? Color.red : memoryMB > 5 ? Color.yellow : Color.green },
                    alignment = TextAnchor.MiddleRight,
                    fontStyle = FontStyle.Bold
                };
                GUI.Label(memoryRect, $"{memoryMB:0.0}MB", memoryStyle);

                if (cache.foldout)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("尺寸:", GUILayout.Width(35));
                        EditorGUILayout.LabelField($"{cache.width}×{cache.height}", GUILayout.Width(65));
                        EditorGUILayout.LabelField("格式:", GUILayout.Width(35));
                        EditorGUILayout.LabelField(cache.format.ToString(), GUILayout.Width(80));
                        EditorGUILayout.LabelField("MipMaps:", GUILayout.Width(70));
                        EditorGUILayout.LabelField(cache.mipmapCount > 1 ? $"是({cache.mipmapCount}级)" : "否", GUILayout.Width(60));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("定位", GUILayout.Width(40), GUILayout.Height(18))) EditorGUIUtility.PingObject(cache.texture);
                        if (GUILayout.Button("×", new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold }, GUILayout.Width(24), GUILayout.Height(18)))
                        {
                            cachedTextureTotalMemory -= cache.memoryUsage;
                            textureCacheList.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("路径:", GUILayout.Width(35));
                        EditorGUILayout.LabelField(System.IO.Path.GetDirectoryName(cache.path), EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    // 添加预览图缓存方法
    private Texture2D GetCachedPreview(Texture2D texture, string texturePath)
    {
        if (previewCache.ContainsKey(texturePath))
        {
            return previewCache[texturePath];
        }

        // 异步获取预览图
        Texture2D preview = AssetPreview.GetAssetPreview(texture);
        if (preview == null)
        {
            // 如果预览图还未准备好，使用默认图标并标记需要重绘
            preview = EditorGUIUtility.IconContent("Texture Icon").image as Texture2D ?? Texture2D.whiteTexture;

            // 延迟获取真实预览图
            EditorApplication.delayCall += () =>
            {
                Texture2D realPreview = AssetPreview.GetAssetPreview(texture);
                if (realPreview != null && !previewCache.ContainsKey(texturePath))
                {
                    previewCache[texturePath] = realPreview;
                    Repaint();
                }
            };
        }
        else
        {
            // 缓存预览图
            previewCache[texturePath] = preview;
        }

        return preview;
    }

    #endregion

    #region Audio GUI

    private void DrawAudioCompressionPage()
    {
        DrawAudioDragAndDropArea();
        DrawAudioSettings();
        DrawAudioCompressionSettings();
        DrawAudioListSection();
    }

    private void DrawAudioDragAndDropArea()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.LabelField("拖拽区域", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("将音频文件或文件夹拖拽到下方区域", MessageType.Info);

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "", EditorStyles.helpBox);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
            GUI.Label(dropArea, "拖拽音频或文件夹到这里", labelStyle);

            HandleAudioDragAndDrop(dropArea);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.BeginHorizontal();
            {
                showAudioSettings = EditorGUILayout.Foldout(showAudioSettings, "音频设置", true);
                GUILayout.FlexibleSpace();
                // 原来使用 selectedAudioClips，这里改为使用音频缓存列表
                if (audioCacheList.Count > 0 && GUILayout.Button("应用设置", GUILayout.Height(20), GUILayout.Width(80)))
                {
                    ApplyAudioSettings();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (showAudioSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("基础设置", EditorStyles.miniBoldLabel);
                    audioSettings.loadType = (AudioClipLoadType)EditorGUILayout.EnumPopup("加载类型:", audioSettings.loadType);
                    audioSettings.preloadAudioData = EditorGUILayout.Toggle("预加载数据:", audioSettings.preloadAudioData);

                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("高级设置", EditorStyles.miniBoldLabel);
                    audioSettings.compressionFormat = (AudioCompressionFormat)EditorGUILayout.EnumPopup("压缩格式:", audioSettings.compressionFormat);
                    audioSettings.quality = EditorGUILayout.Slider("质量:", audioSettings.quality, 0.01f, 1f);
                    audioSettings.sampleRateSetting = (AudioSampleRateSetting)EditorGUILayout.EnumPopup("采样率设置:", audioSettings.sampleRateSetting);
                    if (audioSettings.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
                    {
                        audioSettings.sampleRateOverride = EditorGUILayout.IntField("采样率:", audioSettings.sampleRateOverride);
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("重置设置", GUILayout.Height(25))) ResetAudioSettings();
                        if (GUILayout.Button("从第一个读取", GUILayout.Height(25))) LoadSettingsFromFirstAudio();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioCompressionSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            showAudioCompressionSettings = EditorGUILayout.Foldout(showAudioCompressionSettings, "压缩设置", true);
            if (showAudioCompressionSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("平台设置", EditorStyles.miniBoldLabel);
                    audioSettings.overridePlatformSettings = EditorGUILayout.Toggle("覆盖平台设置", audioSettings.overridePlatformSettings);
                    if (audioSettings.overridePlatformSettings)
                    {
                        audioSettings.targetPlatform = (AudioPlatform)EditorGUILayout.EnumPopup("目标平台:", audioSettings.targetPlatform);
                    }

                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("压缩选项", EditorStyles.miniBoldLabel);
                    audioSettings.forceToMono = EditorGUILayout.Toggle("强制单声道", audioSettings.forceToMono);
                    audioSettings.ambisonic = EditorGUILayout.Toggle("环绕声", audioSettings.ambisonic);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioListSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.BeginHorizontal();
            {
                showAudioList = EditorGUILayout.Foldout(showAudioList, "已选择的音频", true);
                GUILayout.FlexibleSpace();

                if (audioCacheList.Count > 0)
                {
                    EditorGUILayout.LabelField($"({audioCacheList.Count}个)",
                        new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } },
                        GUILayout.Width(40));

                    // 使用缓存的总内存
                    float totalMemoryMB = cachedAudioTotalMemory / (1024f * 1024f);
                    GUIStyle memoryStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        normal =
                        {
                            textColor = totalMemoryMB > 100
                                ? Color.red
                                : totalMemoryMB > 50
                                    ? Color.yellow
                                    : Color.green
                        }
                    };
                    EditorGUILayout.LabelField($"{totalMemoryMB:0.0}MB", memoryStyle, GUILayout.Width(60));

                    if (GUILayout.Button("清空", GUILayout.Height(20), GUILayout.Width(50)))
                    {
                        if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有音频吗？", "确定", "取消"))
                        {
                            ClearAudioCache();
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (showAudioList)
            {
                if (audioCacheList.Count == 0)
                    EditorGUILayout.HelpBox("请添加音频文件", MessageType.Info);
                else
                    DrawAudioList();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioList()
    {
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < audioCacheList.Count; i++)
        {
            var cache = audioCacheList[i];
            if (cache.audio == null) continue;

            string audioPath = cache.path;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                Rect mainRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                Rect buttonRect = new Rect(mainRect.x + 1, mainRect.y + 1, mainRect.width, mainRect.height - 2);

                float memoryMB = cache.memoryUsage / (1024f * 1024f);

                string arrowText = cache.foldout ? "▼" : "▶";
                Texture2D audioIcon = EditorGUIUtility.IconContent("AudioClip Icon").image as Texture2D;
                GUIContent buttonContent = new GUIContent($"{arrowText}  {cache.audio.name}", audioIcon);
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(8, 60, 4, 4)
                };

                if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                {
                    cache.foldout = !cache.foldout;
                }

                Rect memoryRect = new Rect(buttonRect.xMax - 55, buttonRect.y + 6, 50, 16);
                GUIStyle memoryStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal =
                    {
                        textColor = memoryMB > 20
                            ? Color.red
                            : memoryMB > 10
                                ? Color.yellow
                                : Color.green
                    },
                    alignment = TextAnchor.MiddleRight,
                    fontStyle = FontStyle.Bold
                };
                GUI.Label(memoryRect, $"{memoryMB:0.0}MB", memoryStyle);

                if (cache.foldout)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("时长:", GUILayout.Width(35));
                        EditorGUILayout.LabelField($"{cache.length:0.0}s", GUILayout.Width(50));
                        EditorGUILayout.LabelField("频率:", GUILayout.Width(35));
                        EditorGUILayout.LabelField($"{cache.frequency}Hz", GUILayout.Width(70));
                        EditorGUILayout.LabelField("声道:", GUILayout.Width(35));
                        EditorGUILayout.LabelField($"{cache.channels}", GUILayout.Width(30));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("定位", GUILayout.Width(40), GUILayout.Height(18)))
                            EditorGUIUtility.PingObject(cache.audio);
                        if (GUILayout.Button("×",
                                new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold },
                                GUILayout.Width(24), GUILayout.Height(18)))
                        {
                            cachedAudioTotalMemory -= cache.memoryUsage;
                            audioCacheList.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("路径:", GUILayout.Width(35));
                        EditorGUILayout.LabelField(Path.GetDirectoryName(audioPath), EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Model & Footer GUI

    private void DrawModelCompressionPage()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.LabelField("模型压缩功能开发中...", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("模型压缩功能正在开发中，敬请期待！", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            if (lastResult != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                {
                    GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = new Color(0.2f, 0.5f, 0.9f) } };
                    EditorGUILayout.LabelField("最近压缩结果", titleStyle, GUILayout.Width(110));
                    string resultText = $"压缩前: <color=#888>{lastResult.originalSize:0.0}MB</color> → 压缩后: <color=#4CAF50>{lastResult.compressedSize:0.0}MB</color>";
                    EditorGUILayout.LabelField(resultText, new GUIStyle(EditorStyles.label) { richText = true });
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(110));
                    float savedMemory = lastResult.originalSize - lastResult.compressedSize;
                    float compressionRatio = lastResult.originalSize > 0 ? (savedMemory / lastResult.originalSize) * 100 : 0;
                    string savedText = $"节省: <b><color=#{(savedMemory > 0 ? "4CAF50" : "888")}>{savedMemory:0.0}MB</color></b> ({compressionRatio:0.0}%)   处理: <b>{lastResult.processedCount}</b> 个";
                    EditorGUILayout.LabelField(savedText, new GUIStyle(EditorStyles.miniLabel) { richText = true });
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(2);
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fixedHeight = 26 };
                string buttonText = "开始压缩";
                bool isEnabled = false;

                switch (currentPage)
                {
                    case CompressionPage.Texture:
                        buttonText = "开始图片压缩";
                        // 原来使用 selectedTextures，这里改为使用纹理缓存列表
                        isEnabled = textureCacheList.Count > 0;
                        break;
                    case CompressionPage.Audio:
                        buttonText = "开始音频压缩";
                        // 原来使用 selectedAudioClips，这里改为使用音频缓存列表
                        isEnabled = audioCacheList.Count > 0;
                        break;
                    case CompressionPage.Model:
                        buttonText = "开始模型压缩";
                        isEnabled = false;
                        break;
                }

                GUI.enabled = isEnabled;
                if (GUILayout.Button(buttonText, btnStyle, GUILayout.ExpandWidth(true)))
                {
                    StartCompression();
                }
                GUI.enabled = true;

                GUILayout.Space(2);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.EndVertical();
    }

    #endregion
}