using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zoom UI: - (click) | slider (drag) | + (click). Producer layout: -(可点击)【bar】(可滑动)+(可点击)
/// </summary>
public sealed class CameraZoomUI : MonoBehaviour
{
    [SerializeField] private StardewStyleCamera2D cameraController;
    [SerializeField] private Slider zoomSlider;

    private bool suppressSliderCallback;

    private void Awake()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<StardewStyleCamera2D>();

        if (zoomSlider == null)
            zoomSlider = GetComponentInChildren<Slider>();
    }

    private void OnEnable()
    {
        if (cameraController != null)
            cameraController.ZoomChanged += OnCameraZoomChanged;

        if (zoomSlider != null)
            zoomSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void Start()
    {
        EnsureCamera();
        RefreshSliderFromCamera();
    }

    private void OnDisable()
    {
        if (cameraController != null)
            cameraController.ZoomChanged -= OnCameraZoomChanged;

        if (zoomSlider != null)
            zoomSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    public void Bind(StardewStyleCamera2D camera, Slider slider)
    {
        cameraController = camera;
        zoomSlider = slider;
    }

    public void OnZoomOutClicked()
    {
        EnsureCamera();
        cameraController?.ZoomOut();
    }

    public void OnZoomInClicked()
    {
        EnsureCamera();
        cameraController?.ZoomIn();
    }

    private void OnSliderValueChanged(float value)
    {
        if (suppressSliderCallback)
            return;

        EnsureCamera();
        cameraController?.SetZoomSliderValue(value);
    }

    private void OnCameraZoomChanged() => RefreshSliderFromCamera();

    private void RefreshSliderFromCamera()
    {
        if (zoomSlider == null || cameraController == null)
            return;

        suppressSliderCallback = true;
        zoomSlider.SetValueWithoutNotify(cameraController.GetZoomSliderValue());
        suppressSliderCallback = false;
    }

    private void EnsureCamera()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<StardewStyleCamera2D>();
    }
}
