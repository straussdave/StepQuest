using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private QuestDB questDb;

    public event Action<int, int> OnProgressChanged;
    public event Action<string> OnPartUnlocked;
    public event Action<Quest> OnQuestSelected;
    public event Action<Quest> OnQuestCompleted;

    private readonly HashSet<string> unlockedParts = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadActiveQuestState();
        LoadUnlockedParts();
    }

    public void SelectQuest(Quest quest)
    {
        if (quest == null) return;

        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 0);
        PlayerPrefs.SetString(SaveKeys.ACTIVE_QUEST_ID, quest.Id);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 1);
        SaveSystem.DeleteKey(SaveKeys.NEXT_DAY_TEXT_KEY);
        PlayerPrefs.SetString(SaveKeys.NEXT_DAY_TEXT_KEY, quest.nextDayText);
        PlayerPrefs.Save();
        Quest currentQuest = GetCurrentQuest();
        OnQuestSelected?.Invoke(currentQuest);
        OnProgressChanged?.Invoke(0, currentQuest.Steps);
    }

    public void AddSteps(int delta)
    {
        Quest quest = GetCurrentQuest();
        if (quest == null) return;
        if (QuestDoneToday()) return;

        int steps = GetCurrentSteps();
        int newSteps = Mathf.Clamp(steps + delta, 0, quest.Steps);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, newSteps);
        PlayerPrefs.Save();
        OnProgressChanged?.Invoke(newSteps, quest.Steps);
        if (newSteps >= quest.Steps)
            CompleteQuest();
    }


    private void CompleteQuest()
    {
        if (DateUtil.HasDoneQuestToday())
        {
            return;
        }

        if (QuestDoneToday()) return;
        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 1);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0);
        PlayerPrefs.Save();

        var partId = PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID);
        if (!string.IsNullOrEmpty(partId) && unlockedParts.Add(partId))
        {
            SaveUnlockedParts();
            OnPartUnlocked?.Invoke(partId);
        }
        
        DateUtil.MarkQuestDoneToday();
        Quest quest = GetCurrentQuest();
        OnQuestCompleted?.Invoke(quest);
    }

    public bool IsPartUnlocked(string partId)
        => !string.IsNullOrEmpty(partId) && unlockedParts.Contains(partId);

    private void SaveUnlockedParts()
    {
        // store as "id1|id2|id3"
        var s = string.Join("|", unlockedParts);
        PlayerPrefs.SetString(SaveKeys.UNLOCKED_KEY, s);
        PlayerPrefs.Save();
    }

    private void LoadUnlockedParts()
    {
        unlockedParts.Clear();

        var s = PlayerPrefs.GetString(SaveKeys.UNLOCKED_KEY, "");
        if (string.IsNullOrEmpty(s)) return;

        var ids = s.Split('|');
        foreach (var id in ids)
            if (!string.IsNullOrEmpty(id))
                unlockedParts.Add(id);
    }

    public bool CanCompleteQuestToday()
    {
        return !DateUtil.HasDoneQuestToday();
    }

    private void LoadActiveQuestState()
    {
        if (questDb == null)
        {
            Debug.LogError("QuestManager: questDb is not assigned.", this);
            return;
        }

        var isActive = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0) == 1;
        if (!isActive) return;

        var questId = PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID, "");
        if (string.IsNullOrEmpty(questId)) return;

        Quest quest = GetCurrentQuest();

        int currentSteps = GetCurrentSteps();

        OnQuestSelected?.Invoke(quest);
        OnProgressChanged?.Invoke(currentSteps, quest.Steps);
    }

    private void ResetQuestState()
    {
        PlayerPrefs.DeleteKey(SaveKeys.ACTIVE_QUEST_ID);
        PlayerPrefs.DeleteKey(SaveKeys.ACTIVE_QUEST_STEPS);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0);
        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 0);
        PlayerPrefs.Save();
    }

    public bool QuestDoneToday()
    {
        return PlayerPrefs.GetInt(SaveKeys.QUEST_DONE_TODAY, 0) == 1;
    }

    public Quest GetCurrentQuest()
    {
        var questId = PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID, "");
        if (string.IsNullOrEmpty(questId)) return null;
        questDb.TryGetById(questId, out var quest);
        return quest;
    }

    public int GetCurrentSteps()
    {
        return PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
    }
}
