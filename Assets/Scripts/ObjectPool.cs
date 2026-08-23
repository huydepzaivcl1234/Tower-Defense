using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool used by enemies, projectiles and popups.
/// Reuses objects to reduce Instantiate/Destroy GC spikes while also capping inactive objects
/// so long sessions cannot grow the pool (and RAM usage) forever.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Memory Limits")]
    [Tooltip("Maximum inactive instances retained per prefab. Extra released instances are destroyed instead of being kept forever.")]
    [Min(1)] public int maxInactivePerPrefab = 96;

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[prefab] = pool;
        }

        GameObject obj = null;

        // A pooled object might have been destroyed externally. Skip stale queue entries safely.
        while (pool.Count > 0 && obj == null)
            obj = pool.Dequeue();

        if (obj != null)
        {
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
            instanceToPrefab[obj] = prefab;
        }

        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        if (!instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            Destroy(obj);
            return;
        }

        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[prefab] = pool;
        }

        int limit = Mathf.Max(1, maxInactivePerPrefab);
        if (pool.Count >= limit)
        {
            instanceToPrefab.Remove(obj);
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        pools.Clear();
        instanceToPrefab.Clear();
    }
}
