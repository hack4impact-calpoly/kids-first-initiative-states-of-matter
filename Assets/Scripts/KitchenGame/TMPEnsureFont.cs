using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPEnsureFont : MonoBehaviour
{
    private static TMP_FontAsset _defaultFont;

    private void OnEnable()
    {
        var tmp = GetComponent<TMP_Text>();
        if (tmp == null || tmp.font != null) return;

        if (_defaultFont == null)
            _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (_defaultFont != null)
            tmp.font = _defaultFont;
    }
}
