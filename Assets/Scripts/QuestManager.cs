using System;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public string CurrentQuestId { get; private set; }
    public string CurrentQuestName { get; private set; }
    public string CurrentQuestRequirements { get; private set; }

    public int CurrentSteps { get; private set; }
    public int CurrentTargetSteps { get; private set; }

    // UI can subscribe to this
    public event Action<int, int> OnProgressChanged;  // (current, target)

    const string KeyQuestId = "QM_CurrentQuestId";
    const string KeyQuestName = "QM_CurrentQuestName";
    const string KeyReq = "QM_CurrentQuestReq";
    const string KeySteps = "QM_CurrentSteps";
    const string KeyTarget = "QM_TargetSteps";

    public event System.Action OnQuestCompleted;

    public event Action<string> OnPartUnlocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProgress();
    }

    // now includes targetSteps
    public void SelectQuest(string questId, string questName, string requirements, int targetSteps)
    {
        CurrentQuestId = questId;
        CurrentQuestName = questName;
        CurrentQuestRequirements = requirements;
        CurrentTargetSteps = targetSteps;
        CurrentSteps = 0;

        SaveProgress();
        OnProgressChanged?.Invoke(CurrentSteps, CurrentTargetSteps);
        Debug.Log($"Selected quest: {questName} ({questId}) targetSteps={targetSteps}");
    }

    public void AddSteps(int delta)
    {
        if (delta <= 0) return;

        int before = CurrentSteps;

        CurrentSteps += delta;
        if (CurrentSteps > CurrentTargetSteps && CurrentTargetSteps > 0)
            CurrentSteps = CurrentTargetSteps;

        SaveProgress();
        OnProgressChanged?.Invoke(CurrentSteps, CurrentTargetSteps);

        // fire completion event only on first time reaching target
        if (CurrentTargetSteps > 0 &&
            before < CurrentTargetSteps &&
            CurrentSteps >= CurrentTargetSteps)
        {
            MarkPartUnlocked(CurrentQuestId);
            OnQuestCompleted?.Invoke();
            Debug.Log("Quest completed!");
        }
    }


    void SaveProgress()
    {
        PlayerPrefs.SetString(KeyQuestId, CurrentQuestId);
        PlayerPrefs.SetString(KeyQuestName, CurrentQuestName);
        PlayerPrefs.SetString(KeyReq, CurrentQuestRequirements);
        PlayerPrefs.SetInt(KeySteps, CurrentSteps);
        PlayerPrefs.SetInt(KeyTarget, CurrentTargetSteps);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        CurrentQuestId = PlayerPrefs.GetString(KeyQuestId, string.Empty);
        CurrentQuestName = PlayerPrefs.GetString(KeyQuestName, string.Empty);
        CurrentQuestRequirements =
            PlayerPrefs.GetString(KeyReq, string.Empty);
        CurrentSteps = PlayerPrefs.GetInt(KeySteps, 0);
        CurrentTargetSteps = PlayerPrefs.GetInt(KeyTarget, 0);
    }

    public void MarkPartUnlocked(string partId)
    {
        PlayerPrefs.SetInt("Part_" + partId, 1);
        PlayerPrefs.Save();

        OnPartUnlocked?.Invoke(partId);
    }

    public bool IsPartUnlocked(string partId)
    {
        return PlayerPrefs.GetInt("Part_" + partId, 0) == 1;
    }

}
