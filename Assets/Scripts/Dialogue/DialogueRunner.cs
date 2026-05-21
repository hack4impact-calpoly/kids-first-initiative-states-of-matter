using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueRunner : MonoBehaviour
{
    [SerializeField] private DialogueView view;
    [SerializeField] private bool createViewIfMissing = true;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private DialogueSequence initialSequence;
    [SerializeField] private bool playInitialSequenceOnStart;
    [SerializeField] private bool allowManualAdvanceDuringAutoAdvance = true;
    [SerializeField] private UnityEvent started;
    [SerializeField] private UnityEvent lineChanged;
    [SerializeField] private UnityEvent finished;

    private readonly Queue<DialogueRequest> queuedRequests = new Queue<DialogueRequest>();
    private DialogueRequest activeRequest;
    private DialogueSequence activeSequence;
    private Coroutine autoAdvanceRoutine;
    private Coroutine voiceGateRoutine;
    private Coroutine queuedStartRoutine;
    private int currentIndex = -1;
    private int currentLineVersion;
    private float previousTimeScale = 1f;
    private bool pausedTimeScale;
    private bool currentLineReadyToAdvance;

    public event Action DialogueStarted;
    public event Action<DialogueLine> LineChanged;
    public event Action DialogueFinished;

    public bool IsPlaying { get; private set; }
    public int QueuedCount => queuedRequests.Count;
    public DialogueSequence ActiveSequence => activeSequence;
    public int CurrentIndex => currentIndex;
    public DialogueLine CurrentLine => IsPlaying && activeSequence != null ? activeSequence.GetLine(currentIndex) : null;

    private void Awake()
    {
        if (view == null)
            view = FindAnyObjectByType<DialogueView>(FindObjectsInactive.Include);

        if (view == null && createViewIfMissing)
        {
            var viewObject = new GameObject("Dialogue View");
            viewObject.transform.SetParent(transform, false);
            view = viewObject.AddComponent<DialogueView>();
        }

        if (voiceAudioSource == null)
            voiceAudioSource = GetComponent<AudioSource>();

        if (voiceAudioSource == null)
            voiceAudioSource = gameObject.AddComponent<AudioSource>();

        ConfigureVoiceAudioSource();
    }

    private void OnEnable()
    {
        if (view != null)
            view.ContinueRequested += Advance;
    }

    private void Start()
    {
        if (playInitialSequenceOnStart)
            Queue(initialSequence);
    }

    private void OnDisable()
    {
        if (view != null)
            view.ContinueRequested -= Advance;

        ClearQueue();
        EndDialogue(false, false, true);
    }

    public bool Play(DialogueSequence sequence)
    {
        return Queue(sequence);
    }

    public bool Queue(DialogueSequence sequence, Action onStarted = null, Action onFinished = null, Action onCanceled = null)
    {
        if (!CanAccept(sequence))
            return false;

        queuedRequests.Enqueue(new DialogueRequest(sequence, onStarted, onFinished, onCanceled));

        if (!IsPlaying && queuedStartRoutine == null)
            StartNextQueuedDialogue();

        return true;
    }

    public bool PlayNow(DialogueSequence sequence, Action onStarted = null, Action onFinished = null, Action onCanceled = null)
    {
        if (!CanAccept(sequence))
            return false;

        ClearQueue();
        EndDialogue(false, false, true);
        StartDialogue(new DialogueRequest(sequence, onStarted, onFinished, onCanceled));
        return true;
    }

    public void Advance()
    {
        if (!IsPlaying || !currentLineReadyToAdvance)
            return;

        if (currentIndex + 1 >= activeSequence.Count)
        {
            EndDialogue(true, true, false);
            return;
        }

        currentIndex++;
        PresentCurrentLine();
    }

    public void SkipCurrent()
    {
        EndDialogue(true, true, true);
    }

    public void ClearQueue()
    {
        while (queuedRequests.Count > 0)
            queuedRequests.Dequeue().InvokeCanceled();

        StopQueuedStartRoutine();
    }

    private bool CanAccept(DialogueSequence sequence)
    {
        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("DialogueRunner needs a DialogueSequence with at least one line.", this);
            return false;
        }

        if (view == null)
        {
            Debug.LogWarning("DialogueRunner needs a DialogueView to show dialogue.", this);
            return false;
        }

        return true;
    }

    private void StartNextQueuedDialogue()
    {
        if (IsPlaying || queuedRequests.Count == 0)
            return;

        DialogueRequest request = queuedRequests.Dequeue();
        StartDialogue(request);
    }

    private void StartDialogue(DialogueRequest request)
    {
        activeRequest = request;
        activeSequence = request.Sequence;
        currentIndex = 0;
        IsPlaying = true;
        PauseTimeIfNeeded(activeSequence);

        activeRequest.InvokeStarted();
        started?.Invoke();
        DialogueStarted?.Invoke();
        PresentCurrentLine();
    }

    private void PresentCurrentLine()
    {
        StopLineRoutines();
        currentLineVersion++;

        DialogueLine line = CurrentLine;
        PlayLineAudio(line);

        currentLineReadyToAdvance = !ShouldWaitForVoice(line);
        view.ShowLine(line, ShouldShowContinue(line));

        lineChanged?.Invoke();
        LineChanged?.Invoke(line);

        if (!currentLineReadyToAdvance)
            voiceGateRoutine = StartCoroutine(WaitForVoiceThenUnlock(currentLineVersion, line));

        if (line.ShouldAutoAdvance)
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceWhenReady(currentLineVersion, line.AutoAdvanceDelay));
    }

    private void PlayLineAudio(DialogueLine line)
    {
        if (voiceAudioSource == null)
            return;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = null;
        ConfigureVoiceAudioSource();

        if (line == null || line.VoiceClip == null)
            return;

        voiceAudioSource.clip = line.VoiceClip;
        voiceAudioSource.Play();
    }

    private void ConfigureVoiceAudioSource()
    {
        if (voiceAudioSource == null)
            return;

        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.loop = false;
        voiceAudioSource.pitch = 1f;
        voiceAudioSource.spatialBlend = 0f;
        voiceAudioSource.dopplerLevel = 0f;
    }

    private IEnumerator WaitForVoiceThenUnlock(int lineVersion, DialogueLine line)
    {
        while (IsPlaying && lineVersion == currentLineVersion && IsVoicePlaying(line))
            yield return null;

        if (!IsPlaying || lineVersion != currentLineVersion)
            yield break;

        currentLineReadyToAdvance = true;
        view.SetContinueEnabled(ShouldShowContinue(line));
        voiceGateRoutine = null;
    }

    private IEnumerator AutoAdvanceWhenReady(int lineVersion, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        while (IsPlaying && lineVersion == currentLineVersion && !currentLineReadyToAdvance)
            yield return null;

        if (!IsPlaying || lineVersion != currentLineVersion)
            yield break;

        autoAdvanceRoutine = null;
        Advance();
    }

    private bool ShouldWaitForVoice(DialogueLine line)
    {
        return line != null && line.WaitForVoiceClipBeforeAdvance && IsVoicePlaying(line);
    }

    private bool IsVoicePlaying(DialogueLine line)
    {
        return voiceAudioSource != null && line != null && line.VoiceClip != null && voiceAudioSource.clip == line.VoiceClip && voiceAudioSource.isPlaying;
    }

    private bool ShouldShowContinue(DialogueLine line)
    {
        if (line == null || !line.RequiresContinue || !currentLineReadyToAdvance)
            return false;

        return allowManualAdvanceDuringAutoAdvance || !line.ShouldAutoAdvance;
    }

    private void EndDialogue(bool notifyFinished, bool continueQueue, bool forceStopAudio)
    {
        DialogueSequence finishingSequence = activeSequence;
        DialogueRequest finishingRequest = activeRequest;
        bool shouldStopAudio = forceStopAudio || (!notifyFinished && finishingSequence != null && finishingSequence.StopLineAudioWhenSkipped);

        StopLineRoutines();

        if (shouldStopAudio && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = null;
        }

        activeSequence = null;
        activeRequest = default;
        currentIndex = -1;
        currentLineReadyToAdvance = false;

        bool wasPlaying = IsPlaying;
        IsPlaying = false;

        bool hasQueuedDialogue = queuedRequests.Count > 0;
        if (view != null && !hasQueuedDialogue && (finishingSequence == null || finishingSequence.HideViewWhenFinished))
            view.Hide();

        RestoreTimeIfNeeded();

        if (notifyFinished && wasPlaying)
        {
            finishingRequest.InvokeFinished();
            finished?.Invoke();
            DialogueFinished?.Invoke();
        }
        else if (wasPlaying)
        {
            finishingRequest.InvokeCanceled();
        }

        if (!continueQueue)
            return;

        if (ShouldWaitBeforeNextQueuedSequence(finishingSequence, forceStopAudio))
            queuedStartRoutine = StartCoroutine(StartNextQueuedDialogueAfterAudio());
        else
            StartNextQueuedDialogue();
    }

    private bool ShouldWaitBeforeNextQueuedSequence(DialogueSequence finishingSequence, bool forceStopAudio)
    {
        return !forceStopAudio
            && finishingSequence != null
            && finishingSequence.WaitForAudioBeforeNextQueuedSequence
            && queuedRequests.Count > 0
            && voiceAudioSource != null
            && voiceAudioSource.isPlaying;
    }

    private IEnumerator StartNextQueuedDialogueAfterAudio()
    {
        while (voiceAudioSource != null && voiceAudioSource.isPlaying)
            yield return null;

        if (voiceAudioSource != null)
            voiceAudioSource.clip = null;

        queuedStartRoutine = null;
        StartNextQueuedDialogue();
    }

    private void PauseTimeIfNeeded(DialogueSequence sequence)
    {
        if (!sequence.PauseTimeWhilePlaying || pausedTimeScale)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausedTimeScale = true;
    }

    private void RestoreTimeIfNeeded()
    {
        if (!pausedTimeScale)
            return;

        Time.timeScale = previousTimeScale;
        pausedTimeScale = false;
    }

    private void StopLineRoutines()
    {
        if (autoAdvanceRoutine != null)
        {
            StopCoroutine(autoAdvanceRoutine);
            autoAdvanceRoutine = null;
        }

        if (voiceGateRoutine != null)
        {
            StopCoroutine(voiceGateRoutine);
            voiceGateRoutine = null;
        }
    }

    private void StopQueuedStartRoutine()
    {
        if (queuedStartRoutine == null)
            return;

        StopCoroutine(queuedStartRoutine);
        queuedStartRoutine = null;
    }

    private readonly struct DialogueRequest
    {
        public DialogueRequest(DialogueSequence sequence, Action onStarted, Action onFinished, Action onCanceled)
        {
            Sequence = sequence;
            OnStarted = onStarted;
            OnFinished = onFinished;
            OnCanceled = onCanceled;
        }

        public DialogueSequence Sequence { get; }
        private Action OnStarted { get; }
        private Action OnFinished { get; }
        private Action OnCanceled { get; }

        public void InvokeStarted()
        {
            OnStarted?.Invoke();
        }

        public void InvokeFinished()
        {
            OnFinished?.Invoke();
        }

        public void InvokeCanceled()
        {
            OnCanceled?.Invoke();
        }
    }
}
