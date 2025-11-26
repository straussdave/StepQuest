using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "StepQuest/Quest")]
public class Quest : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string partName;
    [SerializeField] private RenderTexture partTexture;
    [SerializeField] private int steps;

    public string Id => id;
    public string PartName => partName;
    public RenderTexture PartTexture => partTexture;
    public int Steps => steps;
}
