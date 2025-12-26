using System;
using System.Collections.Generic;
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
using UnityEngine;

namespace TikTokSDK
{
    /// <summary>
    /// TikTok OpenAPI - 抖音开放接口
    /// </summary>
    public class TTOpenAPI : MonoBehaviour
    {
        [Header("分享素材配置")]
        [Tooltip("分享素材 ID -> 必须在 TT 后台配置")]
        public string templateId = "要实现分享必须配置的素材 ID";

        [Header("订阅消息配置")]
        public List<string> tmplIds = new List<string>
        {
            "**********"
        };

        /// <summary>
        /// 订阅奖励事件（true：订阅成功）
        /// </summary>
        public Action<bool> onSubscribeMessageSuccess;

        private TTGridGamePanelManager.TTGridGamePanel panel;
        private TTGridGamePanelManager manager;

        public static event Action OnShow;
        public static event Action OnClick;
        public static event Action OnClose;
        public static event Action<string> OnError;

        TTPlayerPrefs playrePrefs;

        public void Init()
        {
            playrePrefs = TT.PlayerPrefs;
        }

        #region 加入游戏站 - 不易测试-保留接口

        /// <summary>
        /// 加入游戏站
        /// </summary>
        public void JoinGameClub(string appid, string url, Action onSuccess, Action onFailure)
        {
            var param = new NavigateToMiniProgramParam
            {
                AppId = appid,
                Path = url,
                Success = result =>
                {
                    Debug.Log("加入游戏站成功: " + result.ErrMsg);
                    onSuccess?.Invoke();
                },
                Fail = error =>
                {
                    Debug.LogError($"加入游戏站失败: Code={error.ErrorCode}, Msg={error.ErrMsg}, Type={error.ErrorType}");
                    onFailure?.Invoke();
                }
            };

            TT.NavigateToMiniProgram(param);
        }

        #endregion

        #region 侧边栏 - 已通过测试

        // 检查是否支持侧边栏
        private void CheckSidebarSupport(Action<bool> success)
        {
            TT.CheckScene(
                TTSideBar.SceneEnum.SideBar,
                success: isSupported =>
                {
                    Debug.Log("是否支持侧边栏: " + isSupported);
                    success?.Invoke(isSupported);
                },
                complete: () =>
                {
                    Debug.Log("检查侧边栏支持完成");
                },
                error: (code, msg) =>
                {
                    Debug.LogError($"检查侧边栏支持出错: Code={code}, Msg={msg}");
                    success?.Invoke(false);
                });
        }

        /// <summary>
        /// 打开侧边栏（自动检查是否支持）
        /// </summary>
        /// <param name="success">成功回调</param>
        /// <param name="failure">失败回调</param>
        public void OnNavigateToScene(Action success, Action failure)
        {
            CheckSidebarSupport(isSupported =>
            {
                if (!isSupported)
                {
                    Debug.LogWarning("当前环境不支持侧边栏");
                    failure?.Invoke();
                    return;
                }

                var jsonData = new JsonData();
                jsonData["scene"] = "sidebar";

                TT.NavigateToScene(
                    jsonData,
                    success: () =>
                    {
                        // 从侧边栏进入，发放奖励
                        success?.Invoke();
                    },
                    complete: () =>
                    {
                        Debug.Log("从侧边栏进入完成");
                    },
                    error: (code, msg) =>
                    {
                        Debug.LogError($"从侧边栏进入错误: Code={code}, Msg={msg}");
                        failure?.Invoke();
                    });
            });
        }

        #endregion

        #region 互推资源（四宫格）

        public void ShowFour()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogWarning("[GridFour] WebGL 不支持互推组件");
                return;
            }

            // 防止面板重复
            if (panel != null)
            {
                panel.Destroy();
                CleanGridPanel();
            }

