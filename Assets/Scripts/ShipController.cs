using System;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Serializable]
    public class ShipPart
    {
        public string partId;
        public GameObject prefab;

        [HideInInspector] public GameObject instance;

        // Cached for swapping back and forth
        [HideInInspector] public Renderer[] renderers;
        [HideInInspector] public Material[][] originalSharedMaterials;
        [HideInInspector] public Collider[] colliders;

        // Optional: if you add an outline component (see below)
        [HideInInspector] public Behaviour outlineBehaviour;
    }

    [Header("Parts")]
    public ShipPart[] parts;

    [Header("Locked Visuals")]
    public Material lockedGhostMaterial;

    [Tooltip("If true, locked parts are still visible but non-interactable.")]
    public bool showLockedParts = true;

    void Start()
    {
        foreach (var p in parts)
        {
            if (p.prefab == null)
            {
                Debug.LogWarning($"Ship part '{p.partId}' has no prefab assigned.");
                continue;
            }

            p.instance = Instantiate(p.prefab, transform);
            p.instance.SetActive(true);

            p.renderers = p.instance.GetComponentsInChildren<Renderer>(true);
            p.colliders = p.instance.GetComponentsInChildren<Collider>(true);

            // Cache original shared materials per renderer
            p.originalSharedMaterials = new Material[p.renderers.Length][];
            for (int i = 0; i < p.renderers.Length; i++)
                p.originalSharedMaterials[i] = p.renderers[i].sharedMaterials;

            // Optional: if you use an outline script/component, cache it here
            // (example component name: "Outline" or "QuickOutline")
            p.outlineBehaviour = p.instance.GetComponentInChildren<Behaviour>(true); // replace with specific type if you know it
        }

        ReloadParts();
        var qm = QuestManager.Instance;
        if (qm != null)
            qm.OnPartUnlocked += OnPartUnlocked;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnPartUnlocked -= OnPartUnlocked;
    }

    public void ReloadParts()
    {
        foreach (var p in parts)
        {
            var qm = QuestManager.Instance;
            if (qm == null) return;

            bool unlocked = qm.IsPartUnlocked(p.partId);
            Debug.Log($"Part '{p.partId}' unlocked: {unlocked}");
            ApplyPartState(p, unlocked);
        }
    }

    void OnPartUnlocked(string id)
    {
        foreach (var p in parts)
        {
            if (p.partId == id && p.instance != null)
            {
                ApplyPartState(p, unlocked: true);
                // Optional: play reveal animation here
            }
        }
    }

    void ApplyPartState(ShipPart p, bool unlocked)
    {
        if (p.instance == null) return;

        // If you truly want hidden locked parts sometimes:
        if (!showLockedParts && !unlocked)
        {
            p.instance.SetActive(false);
            return;
        }

        p.instance.SetActive(true);

        // Disable interaction when locked
        if (p.colliders != null)
        {
            foreach (var c in p.colliders)
                if (c != null) c.enabled = unlocked;
        }

        // Swap visuals
        if (p.renderers != null)
        {
            for (int i = 0; i < p.renderers.Length; i++)
            {
                var r = p.renderers[i];
                if (r == null) continue;

                if (unlocked)
                {
                    r.sharedMaterials = p.originalSharedMaterials[i];
                }
                else
                {
                    if (lockedGhostMaterial == null)
                    {
                        Debug.LogWarning("lockedGhostMaterial is not assigned.");
                        continue;
                    }

                    // Keep same material slot count
                    var mats = r.sharedMaterials;
                    var ghostMats = new Material[mats.Length];
                    for (int m = 0; m < ghostMats.Length; m++)
                        ghostMats[m] = lockedGhostMaterial;

                    r.sharedMaterials = ghostMats;
                }
            }
        }

        // Optional outline toggle (see below)
        if (p.outlineBehaviour != null)
            p.outlineBehaviour.enabled = !unlocked;
    }
}
