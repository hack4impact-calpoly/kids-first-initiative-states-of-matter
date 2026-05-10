using UnityEngine;

[CreateAssetMenu(menuName = "Cutscenes/Cutscene Definition")]
public class CutsceneDefinition : ScriptableObject
{
    [Header("Camera")]
    [SerializeField] private bool overrideManagerCameraSettings = true;
    [SerializeField] private float focusOrthographicSize = 2.25f;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private float moveToFocusDuration = 1.15f;
    [SerializeField] private float returnDuration = 0.85f;
    [SerializeField] private bool restoreCameraWhenFinished = true;
    [SerializeField] private AnimationCurve cameraEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Overlay Prefab")]
    [SerializeField] private GameObject overlayPrefab;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private bool destroyOverlayWhenFinished = true;

    [Header("Playback")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private int overlaySortingOrder = 1000;

    public bool OverrideManagerCameraSettings => overrideManagerCameraSettings;
    public float FocusOrthographicSize => focusOrthographicSize;
    public Vector3 FocusOffset => focusOffset;
    public float MoveToFocusDuration => moveToFocusDuration;
    public float ReturnDuration => returnDuration;
    public bool RestoreCameraWhenFinished => restoreCameraWhenFinished;
    public AnimationCurve CameraEase => cameraEase;
    public GameObject OverlayPrefab => overlayPrefab;
    public float HoldDuration => holdDuration;
    public bool DestroyOverlayWhenFinished => destroyOverlayWhenFinished;
    public bool UseUnscaledTime => useUnscaledTime;
    public int OverlaySortingOrder => overlaySortingOrder;
}
