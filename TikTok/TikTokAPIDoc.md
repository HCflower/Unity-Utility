## **抖音开放平台 - 小游戏API接口文**档

一、广告API

<div style="max-width: 800px; overflow-x: auto;">

|API名称 |API介绍 |API参数介绍 |API调用方法 |
|---|:---|---|---|
|ShowRewardedVideoAd(Action onSuccess,Action onFailure) |显示激励视频广告 |onSuccess：播放成功回调  onFailure：播放失败回调 |ADHandler.Instance.ShowRewardedVideoAd( onSuccess: () =>{ },onFailure:()⇒{ }); |
|ShowMultitonRewardedAd(  Action onSuccess, Action onFailure, int rewardTimes = 2, string[] rewardMessages = null, bool progressTip = true) |显示多次播放激励视频广告 |onSuccess：播放成功回调  onFailure：播放失败回调  rewardTimes：连续播放次数  rewardMessages：激励文本信息  progressTip：进度提示 |ADHandler.Instance.ShowMultitonRewardedAd(\ onSuccess: () =>{ },onFailure:()⇒{ }); |
|ShowBannerAd() |显示横幅广告 |无 |ADHandler.Instance.ShowBannerAd(); |
|HideBannerAd() |隐藏横幅广告 |无 |ADHandler.Instance.HideBannerAd(); |
|ResizeBannerAd(int top, int left, int width) |调整横幅广告尺寸 |top：离上边距离，left：离左边距离，width：宽度 |ADHandler.Instance.ResizeBannerAd(200,200,250) |
|ShowInterstitialAd(Action onSuccess = null, Action onFailure = null) |显示插屏广告 |onSuccess：播放成功回调  onFailure：播放失败回调 |ADHandler.Instance.ShowInterstitialAd(  onSuccess: () =>{ },onFailure:()⇒{ }); |
</div>

二、OpenAPI

|API名称 |API介绍 |API参数介绍 |API调用方法 |
|---|---|---|---|
|OnNavigateToScene(Action success, Action failure) |显示侧边栏 |success：成功回调  failure：失败回调 |ADHandler.Instance.TTOpenAPI.\ OnNavigateToScene(success:()⇒{ },failure:()⇒{ }); |
|ShowFour() |显示互推资源位 |无 |ADHandler.Instance.TTOpenAPI.ShowFour(); |
|HideFour() |隐藏互推资源位 |无 |ADHandler.Instance.TTOpenAPI.HideFour(); |
|DestroyFour() |销毁互推资源位 |无 |ADHandler.Instance.TTOpenAPI.DestroyFour(); |
|RequestSubscribeMessage() |请求订阅申请 |无 |ADHandler.Instance.TTOpenAPI.RequestSubscribeMessage(); |
|ShareAppMessageLink(string templateId, string title, string description, Action onSuccess = null, Action<string> onFailed = null, Action onCancel = null) |文本链接分享 |templateId：申请通过的ID，title：标题，description：描述，onSuccess:成功回调，onFailed：失败回调，onCancel：取消回调 |ADHandler.Instance.TTOpenAPI.ShareAppMessageLink("*****"，  "Game Name","这个游戏真好玩"，onSuccess:()⇒{ },onFailed:(err)⇒{ },onCancel:()⇒{ }); |
|ShareAppMessageImage(string templateId, string title, string description, Action onSuccess = null, Action<string> onFailed = null, Action onCancel = null) |图文链接分享 |templateId：申请通过的ID，title：标题，description：描述，onSuccess:成功回调，onFailed：失败回调，onCancel：取消回调 |ADHandler.Instance.TTOpenAPI.ShareAppMessageImage("*****"，  "Game Name","这个游戏真好玩"，onSuccess:()⇒{ },onFailed:(err)⇒{ },onCancel:()⇒{ }); |
|PalyerLogion() |玩家登录 |无 |ADHandler.Instance.TTOpenAPI.PalyerLogion(); |
|SendRankInfo( int dataType, string value, int priority, string zoneId, Action<bool, string> onResult = null) |发送排行榜信息(⚠️：使用排行榜之前要先登录) |dataType：排行依据类型(0：Int，1：String)，  value：数值，priority：优先级，zoneId：分区Id(默认为：default/Test),  onResult:结果回调 |ADHandler.Instance.TTOpenAPI.SendRankInfo(\ 0,"999",0,"default"，(b,s)⇒{ }); |
|RequestRankInfoList( int dataType, string rankTitle, string rankType, string zoneId = "default", string relationType = "", string suffix = "", Action<bool, string> onResult = null) |请求排行榜信息 |排行依据类型(0：Int，1：String)，rankTitle：排行榜标题，rankType：刷新间隔时间(day,week,month/all),  zoneId：分区Id(默认为：default/Test),relationType:关系类型，suffix：排行依据信息后缀，onResult:结果回调 |ADHandler.Instance.TTOpenAPI.SendRankInfo(\ 0,"分数排行","day","default"，onResult:(b,s)⇒{ }); |
|CheckForSensitiveWords(string word, Action<bool> success, Action<string> failed) |检查敏感词 |word：需要检查的词，success：成功回调，failed:失败回调 |ADHandler.Instance.TTOpenAPI.CheckForSensitiveWords("杀死小日本"，success:(b)⇒{ },failed:(s)⇒{ }); |
|SetTTPlayerPrefsValue(string,T); |保存数据到TTPlayerPrefs ← 需要new和Unity  PlayerPrefs用法相同 |string：数据key值，T数据类型 |ADHandler.Instance.TTOpenAPI.SetTTPlayerPrefsValue("小日子被种蘑菇数"，2)； |
|GetTTPlayerPrefsValue(string,T); |获取TTPlayerPrefs数据与Unity  PlayerPrefs用法相同 |string：数据key值，T数据类型 |ADHandler.Instance.TTOpenAPI.GetTTPlayerPrefsValue("小日子被种蘑菇数"，out int count)； |
|SaveTTPlayerPrefs() |保存TTPlayerPrefs数据 |无 |ADHandler.Instance.TTOpenAPI.SaveTTPlayerPrefs(); |
|DeleteTTPlayerPrefsKey(string key) |删除TTPlayerPrefs数据 |key：数据名称 |ADHandler.Instance.TTOpenAPI.DeleteTTPlayerPrefsKey("小日子被种蘑菇数"); |
|ClearAllTTPlayerPrefsKey() |清理所有TTPlayerPrefs数据 |无 |ADHandler.Instance.TTOpenAPI.ClearAllTTPlayerPrefsKey(); |


