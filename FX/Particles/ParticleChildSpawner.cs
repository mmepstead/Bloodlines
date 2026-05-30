using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleChildSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Prefab to instantiate at each particle's spawn position.")]
    public GameObject childPrefab;

    [Tooltip("Destroy the spawned child when its particle dies.")]
    public bool destroyWithParticle = true;

    private ParticleSystem ps;
    private readonly Dictionary<uint, GameObject> spawnedMap = new();

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void LateUpdate()
    {
        if (childPrefab == null || ps == null) return;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
        int count = ps.GetParticles(particles);

        HashSet<uint> liveIds = new();
        for (int i = 0; i < count; i++)
        {
            uint id = particles[i].randomSeed;
            liveIds.Add(id);

            if (!spawnedMap.ContainsKey(id))
            {
                GameObject child = Instantiate(childPrefab, Vector3.zero, Quaternion.identity, transform);
                child.transform.localScale = new Vector3(0.05f,0.05f,1f);
                spawnedMap[id] = child;
            }

            Vector3 worldPos = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local
                ? transform.TransformPoint(particles[i].position)
                : particles[i].position;

            spawnedMap[id].transform.position = worldPos;
        }

        if (destroyWithParticle)
        {
            List<uint> deadIds = new();
            foreach (uint id in spawnedMap.Keys)
            {
                if (!liveIds.Contains(id))
                    deadIds.Add(id);
            }

            foreach (uint id in deadIds)
            {
                if (spawnedMap[id] != null)
                    Destroy(spawnedMap[id]);
                spawnedMap.Remove(id);
            }
        }
    }

    void OnDestroy()
    {
        foreach (var kvp in spawnedMap)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        spawnedMap.Clear();
    }
}