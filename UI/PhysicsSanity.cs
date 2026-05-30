using UnityEngine;

public class PhysicsSanity : MonoBehaviour
{
    [Header("Optional test caps")]
    public bool force60FPS = true;
    public bool disableVSync = true;

    float _accum;
    int _fixedCount;

    void Awake()
    {
        if (disableVSync) QualitySettings.vSyncCount = 0;
        if (force60FPS) Application.targetFrameRate = 60;

        Debug.Log($"[PhysicsSanity] Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        Debug.Log($"[PhysicsSanity] Time.fixedDeltaTime: {Time.fixedDeltaTime}");
        Debug.Log($"[PhysicsSanity] Time.maximumDeltaTime: {Time.maximumDeltaTime}");
        Debug.Log($"[PhysicsSanity] Physics.defaultSolverIterations: {Physics.defaultSolverIterations}, " +
                  $"VelocityIterations: {Physics.defaultSolverVelocityIterations}");
        Debug.Log($"[PhysicsSanity] Auto Simulation: {Physics.autoSimulation}, Auto Sync: {Physics.autoSyncTransforms}");
    }

    void FixedUpdate() { _fixedCount++; }

    void Update()
    {
        _accum += Time.unscaledDeltaTime;
        if (_accum >= 1f)
        {
            var fps = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
            Debug.Log($"[PhysicsSanity] FPS ~{fps:F1}, FixedUpdates last second: {_fixedCount}");
            _accum = 0f;
            _fixedCount = 0;
        }
    }
}