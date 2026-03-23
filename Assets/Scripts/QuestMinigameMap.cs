using System.Collections.Generic;

public static class QuestMinigameMap
{

    public static class MinigameIds
    {
        public const string MG1 = "minigame_1";
        public const string MG2 = "minigame_2";
        public const string MG3 = "minigame_3";
        public const string MG4 = "minigame_4";
    }

    private static readonly Dictionary<string, string> Map = new Dictionary<string, string>()
    {
        { "9",  MinigameIds.MG1 },
        { "10", MinigameIds.MG2 },
        { "11", MinigameIds.MG3 },
        { "12", MinigameIds.MG4 }
    };

    public static string GetMinigameIdForQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        string minigameId;
        return Map.TryGetValue(questId, out minigameId) ? minigameId : null;
    }
}