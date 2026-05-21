using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueConditionState
{
    private static readonly Dictionary<string, DialogueConditionValue> values = new Dictionary<string, DialogueConditionValue>();

    public static event Action<string, DialogueConditionValue> Changed;
    public static event Action<string> Cleared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        values.Clear();
        Changed = null;
        Cleared = null;
    }

    public static void SetBool(string key, bool value)
    {
        SetValue(key, DialogueConditionValue.FromBool(value));
    }

    public static void SetNumber(string key, float value)
    {
        SetValue(key, DialogueConditionValue.FromNumber(value));
    }

    public static void SetText(string key, string value)
    {
        SetValue(key, DialogueConditionValue.FromText(value));
    }

    public static void SetValue(string key, DialogueConditionValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        values[key] = value;
        Changed?.Invoke(key, value);
    }

    public static bool TryGetValue(string key, out DialogueConditionValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = default;
            return false;
        }

        return values.TryGetValue(key, out value);
    }

    public static bool HasValue(string key)
    {
        return TryGetValue(key, out _);
    }

    public static void Clear(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !values.Remove(key))
            return;

        Cleared?.Invoke(key);
    }

    public static void ClearAll()
    {
        string[] keys = new string[values.Count];
        values.Keys.CopyTo(keys, 0);
        values.Clear();

        for (int i = 0; i < keys.Length; i++)
            Cleared?.Invoke(keys[i]);
    }
}
