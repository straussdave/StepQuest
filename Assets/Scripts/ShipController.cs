using System;
using System.Collections;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Serializable]
    public class ShipPart
    {
        public string partId;
        public GameObject prefab;

        [HideInInspector] public GameObject instance;

        [HideInInspector] public Renderer[] renderers;
        [HideInInspector] public Material[][] originalSharedMaterials;
        [HideInInspector] public Collider[] colliders;
        [HideInInspector] public Behaviour outlineBehaviour;
    }

    [Header("Parts")]
    public ShipPart[] parts;

    [Header("Locked Visuals")]
    public Material lockedGhostMaterial;

    [Tooltip("If true, locked parts are still visible but non-interactable.")]
    public bool showLockedParts = true;

    [Header("New Part Animation")]
    public float unlockAnimDuration = 0.45f;
    public float unlockAnimScaleMultiplier = 1.18f;
    public int unlockAnimPulseCount = 2;

    private Coroutine currentUnlockAnim;

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

            p.originalSharedMaterials = new Material[p.renderers.Length][];
            for (int i = 0; i < p.renderers.Length; i++)
                p.originalSharedMaterials[i] = p.renderers[i].sharedMaterials;

            p.outlineBehaviour = p.instance.GetComponentInChildren<Behaviour>(true);
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

    public void PlayPendingUnlockAnimationIfNeeded()
    {
        bool pending = PlayerPrefs.GetInt(SaveKeys.PENDING_COLLECTION_HIGHLIGHT, 0) == 1;
        if (!pending) return;

        string partId = PlayerPrefs.GetString(SaveKeys.LAST_UNLOCKED_PART_ID, "");
        if (string.IsNullOrEmpty(partId)) return;

        ShipPart target = null;
        foreach (var p in parts)
        {
            if (p != null && p.partId == partId)
            {
                target = p;
                break;
            }
        }

        if (target == null || target.instance == null)
        {
            Debug.LogWarning($"[Collection] Could not find part '{partId}' for pending highlight.");
            return;
        }

        if (currentUnlockAnim != null)
            StopCoroutine(currentUnlockAnim);

        currentUnlockAnim = StartCoroutine(PlayUnlockPulse(target));

        PlayerPrefs.SetInt(SaveKeys.PENDING_COLLECTION_HIGHLIGHT, 0);
        PlayerPrefs.Save();
    }

    private IEnumerator PlayUnlockPulse(ShipPart part)
    {
        Transform t = part.instance.transform;
        Vector3 baseScale = t.localScale;
        Vector3 peakScale = baseScale * unlockAnimScaleMultiplier;

        for (int pulse = 0; pulse < unlockAnimPulseCount; pulse++)
        {
            float half = unlockAnimDuration * 0.5f;

            float time = 0f;
            while (time < half)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / half);
                k = EaseOutBack(k);
                t.localScale = Vector3.LerpUnclamped(baseScale, peakScale, k);
                yield return null;
            }

            time = 0f;
            while (time < half)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / half);
                k = Mathf.SmoothStep(0f, 1f, k);
                t.localScale = Vector3.Lerp(peakScale, baseScale, k);
                yield return null;
            }
        }

        t.localScale = baseScale;
        currentUnlockAnim = null;
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    void OnPartUnlocked(string id)
    {
        foreach (var p in parts)
        {
            if (p.partId == id && p.instance != null)
            {
                ApplyPartState(p, true);
            }
        }
    }

    void ApplyPartState(ShipPart p, bool unlocked)
    {
        if (p.instance == null) return;

        if (!showLockedParts && !unlocked)
        {
            p.instance.SetActive(false);
            return;
        }

        p.instance.SetActive(true);

        if (p.colliders != null)
        {
            foreach (var c in p.colliders)
                if (c != null) c.enabled = unlocked;
        }

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

                    var mats = r.sharedMaterials;
                    var ghostMats = new Material[mats.Length];
                    for (int m = 0; m < ghostMats.Length; m++)
                        ghostMats[m] = lockedGhostMaterial;

                    r.sharedMaterials = ghostMats;
                }
            }
        }

        if (p.outlineBehaviour != null)
        {
            p.outlineBehaviour.enabled = !unlocked;
        }
           
    }
}