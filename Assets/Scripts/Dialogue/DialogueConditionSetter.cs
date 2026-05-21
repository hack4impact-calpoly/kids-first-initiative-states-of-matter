using UnityEngine;

public class DialogueConditionSetter : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private bool boolValue = true;
    [SerializeField] private float numberValue;
    [SerializeField] private string textValue;

    public void SetConfiguredBool()
    {
        DialogueConditionState.SetBool(key, boolValue);
    }

    public void SetBool(bool value)
    {
        DialogueConditionState.SetBool(key, value);
    }

    public void SetConfiguredNumber()
    {
        DialogueConditionState.SetNumber(key, numberValue);
    }

    public void SetNumber(float value)
    {
        DialogueConditionState.SetNumber(key, value);
    }

    public void SetConfiguredText()
    {
        DialogueConditionState.SetText(key, textValue);
    }

    public void SetText(string value)
    {
        DialogueConditionState.SetText(key, value);
    }

    public void Clear()
    {
        DialogueConditionState.Clear(key);
    }
}
