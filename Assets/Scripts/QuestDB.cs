using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDB", menuName = "StepQuest/QuestDB")]
public class QuestDB : ScriptableObject
{
    public List<Quest> quests = new List<Quest>();

    private Dictionary<string, Quest> _byId;

    void OnEnable()
    {
        BuildIndex();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        BuildIndex();
    }
#endif

    void BuildIndex()
    {
        _byId = new Dictionary<string, Quest>(StringComparer.Ordinal);

        for (int i = 0; i < quests.Count; i++)
        {
            var q = quests[i];
            if (q == null) continue;

            if (string.IsNullOrWhiteSpace(q.Id))
            {
                Debug.LogWarning($"QuestDatabase: Quest at index {i} has empty Id.", this);
                continue;
            }

            if (_byId.ContainsKey(q.Id))
            {
                Debug.LogWarning($"QuestDatabase: Duplicate quest Id '{q.Id}'.", this);
                continue;
            }

            _byId.Add(q.Id, q);
        }
    }

    public bool TryGetById(string id, out Quest quest)
    {
        quest = null;
        if (string.IsNullOrWhiteSpace(id)) return false;

        if (_byId == null) BuildIndex();
        return _byId.TryGetValue(id, out quest);
    }
}
