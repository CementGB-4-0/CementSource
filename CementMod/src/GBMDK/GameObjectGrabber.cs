using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace GBMDK;

[RegisterTypeInIl2Cpp]
public class GameObjectGrabber : MonoBehaviour
{
    public Il2CppReferenceField<Il2CppSystem.String> addressableSceneKey;
    public Il2CppReferenceField<Il2CppSystem.String> grabbedGameObjectPath;
    public Il2CppValueField<bool> grabOnStart;

    public string AddressableSceneKey => addressableSceneKey.Get();
    public string GrabbedGameObjectPath => grabbedGameObjectPath.Get();
    
    public static Transform? FindRecursive(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        foreach (var o in parent)
        {
            var child = o.Cast<Transform>();
            var foundChild = child.Find(name);
            if (foundChild != null)
            {
                return foundChild;
            }

            foundChild = FindRecursive(child, name);
            if (foundChild != null)
            {
                return foundChild;
            }
        }
        return null;
    }

    public static string GetGameObjectPath(GameObject obj)
    {
        var path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

    public static GameObject? GrabHandle(string sceneKey, string goPath, bool pathDoesntMatter = false)
    {
        var ret = Instantiate(new GameObject($"GrabHandle ({sceneKey}/{goPath})"), null);
        if (string.IsNullOrWhiteSpace(sceneKey) || string.IsNullOrWhiteSpace(goPath))
        {
            Destroy(ret);
            return null;
        }

        var loadTask = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, false);
        if (!loadTask.HandleSynchronousAddressableOperation())
        {
            Destroy(ret);
            return null;
        }

        var additiveScene = loadTask.Result.Scene;
        foreach (var rootGo in additiveScene.GetRootGameObjects())
        {
            if (GetGameObjectPath(rootGo) == goPath)
            {
                _ = Instantiate(rootGo, ret.transform);
                foreach (var dep in GrabGameObjectDependencies(rootGo))
                {
                    _ = Instantiate(dep, ret.transform);
                }
                break;
            }

            var matchingNameGo = FindRecursive(rootGo.transform, goPath.Split('/').Last())?.gameObject;
            if (matchingNameGo == null || GetGameObjectPath(matchingNameGo) != goPath)
                continue;

            _ = Instantiate(matchingNameGo, ret.transform);
            foreach (var dep in GrabGameObjectDependencies(matchingNameGo))
            {
                _ = Instantiate(dep, ret.transform);
            }

            break;
        }

        if (ret.transform.childCount > 0)
        {
            ret = Instantiate(ret, Vector3.zero, Quaternion.identity);
            ret.MakePersistent();
            DontDestroyOnLoad(ret);
        }
        else
        {
            Destroy(ret);
            ret = null;
        }
        _ = SceneManager.UnloadScene(additiveScene);
        return ret;
    }

    private static GameObject[] GrabGameObjectDependencies(GameObject go)
    {
        var ret = new List<GameObject>();

        foreach (var component in go.GetComponents<MonoBehaviour>())
        {
            foreach (var publicProps in component.GetType().GetProperties(BindingFlags.Public))
            {
                switch (publicProps.CanRead)
                {
                    case true when publicProps.PropertyType == typeof(GameObject):
                        {
                            var val = (GameObject?)publicProps.GetValue(component);
                            if (val == null) continue;
                            ret.Add(val);
                            break;
                        }
                    case true
                        when publicProps.PropertyType.IsAssignableTo(typeof(Il2CppSystem.Collections.Generic.IEnumerable<GameObject>)):
                        {
                            var val = (Il2CppSystem.Collections.Generic.IEnumerable<GameObject>?)publicProps.GetValue(component);
                            if (val == null) continue;
                            ret.AddRange(val.ToArray());
                            break;
                        }
                }
            }
        }

        return [.. ret];
    }

    private void Start()
    {
        if (grabOnStart.Get())
        {
            var currentObjHandle = GrabHandle(AddressableSceneKey, GrabbedGameObjectPath);
            if (currentObjHandle == null) return;
            currentObjHandle.transform.SetParent(transform);
        }
    }
}