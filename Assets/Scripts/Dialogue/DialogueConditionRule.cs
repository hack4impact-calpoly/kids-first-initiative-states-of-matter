using System;
using UnityEngine;

public enum DialogueConditionComparison
{
    Exists,
    DoesNotExist,
    Equals,
    NotEquals,
    GreaterThan,
    GreaterOrEqual,
    LessThan,
    LessOrEqual
}

[Serializable]
public class DialogueConditionRule
{
    [SerializeField] private string key;
    [SerializeField] private DialogueConditionValueKind valueKind = DialogueConditionValueKind.Bool;
    [SerializeField] private DialogueConditionComparison comparison = DialogueConditionComparison.Equals;
    [SerializeField] private bool expectedBool = true;
    [SerializeField] private float expectedNumber;
    [SerializeField] private string expectedText;
    [SerializeField] private bool caseSensitiveText;

    public bool IsMet()
    {
        bool hasValue = DialogueConditionState.TryGetValue(key, out DialogueConditionValue currentValue);

        if (comparison == DialogueConditionComparison.Exists)
            return hasValue;

        if (comparison == DialogueConditionComparison.DoesNotExist)
            return !hasValue;

        if (!hasValue || currentValue.Kind != valueKind)
            return false;

        switch (valueKind)
        {
            case DialogueConditionValueKind.Bool:
                return CompareBool(currentValue.BoolValue);
            case DialogueConditionValueKind.Number:
                return CompareNumber(currentValue.NumberValue);
            case DialogueConditionValueKind.Text:
                return CompareText(currentValue.TextValue);
            default:
                return false;
        }
    }

    private bool CompareBool(bool current)
    {
        switch (comparison)
        {
            case DialogueConditionComparison.Equals:
                return current == expectedBool;
            case DialogueConditionComparison.NotEquals:
                return current != expectedBool;
            default:
                return false;
        }
    }

    private bool CompareNumber(float current)
    {
        switch (comparison)
        {
            case DialogueConditionComparison.Equals:
                return Mathf.Approximately(current, expectedNumber);
            case DialogueConditionComparison.NotEquals:
                return !Mathf.Approximately(current, expectedNumber);
            case DialogueConditionComparison.GreaterThan:
                return current > expectedNumber;
            case DialogueConditionComparison.GreaterOrEqual:
                return current >= expectedNumber || Mathf.Approximately(current, expectedNumber);
            case DialogueConditionComparison.LessThan:
                return current < expectedNumber;
            case DialogueConditionComparison.LessOrEqual:
                return current <= expectedNumber || Mathf.Approximately(current, expectedNumber);
            default:
                return false;
        }
    }

    private bool CompareText(string current)
    {
        StringComparison stringComparison = caseSensitiveText ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        switch (comparison)
        {
            case DialogueConditionComparison.Equals:
                return string.Equals(current, expectedText, stringComparison);
            case DialogueConditionComparison.NotEquals:
                return !string.Equals(current, expectedText, stringComparison);
            default:
                return false;
        }
    }
}
