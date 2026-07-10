using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FrozenFlowValidator : MonoBehaviour
{
    public int endX = 8;
    public int endY = 4;

    [Header("Scene Load")]
    public string successSceneName = "GameSelector";

    Dictionary<(int,int), PipeObject> map = new();

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

            // leak = open connection not connected to wet pipe
            if (open != wetNeighbors)
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
        SceneManager.LoadScene(successSceneName);
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

    bool TryGet(int x, int y, out PipeObject p) => map.TryGetValue((x, y), out p);
}
