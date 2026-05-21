using System;

public enum DialogueConditionValueKind
{
    Bool,
    Number,
    Text
}

[Serializable]
public struct DialogueConditionValue
{
    public DialogueConditionValue(DialogueConditionValueKind kind, bool boolValue, float numberValue, string textValue)
    {
        Kind = kind;
        BoolValue = boolValue;
        NumberValue = numberValue;
        TextValue = textValue;
    }

    public DialogueConditionValueKind Kind { get; }
    public bool BoolValue { get; }
    public float NumberValue { get; }
    public string TextValue { get; }

    public static DialogueConditionValue FromBool(bool value)
    {
        return new DialogueConditionValue(DialogueConditionValueKind.Bool, value, value ? 1f : 0f, value ? "true" : "false");
    }

    public static DialogueConditionValue FromNumber(float value)
    {
        return new DialogueConditionValue(DialogueConditionValueKind.Number, value != 0f, value, value.ToString());
    }

    public static DialogueConditionValue FromText(string value)
    {
        return new DialogueConditionValue(DialogueConditionValueKind.Text, !string.IsNullOrEmpty(value), 0f, value);
    }
}
