using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires UI +/- buttons to <see cref="StardewStyleCamera2D"/> zoom (Producer spec: mouse click UI, not keyboard).
/// </summary>
public sealed class CameraZoomUI : MonoBehaviour
{
    [SerializeField] private StardewStyleCamera2D cameraController;

    private void Awake()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<StardewStyleCamera2D>();
    }

    public void OnZoomInClicked()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<StardewStyleCamera2D>();
        cameraController?.ZoomIn();
    }

    public void OnZoomOutClicked()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<StardewStyleCamera2D>();
        cameraController?.ZoomOut();
    }
}