            try
            {
                manager = TT.GetGridGamePanelManager();

                var query = new JsonData { ["from"] = "fourGrid" };
                var pos = new JsonData { ["left"] = 300, ["top"] = 300 };

                panel = manager.CreateGridGamePanel(
                    TTGridGamePanelManager.GridGamePanelCount.Four,
                    query,
                    TTGridGamePanelManager.GridGamePanelSize.Medium,
                    position: pos);

                manager.OnGridGamePanelStateChangeHandler = OnStateChange;

                panel.Show();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GridFour] 创建失败：{e.Message}");
                OnError?.Invoke(e.Message);
                CleanGridPanel();
            }
        }

        public void HideFour()
        {
            panel?.Hide();
        }

        public void DestroyFour()
        {
            panel?.Destroy();
            CleanGridPanel();
        }

        private void OnStateChange(TTGridGamePanelManager.GridGamePanelState state,
                                   string errMsg, string appName)
        {
            switch (state)
            {
                case TTGridGamePanelManager.GridGamePanelState.Show:
                    OnShow?.Invoke();
                    break;
                case TTGridGamePanelManager.GridGamePanelState.Click:
                    OnClick?.Invoke();
                    break;
                case TTGridGamePanelManager.GridGamePanelState.Close:
                    OnClose?.Invoke();
                    CleanGridPanel();
                    break;
                case TTGridGamePanelManager.GridGamePanelState.Error:
                    OnError?.Invoke(errMsg);
                    break;
            }
        }

        private void CleanGridPanel()
        {
            if (manager != null)
            {
                manager.OnGridGamePanelStateChangeHandler = null;
            }

            panel = null;
            manager = null;
        }

        #endregion

        #region 游戏订阅消息

        /// <summary>
        /// 申请订阅消息
        /// </summary>
        public void RequestSubscribeMessage()
        {
            if (tmplIds == null || tmplIds.Count == 0)
            {
                Debug.LogWarning("[Subscribe] tmplIds 为空，无法申请订阅");
                return;
            }

            Debug.Log("开始申请订阅消息");

            TT.RequestSubscribeMessage(
                tmplIds: tmplIds,
                success: result =>
                {
                    foreach (var item in result)
                    {
                        Debug.Log($"订阅消息申请结果：{item.Key} - {item.Value}");
                        if (item.Value == "accept")
                        {
                            Debug.Log("订阅成功");
                            onSubscribeMessageSuccess?.Invoke(true);
                            onSubscribeMessageSuccess = null;
                        }
                    }
                });
        }

        #endregion

        #region 分享功能

        // 通用分享方法
        private void ShareAppMessageInternal(JsonData shareData, Action onSuccess, Action<string> onFailed, Action onCancel)
        {
            TT.ShareAppMessage(
                shareData,
                data =>
                {
                    Debug.Log("分享成功");
                    onSuccess?.Invoke();
                },
                message =>
                {
                    Debug.Log($"分享失败: {message}");
                    onFailed?.Invoke(message);
                },
                () =>
                {
                    Debug.Log("分享取消");
                    onCancel?.Invoke();
                }
            );
        }

        /// <summary>
        /// 分享游戏 - 默认链接分享
        /// </summary>
        public void ShareAppMessageLink(string templateId, string title, string description, Action onSuccess = null, Action<string> onFailed = null, Action onCancel = null)
        {
            var shareData = new JsonData
            {
                ["templateId"] = templateId,
                ["query"] = "",
                ["title"] = title,
                ["desc"] = description
            };
            ShareAppMessageInternal(shareData, onSuccess, onFailed, onCancel);
        }

        /// <summary>
        /// 分享游戏 - 图文分享
        /// </summary>
        public void ShareAppMessageImage(string templateId, string title, string description, Action onSuccess = null, Action<string> onFailed = null, Action onCancel = null)
        {
            var shareData = new JsonData
            {
                ["channel"] = "article",
                ["templateId"] = templateId,
                ["query"] = "",
                ["title"] = title,
                ["desc"] = description
            };
            ShareAppMessageInternal(shareData, onSuccess, onFailed, onCancel);
        }

        #endregion

        #region 登录

        public void PalyerLogion()
        {
            bool force = true;
            TT.Login((string code, string anonymousCode, bool isLogin) =>
            {
                Debug.Log($"TT:登录成功 code:{code} anonymousCode:{anonymousCode} isLogin:{isLogin}");
            },
            (msg) =>
            {
                Debug.Log($"TestLogin: force:{force},{msg}");
            },
            force);
        }

        #endregion

        #region 排行榜 - 使用前先登录 - 已通过测试

        /// <summary>
        /// 发送排行榜数据
        /// </summary>
        /// <remarks>
        /// <para>dataType：数据类型 0=int 1=string</para>
        /// <para>value：展示的数值，dataType为0时只能传正数的字符串</para>
        /// <para>priority：优先级，数值越大优先级越高，默认为0</para>
        /// <para>zoneId：分区ID</para>
        /// <para>onResult：结果回调，bool为成功与否，string为消息</para>
        /// </remarks>
        public void SendRankInfo(
            int dataType,
            string value,
            int priority,
            string zoneId,
            Action<bool, string> onResult = null)
        {
            // 参数校验
            if (dataType == 0 && (!int.TryParse(value, out int intValue) || intValue < 0))
            {
                Debug.LogError("SendRankInfo: dataType为0时，value必须为正整数字符串");
                onResult?.Invoke(false, "value参数无效");
                return;
            }
            if (string.IsNullOrEmpty(zoneId))
            {
                Debug.LogError("SendRankInfo: zoneId不能为空");
                onResult?.Invoke(false, "zoneId参数无效");
                return;
            }

            var paramJson = new JsonData
            {
                ["dataType"] = dataType,
                ["value"] = value,
                ["priority"] = priority,
                ["zoneId"] = zoneId
            };
            Debug.Log($"SetImRankData param:{paramJson.ToJson()}");

            TT.SetImRankData(paramJson, (success, msg) =>
            {
                if (success)
                {
                    Debug.Log($"SetImRankData 成功: {msg}");
                }
                else
                {
                    Debug.LogError($"SetImRankData 失败: {msg}");
                }
                onResult?.Invoke(success, msg);
            });
        }

        /// <summary>
        /// 获取排行榜数据
        /// </summary>
        /// <remarks>
        /// <para>dataType：解析类型 0=int 1=string</para>
        /// <para>rankTitle：排行榜标题</para>
        /// <para>rankType：刷新时间间隔，day/week/month/all</para>
        /// <para>zoneId：分区ID，默认default/test</para>
        /// <para>relationType：关系类型（可选）</para>
        /// <para>suffix：后缀（可选）</para>
        /// <para>onResult：结果回调，bool为成功与否，string为消息</para>
        /// </remarks>
        public void RequestRankInfoList(
            int dataType,
            string rankTitle,
            string rankType,
            string zoneId = "default",
            string relationType = "",
            string suffix = "",
            Action<bool, string> onResult = null)
        {
            // 参数校验
            if (string.IsNullOrEmpty(rankType) ||
                !(rankType == "day" || rankType == "week" || rankType == "month" || rankType == "all"))
            {
                Debug.LogError("RequestRankInfoList: rankType参数无效");
                onResult?.Invoke(false, "rankType参数无效");
                return;
            }
            if (string.IsNullOrEmpty(rankTitle))
            {
                Debug.LogError("RequestRankInfoList: rankTitle不能为空");
                onResult?.Invoke(false, "rankTitle参数无效");
                return;
            }
            if (string.IsNullOrEmpty(zoneId))
            {
                Debug.LogError("RequestRankInfoList: zoneId不能为空");
                onResult?.Invoke(false, "zoneId参数无效");
                return;
            }

            var paramJson = new JsonData
            {
                ["rankType"] = rankType,
                ["dataType"] = dataType,
                ["relationType"] = relationType,
                ["suffix"] = suffix,
                ["rankTitle"] = rankTitle,
                ["zoneId"] = zoneId,
            };
            Debug.Log($"GetImRankList param:{paramJson.ToJson()}");
            TT.GetImRankList(paramJson, (success, msg) =>
            {
                if (success)
                {
                    Debug.Log($"GetImRankList 成功: {msg}");
                }
                else
                {
                    Debug.LogError($"GetImRankList 失败: {msg}");
                }
                onResult?.Invoke(success, msg);
            });
        }

        #endregion

        #region 签到 - 道具&礼包 - 文档信息较少后续补充


        #endregion

        #region 检查敏感词

        /// <summary>
        /// 检查敏感词
        /// </summary>
        /// <param name="word">需要检查的敏感词的字符串</param>
        /// <param name="success">是敏感词回调</param>
        /// <param name="failed">不是敏感词回调</param>
        public void CheckForSensitiveWords(string word, Action<bool> success, Action<string> failed)
        {
            TT.SensitiveWordCheck(word,
            success =>
            {
                Debug.Log(" 是否是敏感词:" + success);
            },
            failed =>
            {
                Debug.Log("检测失败，原因:" + failed);
            });
        }

        #endregion

        #region TTPlayerPrefs 

        public void SetTTPlayerPrefsValue(string key, int value)
        {
            playrePrefs.SetInt(key, value);
        }

        public void SetTTPlayerPrefsValue(string key, float value)
        {
            playrePrefs.SetFloat(key, value);
        }

        public void SetTTPlayerPrefsValue(string key, string value)
        {
            playrePrefs.SetString(key, value);
        }

        public void GetTTPlayerPrefsValue(string key, out int value)
        {
            value = playrePrefs.GetInt(key);
        }

        public void GetTTPlayerPrefsValue(string key, out float value)
        {
            value = playrePrefs.GetFloat(key);
        }

        public void GetTTPlayerPrefsValue(string key, out string value)
        {
            value = playrePrefs.GetString(key);
        }

        public void DeleteTTPlayerPrefsKey(string key)
        {
            playrePrefs.DeleteKey(key);
        }

        public void ClearAllTTPlayerPrefsKey()
        {
            playrePrefs.DeleteAll();
        }

        public void SaveTTPlayerPrefs()
        {
            playrePrefs.Save();
        }

        #endregion
    }
}