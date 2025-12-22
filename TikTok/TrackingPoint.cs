using System.Collections.Generic;
using UnityEngine;
using TTSDK;
using System;

public class TrackingPoint
{
    /// <summary>
    /// 发送埋点信息，并输出事件执行结果和系统时间
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="trackingPointName">埋点点位名称</param>
    /// <returns>事件是否执行成功</returns>
    public bool SendTrackingPointInfo(string eventName, string trackingPointName)
    {
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        Dictionary<string, string> trackingInfo = new Dictionary<string, string>
        {
            { "trackingPointName", trackingPointName },
            { "trackingTime", time }
        };

        bool success = false;
        try
        {
            TT.ReportAnalytics(eventName, trackingInfo);
            success = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackingPoint] 埋点事件发送失败: {eventName}, 错误: {ex.Message}");
        }

        Debug.Log($"[TrackingPoint] 埋点事件: {eventName}, 点位: {trackingPointName}, 执行结果: {(success ? "成功" : "失败")}, 系统时间: {time}");

        return success;
    }
}