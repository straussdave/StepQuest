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
    [SerializeField] GameObject progressBarRoot; // "Progress Bar" object under Canvas

    bool selectionMade = false;

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

        // Build pool
        List<Quest> pool = new List<Quest>();
        foreach (var q in quests)
        {
            if (q == null) continue;
            if (!qm.IsPartUnlocked(q.Id))
                pool.Add(q);
        }


        // Rule: If robot is not unlocked -> ONLY offer robot
        bool robotLocked = robotQuest != null && !qm.IsPartUnlocked(robotQuest.Id);

        if (robotLocked)
        {
            AssignSlot(0, robotQuest);

            if (slots[1].root) slots[1].root.SetActive(false);

            selectionMade = false;
            return;
        }

        for (int i = 0; i < slots.Length; i++)
            if (slots[i].root) slots[i].root.SetActive(true);

        if (robotQuest != null)
            pool.RemoveAll(q => q == robotQuest);

        // Slot 0
        Quest q0 = DrawRandom(pool);
        if (q0 != null) AssignSlot(0, q0);
        else { if (slots[0].root) slots[0].root.SetActive(false); }

        // Slot 1
        Quest q1 = DrawRandom(pool);
        if (q1 != null) AssignSlot(1, q1);
        else { if (slots[1].root) slots[1].root.SetActive(false); }

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
