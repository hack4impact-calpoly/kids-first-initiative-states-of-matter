using UnityEngine;
using System.Collections;

/// Attach to an IceFlask child of HotPlate.
/// When activated: ice shrinks & fades out, water rises.
public class IceMeltEffect : DeviceEffect
{
    [Header("References")]
    public Transform ice;
    public Transform water;

    [Header("Melt Settings")]
    public float meltDuration = 3f;
    public float waterRiseAmount = 0.5f;

    Vector3 iceStartScale;
    Vector3 waterStartPos;
    SpriteRenderer iceRenderer;

    void Start()
    {
        if (ice != null)
        {
            iceStartScale = ice.localScale;
            iceRenderer = ice.GetComponent<SpriteRenderer>();
        }
        if (water != null)
        {
            waterStartPos = water.localPosition;
            water.gameObject.SetActive(false);
        }
    }

    public override void Activate()
    {
        StartCoroutine(MeltRoutine());
    }

    IEnumerator MeltRoutine()
    {
        if (water != null) water.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < meltDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / meltDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (ice != null)
            {
                ice.localScale = Vector3.Lerp(iceStartScale, Vector3.zero, smooth);
                if (iceRenderer != null)
                {
                    Color c = iceRenderer.color;
                    c.a = 1f - smooth;
                    iceRenderer.color = c;
                }
            }

            if (water != null)
            {
                water.localPosition = waterStartPos + Vector3.up * (waterRiseAmount * smooth);
            }

            yield return null;
        }

        if (ice != null) ice.gameObject.SetActive(false);
    }
}
