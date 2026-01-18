using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Choicepanel : MonoBehaviour
{
    [Header("Quest Pool")]
    public List<Quest> quests;            // assign in Inspector

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
        if (quests == null || quests.Count < slots.Length)
        {
            Debug.LogError($"{name}: Not enough quests in list to fill all slots.");
            return;
        }

        // copy so we can draw distinct quests
        List<Quest> pool = new List<Quest>(quests);

        for (int i = 0; i < slots.Length; i++)
        {
            int index = Random.Range(0, pool.Count);
            Quest q = pool[index];
            pool.RemoveAt(index);

            slots[i].quest = q;

            if (slots[i].topText) slots[i].topText.text = q.PartName;
            if (slots[i].bottomText) slots[i].bottomText.text = $"{q.Steps} Steps";
            if (slots[i].iconImage) slots[i].iconImage.texture = q.PartTexture;

            if (slots[i].button)
            {
                int captured = i; // avoid closure bug
                slots[i].button.onClick.RemoveAllListeners();
                slots[i].button.onClick.AddListener(() => OnClicked(captured));
                slots[i].button.interactable = true;
            }
        }

        selectionMade = false;
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
