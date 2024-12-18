using UnityEngine;

namespace CementGB.Mod.Utilities;

public static class GameObjectUtilities
{
    public static void MakePersistent(this UnityEngine.Object go, bool dontDestroyOnLoad = false) => go.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
}