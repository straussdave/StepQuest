using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ShipRotateInput : MonoBehaviour
{
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

    Vector2 lastPos;
    bool dragging;
    int activePointerId = int.MinValue;

    float yaw;
    float pitch;

    void Awake()
    {
        if (target == null) target = transform;
        var euler = target.localEulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);
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
        }

        ApplyRotation();
    }

    void RotateFromDelta(Vector2 delta)
    {
        float dx = delta.x / Screen.width * 1000f;
        float dy = delta.y / Screen.height * 1000f;

        yaw += dx * yawSpeed;

        float pitchDelta = dy * pitchSpeed * (invertPitch ? 1f : -1f);
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
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
}
