using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public Quest CurrentQuest { get; private set; }
    public int CurrentSteps { get; private set; }

    // Events expected by your existing scripts

    public event Action<Quest> OnQuestSelected;
    public event Action<int, int> OnProgressChanged; // current, target
    public event Action OnQuestCompleted;
    public event Action<string> OnPartUnlocked;      // partId

    // Unlock tracking (by partId = quest.Id)
    private readonly HashSet<string> unlockedParts = new HashSet<string>();

    private bool questCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectQuest(Quest quest)
    {
        if (quest == null) return;

        questCompleted = false;

        CurrentQuest = quest;
        CurrentSteps = 0;

        OnQuestSelected?.Invoke(CurrentQuest);
        OnProgressChanged?.Invoke(CurrentSteps, quest.Steps);
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
        if (CurrentQuest == null) return;

        var partId = CurrentQuest.Id;

        if (!string.IsNullOrEmpty(partId) && unlockedParts.Add(partId))
            OnPartUnlocked?.Invoke(partId);

        questCompleted = true;
        OnQuestCompleted?.Invoke();
    }

    public bool IsPartUnlocked(string partId)
    {
        if (string.IsNullOrEmpty(partId)) return false;
        return unlockedParts.Contains(partId);
    }
}
