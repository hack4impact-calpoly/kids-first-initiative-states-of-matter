using System.Collections.Generic;
using UnityEngine;

public class FrozenFlowValidator : MonoBehaviour
{
    public int endX = 8;
    public int endY = 4;

    Dictionary<(int,int), PipeObject> map = new();

    private void Start()
    {
        StageProgressService.BeginStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);
    }

    public bool Validate()
    {
        var pipes = FindObjectsByType<PipeObject>(FindObjectsSortMode.None);
        map.Clear();

        PipeObject end = null;

        foreach (var p in pipes)
        {
            map[(p.xPos, p.yPos)] = p;
            if (p.xPos == endX && p.yPos == endY) end = p;
        }

        if (end == null) { Debug.LogError($"No pipe at ({endX},{endY})."); return false; }
        if (!end.water)  { Debug.Log("FAIL: water did not reach end."); return false; }

        foreach (var p in pipes)
        {
            if (!p.water) continue;

            int open = CountOpen(p);
            int wetNeighbors = CountConnectedWetNeighbors(p);
            int frozenBlocks = CountAdjacentFrozenBlocks(p);
            int externalOpenings = CountExternalOpenings(p);

            if (open != wetNeighbors + frozenBlocks + externalOpenings)
            {
                Debug.Log($"FAIL: leak at ({p.xPos},{p.yPos}).");
                return false;
            }
        }

        // End must have exactly 1 wet connection (one “flowing output” into it)
        if (CountConnectedWetNeighbors(end) != 1)
        {
            Debug.Log("FAIL: end has multiple outputs or is isolated.");
            return false;
        }

        Debug.Log("SUCCESS");
        StageProgressService.CompleteStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);
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

    int CountConnectedWetNeighbors(PipeObject p)
    {
        int c = 0;

        if (p.northConnection && TryGet(p.xPos, p.yPos + 1, out var n) && n.water && n.southConnection) c++;
        if (p.southConnection && TryGet(p.xPos, p.yPos - 1, out var s) && s.water && s.northConnection) c++;
        if (p.eastConnection  && TryGet(p.xPos + 1, p.yPos, out var e) && e.water && e.westConnection)  c++;
        if (p.westConnection  && TryGet(p.xPos - 1, p.yPos, out var w) && w.water && w.eastConnection)  c++;

        return c;
    }

    int CountAdjacentFrozenBlocks(PipeObject p)
    {
        int c = 0;
        if (p.northConnection && TryGet(p.xPos, p.yPos + 1, out var n) && n.isFrozen) c++;
        if (p.southConnection && TryGet(p.xPos, p.yPos - 1, out var s) && s.isFrozen) c++;
        if (p.eastConnection && TryGet(p.xPos + 1, p.yPos, out var e) && e.isFrozen) c++;
        if (p.westConnection && TryGet(p.xPos - 1, p.yPos, out var w) && w.isFrozen) c++;
        return c;
    }

    int CountExternalOpenings(PipeObject p)
    {
        int c = 0;
        if (p.northConnection && !map.ContainsKey((p.xPos, p.yPos + 1))) c++;
        if (p.southConnection && !map.ContainsKey((p.xPos, p.yPos - 1))) c++;
        if (p.eastConnection && !map.ContainsKey((p.xPos + 1, p.yPos))) c++;
        if (p.westConnection && !map.ContainsKey((p.xPos - 1, p.yPos))) c++;
        return c;
    }

    bool TryGet(int x, int y, out PipeObject p) => map.TryGetValue((x, y), out p);
}
