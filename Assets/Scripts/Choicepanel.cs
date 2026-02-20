using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Choicepanel : MonoBehaviour
{
    [Header("Special Quest")]
    [SerializeField] private Quest robotQuest;

    [Header("Quest Pool")]
    public List<Quest> quests;

    [System.Serializable]
    public class ChoiceSlot
    {
        [Header("Root Object")]
        public GameObject root;           // QuestChoice GameObject

        [Header("Texts + Icon")]
        public TMP_Text topText;
        public TMP_Text bottomText;
        public RawImage iconImage;

        [Header("Interaction")]
        public Button button;             // Button component on QuestChoice

        [HideInInspector] public Quest quest;
    }

    [Header("Slots (your 2 QuestChoice children)")]
    public ChoiceSlot[] slots;            // size = 2

    [Header("Global UI")]
    [SerializeField] private GameObject progressBarRoot; // "Progress Bar" object under Canvas

    private bool selectionMade = false;

    void Start()
    {
        // hide global progress bar at start
        if (progressBarRoot != null)
            progressBarRoot.SetActive(false);

        SetupSlots();
    }

    void SetupSlots()
    {
        if (quests == null || quests.Count == 0)
        {
            Debug.LogError($"{name}: Quest list is empty.");
            return;
        }

        if (slots == null || slots.Length < 2)
        {
            Debug.LogError($"{name}: Slots must be size 2.");
            return;
        }

        var qm = QuestManager.Instance;
        if (qm == null)
        {
            Debug.LogError($"{name}: QuestManager.Instance is null.");
            return;
        }

        // Build pools (only quests that are NOT unlocked yet)
        List<Quest> storyPool = new List<Quest>();
        List<Quest> normalPool = new List<Quest>();

        foreach (var q in quests)
        {
            if (q == null) continue;
            if (qm.IsPartUnlocked(q.Id)) continue;

            if (q.IsStoryQuest) storyPool.Add(q);
            else normalPool.Add(q);
        }

        // Rule 1: If robot is not unlocked -> ONLY offer robot (first quest)
        bool robotLocked = robotQuest != null && !qm.IsPartUnlocked(robotQuest.Id);
        if (robotLocked)
        {
            AssignSlot(0, robotQuest);

            if (slots[1].root) slots[1].root.SetActive(false);

            selectionMade = false;
            return;
        }

        // Ensure robot quest doesn't appear again after it was unlocked
        if (robotQuest != null)
        {
            storyPool.RemoveAll(q => q == robotQuest);
            normalPool.RemoveAll(q => q == robotQuest);
        }

        // Default: show both roots (we'll hide as needed below)
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].root) slots[i].root.SetActive(true);

        Quest slot0 = null;
        Quest slot1 = null;

        if (storyPool.Count > 0)
        {
            // Always show the next story quest in sequence
            slot0 = DrawNextStory(storyPool);

            // If ONLY story quests are available -> show ONE quest only
            if (normalPool.Count == 0)
            {
                slot1 = null;
            }
            else
            {
                // Otherwise fill slot 1: prefer normal; if none, allow story
                slot1 = DrawAnyPreferNormal(normalPool, storyPool);
            }
        }
        else
        {
            // No story quests available -> pick up to two from normal pool
            if (normalPool.Count > 0) slot0 = DrawRandom(normalPool);
            if (normalPool.Count > 0) slot1 = DrawRandom(normalPool);
        }

        // Assign slot 0
        if (slot0 != null) AssignSlot(0, slot0);
        else if (slots[0].root) slots[0].root.SetActive(false);

        // Assign / hide slot 1
        if (slot1 != null)
        {
            AssignSlot(1, slot1);
        }
        else
        {
            if (slots[1].root) slots[1].root.SetActive(false);
        }

        selectionMade = false;
    }

    Quest DrawRandom(List<Quest> pool)
    {
        if (pool == null || pool.Count == 0) return null;
        int index = Random.Range(0, pool.Count);
        Quest q = pool[index];
        pool.RemoveAt(index);
        return q;
    }

    Quest DrawNextStory(List<Quest> storyPool)
    {
        if (storyPool == null || storyPool.Count == 0) return null;

        Quest best = null;
        for (int i = 0; i < storyPool.Count; i++)
        {
            var q = storyPool[i];
            if (q == null) continue;

            if (best == null) best = q;
            else
            {
                if (q.StoryOrder < best.StoryOrder) best = q;
                else if (q.StoryOrder == best.StoryOrder && string.CompareOrdinal(q.Id, best.Id) < 0)
                    best = q; // deterministic tie-break
            }
        }

        if (best != null) storyPool.Remove(best);
        return best;
    }

    Quest DrawAnyPreferNormal(List<Quest> normalPool, List<Quest> storyPool)
    {
        if (normalPool != null && normalPool.Count > 0) return DrawRandom(normalPool);
        if (storyPool != null && storyPool.Count > 0) return DrawRandom(storyPool);
        return null;
    }

    void AssignSlot(int i, Quest q)
    {
        if (slots[i].root) slots[i].root.SetActive(true);
        slots[i].quest = q;

        if (slots[i].topText) slots[i].topText.text = q != null ? q.PartName : "N/A";
        if (slots[i].bottomText) slots[i].bottomText.text = q != null ? $"{q.Steps} Steps" : "";
        if (slots[i].iconImage) slots[i].iconImage.texture = q != null ? q.PartTexture : null;

        if (slots[i].button)
        {
            int captured = i;
            slots[i].button.onClick.RemoveAllListeners();
            slots[i].button.onClick.AddListener(() => OnClicked(captured));
            slots[i].button.interactable = (q != null);
        }
    }

    public void OnClicked(int slotIndex)
    {
        if (selectionMade) return;
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        var q = slots[slotIndex].quest;
        if (q == null || QuestManager.Instance == null) return;

        selectionMade = true;

        QuestManager.Instance.SelectQuest(q);

        if (progressBarRoot != null)
            progressBarRoot.SetActive(true);

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].root) continue;

            if (i == slotIndex)
            {
                if (slots[i].button)
                    slots[i].button.interactable = false;
            }
            else
            {
                slots[i].root.SetActive(false);
            }
        }
    }
}