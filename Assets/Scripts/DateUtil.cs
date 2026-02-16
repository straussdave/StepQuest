using System;
using UnityEngine;

public class DateUtil
{
    private const string LastQuestDayKey = "STEPQUEST_LAST_QUEST_DAY"; // int YYYYMMDD

    public static int TodayKey(DateTime? now = null)
    {
        var d = (now ?? DateTime.Now).Date;
        return d.Year * 10000 + d.Month * 100 + d.Day;
    }

    public static bool HasDoneQuestToday(DateTime? now = null)
        => PlayerPrefs.GetInt(LastQuestDayKey, 0) == TodayKey(now);

    public static void MarkQuestDoneToday(DateTime? now = null)
    {
        PlayerPrefs.SetInt(LastQuestDayKey, TodayKey(now));
        PlayerPrefs.Save();
    }

    // Optional: for debug/testing
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(LastQuestDayKey);
        PlayerPrefs.Save();
    }
}
