using UnityEngine;

public class RotateHintController : MonoBehaviour
{
    [SerializeField] private GameObject rotateHint;

    void Start()
    {
        bool hasRotated = ShipRotateInput.HasUserRotatedShip();

        if (rotateHint != null)
            rotateHint.SetActive(!hasRotated);
    }

    void OnEnable()
    {
        ShipRotateInput.OnFirstShipRotation += HandleFirstShipRotation;
    }

    void OnDisable()
    {
        ShipRotateInput.OnFirstShipRotation -= HandleFirstShipRotation;
    }

    void HandleFirstShipRotation()
    {
        if (rotateHint != null)
            rotateHint.SetActive(false);
    }
}