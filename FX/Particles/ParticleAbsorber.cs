using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAbsorber : MonoBehaviour
{
    public enum TriggerMode { Timer, Count, Manual }

    [Header("References")]
    public Transform player;
    public ParticleSystem absorptionSystemPrefab;

    [Header("Trigger")]
    public TriggerMode triggerMode = TriggerMode.Timer;

    [Tooltip("(Timer) Seconds after emission stops before absorb triggers.")]
    [Min(0f)]
    public float activationDelay = 0.5f;

    [Tooltip("(Count) Activates when live particle count falls to this value.")]
    [Min(0)]
    public int activationThreshold = 5;

    [Header("Spawn")]
    [Range(1, 6)]
    public int spawnCount = 3;

    [Header("Homing")]
    [Min(0.1f)]
    public float homingSpeed = 6f;

    [Range(0f, 3f)]
    public float proximityAcceleration = 1f;

    [Header("Burst / Disappear")]
    [Min(0.05f)]
    public float burstRadius = 0.8f;
    public float burstUpSpeed = 2.5f;

    [Min(0.1f)]
    public float shrinkSpeed = 3f;

    // ── private state ──────────────────────────────────────

    private ParticleSystem _source;
    private ParticleSystem _absorptionSystem;
    private bool _triggered;

    private bool  _emissionStopped;
    private float _emissionStoppedTime;

    private readonly List<AbsorbedParticle> _absorbed = new List<AbsorbedParticle>(64);
    private ParticleSystem.Particle[] _particleBuffer = new ParticleSystem.Particle[512];

    // ── lifecycle ──────────────────────────────────────────

    private void Awake()
    {
        player = player ? player : GameObject.FindGameObjectWithTag("Player").transform;
        _source = GetComponent<ParticleSystem>();
        var main = _source.main;
        main.stopAction = ParticleSystemStopAction.None;

        if (main.simulationSpace != ParticleSystemSimulationSpace.World)
            Debug.LogWarning("[ParticleAbsorber] Source Particle System should use World simulation space.");

        // Create the single shared absorption system.
        _absorptionSystem = Instantiate(absorptionSystemPrefab, Vector3.zero, Quaternion.identity);
        var absMain = _absorptionSystem.main;
        absMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = _absorptionSystem.emission;
        emission.enabled = false;
        _absorptionSystem.Play();
    }

    private void Update()
    {
        if (!_triggered) { CheckTrigger(); return; }
        StepAbsorbedParticles();
    }

    // ── trigger ────────────────────────────────────────────

    private void CheckTrigger()
    {
        switch (triggerMode)
        {
            case TriggerMode.Timer:
                if (!_emissionStopped)
                {
                    if (_source.isPlaying)
                    {
                        _emissionStopped = true;
                        _emissionStoppedTime = Time.time;
                    }
                    return; // don't fall through until emission has stopped
                }
                if (Time.time >= _emissionStoppedTime + activationDelay)
                    Activate();
                break;

            case TriggerMode.Count:
                if (_source.particleCount <= activationThreshold)
                    Activate();
                break;
        }
    }

    // ── activation ─────────────────────────────────────────

    private void Activate()
    {
        CaptureAndSpawn();
        _triggered = true;
        _source.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void CaptureAndSpawn()
    {
        if (_source.particleCount > _particleBuffer.Length)
            _particleBuffer = new ParticleSystem.Particle[_source.particleCount * 2];

        int count = _source.GetParticles(_particleBuffer);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos  = _particleBuffer[i].position;
            Vector3 vel  = _particleBuffer[i].velocity;
            Color32 col  = Color.yellow; // we could also capture startColor, but it might be expensive if using a gradient
            float   size = _particleBuffer[i].startSize;

            for (int s = 0; s < spawnCount; s++)
            {
                Vector3 spread = Random.insideUnitSphere * (size * 0.25f);

                // Emit one particle into the shared system at this position.
                var ep = new ParticleSystem.EmitParams
                {
                    position   = pos + spread,
                    velocity   = vel + spread * 0.5f,
                    startSize  = size * Random.Range(0.3f, 0.7f),
                    startColor = col,
                    startLifetime = 9999f   // we control lifetime manually via size
                };
                _absorptionSystem.Emit(ep, 1);

                _absorbed.Add(new AbsorbedParticle
                {
                    position    = pos + spread,
                    velocity    = vel + spread * 0.5f,
                    color       = col,
                    currentSize = size * Random.Range(0.3f, 0.7f),
                    isBursting  = false
                });
            }
        }
    }

    // ── simulation ─────────────────────────────────────────

    private void StepAbsorbedParticles()
    {
        if (player == null) return;

        Vector3 playerPos = player.position;
        float dt = Time.deltaTime;

        // We'll write updated positions back into the live PS particles.
        int liveCount = _absorptionSystem.particleCount;
        if (liveCount == 0) return;

        if (_particleBuffer.Length < liveCount)
            _particleBuffer = new ParticleSystem.Particle[liveCount * 2];

        _absorptionSystem.GetParticles(_particleBuffer, liveCount);

        for (int i = _absorbed.Count - 1; i >= 0; i--)
        {
            AbsorbedParticle ap = _absorbed[i];

            if (!ap.isBursting)
            {
                Vector3 toPlayer = playerPos - ap.position;
                float dist = toPlayer.magnitude;

                float speedMod = 1f + proximityAcceleration * (1f / Mathf.Max(dist, 0.1f));
                Vector3 homingDir = dist > 0.001f ? toPlayer / dist : Vector3.zero;
                ap.velocity = Vector3.Lerp(ap.velocity, homingDir * homingSpeed * speedMod, dt * 4f);
                ap.position += ap.velocity * dt;

                if (dist <= burstRadius)
                {
                    ap.isBursting = true;
                    ap.velocity = Vector3.up * burstUpSpeed
                                  + Random.insideUnitSphere * (burstUpSpeed * 0.3f);
                }
            }
            else
            {
                ap.velocity += Physics.gravity * dt * 0.1f;
                ap.position += ap.velocity * dt;
                ap.currentSize = Mathf.MoveTowards(ap.currentSize, 0f, shrinkSpeed * dt);

                if (ap.currentSize <= 0.001f)
                {
                    // Kill the corresponding PS particle by zeroing its lifetime.
                    if (i < liveCount)
                        _particleBuffer[i].remainingLifetime = 0f;

                    _absorbed.RemoveAt(i);
                    continue;
                }
            }

            if (i < liveCount)
            {
                _particleBuffer[i].position        = ap.position;
                _particleBuffer[i].startSize       = ap.currentSize;
                _particleBuffer[i].startColor      = ap.color;
                _particleBuffer[i].remainingLifetime = 9999f;
            }

            _absorbed[i] = ap;
        }

        _absorptionSystem.SetParticles(_particleBuffer, liveCount);

        if (_absorbed.Count == 0)
            Destroy(gameObject);
    }

    // ── data ───────────────────────────────────────────────

    private struct AbsorbedParticle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Color32 color;
        public float currentSize;
        public bool isBursting;
    }

    // ── public API ─────────────────────────────────────────

    public void ForceActivate()
    {
        if (!_triggered) Activate();
    }

    public bool IsComplete => _triggered && _absorbed.Count == 0;
}