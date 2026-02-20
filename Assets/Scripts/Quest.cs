using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "StepQuest/Quest")]
public class Quest : ScriptableObject
{
    [Header("Quest")]
    [SerializeField] private string id;
    [SerializeField] private string partName;
    [SerializeField] private RenderTexture partTexture;
    [SerializeField] private int steps;
    [SerializeField] private bool isStoryQuest = false;

    [Header("Story Sequence")]
    [Tooltip("Only used if IsStoryQuest is true. Lower number = earlier quest.")]
    [Min(0)]
    [SerializeField] private int storyOrder = 0;

    [Header("Dialogue")]
    [TextArea(2, 6)][SerializeField] private string chooseText;
    [TextArea(2, 6)][SerializeField] private string completedText;
    [TextArea(2, 6)][SerializeField] public string nextDayText;
    [SerializeField] public bool showPortrait = true;

    public string Id => id;
    public string PartName => partName;
    public RenderTexture PartTexture => partTexture;
    public int Steps => steps;

    public string ChooseText => chooseText;
    public string CompletedText => completedText;

    public bool IsStoryQuest => isStoryQuest;
    public int StoryOrder => storyOrder;
}