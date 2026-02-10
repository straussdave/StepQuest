using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public Quest CurrentQuest { get; private set; }
    public int CurrentSteps { get; private set; }

    public event Action<int, int> OnProgressChanged;
    public event Action<string> OnPartUnlocked;
    public event Action<Quest> OnQuestSelected;
    public event Action<Quest> OnQuestCompleted;

    private readonly HashSet<string> unlockedParts = new HashSet<string>();

    private bool questCompleted;

    const string UNLOCKED_KEY = "UNLOCKED_PART_IDS"; // PlayerPrefs key

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadUnlockedParts();
    }

    public void SelectQuest(Quest quest)
    {
        if (quest == null) return;

        CurrentQuest = quest;
        CurrentSteps = 0;
        questCompleted = false;

        OnQuestSelected?.Invoke(CurrentQuest);
        OnProgressChanged?.Invoke(CurrentSteps, CurrentQuest.Steps);
    }

    public void AddSteps(int delta)
    {
        if (CurrentQuest == null) return;
        if (questCompleted) return;

        CurrentSteps = Mathf.Clamp(CurrentSteps + delta, 0, CurrentQuest.Steps);
        OnProgressChanged?.Invoke(CurrentSteps, CurrentQuest.Steps);

        if (CurrentSteps >= CurrentQuest.Steps)
            CompleteQuest();
    }


    private void CompleteQuest()
    {
        if (questCompleted) return;
        questCompleted = true;

        var partId = CurrentQuest.Id;
        if (!string.IsNullOrEmpty(partId) && unlockedParts.Add(partId))
        {
            SaveUnlockedParts();
            OnPartUnlocked?.Invoke(partId);
        }

        OnQuestCompleted?.Invoke(CurrentQuest);
    }

    public bool IsPartUnlocked(string partId)
        => !string.IsNullOrEmpty(partId) && unlockedParts.Contains(partId);

    private void SaveUnlockedParts()
    {
        // store as "id1|id2|id3"
        var s = string.Join("|", unlockedParts);
        PlayerPrefs.SetString(UNLOCKED_KEY, s);
        PlayerPrefs.Save();
    }

    private void LoadUnlockedParts()
    {
        unlockedParts.Clear();

        var s = PlayerPrefs.GetString(UNLOCKED_KEY, "");
        if (string.IsNullOrEmpty(s)) return;

        var ids = s.Split('|');
        foreach (var id in ids)
            if (!string.IsNullOrEmpty(id))
                unlockedParts.Add(id);
    }
}
