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
    public event Action OnAllQuestsCompleted;

    private readonly HashSet<string> unlockedParts = new HashSet<string>();
    private bool allQuestsCompletedFired = false;

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
        LoadActiveQuestState();

        CheckAllQuestsCompleted();
    }

    public void SelectQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("[UserAction] Tried selecting a null quest.");
            return;
        }

        Debug.Log($"[UserAction] Selecting quest: {quest.Id} ({quest.PartName}), targetSteps={quest.Steps}.");

        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 0);
        PlayerPrefs.SetString(SaveKeys.ACTIVE_QUEST_ID, quest.Id);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 1);

        SaveSystem.DeleteKey(SaveKeys.NEXT_DAY_TEXT_KEY);
        PlayerPrefs.SetString(SaveKeys.NEXT_DAY_TEXT_KEY, quest.nextDayText);

        PlayerPrefs.Save();

        Quest currentQuest = GetCurrentQuest();
        if (currentQuest == null)
        {
            Debug.LogError("[Quest] Selected quest could not be reloaded from QuestDB.");
            return;
        }

        OnQuestSelected?.Invoke(currentQuest);
        OnProgressChanged?.Invoke(0, currentQuest.Steps);
    }

    public void AddSteps(int delta)
    {
        Quest quest = GetCurrentQuest();
        if (quest == null)
        {
            Debug.Log("[StepTracking] Ignored step delta because there is no active quest.");
            return;
        }

        if (QuestDoneToday())
        {
            Debug.Log("[StepTracking] Ignored step delta because quest is already done today.");
            return;
        }

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
            Debug.Log("[Quest] Completion blocked because DateUtil indicates quest already completed today.");
            return;
        }

        if (QuestDoneToday())
        {
            Debug.Log("[Quest] Completion blocked because quest is already marked done today.");
            return;
        }

        // Capture quest BEFORE changing flags / state
        Quest completedQuest = GetCurrentQuest();
        string partId = completedQuest != null ? completedQuest.Id : PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID);

        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 1);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(partId) && unlockedParts.Add(partId))
        {
            Debug.Log($"[Quest] Unlocked part: {partId}.");
            SaveUnlockedParts();

            PlayerPrefs.SetString(SaveKeys.LAST_UNLOCKED_PART_ID, partId);
            PlayerPrefs.SetInt(SaveKeys.PENDING_COLLECTION_HIGHLIGHT, 1);
            PlayerPrefs.Save();

            OnPartUnlocked?.Invoke(partId);
        }

        Debug.Log($"[Quest] Quest completed: {(completedQuest != null ? completedQuest.Id : partId)}.");

        DateUtil.MarkQuestDoneToday();

        OnQuestCompleted?.Invoke(completedQuest);

        // Fire end-sequence trigger if everything is now unlocked
        CheckAllQuestsCompleted();
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
        {
            if (!string.IsNullOrEmpty(id))
                unlockedParts.Add(id);
        }
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
        if (quest == null)
        {
            Debug.LogWarning($"[Quest] Active quest id '{questId}' could not be found in QuestDB.");
            return;
        }

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
        if (questDb == null)
        {
            Debug.LogError("QuestManager: questDb is not assigned.", this);
            return null;
        }

        var questId = PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID, "");
        if (string.IsNullOrEmpty(questId)) return null;

        questDb.TryGetById(questId, out var quest);
        return quest;
    }

    public int GetCurrentSteps()
    {
        return PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
    }

    public bool CheckAllQuestsCompleted()
    {
        if (allQuestsCompletedFired) return true;

        if (questDb == null)
        {
            Debug.LogWarning("[Quest] Cannot check all quests completed because questDb is null.");
            return false;
        }

        var allQuests = questDb.AllQuests; // assumes QuestDB exposes this
        if (allQuests == null || allQuests.Count == 0)
        {
            Debug.LogWarning("[Quest] QuestDB has no quests. Skipping all-quests-completed check.");
            return false;
        }

        for (int i = 0; i < allQuests.Count; i++)
        {
            var q = allQuests[i];
            if (q == null) continue;

            if (!IsPartUnlocked(q.Id))
                return false; // still unfinished quests exist
        }

        allQuestsCompletedFired = true;
        Debug.Log("[Quest] All quests completed.");
        OnAllQuestsCompleted?.Invoke();
        return true;
    }
}