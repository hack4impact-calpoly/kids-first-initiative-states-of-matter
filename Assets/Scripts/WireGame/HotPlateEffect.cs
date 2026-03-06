using UnityEngine;
using System.Collections;

/// Attach to HotPlate. 3-sprite setup:
///   Off state (shown at start), On state (shown on activate),
///   Glow (shown + pulses on activate).
public class HotPlateEffect : DeviceEffect
{
    [Header("Sprite References")]
    public GameObject offState;
    public GameObject onState;
    public GameObject glowEffect;

    [Header("Pulse Settings (applied to Glow)")]
    public bool pulseGlow = true;
    public float pulseSpeed = 2f;
    public float pulseMinAlpha = 0.3f;

    SpriteRenderer glowRenderer;

    void Start()
    {
        if (onState != null)
            onState.SetActive(false);

        if (glowEffect != null)
        {
            glowRenderer = glowEffect.GetComponent<SpriteRenderer>();
            glowEffect.SetActive(false);
        }
    }

    public override void Activate()
    {
        if (onState != null) onState.SetActive(true);
        if (glowEffect != null) glowEffect.SetActive(true);

        if (pulseGlow && glowRenderer != null)
        {
            StartCoroutine(PulseRoutine());
        }
    }

    IEnumerator PulseRoutine()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(pulseMinAlpha, 1f, t);
            Color c = glowRenderer.color;
            c.a = alpha;
            glowRenderer.color = c;
            yield return null;
        }
    }
}
