using UnityEngine;
using System.Collections;

/// Attach to a CandleFlask child of HotPlate.
/// When activated: candle sinks & rotates, wax stretches up, flame follows candle then disappears.
public class CandleMeltEffect : DeviceEffect
{
    [Header("References")]
    public Transform candle;
    public Transform liquid;
    public Transform flame;

    [Header("Melt Settings")]
    public float meltDuration = 3f;
    public float candleSinkAmount = 0.5f;
    public float candleRotation = -15f;
    public float liquidMaxScaleY = 2f;

    Vector3 candleStartPos;
    Vector3 liquidStartScale;
    Quaternion candleStartRot;
    Vector3 flameOffset;

    void Start()
    {
        if (candle != null)
        {
            candleStartPos = candle.localPosition;
            candleStartRot = candle.localRotation;
        }
        if (liquid != null)
        {
            liquidStartScale = liquid.localScale;
            liquid.localScale = new Vector3(liquidStartScale.x, 0f, liquidStartScale.z);
            liquid.gameObject.SetActive(false);
        }
        if (flame != null && candle != null)
        {
            flameOffset = flame.localPosition - candle.localPosition;
        }
    }

    public override void Activate()
    {
        StartCoroutine(MeltRoutine());
    }

    IEnumerator MeltRoutine()
    {
        if (liquid != null) liquid.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < meltDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / meltDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (candle != null)
            {
                candle.localPosition = candleStartPos + Vector3.down * (candleSinkAmount * smooth);
                candle.localRotation = candleStartRot * Quaternion.Euler(0f, 0f, candleRotation * smooth);
            }

            // Stretch wax upward from its base
            if (liquid != null)
            {
                float yScale = Mathf.Lerp(0f, liquidMaxScaleY, smooth);
                liquid.localScale = new Vector3(liquidStartScale.x, yScale, liquidStartScale.z);
            }

            // Flame follows candle position + rotation
            if (flame != null && candle != null)
            {
                flame.localPosition = candle.localPosition + candle.localRotation * flameOffset;
                flame.localRotation = candle.localRotation;
            }

            yield return null;
        }

        if (flame != null) flame.gameObject.SetActive(false);
    }
}
