using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a boss-style health bar UI. Attach this to the "Boss Health Bar" GameObject.
///
/// Setup expected (all Image components, Type = Filled, Fill Method = Horizontal, Fill Origin = Left):
///   Boss Health Bar (this script + CanvasGroup, added automatically)
///     ├─ Chip Bar   (Image, white)   <-- must be EARLIER sibling (renders behind)
///     ├─ Red Bar    (Image, red)     <-- must be LATER sibling (renders in front)
///     └─ (optional) Blood Drip (ParticleSystem), pivot/anchors matching the bars so
///                    localPosition.x lines up with the bar's fill width.
///
/// How the flash works: Red Bar always snaps instantly to the true health %, since it's
/// drawn on top. Chip Bar sits behind at the *previous* health % — because Red Bar covers
/// everything from 0 up to the new %, the only part of Chip Bar peeking out is exactly the
/// sliver that was just lost. Shrinking Chip Bar's fillAmount down to the new % makes that
/// white sliver recede to nothing. No masking or manual position math required.
///
/// Wiring:
///   - Subscribes to Enemy.OnHealthChanged / OnDeath and EnemyAI.OnAggro / OnDeaggro.
///   - The GameObject itself is expected to stay active at all times; visibility is
///     handled purely via CanvasGroup.alpha so event subscriptions never get dropped.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BossHealthBar : MonoBehaviour
{
    [Header("Targets")]
    public Enemy enemy;
    public EnemyAI enemyAI;

    [Header("Bars (both Filled / Horizontal / Origin Left)")]
    [Tooltip("Renders in FRONT. Always shows the true current health instantly.")]
    public Image redBar;
    [Tooltip("Renders BEHIND redBar. Holds the previous health value and shrinks down to reveal the hit.")]
    public Image chipBar;

    [Header("Appear / Disappear")]
    public float fadeDuration = 0.35f;
    [Tooltip("If true, the bar also fades out when the enemy loses aggro and returns to Idle.")]
    public bool hideOnDeaggro = true;

    [Header("Hit Flash")]
    public Color chipColor = Color.white;
    [Tooltip("How long the exposed white sliver holds still before it recedes.")]
    public float flashHoldDuration = 0.12f;
    [Tooltip("How long it takes the white sliver to shrink away.")]
    public float dropDuration = 0.35f;

    [Header("Low Health Blood Drip")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.25f;
    [Tooltip("Particle system that clings to the current end of the bar once health is low. Leave empty to skip this feature.")]
    public ParticleSystem bloodDripParticles;

    private CanvasGroup canvasGroup;
    private float currentPercent = 1f;
    private bool bloodDripActive;
    private Coroutine chipRoutine;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (chipBar != null)
            chipBar.color = chipColor;

        if (bloodDripParticles != null)
        {
            bloodDripParticles.Stop();
            bloodDripParticles.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Sync to whatever the enemy's actual health is right now (handles enemies
        // that don't start at 100%, or a bar that gets assigned after spawn).
        SyncToCurrentHealth();
    }

    private void OnEnable()
    {
        Subscribe(enemy, enemyAI);
    }

    private void OnDisable()
    {
        Unsubscribe(enemy, enemyAI);
    }

    /// <summary>
    /// Re-point this bar at a different enemy at runtime (e.g. one shared bar reused
    /// across sequential boss encounters instead of a bar-per-enemy).
    /// </summary>
    public void SetTarget(Enemy newEnemy, EnemyAI newEnemyAI)
    {
        Unsubscribe(enemy, enemyAI);
        enemy = newEnemy;
        enemyAI = newEnemyAI;
        Subscribe(enemy, enemyAI);
        SyncToCurrentHealth();
    }

    private void Subscribe(Enemy e, EnemyAI ai)
    {
        if (e != null)
        {
            e.OnHealthChanged += HandleHealthChanged;
            e.OnDeath += HandleDeath;
        }
        if (ai != null)
        {
            ai.OnAggro += HandleAggro;
            if (hideOnDeaggro) ai.OnDeaggro += HandleDeaggro;
        }
    }

    private void Unsubscribe(Enemy e, EnemyAI ai)
    {
        if (e != null)
        {
            e.OnHealthChanged -= HandleHealthChanged;
            e.OnDeath -= HandleDeath;
        }
        if (ai != null)
        {
            ai.OnAggro -= HandleAggro;
            ai.OnDeaggro -= HandleDeaggro;
        }
    }

    private void SyncToCurrentHealth()
    {
        if (enemy == null || redBar == null || chipBar == null) return;

        currentPercent = enemy.HealthPercent > 0f ? enemy.HealthPercent : 1f;
        redBar.fillAmount = currentPercent;
        chipBar.fillAmount = currentPercent; // no exposed sliver when idle
        CheckBloodDrip(currentPercent);
    }

    // ----------------------------
    // Event handlers
    // ----------------------------

    private void HandleAggro()
    {
        SetVisible(true);
    }

    private void HandleDeaggro()
    {
        SetVisible(false);
    }

    private void HandleDeath()
    {
        if (bloodDripParticles != null)
        {
            bloodDripParticles.Stop();
            bloodDripActive = false;
        }
        SetVisible(false);
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        float newPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        float oldPercent = currentPercent;
        currentPercent = newPercent;

        // Red Bar always shows the truth immediately.
        redBar.fillAmount = newPercent;
        CheckBloodDrip(newPercent);

        // Chip Bar snaps up to expose the pre-hit amount, then recedes to match.
        chipBar.fillAmount = oldPercent;
        if (chipRoutine != null) StopCoroutine(chipRoutine);
        chipRoutine = StartCoroutine(ShrinkChip(newPercent));
    }

    // ----------------------------
    // Visuals
    // ----------------------------

    private IEnumerator ShrinkChip(float targetPercent)
    {
        float fromPercent = chipBar.fillAmount;
        yield return new WaitForSeconds(flashHoldDuration);

        float t = 0f;
        while (t < dropDuration)
        {
            t += Time.deltaTime;
            chipBar.fillAmount = Mathf.Lerp(fromPercent, targetPercent, Mathf.Clamp01(t / dropDuration));
            yield return null;
        }
        chipBar.fillAmount = targetPercent;
        chipRoutine = null;
    }

    private void CheckBloodDrip(float percent)
    {
        if (bloodDripParticles == null) return;

        bool shouldBeActive = percent > 0f && percent <= lowHealthThreshold;

        if (shouldBeActive && !bloodDripActive)
        {
            bloodDripActive = true;
            bloodDripParticles.gameObject.SetActive(true);
            bloodDripParticles.Play();
        }
        else if (!shouldBeActive && bloodDripActive)
        {
            bloodDripActive = false;
            bloodDripParticles.Stop();
            bloodDripParticles.gameObject.SetActive(false);
        }

        if (shouldBeActive)
            UpdateBloodDripPosition(percent);
    }

    private void UpdateBloodDripPosition(float percent)
    {
        // Assumes bloodDripParticles shares the bars' left-pivoted RectTransform space,
        // so local x = percent * full bar width lands exactly on the current bar edge.
        float width = redBar.rectTransform.rect.width;
        Vector3 pos = bloodDripParticles.transform.localPosition;
        pos.x = percent * width;
        bloodDripParticles.transform.localPosition = pos;
    }

    private void SetVisible(bool visible)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(visible ? 1f : 0f));
    }

    private IEnumerator FadeRoutine(float target)
    {
        bool visible = target == 1f;
        if(!visible)
        {
            yield return new WaitForSeconds(2f); // small delay to avoid flicker when dying
        }
        gameObject.GetComponent<SpriteRenderer>().enabled = visible;
        foreach (Transform child in transform)
        {
            var spriteRenderer = child.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;

            var canvas = child.GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = visible;

            var textMeshPro = child.GetComponent<TMPro.TMP_Text>();
            if (textMeshPro != null)
                textMeshPro.enabled = visible;
        }
        canvasGroup.interactable = target > 0.5f;
        canvasGroup.blocksRaycasts = target > 0.5f;

        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
        fadeRoutine = null;
    }
}