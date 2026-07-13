using System.Runtime.InteropServices;
using UnityEngine;

public static class StageProgressWebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void KFI_PostUnityProgress(string json);
#endif

    public static void Post(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        KFI_PostUnityProgress(json);
#else
        Debug.Log("[StageProgress] Web payload: " + json);
#endif
    }
}
