using System;
using System.Collections;
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
    public static event Action<int> OnMinigameUnlocked;
    [SerializeField] private TabSlider tabSlider;

    private readonly HashSet<string> unlockedParts = new HashSet<string>();
    private bool allQuestsCompletedFired = false;
    private bool questCompletionInProgress = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SaveMigration.Run();

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
        StepProgressSync.ResetForNewQuest(-1);

        PlayerPrefs.SetString(SaveKeys.NEXT_DAY_TEXT_KEY, quest.nextDayText);

        PlayerPrefs.Save();

        AnalyticsLogger.Instance?.LogQuestSelected(quest);

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
            if (TryCompleteFinishedActiveQuest(quest, "step delta arrived while done-today flag was set"))
                return;

            Debug.Log("[StepTracking] Ignored step delta because quest is already done today.");
            return;
        }

        int steps = GetCurrentSteps();
        int newSteps = Mathf.Clamp(steps + delta, 0, quest.Steps);
        int appliedDelta = newSteps - steps;

        AnalyticsLogger.Instance?.LogQuestStepDelta(quest, appliedDelta);

        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, newSteps);
        PlayerPrefs.Save();

        OnProgressChanged?.Invoke(newSteps, quest.Steps);

        if (newSteps >= quest.Steps)
            CompleteQuest();
    }

    public void SetCurrentStepsFromSave(int steps)
    {
        Quest quest = GetCurrentQuest();
        int sanitizedSteps = Mathf.Max(0, steps);

        if (quest != null)
            sanitizedSteps = Mathf.Min(sanitizedSteps, quest.Steps);

        if (PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0) != sanitizedSteps)
        {
            PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, sanitizedSteps);
            PlayerPrefs.Save();
        }

        Debug.Log(
            $"[Quest] Restored current steps from save. " +
            $"activeQuestId={PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID, "")}, steps={sanitizedSteps}."
        );

        if (quest == null)
            return;

        OnProgressChanged?.Invoke(sanitizedSteps, quest.Steps);

        if (sanitizedSteps >= quest.Steps)
            CompleteQuest(ignoreDoneToday: !IsPartUnlocked(quest.Id));
    }

    public void RepairOrResetActiveQuest()
    {
        RepairOrRebaseActiveQuest();
    }

    // Hook this to a UI Button when support needs to recover a stuck active quest.
    public void RepairOrRebaseActiveQuest()
    {
        Quest quest = GetCurrentQuest();

        if (quest == null)
        {
            Debug.LogWarning("[QuestRecovery] No valid active quest found. Clearing stale quest state.");
            ResetQuestState();
            OnProgressChanged?.Invoke(0, 0);
            AnalyticsLogger.Instance?.LogEvent("quest_recovery", extra: "action=clear_stale_state;reason=no_valid_active_quest");
            return;
        }

        int currentSteps = Mathf.Clamp(GetCurrentSteps(), 0, quest.Steps);

        if (currentSteps >= quest.Steps)
        {
            Debug.Log($"[QuestRecovery] Active quest '{quest.Id}' is already complete. Finalizing completion.");

            if (PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0) != quest.Steps)
            {
                PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, quest.Steps);
                PlayerPrefs.Save();
                OnProgressChanged?.Invoke(quest.Steps, quest.Steps);
            }

            AnalyticsLogger.Instance?.LogEvent("quest_recovery", quest, quest.Steps, extra: "action=complete_finished_quest");
            CompleteQuest(immediate: true, ignoreDoneToday: true);
            return;
        }

        Debug.Log($"[QuestRecovery] Active quest '{quest.Id}' is not complete. Rebasing step baseline while preserving progress.");
        AnalyticsLogger.Instance?.LogEvent("quest_recovery", quest, quest.Steps, extra: "action=rebase_incomplete_progress");
        RebaseCurrentQuestProgress(quest, currentSteps);
    }

    private bool TryCompleteFinishedActiveQuest(Quest quest, string reason)
    {
        if (quest == null)
            return false;

        if (PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0) != 1)
            return false;

        if (GetCurrentSteps() < quest.Steps)
            return false;

        if (IsPartUnlocked(quest.Id))
            return false;

        Debug.Log($"[QuestRecovery] Completing active quest '{quest.Id}' despite stale done-today state. reason={reason}.");
        AnalyticsLogger.Instance?.LogEvent("quest_recovery", quest, quest.Steps, extra: $"action=auto_complete_finished_quest;reason={reason}");
        CompleteQuest(ignoreDoneToday: true);
        return true;
    }

    private void CompleteQuest(bool immediate = false, bool ignoreDoneToday = false)
    {
        if (questCompletionInProgress)
        {
            Debug.Log("[Quest] Completion already in progress. Ignoring duplicate completion request.");
            return;
        }

        Quest quest = GetCurrentQuest();
        if (quest == null)
        {
            Debug.LogWarning("[Quest] Completion requested, but there is no valid active quest.");
            return;
        }

        if (!ignoreDoneToday && QuestDoneToday())
        {
            Debug.Log("[Quest] Completion blocked because quest is already marked done today.");
            return;
        }

        questCompletionInProgress = true;

        if (immediate)
            FinishQuestCompletion();
        else
            StartCoroutine(GoHomeAndWaitBeforeCompletingQuest());
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

        if (currentSteps >= quest.Steps && !IsPartUnlocked(quest.Id))
            StartCoroutine(CompleteRestoredFinishedQuestNextFrame());
    }

    private void ResetQuestState()
    {
        PlayerPrefs.SetString(SaveKeys.ACTIVE_QUEST_ID, "");
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0);
        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 0);
        StepProgressSync.ClearWhenNoActiveQuest();
        PlayerPrefs.Save();
    }

    private void RebaseCurrentQuestProgress(Quest quest, int preservedSteps)
    {
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 1);
        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 0);

        int lastAndroidTotal = PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1);
        StepProgressSync.RebasePreservingProgress(lastAndroidTotal, preservedSteps);
        PlayerPrefs.Save();

        OnProgressChanged?.Invoke(preservedSteps, quest != null ? quest.Steps : 0);
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

    private IEnumerator GoHomeAndWaitBeforeCompletingQuest()
    {
        float waitSeconds = 0f;

        if (tabSlider != null)
        {
            try
            {
                tabSlider.ShowHome();
                waitSeconds = Mathf.Max(0f, tabSlider.Duration);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Quest] Could not switch to home tab before completion. Completing anyway. {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[Quest] No TabSlider assigned for completion transition. Completing immediately.");
        }

        if (waitSeconds > 0f)
            yield return new WaitForSeconds(waitSeconds);

        FinishQuestCompletion();
    }

    private IEnumerator CompleteRestoredFinishedQuestNextFrame()
    {
        yield return null;

        Quest quest = GetCurrentQuest();
        if (quest == null)
            yield break;

        if (PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0) != 1)
            yield break;

        if (GetCurrentSteps() < quest.Steps || IsPartUnlocked(quest.Id))
            yield break;

        Debug.Log($"[QuestRecovery] Restored quest '{quest.Id}' was already complete. Finalizing saved completion.");
        AnalyticsLogger.Instance?.LogEvent("quest_recovery", quest, quest.Steps, extra: "action=auto_complete_restored_finished_quest");
        CompleteQuest(ignoreDoneToday: true);
    }

    private void FinishQuestCompletion()
    {
        Quest completedQuest = GetCurrentQuest();
        string partId = completedQuest != null ? completedQuest.Id : PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID);

        if (string.IsNullOrEmpty(partId))
        {
            Debug.LogError("[Quest] Cannot finish quest completion because no quest id is available.");
            questCompletionInProgress = false;
            return;
        }

        if (completedQuest != null)
            PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, completedQuest.Steps);

        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 1);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0);
        PlayerPrefs.SetInt(SaveKeys.PENDING_QUEST_COMPLETION_DIALOGUE, 1);
        StepProgressSync.ClearWhenNoActiveQuest();
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(partId) && unlockedParts.Add(partId))
        {
            Debug.Log($"[Quest] Unlocked part: {partId}.");
            SaveUnlockedParts();

            AnalyticsLogger.Instance?.LogPartUnlocked(completedQuest);

            PlayerPrefs.SetString(SaveKeys.LAST_UNLOCKED_PART_ID, partId);
            PlayerPrefs.SetInt(SaveKeys.PENDING_COLLECTION_HIGHLIGHT, 1);
            PlayerPrefs.Save();

            OnPartUnlocked?.Invoke(partId);
        }

        SaveSystem.UnlockMinigameForQuest(partId);

        Debug.Log($"[Quest] Quest completed: {(completedQuest != null ? completedQuest.Id : partId)}.");

        DateUtil.MarkQuestDoneToday();

        AnalyticsLogger.Instance?.LogQuestCompleted(completedQuest);

        OnQuestCompleted?.Invoke(completedQuest);

        CheckAllQuestsCompleted();

        questCompletionInProgress = false;
    }
}
