using System;
using UnityEngine;

public static class DateUtil
{
    public static int TodayKey(DateTime? now = null)
    {
        var d = (now ?? DateTime.Now).Date;
        return d.Year * 10000 + d.Month * 100 + d.Day;
    }

    public static bool HasDoneQuestToday(DateTime? now = null)
    {
        return PlayerPrefs.GetInt(SaveKeys.LAST_QUEST_DAY_KEY, 0) == TodayKey(now);
    }

    public static void MarkQuestDoneToday(DateTime? now = null)
    {
        EnsureStartDayExists(now);

        int today = TodayKey(now);

        PlayerPrefs.SetInt(SaveKeys.LAST_QUEST_DAY_KEY, today);

        // Keep your old flag too, if other code still depends on it.
        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 1);

        PlayerPrefs.Save();
    }

    public static void EnsureStartDayExists(DateTime? now = null)
    {
        if (!PlayerPrefs.HasKey(SaveKeys.START_DAY_KEY))
        {
            PlayerPrefs.SetInt(SaveKeys.START_DAY_KEY, TodayKey(now));
            PlayerPrefs.Save();
        }
    }

    public static bool HasStartDay()
    {
        return PlayerPrefs.HasKey(SaveKeys.START_DAY_KEY);
    }

    public static int GetStartDayKey()
    {
        return PlayerPrefs.GetInt(SaveKeys.START_DAY_KEY, 0);
    }

    public static int GetCurrentSol(DateTime? now = null)
    {
        EnsureStartDayExists(now);

        int startKey = PlayerPrefs.GetInt(SaveKeys.START_DAY_KEY, 0);
        DateTime startDate = KeyToDate(startKey);
        DateTime currentDate = (now ?? DateTime.Now).Date;

        int dayDifference = (currentDate - startDate).Days;

        // First day is Sol 1
        return Mathf.Max(1, dayDifference + 1);
    }

    private static DateTime KeyToDate(int key)
    {
        int year = key / 10000;
        int month = (key / 100) % 100;
        int day = key % 100;

        return new DateTime(year, month, day);
    }

    public static void Clear()
    {
        SaveSystem.DeleteKey(SaveKeys.LAST_QUEST_DAY_KEY);
        SaveSystem.DeleteKey(SaveKeys.START_DAY_KEY);
        SaveSystem.DeleteKey(SaveKeys.QUEST_DONE_TODAY);
        PlayerPrefs.Save();
    }
}