using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CementGB.Mod.Modules.PoolingModule;

/// <summary>
/// Allows users to register poolable prefabs, instantiate them, and pool instances of those prefabs.
/// This is useful for when lots of objects need to be spawned in, because objects are reused, and don't need to be destroyed or created.
/// </summary>
[RegisterTypeInIl2Cpp]
public class Pool : MonoBehaviour
{
    /// <summary>
    /// Dictionary that gets a prefab from a given id
    /// </summary>
    private static readonly Dictionary<int, GameObject> idToObject = new();

    /// <summary>
    /// Dictionary that gets a id from a given prefab
    /// </summary>
    private static readonly Dictionary<GameObject, int> objectToId = new();

    private static readonly List<GameObject> spawnedObjects = new();
    private static readonly List<GameObject> pooledObjects = new();

    /// <summary>
    /// Dictionary that corresponds ids to actions
    /// </summary>
    private static readonly Dictionary<int, Action<GameObject>> resetActions = new();

    private void Awake()
    {
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)SceneChanged);
    }

    private void SceneChanged(Scene _, LoadSceneMode __)
    {
        spawnedObjects.Clear();
        pooledObjects.Clear();
    }

    /// <summary>
    /// Registers a prefab and reset action which allows users to instantiate objects with the pooling system.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="reset"></param>
    private static void BaseRegisterPrefab(GameObject prefab, Action<GameObject> reset)
    {
        // Checks if the prefab has already been registered
        if (objectToId.ContainsKey(prefab))
        {
            Melon<Mod>.Logger.Error("You are trying to register a prefab, that has already been registered.");
            return;
        }

        var nextId = idToObject.Count;

        // Asigns an action to an id
        resetActions[nextId] = reset;

        // Sets the dictionary values
        idToObject[nextId] = prefab;
        objectToId[prefab] = nextId;
    }

    /// <summary>
    /// Overload for RegisterPrefab
    /// </summary>
    /// <param name="prefab"></param>
    public static void RegisterPrefab(GameObject prefab)
    {
        BaseRegisterPrefab(prefab, null);
    }

    /// <summary>
    /// Overload for RegisterPrefab
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="reset"></param>
    public static void RegisterPrefab(GameObject prefab, Action<GameObject> reset)
    {
        BaseRegisterPrefab(prefab, reset);
    }

    /// <summary>
    /// Pools an object so that it can be instantiated later
    /// </summary>
    /// <param name="gameObject"></param>
    public static void PoolObject(GameObject gameObject)
    {
        // Checks if the object was spawned with the pooling system
        if (!spawnedObjects.Contains(gameObject))
        {
            Melon<Mod>.Logger.Error("You can only pool objects spawned using the pooling system.");
            return;
        }

        // Removes it so that it can't be pooled again
        spawnedObjects.Remove(gameObject);

        // Pools the object and sets it to inactive
        gameObject.SetActive(false);
        pooledObjects.Add(gameObject);
    }

    /// <summary>
    /// Instantiates a new object or finds another object with the same id. Base for all instantiate overloads.
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private static GameObject BaseInstantiate(GameObject prefab)
    {
        // Checks if the prefab has been registered
        if (!objectToId.ContainsKey(prefab))
        {
            Melon<Mod>.Logger.Error("You need to register a prefab before you can spawn it.");
            return null;
        }

        var id = objectToId[prefab];

        // Finds a pooled object of the same prefab
        var @object = GetPooledObject(id);
        if (@object == null)
        {
            @object = GameObject.Instantiate(prefab);
            @object.AddComponent<Poolable>().SetId(id);
        }
        else
        {
            resetActions[id]?.Invoke(@object);

            // In case the custom reset action destroys the object
            if (@object == null)
            {
                @object = GameObject.Instantiate(prefab);
                @object.AddComponent<Poolable>().SetId(id);
            }

            pooledObjects.Remove(@object);
        }

        @object.name = prefab.name;

        // Adds the object to spawned objects list, so that it can be pooled
        spawnedObjects.Add(@object);
        @object.SetActive(true);

        return @object;
    }


    /// <summary>
    /// Overload for instantiate
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public static GameObject Instantiate(GameObject prefab)
    {
        var @object = BaseInstantiate(prefab);
        if (@object == null) return null;

        // Resets object
        @object.transform.position = Vector3.zero;
        @object.transform.rotation = Quaternion.identity;
        @object.transform.SetParent(null);

        return @object;
    }

    /// <summary>
    /// Overload for instantiate
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static GameObject Instantiate(GameObject prefab, Transform parent)
    {
        var @object = BaseInstantiate(prefab);
        if (@object == null) return null;

        // Resets and sets values of object
        @object.transform.position = Vector3.zero;
        @object.transform.rotation = Quaternion.identity;
        @object.transform.SetParent(parent);

        return @object;
    }


    /// <summary>
    /// Overload for instantiate
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static GameObject Instantiate(GameObject prefab, Vector3 position)
    {
        var @object = BaseInstantiate(prefab);
        if (@object == null) return null;

        // Resets and sets values of object
        @object.transform.position = position;
        @object.transform.rotation = Quaternion.identity;
        @object.transform.SetParent(null);

        return @object;
    }


    /// <summary>
    /// Overload for instantiate
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var @object = BaseInstantiate(prefab);
        if (@object == null) return null;

        // Resets and sets values of object
        @object.transform.position = position;
        @object.transform.rotation = rotation;
        @object.transform.SetParent(null);

        return @object;
    }


    /// <summary>
    /// Gets a pooled object from the pooled objects list, when given an id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private static GameObject GetPooledObject(int id)
    {
        foreach (var pooledObject in pooledObjects)
        {
            // Checks if id matches
            if (Poolable.GetId(pooledObject) == id)
            {
                return pooledObject;
            }
        }

        return null;
    }

    // Used to store data about pooled objects. Private to prevent users from instantiating fake poolable objects
    [RegisterTypeInIl2Cpp]
    private class Poolable : MonoBehaviour
    {
        // Stores the id so that the pooling system knows which prefab it is an instance of
        private int id;

        // Used to make sure the id only gets set once
        private bool setId = false;

        // Sets the id
        public void SetId(int id)
        {
            // Makes sure the id only gets set once
            if (setId) return;

            this.id = id;
            setId = true;
        }

        // Gets the id of an object
        public static int GetId(GameObject gameObject)
        {
            var poolable = gameObject.GetComponent<Poolable>();
            if (poolable != null)
            {
                return poolable.id;
            }

            // If the object has no poolable component, it wasn't spawned by the Poolable system
            return -1;
        }
    }
}
