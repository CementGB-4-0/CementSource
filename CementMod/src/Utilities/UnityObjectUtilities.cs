using UnityEngine;

namespace CementGB.Mod.Utilities;

public static class UnityObjectUtilities
{
    /// <summary>
    ///     Prevents a UnityEngine.Object from being garbage collected or otherwise automatically destroyed.
    ///     Sets the hideFlags of the <paramref name="objec" /> to <c>HideFlags.HideAndDontSave</c> and
    ///     <c>HideFlags.DontUnloadUnusedAsset</c>.
    /// </summary>
    /// <param name="objec"></param>
    /// <param name="dontDestroyOnLoad">Whether to also make sure the object stays persistent between scenes or not.</param>
    public static void MakePersistent(this Object objec, bool dontDestroyOnLoad = false)
    {
        objec.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        if (dontDestroyOnLoad)
        {
            Object.DontDestroyOnLoad(objec);
        }
    }
}