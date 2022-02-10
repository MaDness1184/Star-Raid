using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuInput : MonoBehaviour
{
    public void OnDebugConsole(InputAction.CallbackContext context)
    {
        if (context.performed)
            DebugConsole.main.ToggleConsole();
    }

    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (context.performed)
            DebugConsole.main.RunCommand();
    }
}
