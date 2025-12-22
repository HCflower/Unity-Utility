using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using System;
using TTSDK;

namespace TikTokAdSdk
{
    public class MaiDian : MonoBehaviour
    {

        [Header("配置参数")]
        private string appid;
        [Tooltip("在这里填入你的远程地址，不带 https://")]
        [SerializeField]
        private string remoteUrl = "proxy.qingningyule.cn";

        private const string LastClaimKey = "maomiaidian";
        private const string OpenIdKey = "maomiopenidKey";
        private string clickId;
        private TTSystemInfo platformInfo;

        public void Init(string appid)
        {
            this.appid = appid;
            HandleClickId();   // 激活埋点
            InitOpenId();      // OpenId 上报
        }

        #region —— Active 激活埋点 ——

        private void HandleClickId()
        {
            clickId = PlayerPrefs.GetString("xxxClickId", null);
            if (string.IsNullOrEmpty(clickId))
                ExtractClickIdFromLaunch();

            if (string.IsNullOrEmpty(clickId) || !CanClaimReward("")) return;

            SendConversion("active", clickId);

            // 本月内只激活一次
            PlayerPrefs.SetString(LastClaimKey + "", DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
        }

        private void ExtractClickIdFromLaunch()
        {
            if (TT.s_ContainerEnv == null) return;
            var opt = TT.GetLaunchOptionsSync();
            if (opt?.Query != null && opt.Query.TryGetValue("ad_params", out var raw) && !string.IsNullOrEmpty(raw))
            {
                string decoded = Uri.UnescapeDataString(raw);
                Debug.Log($"[LaunchOption] ad_params: {decoded}");
                try
                {
                    var j = JObject.Parse(decoded);
                    clickId = j.SelectToken("log_extra.clickid")?.ToString()
                            ?? j["clickid"]?.ToString();
                    if (!string.IsNullOrEmpty(clickId))
                    {
                        PlayerPrefs.SetString("xxxClickId", clickId);
                        PlayerPrefs.Save();
                        Debug.Log($"提取到 clickId: {clickId}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"解析 ad_params JSON 失败: {ex}");
                }
            }
        }

        #endregion

        #region —— 关键行为埋点 ——

        public void SendLTROI()
        {
            if (string.IsNullOrEmpty(clickId))
            {
                Debug.LogWarning("================================================clickid+++++++++++Error");
                return;
            }

            SendConversion("lt_roi", clickId);
        }

        /// <summary>
        /// 外部在关键行为（如看完广告）后调用此方法触发 game_addiction 埋点
        /// </summary>
        public void SendGameAddiction()
        {
            if (string.IsNullOrEmpty(clickId))
            {
                Debug.LogWarning("game_addiction：clickId 为空，无法发送");
                return;
            }
            SendConversion("game_addiction", clickId);
        }

        #endregion

        #region —— 统一 Conversion 发送 ——

        public void SendConversion(string eventType, string clickid)
        {
            var payload = new Dictionary<string, object>
            {
                ["event_type"] = eventType,
                ["context"] = new Dictionary<string, object>
                {
                    ["ad"] = new Dictionary<string, object>
                    {
                        ["callback"] = clickid
                    }
                },
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string json = JsonConvert.SerializeObject(payload);
            string url = "https://analytics.oceanengine.com/api/v2/conversion";

            SendRequest(
                url,
                "POST",
                json,
                null,
                raw => Debug.Log($"[{eventType}] 回包原始: {raw}")
            );
        }

        #endregion

        #region —— OpenID 上报 ——

        private void InitOpenId()
        {
            if (TT.s_ContainerEnv == null) return;

            platformInfo = TT.GetSystemInfo();
            if (platformInfo == null) return;

            if (!CanClaimReward(platformInfo.model)) return;

            TT.Login(
                (code, anon, isLogin) =>
                {
                    if (!isLogin) return;
                    TT.GetUserInfo(
                        (ref TTUserInfo ui) => OnGotUserInfo(code, anon, ui),
                        err => Debug.LogError($"GetUserInfo 失败: {err}")
                    );
                },
                err => Debug.LogError($"Login 失败: {err}"),
                true
            );
        }

        private void OnGotUserInfo(string code, string anon, TTUserInfo user)
        {
            var req = new Dictionary<string, object>
            {
                ["appid"] = appid,
                ["code"] = code,
                ["anonymousCode"] = anon,
                ["system"] = platformInfo.system,
                ["platform"] = platformInfo.platform,
                ["brand"] = platformInfo.brand,
                ["modelx"] = platformInfo.model,
                ["version"] = platformInfo.system,
                ["appName"] = platformInfo.hostName,
                ["SDKVersion"] = platformInfo.sdkVersion,
                ["nickname"] = user.nickName ?? "",
                ["avatar"] = user.avatarUrl ?? "",
                ["gender"] = user.gender,
                ["city"] = user.city ?? "",
                ["province"] = user.province ?? "",
                ["country"] = user.country ?? ""
            };

            string json = JsonConvert.SerializeObject(req);
            string url = $"https://{remoteUrl}/ecpm/maiDian/callback";

            SendRequest(
                url,
                "POST",
                json,
                dict =>
                {
                    if (dict.TryGetValue("d", out var dObj)
                        && dObj is JObject dJ
                        && dJ.TryGetValue("user", out var uObj)
                        && uObj is JObject uJ
                        && uJ.TryGetValue("openid", out var o))
                    {
                        string openid = o.ToString();
                        PlayerPrefs.SetString(OpenIdKey, openid);
                        PlayerPrefs.SetString(LastClaimKey + platformInfo.model, DateTime.UtcNow.ToString("o"));
                        PlayerPrefs.Save();
                        Debug.Log($"openid 上报成功: {openid}");
                    }
                },
                raw => Debug.Log($"[OpenID 回包] {raw}")
            );
        }

        #endregion

        #region —— 公共工具 ——

        private bool CanClaimReward(string key)
        {
            var last = PlayerPrefs.GetString(LastClaimKey + key, null);

            if (string.IsNullOrEmpty(last)) return true;
            return DateTime.Parse(last) < DateTime.UtcNow.AddMonths(-1);
        }

        private void SendRequest(
            string url,
            string method,
            string jsonData,
            Action<Dictionary<string, object>> callbackDict,
            Action<string> callbackRaw = null)
        {
            var opts = new TTRequest.InnerOptions
            {
                Method = method,
                Data = jsonData
            };
            opts.Header["content-type"] = "application/json";
            // TODO：优化一下
            TT.Request(
                url,
                opts,
                res =>
                {
                    if (res.StatusCode != 200)
                    {
                        Debug.LogError($"HTTP {res.StatusCode} 错误: {res.ErrMsg}");
                        return;
                    }
                    callbackRaw?.Invoke(res.Data);

                    if (callbackDict != null)
                    {
                        try
                        {
                            var j = JObject.Parse(res.Data);
                            var d = j.ToObject<Dictionary<string, object>>();
                            callbackDict(d);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"字典解析失败: {ex}");
                        }
                    }
                },
                err => Debug.LogError($"请求失败: {err.ErrMsg}")
            );
        }

        #endregion
    }
}
