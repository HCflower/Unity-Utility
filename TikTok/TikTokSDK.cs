/// =========================================================================
/// 类名：TikTok广告管理器
/// TTSDK版本：6.2.1+
/// 创建者：HCFlower
/// 创建时间：2025.12.11
/// =========================================================================
using FFramework.Architecture;
using System;
using TTSDK;
using UnityEngine;

namespace TikTokAdSdk
{
    // 确保组件
    [RequireComponent(typeof(MaiDian))]
    [RequireComponent(typeof(TTOpenAPI))]
    public class TikTokSDK : SingletonMono<TikTokSDK>
    {
        [Tooltip("App ID 主要是用于标识")] public string appid = "";

        #region 广告ID参数 => 请在这里配置你的广告ID
        [Tooltip("激励视频广告ID")] public string rewardedVideoId = "4ghcl0a9ajk1a9l91i";
        [Tooltip("Banner广告ID")] public string bannerId = "234as1kc2ci3hhdbfl";
        [Tooltip("插屏广告ID")] public string interstitialId = "84k0g95i95435fpoq1";

        private TTRewardedVideoAd rewardedVideoAd;
        private TTBannerAd bannerAd;
        private TTInterstitialAd interstitialAd;
        private TTBannerStyle bannerStyle;

        // 幂等初始化保护（可选）
        private static bool s_TTInited;

        // 时间控制字段
        private float gameStartTime = 0f;
        private float lastInterstitialAdTime = -1f;
        private float lastRewardedAdTime = -1f;

        private const float firstInterstitialForbiddenSeconds = 30f;
        private const float interstitialIntervalSeconds = 60f;
        private const float interstitialAfterRewardSeconds = 60f;
        #endregion

        #region 其它接口 - 待完善

        [SerializeField] private MaiDian maiDian;
        public MaiDian MaiDian => maiDian;
        [SerializeField] private TTOpenAPI ttOpenAPI;
        public TTOpenAPI TTOpenAPI => ttOpenAPI;

        #endregion

        #region 初始化方法

        protected override void InitializeSingleton()
        {
            gameStartTime = Time.time;

            // 直接赋值
            maiDian = GetComponent<MaiDian>();
            maiDian.Init(appid); // 初始化埋点

            ttOpenAPI = GetComponent<TTOpenAPI>();
            ttOpenAPI.Init();   // 初始化TTOpenAPI

            InitTTSDK();
            InitTTBannerStyle();
        }

        public void InitTTSDK()
        {
            // 幂等保护，防止与其它类重复 InitSDK
            if (!s_TTInited)
            {
                TT.InitSDK((code, msg) =>
                {
                    Debug.Log($"TikTok SDK初始化完成: {code} - {msg}");
                });
                s_TTInited = true;
            }
        }

        private void InitTTBannerStyle()
        {
            if (!string.IsNullOrEmpty(bannerId))
            {
                Debug.Log("初始化Banner广告ID：" + bannerId);
                bannerStyle = new TTBannerStyle
                {
                    left = 10,
                    top = 10,
                    width = 320,
                };
            }
        }

        protected override void OnDestroy()
        {
            if (rewardedVideoAd != null)
            {
                rewardedVideoAd.Destroy();
                rewardedVideoAd = null;
            }
            if (bannerAd != null)
            {
                bannerAd.Destroy();
                bannerAd = null;
            }
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }
        }

        #endregion

        #region 激励视频广告方法

