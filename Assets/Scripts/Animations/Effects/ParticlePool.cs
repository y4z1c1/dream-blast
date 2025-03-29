using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// particle pool is a pool of particle systems that can be used to create particle effects.
public class ParticlePool : MonoBehaviour
{
    private ParticleSystem prefab;
    private Queue<ParticleSystem> inactiveParticles = new Queue<ParticleSystem>();
    private List<ParticleSystem> activeParticles = new List<ParticleSystem>();
    private Transform poolParent;
    private string poolName;
    private bool debugMode = false;

    // initialize the pool
    public void Initialize(ParticleSystem prefab, int initialSize, Transform parent, bool enableDebugMode = false)
    {
        this.prefab = prefab;
        this.poolParent = parent;
        this.poolName = prefab != null ? prefab.name : "Unknown";
        this.debugMode = enableDebugMode;

        // create initial particles
        for (int i = 0; i < initialSize; i++)
        {
            CreateParticle();
        }
    }

    // create a new particle instance and add to inactive queue
    private void CreateParticle()
    {
        if (prefab == null)
            return;

        // instantiate new particle
        ParticleSystem newParticle = Object.Instantiate(prefab, poolParent);

        // stop it and disable game object
        newParticle.Stop();
        newParticle.gameObject.SetActive(false);

        // add to inactive queue
        inactiveParticles.Enqueue(newParticle);

        if (debugMode)
            Debug.Log($"[POOL] Created new particle for {poolName} pool");
    }

    // get a particle from the pool
    public ParticleSystem GetParticle()
    {
        // if no inactive particles, create a new one
        if (inactiveParticles.Count == 0)
        {
            if (debugMode)
                Debug.LogWarning($"[POOL] Pool limit reached for '{poolName}' - expanding beyond initial size. Current size: {activeParticles.Count}");

            CreateParticle();
        }

        // get particle from queue
        ParticleSystem particle = inactiveParticles.Dequeue();

        // activate it
        particle.gameObject.SetActive(true);

        // add to active list
        activeParticles.Add(particle);

        return particle;
    }

    // return a particle to the pool
    public void ReturnParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        // stop particle
        particle.Stop();

        // reset transform
        particle.transform.SetParent(poolParent);

        // deactivate
        particle.gameObject.SetActive(false);

        // remove from active list and add to inactive queue
        if (activeParticles.Contains(particle))
        {
            activeParticles.Remove(particle);
            inactiveParticles.Enqueue(particle);
        }
    }

    // return all active particles to the pool
    public void ReturnAllParticles()
    {
        // create a copy of the list to avoid modification during iteration
        List<ParticleSystem> particlesToReturn = new List<ParticleSystem>(activeParticles);

        foreach (ParticleSystem particle in particlesToReturn)
        {
            ReturnParticle(particle);
        }
    }

    // get the number of active particles
    public int GetActiveCount()
    {
        return activeParticles.Count;
    }

    // get the number of inactive particles
    public int GetInactiveCount()
    {
        return inactiveParticles.Count;
    }

    // set logging state
    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
    }

    // get the total count of particles in the pool (active + inactive)
    public int GetTotalCount()
    {
        return activeParticles.Count + inactiveParticles.Count;
    }

    // expand the pool by creating additionalParticles
    public void ExpandPool(int additionalParticles)
    {
        if (additionalParticles <= 0) return;

        if (debugMode)
            Debug.Log($"[POOL] Expanding {poolName} pool by {additionalParticles} particles");

        for (int i = 0; i < additionalParticles; i++)
        {
            CreateParticle();
        }
    }
}