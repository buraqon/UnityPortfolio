using UnityEngine;
using UnityEngine.InputSystem;

public class CursorToggle : MonoBehaviour
{
    private void Start()
    {
        SetLocked(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetLocked(Cursor.lockState != CursorLockMode.Locked);
    }

    private void SetLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
