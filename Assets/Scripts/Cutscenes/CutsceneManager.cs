using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public sealed class CutsceneContext
{
    public CutsceneContext(Camera sceneCamera, Transform focusTarget, RectTransform overlayRoot, bool useUnscaledTime, GameObject overlayInstance = null)
    {
        SceneCamera = sceneCamera;
        FocusTarget = focusTarget;
        OverlayRoot = overlayRoot;
        UseUnscaledTime = useUnscaledTime;
        OverlayInstance = overlayInstance;
    }

    public Camera SceneCamera { get; }
    public Transform FocusTarget { get; }
    public RectTransform OverlayRoot { get; }
    public GameObject OverlayInstance { get; }
    public bool UseUnscaledTime { get; }
    public float DeltaTime => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
}

public class CutsceneManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private float focusOrthographicSize = 2.25f;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private float moveToFocusDuration = 1.15f;
    [SerializeField] private float returnDuration = 0.85f;
    [SerializeField] private bool restoreCameraWhenFinished = true;
    [SerializeField] private AnimationCurve cameraEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Playback")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private int overlaySortingOrder = 1000;
    [SerializeField] private bool lockWorldMouseInput = true;

    private RectTransform overlayRoot;
    private GameObject overlayCanvasObject;
    private GameObject activeOverlayInstance;
    private Coroutine currentRoutine;
    private readonly List<MonoBehaviour> lockedMouseHandlers = new List<MonoBehaviour>();
    private CameraSnapshot activeStartSnapshot;
    private CutscenePlaybackSettings activeSettings;
    private Action activeFinished;
    private ICutsceneAnimation activeAnimation;
    private CutsceneContext activeContext;
    private bool hasActiveSnapshot;
    private static readonly Dictionary<Type, bool> mouseMessageCache = new Dictionary<Type, bool>();
    private static int activePlaybackCount;

    public bool IsPlaying { get; private set; }
    public static bool AnyCutscenePlaying => activePlaybackCount > 0;

    private void Awake()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;
    }

    public bool TryPlay(Transform focusTarget, MonoBehaviour animationBehaviour, Action finished = null)
    {
        if (animationBehaviour != null && !(animationBehaviour is ICutsceneAnimation))
        {
            Debug.LogWarning($"{animationBehaviour.name} cannot play as a cutscene because it does not implement ICutsceneAnimation.");
            return false;
        }

        return TryPlay(focusTarget, animationBehaviour as ICutsceneAnimation, finished);
    }

    public bool TryPlay(Transform focusTarget, ICutsceneAnimation animation, Action finished = null)
    {
        if (IsPlaying || AnyCutscenePlaying)
            return false;

        if (animation == null)
        {
            Debug.LogWarning("CutsceneManager needs an animation when no CutsceneDefinition is provided.");
            return false;
        }

        if (sceneCamera == null)
            sceneCamera = Camera.main;

        if (sceneCamera == null)
        {
            Debug.LogWarning("CutsceneManager could not find a camera to animate.");
            return false;
        }

        currentRoutine = StartCoroutine(PlayRoutine(new CutsceneRequest(focusTarget, null, animation, finished)));
        return true;
    }

    public bool TryPlay(CutsceneDefinition definition, Transform focusTarget, MonoBehaviour animationOverride = null, Action finished = null)
    {
        if (animationOverride != null && !(animationOverride is ICutsceneAnimation))
        {
            Debug.LogWarning($"{animationOverride.name} cannot play as a cutscene because it does not implement ICutsceneAnimation.");
            return false;
        }

        return TryPlay(definition, focusTarget, animationOverride as ICutsceneAnimation, finished);
    }

    public bool TryPlay(CutsceneDefinition definition, Transform focusTarget, ICutsceneAnimation animationOverride = null, Action finished = null)
    {
        if (IsPlaying || AnyCutscenePlaying)
            return false;

        if (definition == null && animationOverride == null)
        {
            Debug.LogWarning("CutsceneManager needs a CutsceneDefinition, an animation, or both.");
            return false;
        }

        if (sceneCamera == null)
            sceneCamera = Camera.main;

        if (sceneCamera == null)
        {
            Debug.LogWarning("CutsceneManager could not find a camera to animate.");
            return false;
        }

        currentRoutine = StartCoroutine(PlayRoutine(new CutsceneRequest(focusTarget, definition, animationOverride, finished)));
        return true;
    }

    public void SkipCurrent()
    {
        StopCurrentPlayback(true);
    }

    private void OnDisable()
    {
        StopCurrentPlayback(false);
    }

    private IEnumerator PlayRoutine(CutsceneRequest request)
    {
        IsPlaying = true;
        CutscenePlaybackSettings settings;
        CameraSnapshot startSnapshot;

        try
        {
            settings = CutscenePlaybackSettings.From(this, request.Definition);

            EnsureOverlay(settings.OverlaySortingOrder);
            DestroyActiveOverlayInstance();
            SetOverlayActive(true);

            startSnapshot = CameraSnapshot.From(sceneCamera);
            BeginPlayback(request.Finished, settings, startSnapshot);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            CleanupPrePlaybackFailure();
            yield break;
        }

        Vector3 focusPosition;
        float focusSize;

        try
        {
            focusPosition = ResolveFocusPosition(request.FocusTarget, settings.FocusOffset, startSnapshot.Position.z);
            focusSize = sceneCamera.orthographic ? settings.FocusOrthographicSize : sceneCamera.fieldOfView;
        }
        catch (Exception exception)
        {
            FailCurrentPlayback(exception);
            yield break;
        }

        yield return RunPlaybackStep(MoveCamera(startSnapshot, focusPosition, focusSize, settings.MoveToFocusDuration, settings.UseUnscaledTime, settings.CameraEase));
        if (!IsPlaying)
            yield break;

        ICutsceneAnimation animation;

        try
        {
            activeOverlayInstance = InstantiateOverlayPrefab(settings.OverlayPrefab);
            animation = request.AnimationOverride ?? FindAnimation(activeOverlayInstance);
        }
        catch (Exception exception)
        {
            FailCurrentPlayback(exception);
            yield break;
        }

        if (animation != null)
        {
            var context = new CutsceneContext(sceneCamera, request.FocusTarget, overlayRoot, settings.UseUnscaledTime, activeOverlayInstance);
            activeAnimation = animation;
            activeContext = context;
            IEnumerator animationRoutine;
            try
            {
                animationRoutine = animation.Play(context);
            }
            catch (Exception exception)
            {
                FailCurrentPlayback(exception);
                yield break;
            }

            yield return RunPlaybackStep(animationRoutine);
            if (!IsPlaying)
                yield break;

            activeAnimation = null;
            activeContext = null;
        }
        else if (activeOverlayInstance != null && settings.HoldDuration > 0f)
        {
            yield return RunPlaybackStep(Wait(settings.HoldDuration, settings.UseUnscaledTime));
            if (!IsPlaying)
                yield break;
        }

        try
        {
            if (settings.DestroyOverlayWhenFinished)
                DestroyActiveOverlayInstance();
            else if (activeOverlayInstance != null)
                activeOverlayInstance.SetActive(false);
        }
        catch (Exception exception)
        {
            FailCurrentPlayback(exception);
            yield break;
        }

        if (settings.RestoreCameraWhenFinished)
        {
            IEnumerator returnCameraRoutine;
            try
            {
                returnCameraRoutine = MoveCamera(CameraSnapshot.From(sceneCamera), startSnapshot.Position, startSnapshot.Size, settings.ReturnDuration, settings.UseUnscaledTime, settings.CameraEase);
            }
            catch (Exception exception)
            {
                FailCurrentPlayback(exception);
                yield break;
            }

            yield return RunPlaybackStep(returnCameraRoutine);
            if (!IsPlaying)
                yield break;
        }

        FinishPlayback(true);
    }

    private void BeginPlayback(Action finished, CutscenePlaybackSettings settings, CameraSnapshot startSnapshot)
    {
        activeFinished = finished;
        activeSettings = settings;
        activeStartSnapshot = startSnapshot;
        hasActiveSnapshot = true;
        activePlaybackCount++;
        LockWorldMouseHandlers();
    }

    private void StopCurrentPlayback(bool invokeFinished)
    {
        if (!IsPlaying && currentRoutine == null)
            return;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        CleanupActiveAnimation();
        DestroyActiveOverlayInstance();

        if (hasActiveSnapshot && activeSettings.RestoreCameraWhenFinished && sceneCamera != null)
            ApplyCamera(activeStartSnapshot.Position, activeStartSnapshot.Size);

        FinishPlayback(invokeFinished);
    }

    private IEnumerator RunPlaybackStep(IEnumerator routine)
    {
        if (routine == null)
            yield break;

        while (IsPlaying)
        {
            object current;
            try
            {
                if (!routine.MoveNext())
                    yield break;

                current = routine.Current;
            }
            catch (Exception exception)
            {
                FailCurrentPlayback(exception);
                yield break;
            }

            if (current is IEnumerator nestedRoutine)
            {
                yield return RunPlaybackStep(nestedRoutine);
                if (!IsPlaying)
                    yield break;
            }
            else
            {
                yield return current;
            }
        }
    }

    private void FailCurrentPlayback(Exception exception)
    {
        Debug.LogException(exception, this);
        CleanupActiveAnimation();
        DestroyActiveOverlayInstance();

        if (hasActiveSnapshot && activeSettings.RestoreCameraWhenFinished && sceneCamera != null)
            ApplyCamera(activeStartSnapshot.Position, activeStartSnapshot.Size);

        FinishPlayback(false);
    }

    private void CleanupPrePlaybackFailure()
    {
        DestroyActiveOverlayInstance();
        SetOverlayActive(false);
        IsPlaying = false;
        currentRoutine = null;
    }

    private void FinishPlayback(bool invokeFinished)
    {
        SetOverlayActive(false);
        UnlockWorldMouseHandlers();
        IsPlaying = false;
        currentRoutine = null;
        activeAnimation = null;
        activeContext = null;
        hasActiveSnapshot = false;

        if (activePlaybackCount > 0)
            activePlaybackCount--;

        Action finished = activeFinished;
        activeFinished = null;

        if (invokeFinished)
            finished?.Invoke();
    }

    private void CleanupActiveAnimation()
    {
        if (activeAnimation is ICutsceneAnimationCleanup cleanup)
            cleanup.Cleanup(activeContext);

        activeAnimation = null;
        activeContext = null;
    }

    private Vector3 ResolveFocusPosition(Transform focusTarget, Vector3 activeFocusOffset, float cameraZ)
    {
        Vector3 position = focusTarget != null ? focusTarget.position + activeFocusOffset : sceneCamera.transform.position;
        position.z = cameraZ;
        return position;
    }

    private IEnumerator MoveCamera(CameraSnapshot start, Vector3 targetPosition, float targetSize, float duration, bool useUnscaled, AnimationCurve ease)
    {
        if (duration <= 0f)
        {
            ApplyCamera(targetPosition, targetSize);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = ease != null ? ease.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
            Vector3 position = Vector3.LerpUnclamped(start.Position, targetPosition, eased);
            float size = Mathf.LerpUnclamped(start.Size, targetSize, eased);
            ApplyCamera(position, size);
            yield return null;
        }

        ApplyCamera(targetPosition, targetSize);
    }

    private void ApplyCamera(Vector3 position, float size)
    {
        sceneCamera.transform.position = position;

        if (sceneCamera.orthographic)
            sceneCamera.orthographicSize = Mathf.Max(0.01f, size);
        else
            sceneCamera.fieldOfView = Mathf.Clamp(size, 1f, 179f);
    }

    private GameObject InstantiateOverlayPrefab(GameObject overlayPrefab)
    {
        if (overlayPrefab == null)
            return null;

        return Instantiate(overlayPrefab, overlayRoot, false);
    }

    private ICutsceneAnimation FindAnimation(GameObject overlayInstance)
    {
        if (overlayInstance == null)
            return null;

        MonoBehaviour[] behaviours = overlayInstance.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ICutsceneAnimation animation)
                return animation;
        }

        return null;
    }

    private IEnumerator Wait(float duration, bool useUnscaled)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void DestroyActiveOverlayInstance()
    {
        if (activeOverlayInstance != null)
            Destroy(activeOverlayInstance);

        activeOverlayInstance = null;
    }

    private void LockWorldMouseHandlers()
    {
        if (!lockWorldMouseInput)
            return;

        UnlockWorldMouseHandlers();

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this || !behaviour.enabled)
                continue;

            if (behaviour.transform == transform || behaviour.transform.IsChildOf(transform))
                continue;

            if (!HandlesMouseMessages(behaviour.GetType()))
                continue;

            behaviour.enabled = false;
            lockedMouseHandlers.Add(behaviour);
        }
    }

    private void UnlockWorldMouseHandlers()
    {
        for (int i = 0; i < lockedMouseHandlers.Count; i++)
        {
            MonoBehaviour behaviour = lockedMouseHandlers[i];
            if (behaviour != null)
                behaviour.enabled = true;
        }

        lockedMouseHandlers.Clear();
    }

    private static bool HandlesMouseMessages(Type type)
    {
        if (type == null)
            return false;

        if (mouseMessageCache.TryGetValue(type, out bool cached))
            return cached;

        Type current = type;
        while (current != null && current != typeof(MonoBehaviour))
        {
            if (DeclaresMouseMessage(current))
            {
                mouseMessageCache[type] = true;
                return true;
            }

            current = current.BaseType;
        }

        mouseMessageCache[type] = false;
        return false;
    }

    private static bool DeclaresMouseMessage(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        return type.GetMethod("OnMouseDown", flags) != null
            || type.GetMethod("OnMouseDrag", flags) != null
            || type.GetMethod("OnMouseEnter", flags) != null
            || type.GetMethod("OnMouseExit", flags) != null
            || type.GetMethod("OnMouseOver", flags) != null
            || type.GetMethod("OnMouseUp", flags) != null
            || type.GetMethod("OnMouseUpAsButton", flags) != null;
    }

    private void EnsureOverlay(int sortingOrder)
    {
        if (overlayRoot != null)
        {
            Canvas existingCanvas = overlayCanvasObject.GetComponent<Canvas>();
            existingCanvas.sortingOrder = sortingOrder;
            return;
        }

        overlayCanvasObject = new GameObject("Cutscene Overlay Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlayCanvasObject.transform.SetParent(transform, false);

        Canvas canvas = overlayCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = overlayCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayRoot = overlayCanvasObject.GetComponent<RectTransform>();
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        var blockerObject = new GameObject("Input Blocker", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        blockerObject.transform.SetParent(overlayRoot, false);

        RectTransform blockerRect = blockerObject.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        CanvasGroup blockerGroup = blockerObject.GetComponent<CanvasGroup>();
        blockerGroup.alpha = 1f;
        blockerGroup.blocksRaycasts = true;
        blockerGroup.interactable = true;

        Image blockerImage = blockerObject.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = true;
    }

    private void SetOverlayActive(bool active)
    {
        if (overlayCanvasObject != null)
            overlayCanvasObject.SetActive(active);
    }

    private readonly struct CameraSnapshot
    {
        public CameraSnapshot(Vector3 position, float size)
        {
            Position = position;
            Size = size;
        }

        public Vector3 Position { get; }
        public float Size { get; }

        public static CameraSnapshot From(Camera camera)
        {
            float size = camera.orthographic ? camera.orthographicSize : camera.fieldOfView;
            return new CameraSnapshot(camera.transform.position, size);
        }
    }

    private sealed class CutsceneRequest
    {
        public CutsceneRequest(Transform focusTarget, CutsceneDefinition definition, ICutsceneAnimation animationOverride, Action finished)
        {
            FocusTarget = focusTarget;
            Definition = definition;
            AnimationOverride = animationOverride;
            Finished = finished;
        }

        public Transform FocusTarget { get; }
        public CutsceneDefinition Definition { get; }
        public ICutsceneAnimation AnimationOverride { get; }
        public Action Finished { get; }
    }

    private readonly struct CutscenePlaybackSettings
    {
        private CutscenePlaybackSettings(
            float focusOrthographicSize,
            Vector3 focusOffset,
            float moveToFocusDuration,
            float returnDuration,
            bool restoreCameraWhenFinished,
            AnimationCurve cameraEase,
            GameObject overlayPrefab,
            float holdDuration,
            bool destroyOverlayWhenFinished,
            bool useUnscaledTime,
            int overlaySortingOrder)
        {
            FocusOrthographicSize = focusOrthographicSize;
            FocusOffset = focusOffset;
            MoveToFocusDuration = moveToFocusDuration;
            ReturnDuration = returnDuration;
            RestoreCameraWhenFinished = restoreCameraWhenFinished;
            CameraEase = cameraEase;
            OverlayPrefab = overlayPrefab;
            HoldDuration = holdDuration;
            DestroyOverlayWhenFinished = destroyOverlayWhenFinished;
            UseUnscaledTime = useUnscaledTime;
            OverlaySortingOrder = overlaySortingOrder;
        }

        public float FocusOrthographicSize { get; }
        public Vector3 FocusOffset { get; }
        public float MoveToFocusDuration { get; }
        public float ReturnDuration { get; }
        public bool RestoreCameraWhenFinished { get; }
        public AnimationCurve CameraEase { get; }
        public GameObject OverlayPrefab { get; }
        public float HoldDuration { get; }
        public bool DestroyOverlayWhenFinished { get; }
        public bool UseUnscaledTime { get; }
        public int OverlaySortingOrder { get; }

        public static CutscenePlaybackSettings From(CutsceneManager manager, CutsceneDefinition definition)
        {
            bool useDefinitionCamera = definition != null && definition.OverrideManagerCameraSettings;
            return new CutscenePlaybackSettings(
                useDefinitionCamera ? definition.FocusOrthographicSize : manager.focusOrthographicSize,
                useDefinitionCamera ? definition.FocusOffset : manager.focusOffset,
                useDefinitionCamera ? definition.MoveToFocusDuration : manager.moveToFocusDuration,
                useDefinitionCamera ? definition.ReturnDuration : manager.returnDuration,
                useDefinitionCamera ? definition.RestoreCameraWhenFinished : manager.restoreCameraWhenFinished,
                useDefinitionCamera ? definition.CameraEase : manager.cameraEase,
                definition != null ? definition.OverlayPrefab : null,
                definition != null ? definition.HoldDuration : 0f,
                definition == null || definition.DestroyOverlayWhenFinished,
                definition != null ? definition.UseUnscaledTime : manager.useUnscaledTime,
                definition != null ? definition.OverlaySortingOrder : manager.overlaySortingOrder);
        }
    }
}
