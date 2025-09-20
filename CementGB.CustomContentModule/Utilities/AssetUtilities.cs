using System.Collections;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent.Utilities;

public static class AssetUtilities
{
    /// <summary>
    ///     Checks if the provided AsyncOperationHandle succeeded. Checks if the handle is valid, status is succeeded, and
    ///     result is not null.
    ///     Used in <see cref="HandleAsynchronousAddressableOperation{T}" /> and
    ///     <see cref="HandleSynchronousAddressableOperation{T}" />, plus several other Addressable loading methods in Cement.
    /// </summary>
    /// <param name="handle">The handle for the operation you want to verify succeeded.</param>
    /// <returns>Whether the passed handle succeeded, and the result is not null.</returns>
    public static bool IsHandleSuccess(AsyncOperationHandle handle)
    {
        return handle.IsValid() &&
               handle is { Status: AsyncOperationStatus.Succeeded, Result: not null };
    }

    /// <summary>
    ///     A coroutine method that asynchronously waits for the handle to complete, and then checks its success. If the handle
    ///     fails, a verbose message is logged to the console and the handle is released.
    ///     You may want to call <see cref="IsHandleSuccess" /> after this operation if you need to handle failure or success
    ///     differently.
    /// </summary>
    /// <param name="handle">The handle to yield until completion and check success for.</param>
    /// <typeparam name="T">The result type of the handle.</typeparam>
    /// <returns>An IEnumerator you can yield for in an existing coroutine or start using <c>MelonCoroutines.Start</c>.</returns>
    public static IEnumerator HandleAsynchronousAddressableOperation<T>(this AsyncOperationHandle<T> handle)
        where T : Il2CppObjectBase
    {
        if (!handle.IsDone)
        {
            yield return handle;
        }

        if (!IsHandleSuccess(handle))
        {
            LoggingUtilities.VerboseLog(
                ConsoleColor.DarkRed,
                $"Failed to perform action in asynchronous Addressable handle! | OperationException: {(handle.IsValid() ? handle.OperationException.ToString() : "INVALID HANDLE!")} | Result == null: {!handle.IsValid() || handle.Result == null}");
            if (handle.IsValid())
            {
                handle.Release();
            }

            yield break;
        }

        var res = handle.Result;

        var obj = res.TryCast<Object>();
        obj?.MakePersistent();
    }

    /// <summary>
    ///     Synchronously waits for the provided handle to complete, then checks if it succeeded. If the handle fails, a
    ///     verbose message is logged to the console and the handle is released.
    ///     You may want to call <see cref="IsHandleSuccess" /> after this operation if you need to handle failure or success
    ///     differently.
    /// </summary>
    /// <param name="handle">The operation to wait synchronously for, and then check success.</param>
    /// <typeparam name="T">The result type of the handle.</typeparam>
    /// <returns>True if the handle succeeded, false if it didn't.</returns>
    public static bool HandleSynchronousAddressableOperation<T>(this AsyncOperationHandle<T> handle)
        where T : Il2CppObjectBase
    {
        var res = handle.WaitForCompletion();

        if (!IsHandleSuccess(handle))
        {
            LoggingUtilities.VerboseLog(
                ConsoleColor.DarkRed,
                $"Failed to perform action in synchronous Addressable handle! | OperationException: {(handle.IsValid() ? handle.OperationException.ToString() : "INVALID HANDLE!")} | Result == null: {!handle.IsValid() || handle.Result == null}");
            if (handle.IsValid())
            {
                handle.Release();
            }

            return false;
        }

        var obj = res.TryCast<Object>();
        obj?.MakePersistent();

        return true;
    }

    /// <summary>
    ///     Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load.</typeparam>
    /// <param name="bundle">The bundle to load the asset from.</param>
    /// <param name="name">The exact name of the asset to load.</param>
    /// <returns>The loaded asset with <c>hideFlags</c> set persistently.</returns>
    public static T? LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : Object
    {
        var asset = bundle.LoadAsset<T>(name);

        if (!asset)
        {
            return null;
        }

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

        request.add_completed(
            (Il2CppSystem.Action<AsyncOperation>)(a =>
            {
                if (!request.asset)
                {
                    return;
                }

                var result = request.asset.TryCast<T>();
                if (result == null)
                {
                    return;
                }

                result.MakePersistent();
                onLoaded?.Invoke(result);
            }));
    }

    /// <summary>
    ///     Shorthand for asynchronously loading every asset from a bundle, and preventing the result from being garbage
    ///     collected.
    /// </summary>
    /// <param name="bundle"></param>
    /// <param name="onLoaded">Method to perform once the assets are loaded from the bundle.</param>
    /// <typeparam name="T">The result type. Must derive from UnityEngine.Object.</typeparam>
    public static void LoadAllAssetsPersistentAsync<T>(this AssetBundle bundle, Action<T> onLoaded)
        where T : Object
    {
        var request = bundle.LoadAllAssetsAsync<T>();

        request.add_completed(
            new Action<AsyncOperation>(a =>
            {
                if (!request.asset)
                {
                    return;
                }

                var result = request.asset.TryCast<T>();
                if (result == null)
                {
                    return;
                }

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