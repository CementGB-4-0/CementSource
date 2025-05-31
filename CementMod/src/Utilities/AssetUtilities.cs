using System;
using System.Collections;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace CementGB.Mod.Utilities;

public static class AssetUtilities
{
    public static bool IsHandleSuccess(AsyncOperationHandle handle)
    {
        return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null;
    }

    public static IEnumerator HandleAsynchronousAddressableOperation<T>(this AsyncOperationHandle<T> handle)
        where T : Il2CppObjectBase
    {
        if (!handle.IsDone)
            yield return handle;

        if (!IsHandleSuccess(handle))
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                $"Failed to load asset from asynchronous Addressable handle! | OperationException: {(handle.IsValid() ? handle.OperationException.ToString() : "INVALID HANDLE!")} | Result == null: {!handle.IsValid() || handle.Result == null}");
            if (handle.IsValid()) handle.Release();
            yield break;
        }

        var res = handle.Result;

        var obj = res.TryCast<Object>();
        if (obj)
            obj.MakePersistent();
    }

    public static bool HandleSynchronousAddressableOperation<T>(this AsyncOperationHandle<T> handle)
        where T : Il2CppObjectBase
    {
        var res = handle.WaitForCompletion();

        if (!IsHandleSuccess(handle))
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                $"Failed to load asset from synchronous Addressable handle! | OperationException: {(handle.IsValid() ? handle.OperationException.ToString() : "INVALID HANDLE!")} | Result == null: {!handle.IsValid() || handle.Result == null}");
            handle.Release();
            return false;
        }

        var obj = res.TryCast<Object>();
        if (obj)
            obj.MakePersistent();

        return true;
    }

    /// <summary>
    ///     Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load.</typeparam>
    /// <param name="bundle">The bundle to load the asset from.</param>
    /// <param name="name">The exact name of the asset to load.</param>
    /// <returns>The loaded asset with <c>hideFlags</c> set persistently.</returns>
    public static T LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : Object
    {
        var asset = bundle.LoadAsset<T>(name);

        if (!asset)
            return null;
        asset.MakePersistent();
        return asset;
    }

    /// <summary>
    ///     Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected.
    ///     This method will execute the callback when async loading is complete.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load.</typeparam>
    /// <param name="bundle">The bundle to load the asset from.</param>
    /// <param name="name">The exact name of the asset to load.</param>
    /// <param name="onLoaded">The callback to execute once the asset loads. Takes the loaded asset as a parameter.</param>
    public static void LoadPersistentAssetAsync<T>(this AssetBundle bundle, string name, Action<T> onLoaded)
        where T : Object
    {
        var request = bundle.LoadAssetAsync<T>(name);

        request.add_completed((Il2CppSystem.Action<AsyncOperation>)(a =>
        {
            if (request.asset == null)
                return;

            var result = request.asset.TryCast<T>();
            if (result == null)
                return;

            result.MakePersistent();
            onLoaded?.Invoke(result);
        }));
    }

    public static void LoadAllAssetsPersistentAsync<T>(this AssetBundle bundle, Action<T> onLoaded)
        where T : Object
    {
        var request = bundle.LoadAllAssetsAsync<T>();

        request.add_completed(new Action<AsyncOperation>(a =>
        {
            if (request.asset == null)
                return;

            var result = request.asset.TryCast<T>();
            if (result == null)
                return;

            result.MakePersistent();
            onLoaded?.Invoke(result);
        }));
    }

    /*     /// <summary>
        /// A hacky Coroutine that calls Shader.Find on all materials on the passed GameObject and children, or all objects in the scene if not specified, and passes in the name of the current shader, setting the Material.shader value.
        /// </summary>
        public static IEnumerator RefindMaterials(GameObject parent=null)
        {
            yield return new WaitForEndOfFrame();
            Mod.Logger.Warning("Refreshing materials. . .");
            Il2CppArrayBase<MeshRenderer> renderers;
            if (parent == null)
                renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            else
            {
                var rendList = new List<MeshRenderer>();
                rendList.AddRange(parent.GetComponents<MeshRenderer>());
                rendList.AddRange(parent.GetComponentsInChildren<MeshRenderer>());

                renderers = new Il2CppReferenceArray<MeshRenderer>([.. rendList]);
            }

            foreach (var meshRenderer in renderers)
            {
                foreach (var material in meshRenderer.materials)
                {
                    material.shader = Shader.Find(material.shader.name);
                }
            }
        } */
}