        public void ShowRewardedVideoAd(Action onSuccess, Action onFailure)
        {
            // 防重入：已有实例意味着正在加载/展示
            if (rewardedVideoAd != null)
            {
                Debug.LogWarning("已有激励视频广告正在加载或展示，忽略本次调用。");
                onFailure?.Invoke();
                return;
            }

            // TODO：发送用户关键行为
            MaiDian?.SendGameAddiction();

            CreateRewardedVideoAdParam param = new CreateRewardedVideoAdParam
            {
                AdUnitId = rewardedVideoId,
            };
            rewardedVideoAd = TT.CreateRewardedVideoAd(param);

            rewardedVideoAd.OnClose += (isEnded, count) =>
            {
                if (isEnded)
                {
                    Debug.Log($"激励视频广告观看完成，发放奖励，次数=>{count}");
                    lastRewardedAdTime = Time.time;
                    onSuccess?.Invoke();
                    // TODO：发送用户关键行为
                    MaiDian?.SendGameAddiction();
                }
                else
                {
                    Debug.Log("激励视频广告未观看完成，未发放奖励");
                    onFailure?.Invoke();
                }
                rewardedVideoAd = null;
            };

            rewardedVideoAd.OnError += (code, msg) =>
            {
                Debug.Log($"激励视频广告加载/展示失败: {code} - {msg}");
                onFailure?.Invoke();
                rewardedVideoAd = null;
            };

            rewardedVideoAd.OnLoad += () =>
            {
                Debug.Log("激励视频广告加载成功，准备展示");
                rewardedVideoAd.Show();

                // TODO: 发送用户行为 / LTROI
                Debug.LogWarning("第一次尝试roi 回调 game_addition 方法回传");
                MaiDian.SendGameAddiction();
                Debug.LogWarning("第二次尝试roi 回调 SendLTROI 方法回传");
                MaiDian.SendLTROI();
            };

            Debug.Log("开始加载激励视频广告");
            rewardedVideoAd.Load();
        }

        public void ShowMultitonRewardedAd(Action onSuccess, Action onFailure, int rewardTimes = 2,
            string[] rewardMessages = null, bool progressTip = true)
        {
            if (rewardedVideoAd != null)
            {
                Debug.LogWarning("已有激励视频广告正在加载或展示，忽略本次调用。");
                onFailure?.Invoke();
                return;
            }

            // TODO：发送用户关键行为
            MaiDian?.SendGameAddiction();

            if (rewardMessages == null || rewardMessages.Length == 0)
            {
                rewardMessages = new string[]
                {
                    "观看完整视频，领取奖励！",
                    "继续观看，奖励更多！",
                    "再坚持一下，马上有奖励！",
                };
            }

            CreateRewardedVideoAdParam param = new CreateRewardedVideoAdParam
            {
                AdUnitId = rewardedVideoId,
                Multiton = true,
                MultitonRewardTimes = Mathf.Clamp(rewardTimes, 1, 4),
                MultitonRewardMsg = new System.Collections.Generic.List<string>(rewardMessages),
                ProgressTip = progressTip
            };
            rewardedVideoAd = TT.CreateRewardedVideoAd(param);

            rewardedVideoAd.OnClose += (isEnded, count) =>
            {
                if (isEnded)
                {
                    Debug.Log($"激励视频广告观看完成，发放奖励，次数=>{count}");
                    lastRewardedAdTime = Time.time;
                    onSuccess?.Invoke();

                    // TODO：发送用户关键行为
                    MaiDian?.SendGameAddiction();
                }
                else
                {
                    Debug.Log("激励视频广告未观看完成，未发放奖励");
                    onFailure?.Invoke();
                }
                rewardedVideoAd = null;
            };

            rewardedVideoAd.OnError += (code, msg) =>
            {
                Debug.Log($"激励视频广告加载/展示失败: {code} - {msg}");
                onFailure?.Invoke();
                rewardedVideoAd = null;
            };

            rewardedVideoAd.OnLoad += () =>
            {
                Debug.Log("激励视频广告加载成功，准备展示");
                rewardedVideoAd.Show();

                // TODO: 发送用户行为 / LTROI
                MaiDian.SendGameAddiction();
                MaiDian.SendLTROI();
            };

            Debug.Log($"开始加载再得激励视频广告，次数：{rewardTimes}");
            rewardedVideoAd.Load();
        }

        #endregion

        #region Banner广告方法

