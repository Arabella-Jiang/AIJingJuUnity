using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Character attributes window (角色属性). C toggles; blocks player input while open.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterAttributesPanel : FullscreenGameplayPanel
{
    private void Update()
    {
        if (!WasToggleKeyPressed())
            return;

        Toggle();
    }

    public void OnCloseClicked() => Close();

    private static bool WasToggleKeyPressed()
    {
        if (Keyboard.current != null)
            return Keyboard.current.cKey.wasPressedThisFrame;

        return Input.GetKeyDown(KeyCode.C);
    }
}
