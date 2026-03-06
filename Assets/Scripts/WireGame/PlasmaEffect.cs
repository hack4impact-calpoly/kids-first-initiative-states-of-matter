using UnityEngine;
using System.Collections;

/// Attach to PlasmaTube. 4-sprite setup:
///   TubeBody (always visible), InteriorOff (hidden on activate),
///   InteriorOn (shown on activate), Glow (shown + pulses on activate).
public class PlasmaEffect : DeviceEffect
{
    [Header("Sprite References")]
    public GameObject dormantState;
    public GameObject activeState;
    public GameObject glowEffect;

    [Header("Pulse Settings (applied to Glow)")]
    public bool pulseGlow = true;
    public float pulseSpeed = 2f;
    public float pulseMinAlpha = 0.3f;

    SpriteRenderer glowRenderer;

    void Start()
    {
        if (activeState != null)
            activeState.SetActive(false);

        if (glowEffect != null)
        {
            glowRenderer = glowEffect.GetComponent<SpriteRenderer>();
            glowEffect.SetActive(false);
        }
    }

    public override void Activate()
    {
        if (dormantState != null) dormantState.SetActive(false);
        if (activeState != null) activeState.SetActive(true);
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
