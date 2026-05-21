using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueConditionMode
{
    All,
    Any
}

[Serializable]
public class DialogueConditionSet
{
    [SerializeField] private DialogueConditionMode mode = DialogueConditionMode.All;
    [SerializeField] private List<DialogueConditionRule> rules = new List<DialogueConditionRule>();

    public bool IsMet()
    {
        if (rules == null || rules.Count == 0)
            return true;

        if (mode == DialogueConditionMode.Any)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] != null && rules[i].IsMet())
                    return true;
            }

            return false;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i] == null || !rules[i].IsMet())
                return false;
        }

        return true;
    }
}