        public void ShowBannerAd()
        {
            // 若已有无效实例则清理
            if (bannerAd != null && bannerAd.IsInvalid())
            {
                bannerAd.Destroy();
                bannerAd = null;
            }

            if (bannerAd == null)
            {
                CreateBannerAdParam param = new CreateBannerAdParam
                {
                    BannerAdId = bannerId,
                    Style = bannerStyle,
                    AdIntervals = 60
                };
                bannerAd = TT.CreateBannerAd(param);

                bannerAd.OnError += (errorCode, errorMsg) =>
                {
                    Debug.LogError($"Banner广告错误: {errorCode} - {errorMsg}");
                };
                bannerAd.OnClose += () =>
                {
                    Debug.Log("Banner广告关闭");
                };
                bannerAd.OnResize += (width, height) =>
                {
                    Debug.Log($"Banner广告尺寸改变: width={width}, height={height}");
                };
                bannerAd.OnLoad += () =>
                {
                    Debug.Log("Banner广告加载完成");
                    bannerAd?.Show();
                };
            }
        }

        public void HideBannerAd()
        {
            if (bannerAd != null)
            {
                bannerAd.Hide();
                bannerAd.Destroy();
                bannerAd = null;
            }
        }

        public void ResizeBannerAd(int top, int left, int width)
        {
            if (bannerAd != null)
            {
                bannerStyle.top = top;
                bannerStyle.left = left;
                bannerStyle.width = width;
                bannerAd.ReSize(bannerStyle);
            }
        }
        #endregion

        #region 插屏广告方法

        public void ShowInterstitialAd(Action onSuccess = null, Action onFailure = null)
        {
            // 防重入：正在加载/展示中的插屏
            if (interstitialAd != null)
            {
                Debug.LogWarning("已有插屏广告正在加载或展示，忽略本次调用。");
                onFailure?.Invoke();
                return;
            }

            float now = Time.time;

            float sinceGameStart = now - gameStartTime;
            if (sinceGameStart < firstInterstitialForbiddenSeconds)
            {
                Debug.Log($"[插屏限制] 启动后前{firstInterstitialForbiddenSeconds}s禁止展示插屏，当前已运行: {sinceGameStart:F2}s");
                onFailure?.Invoke();
                return;
            }

            if (lastInterstitialAdTime > 0f)
            {
                float sinceLastInterstitial = now - lastInterstitialAdTime;
                if (sinceLastInterstitial < interstitialIntervalSeconds)
                {
                    Debug.Log($"[插屏限制] 距离上一次插屏不足{interstitialIntervalSeconds}s，当前间隔: {sinceLastInterstitial:F2}s");
                    onFailure?.Invoke();
                    return;
                }
            }

            if (lastRewardedAdTime > 0f)
            {
                float sinceLastRewarded = now - lastRewardedAdTime;
                if (sinceLastRewarded < interstitialAfterRewardSeconds)
                {
                    Debug.Log($"[插屏限制] 距离上一次激励视频不足{interstitialAfterRewardSeconds}s，当前间隔: {sinceLastRewarded:F2}s");
                    onFailure?.Invoke();
                    return;
                }
            }

            CreateInterstitialAdParam param = new CreateInterstitialAdParam
            {
                InterstitialAdId = interstitialId,
            };

            interstitialAd = TT.CreateInterstitialAd(param);

            interstitialAd.OnError += (code, msg) =>
            {
                Debug.LogError($"插屏广告加载失败: {code} - {msg}");
                onFailure?.Invoke();
                interstitialAd = null;
            };

            interstitialAd.OnClose += () =>
            {
                Debug.Log("插屏广告关闭");
                lastInterstitialAdTime = Time.time;
                onSuccess?.Invoke();
                interstitialAd = null;
            };

            interstitialAd.OnLoad += () =>
            {
                Debug.Log("插屏广告加载成功，准备展示");
                interstitialAd.Show();
            };

            Debug.Log("开始加载插屏广告");
            interstitialAd.Load();
        }

        #endregion
    }
}