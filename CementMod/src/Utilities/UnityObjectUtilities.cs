using UnityEngine;

namespace CementGB.Mod.Utilities;

public static class UnityObjectUtilities
{
    public static void MakePersistent(this Object objec, bool dontDestroyOnLoad = false)
    {
        objec.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        if (dontDestroyOnLoad) Object.DontDestroyOnLoad(objec);
    }
}