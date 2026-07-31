using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool: instead of Instantiate/Destroy on every shot or damage popup (which
/// creates garbage-collection pressure and is a common cause of FPS drops when many towers
/// fire quickly or many enemies are on screen), reuses a pool of already-created instances.
///
/// Usage: replace `Instantiate(prefab, pos, rot)` with `ObjectPool.Instance.Get(prefab, pos, rot)`,
/// and replace `Destroy(gameObject)` with `ObjectPool.Instance.Release(gameObject)`.
/// Put this on a single empty "ObjectPool" GameObject in the scene - one per scene.
/// Everything works fine (just without the performance benefit) if this object is missing,
/// since Projectile/DamagePopup/Tower/Enemy all fall back to normal Instantiate/Destroy
/// when ObjectPool.Instance is null.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

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

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true); // re-fires OnEnable, which components use to reset their per-use state
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
            instanceToPrefab[obj] = prefab; // remember which pool this instance belongs to, for when it's released
        }
        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        if (!instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            Destroy(obj); // wasn't something Get() created (shouldn't normally happen) - just destroy it
            return;
        }

        obj.SetActive(false);
        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[prefab] = pool;
        }
        pool.Enqueue(obj);
    }
}