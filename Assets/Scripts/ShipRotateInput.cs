using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipRotateInput : MonoBehaviour
{
    public static event Action OnFirstShipRotation;

    [Header("Target")]
    public Transform target; // ShipRoot

    [Header("Rotation")]
    public float yawSpeed = 0.25f;
    public float pitchSpeed = 0.18f;
    public bool invertPitch = false;
    public float minPitch = -35f;
    public float maxPitch = 35f;

    [Header("Optional smoothing")]
    public float smoothing = 12f; // 0 = no smoothing

    [Header("First Rotation Detection")]
    [SerializeField] private float firstRotationThreshold = 4f; // sqrMagnitude threshold

    Vector2 lastPos;
    bool dragging;
    int activePointerId = int.MinValue;

    float yaw;
    float pitch;

    bool firstRotationAlreadyHandled;

    void Awake()
    {
        if (target == null) target = transform;

        var euler = target.localEulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);

        firstRotationAlreadyHandled = HasUserRotatedShip();
    }

    void OnEnable()
    {
        dragging = false;
        activePointerId = int.MinValue;
    }

    void Update()
    {
        if (target == null) return;

        // --- Touch (mobile) ---
        var ts = Touchscreen.current;
        if (ts != null)
        {
            var t = ts.primaryTouch;

            if (t.press.wasPressedThisFrame)
            {
                int touchId = t.touchId.ReadValue();

                dragging = true;
                activePointerId = touchId;
                lastPos = t.position.ReadValue();
                Debug.Log($"[UserAction] Started rotating ship via touch. touchId={touchId}.");
            }

            if (dragging && t.press.isPressed)
            {
                int touchId = t.touchId.ReadValue();
                if (touchId == activePointerId)
                {
                    Vector2 pos = t.position.ReadValue();
                    var phase = t.phase.ReadValue();

                    if (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                        phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                    {
                        RotateFromDelta(pos - lastPos);
                        lastPos = pos;
                    }
                }
            }

            if (dragging && t.press.wasReleasedThisFrame)
            {
                dragging = false;
                activePointerId = int.MinValue;
                Debug.Log($"[UserAction] Stopped rotating ship via touch. yaw={yaw:F1}, pitch={pitch:F1}.");
            }

            ApplyRotation();
            return;
        }

        // --- Mouse (editor/desktop) ---
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragging = true;
            activePointerId = int.MinValue;
            lastPos = mouse.position.ReadValue();
            Debug.Log("[UserAction] Started rotating ship via mouse drag.");
        }
        else if (dragging && mouse.leftButton.isPressed)
        {
            Vector2 pos = mouse.position.ReadValue();
            RotateFromDelta(pos - lastPos);
            lastPos = pos;
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
            Debug.Log($"[UserAction] Stopped rotating ship via mouse drag. yaw={yaw:F1}, pitch={pitch:F1}.");
        }

        ApplyRotation();
    }

    void RotateFromDelta(Vector2 delta)
    {
        TryMarkFirstRotation(delta);

        float dx = delta.x / Screen.width * 1000f;
        float dy = delta.y / Screen.height * 1000f;

        yaw += dx * yawSpeed;

        float pitchDelta = dy * pitchSpeed * (invertPitch ? 1f : -1f);
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
    }

    void TryMarkFirstRotation(Vector2 delta)
    {
        if (firstRotationAlreadyHandled) return;
        if (delta.sqrMagnitude < firstRotationThreshold) return;

        firstRotationAlreadyHandled = true;

        PlayerPrefs.SetInt(SaveKeys.HAS_ROTATED_SHIP, 1);
        PlayerPrefs.Save();

        Debug.Log("[UserAction] User rotated ship for the first time.");
        OnFirstShipRotation?.Invoke();
    }

    void ApplyRotation()
    {
        Quaternion desired = Quaternion.Euler(pitch, -yaw, 0f);
        if (smoothing <= 0f)
            target.localRotation = desired;
        else
            target.localRotation = Quaternion.Slerp(target.localRotation, desired, Time.deltaTime * smoothing);
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    public static bool HasUserRotatedShip()
    {
        return PlayerPrefs.GetInt(SaveKeys.HAS_ROTATED_SHIP, 0) == 1;
    }
}