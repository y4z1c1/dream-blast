using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    private Dictionary<string, ParticlePool> particlePools = new Dictionary<string, ParticlePool>();

    [System.Serializable]
    public class ParticleSystemInfo
    {
        public string poolName;
        public ParticleSystem prefab;
        public int initialPoolSize = 5;
    }

    [Header("Particle System Prefabs")]
    [SerializeField] private List<ParticleSystemInfo> particleSystems = new List<ParticleSystemInfo>();

    [Header("Settings")]
    [SerializeField] private Transform poolParent;

    [Header("Debug")]
    [SerializeField] private bool particlesEnabled = true;
    [SerializeField] private bool logParticleUsage = false;
    [SerializeField] private bool monitorPoolSizes = true;

    // store max usage for reporting
    private Dictionary<string, int> maxPoolUsage = new Dictionary<string, int>();

    private void Awake()
    {
        // create parent for pooled objects if not assigned
        if (poolParent == null)
        {
            GameObject poolObj = new GameObject("ParticlePools");
            poolObj.transform.SetParent(transform);
            poolParent = poolObj.transform;
        }

        // initialize all particle pools
        InitializePools();
    }

    /// initialize all particle pools based on configuration
    private void InitializePools()
    {
        foreach (ParticleSystemInfo info in particleSystems)
        {
            if (info.prefab == null)
            {
                Debug.LogWarning($"Null particle prefab for pool: {info.poolName}");
                continue;
            }

            // create new pool by adding component
            ParticlePool pool = poolParent.gameObject.AddComponent<ParticlePool>();
            pool.Initialize(info.prefab, info.initialPoolSize, poolParent, logParticleUsage);
            particlePools[info.poolName] = pool;

            // initialize tracking
            maxPoolUsage[info.poolName] = 0;

            Debug.Log($"Created particle pool: {info.poolName} with {info.initialPoolSize} instances");
        }
    }

    /// play a particle effect at the specified position
    public ParticleSystem PlayEffect(string effectName, Vector3 position, float duration = 1.0f, Transform parent = null)
    {
        // skip if particles are disabled
        if (!particlesEnabled)
        {
            if (logParticleUsage)
                Debug.Log($"[PARTICLE] Skipped effect {effectName} (particles disabled)");
            return null;
        }

        if (logParticleUsage)
            Debug.Log($"[PARTICLE] Playing effect {effectName} at {position}");

        if (!particlePools.ContainsKey(effectName))
        {
            Debug.LogWarning($"Particle effect not found: {effectName}");
            Debug.Log($"Available effects: {string.Join(", ", particlePools.Keys)}");
            return null;
        }

        // get particle from pool
        ParticleSystem particleInstance = particlePools[effectName].GetParticle();

        if (particleInstance == null)
        {
            Debug.LogWarning($"Failed to get particle from pool: {effectName}");
            // auto-resize the pool by doubling its size
            int currentSize = particlePools[effectName].GetTotalCount();
            int newSize = currentSize * 2;
            Debug.Log($"[POOL] Auto-expanding pool '{effectName}' from {currentSize} to {newSize} particles");

            // expand the pool
            particlePools[effectName].ExpandPool(newSize - currentSize);

            // try to get a particle again
            particleInstance = particlePools[effectName].GetParticle();

            if (particleInstance == null)
            {
                Debug.LogError($"Failed to get particle after expanding pool: {effectName}");
                return null;
            }
        }

        // update max usage tracking
        if (monitorPoolSizes)
        {
            int currentUsage = particlePools[effectName].GetActiveCount();
            if (currentUsage > maxPoolUsage[effectName])
            {
                maxPoolUsage[effectName] = currentUsage;
                Debug.Log($"[POOL] New maximum usage for '{effectName}': {currentUsage} particles");
            }
        }

        // set position
        particleInstance.transform.position = position;

        // ensure the scale is correct before parenting to avoid scale inheritance issues
        particleInstance.transform.localScale = Vector3.one;

        // if parent is specified, make this a child of that parent
        if (parent != null)
        {
            particleInstance.transform.SetParent(parent);

            // log warning if parent has zero scale
            if (parent.localScale == Vector3.zero)
            {
                Debug.LogWarning($"[PARTICLE] Parent of {effectName} particle has zero scale! This may cause particles to be invisible.");
            }

            // force the local scale to stay at 1 even after parenting
            particleInstance.transform.localScale = Vector3.one;

            if (logParticleUsage)
                Debug.Log($"[PARTICLE] Parented to {parent.name} with scale {parent.localScale}");
        }

        // play the effect
        particleInstance.Play();

        if (logParticleUsage)
            Debug.Log($"[PARTICLE] Started playback for {effectName}");

        // return to pool after duration
        StartCoroutine(ReturnToPoolAfterDuration(particleInstance, effectName, duration));

        return particleInstance;
    }

    /// return particle to pool after specified duration
    private IEnumerator ReturnToPoolAfterDuration(ParticleSystem particle, string poolName, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (particle != null)
        {
            // stop emission
            var emission = particle.emission;
            emission.enabled = false;

            // wait for existing particles to die
            yield return new WaitForSeconds(particle.main.startLifetime.constantMax);

            // reset emission
            emission.enabled = true;

            // return to pool
            particlePools[poolName].ReturnParticle(particle);

            if (logParticleUsage)
                Debug.Log($"[PARTICLE] Returned {poolName} particle to pool");
        }
    }

    /// stop and return all active particles to their pools
    public void StopAllParticles()
    {
        foreach (var pool in particlePools.Values)
        {
            pool.ReturnAllParticles();
        }

        if (logParticleUsage)
            Debug.Log("[PARTICLE] All particles stopped and returned to pools");
    }

    /// toggle particles on/off
    public void SetParticlesEnabled(bool enabled)
    {
        if (particlesEnabled != enabled)
        {
            particlesEnabled = enabled;
            Debug.Log($"[PARTICLE] Particles {(enabled ? "enabled" : "disabled")}");

            // if disabling, return all active particles to pools
            if (!enabled)
            {
                StopAllParticles();
            }
        }
    }

    /// toggle particle logging
    public void SetParticleLogging(bool enabled)
    {
        logParticleUsage = enabled;
        Debug.Log($"[PARTICLE] Particle logging {(enabled ? "enabled" : "disabled")}");

        // update logging in pool objects
        foreach (var pool in particlePools.Values)
        {
            pool.SetLogging(enabled);
        }
    }

    /// print pool usage statistics
    [ContextMenu("Print Pool Statistics")]
    public void PrintPoolStatistics()
    {
        Debug.Log("===== PARTICLE POOL STATISTICS =====");
        foreach (var poolName in particlePools.Keys)
        {
            ParticlePool pool = particlePools[poolName];
            int activeCount = pool.GetActiveCount();
            int inactiveCount = pool.GetInactiveCount();
            int maxUsage = maxPoolUsage[poolName];

            Debug.Log($"Pool '{poolName}': Active={activeCount}, Inactive={inactiveCount}, Max Usage={maxUsage}");
        }
        Debug.Log("===================================");
    }

    /// reset max usage statistics
    [ContextMenu("Reset Usage Statistics")]
    public void ResetUsageStatistics()
    {
        foreach (var poolName in maxPoolUsage.Keys)
        {
            maxPoolUsage[poolName] = 0;
        }
        Debug.Log("[PARTICLE] Usage statistics reset");
    }
}