using System.Collections.Generic;
using UnityEngine;

public class SteamFlowValidator : MonoBehaviour
{
    public int endX = 8;
    public int endY = 4;

    Dictionary<(int, int), PipeObject> map = new();

    public bool Validate()
    {
        var pipes = FindObjectsOfType<PipeObject>();
        map.Clear();
        PipeObject end = null;

        foreach (var p in pipes)
        {
            map[(p.xPos, p.yPos)] = p;
            if (p.xPos == endX && p.yPos == endY) end = p;
        }

        if (end == null) { Debug.LogError($"No pipe at ({endX},{endY})."); return false; }
        if (!end.steam)  { Debug.Log("FAIL: steam did not reach end."); return false; }

        foreach (var p in pipes)
        {
            if (!p.steam) continue;

            int open = CountOpen(p);
            int steamNeighbors = CountConnectedSteamNeighbors(p);

            if (open != steamNeighbors)
            {
                Debug.Log($"FAIL: leak at ({p.xPos},{p.yPos}).");
                return false;
            }
        }

        if (CountConnectedSteamNeighbors(end) != 1)
        {
            Debug.Log("FAIL: end has multiple outputs or is isolated.");
            return false;
        }

        Debug.Log("SUCCESS");
        return true;
    }

    int CountOpen(PipeObject p)
    {
        int c = 0;
        if (p.northConnection) c++;
        if (p.southConnection) c++;
        if (p.eastConnection)  c++;
        if (p.westConnection)  c++;
        return c;
    }

    int CountConnectedSteamNeighbors(PipeObject p)
    {
        int c = 0;
        if (p.northConnection && TryGet(p.xPos, p.yPos + 1, out var n) && n.steam && n.southConnection) c++;
        if (p.southConnection && TryGet(p.xPos, p.yPos - 1, out var s) && s.steam && s.northConnection) c++;
        if (p.eastConnection  && TryGet(p.xPos + 1, p.yPos, out var e) && e.steam && e.westConnection)  c++;
        if (p.westConnection  && TryGet(p.xPos - 1, p.yPos, out var w) && w.steam && w.eastConnection)  c++;
        return c;
    }

    bool TryGet(int x, int y, out PipeObject p) => map.TryGetValue((x, y), out p);
}