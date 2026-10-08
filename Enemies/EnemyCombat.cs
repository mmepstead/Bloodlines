using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemyCombat : MonoBehaviour
{
    public Animator animator;
    public AudioSource audioSource;
    public List<WeightedCombination> possibleCombinations;
    public EnemyAttack currentAttack;
    private Coroutine currentComboCoroutine;
    private bool isInterrupted;

    [Header("Quick Step Settings")]
    public bool canQuickStep = false;
    public float maxQuickStepRange = 3f;
    public float quickStepVerticalMovement = 0.3f;
    public static float quickStepBuffer = 0.5f; // Static so accessible from any enemy
    private GameObject attackHitbox;

    [Header("Variety Settings")]
    // How long (in seconds) a combo's weight stays suppressed after being used.
    // Weight recovers linearly from varietyMinWeightMultiplier back to full weight over this window.
    public float varietyRecencyWindow = 15f;
    // How much a combo's weight is multiplied by the instant it's used (0 = fully excluded until it recovers, 1 = no penalty at all).
    [Range(0f, 1f)]
    public float varietyMinWeightMultiplier = 0.1f;
    // Tracks the last Time.time each combination was executed, used to penalize recently-used moves.
    private Dictionary<Combination, float> lastUsedTime = new Dictionary<Combination, float>();
    // Cached to avoid repeated GetComponent calls
    private EnemyAI enemyAI;
    private Move moveComponent;
    private Transform playerTransform;

    // Available combinations list (updated each frame)
    private List<AvailableCombo> availableCombos = new List<AvailableCombo>();

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        moveComponent = GetComponent<Move>();
    }

    private void Start()
    {
        playerTransform = enemyAI.player;
    }

    private void Update()
    {
        // Update available combinations list every frame
        UpdateAvailableCombinations();
    }

    public void enableAttackCollider()
    {
        if (currentAttack != null && currentAttack.hitboxPrefab != null)
        {
            attackHitbox = Instantiate(currentAttack.hitboxPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    public void disableAttackCollider()
    {
        if (attackHitbox != null)
        {
            Destroy(attackHitbox);
        }
    }

    /// <summary>
    /// Builds a list of currently available combinations, including Quick Step options if applicable.
    /// </summary>
    private void UpdateAvailableCombinations()
    {
        availableCombos.Clear();
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Add direct range combos
        foreach (var weightedCombo in possibleCombinations)
        {
            if (weightedCombo.combination == null || weightedCombo.combination.onCooldown)
                continue;

            // Check if player is in range
            if (distanceToPlayer <= weightedCombo.range && weightedCombo.weight > 0)
            {
                availableCombos.Add(new AvailableCombo
                {
                    comboName = weightedCombo.combination.name,
                    combination = weightedCombo.combination,
                    weight = weightedCombo.weight * GetRecencyMultiplier(weightedCombo.combination),
                    requiresQuickStep = false,
                    quickStepDistance = 0f
                });
            }
        }

        // Add Quick Step options if enabled
        if (canQuickStep)
        {
            foreach (var weightedCombo in possibleCombinations)
            {
                if (weightedCombo.combination == null || 
                    weightedCombo.combination.onCooldown || 
                    !weightedCombo.combination.quickStepFirst)
                    continue;

                // Check if player is OUT of range
                if (distanceToPlayer > weightedCombo.range)
                {
                    float distanceNeeded = distanceToPlayer - weightedCombo.range;
                    float desiredStepDistance = Mathf.Min(distanceNeeded + quickStepBuffer, maxQuickStepRange);

                    // Only add if we can actually close the gap
                    if (desiredStepDistance > 0f && desiredStepDistance <= maxQuickStepRange)
                    {
                        availableCombos.Add(new AvailableCombo
                        {
                            comboName = "QuickStep-" + weightedCombo.combination.name,
                            combination = weightedCombo.combination,
                            weight = weightedCombo.weight * GetRecencyMultiplier(weightedCombo.combination),
                            requiresQuickStep = true,
                            quickStepDistance = desiredStepDistance
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns a weight multiplier in [varietyMinWeightMultiplier, 1] based on how recently
    /// this combination was last executed. A combo used just now gets the minimum multiplier;
    /// one that hasn't been used in varietyRecencyWindow seconds (or ever) gets full weight.
    /// This discourages the enemy from repeating the same couple of moves back-to-back
    /// without ever fully ruling a move out.
    /// </summary>
    private float GetRecencyMultiplier(Combination combo)
    {
        if (combo == null || !lastUsedTime.TryGetValue(combo, out float lastTime))
            return 1f; // Never used before, so no penalty

        float timeSinceUsed = Time.time - lastTime;
        if (timeSinceUsed >= varietyRecencyWindow || varietyRecencyWindow <= 0f)
            return 1f;

        float recoveryFraction = timeSinceUsed / varietyRecencyWindow; // 0 = just used, 1 = fully recovered
        return Mathf.Lerp(varietyMinWeightMultiplier, 1f, recoveryFraction);
    }

    public void TriggerRandomAttackCombo()
    {
        if (currentComboCoroutine != null) return;
        if (isInterrupted)
        {
            enemyAI.attacking = false;
            isInterrupted = false;
            return;
        }

        AvailableCombo selected = GetAvailableRandomCombo();
        if (selected != null)
        {
            currentComboCoroutine = StartCoroutine(ExecuteComboWrapper(selected));
        }
        else
        {
            enemyAI.attacking = false;
        }
    }

    public void TriggerSpecificCombo(Combination combo)
    {
        if (currentComboCoroutine != null || combo == null || combo.onCooldown) return;
        currentComboCoroutine = StartCoroutine(ExecuteComboWrapper(
            new AvailableCombo
            {
                comboName = combo.name,
                combination = combo,
                weight = 1f,
                requiresQuickStep = false,
                quickStepDistance = 0f
            }));
    }

    public void InterruptCombo()
    {
        isInterrupted = true;
        currentAttack = null;

        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
            currentComboCoroutine = null;
            isInterrupted = false;
            enemyAI.attacking = false;
        }
    }

    private IEnumerator ExecuteComboWrapper(AvailableCombo availableCombo)
    {
        Combination combo = availableCombo.combination;

        // Mark this combo as just-used so variety weighting suppresses it for a while.
        lastUsedTime[combo] = Time.time;

        // Execute Quick Step first if needed
        if (availableCombo.requiresQuickStep)
        {
            yield return StartCoroutine(ExecuteQuickStep(availableCombo.quickStepDistance));
        }

        // Execute the actual combo
        yield return combo.Execute(() => isInterrupted, animator, this);
        
        // Wait for the final attack animation to actually finish playing
        yield return WaitForAttackAnimationToComplete();
        
        
        // Reset attacking flag NOW so enemy can move/chase during cooldown
        enemyAI.attacking = false;
        isInterrupted = false;
        currentComboCoroutine = null;
        
        // Start cooldown in the background without blocking
        // This allows the enemy to chase/walk while the combo is on cooldown
        StartCoroutine(combo.startCooldown());
    }

    /// <summary>
    /// Waits until the current animator state finishes playing.
    /// This ensures attacking stays true for the entire animation duration.
    /// </summary>
    private IEnumerator WaitForAttackAnimationToComplete()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime;

        // Wait until the animation has progressed (avoid instant returns)
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }

    /// <summary>
    /// Executes the quick step animation and movement.
    /// </summary>
    private IEnumerator ExecuteQuickStep(float stepDistance)
    {
        // Trigger the QuickStep animation
        animator.SetTrigger("QuickStep");

        // Calculate step parameters
        float stepDuration = 0.2f; // Can be tweaked as needed
        float maxSpeed = stepDistance / stepDuration;
        
        // Determine direction (toward player)
        int direction = playerTransform.position.x > transform.position.x ? 1 : -1;

        // Execute the actual movement
        moveComponent.QuickStep(direction, maxSpeed, quickStepVerticalMovement, stepDuration);

        // Wait for quick step to complete
        yield return new WaitForSeconds(stepDuration);
    }

    /// <summary>
    /// Checks if there are any available combos in range.
    /// Used by EnemyAI to determine if enemy should attack or chase.
    /// </summary>
    public bool CanAttackPlayer(float distanceToPlayer)
    {
        foreach (var combo in availableCombos)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Selects a random available combo based on weights.
    /// </summary>
    private AvailableCombo GetAvailableRandomCombo()
    {
        if (availableCombos.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var combo in availableCombos)
            totalWeight += combo.weight;

        float randomValue = Random.value * totalWeight;
        float current = 0f;

        foreach (var combo in availableCombos)
        {
            current += combo.weight;
            if (randomValue <= current)
                return combo;
        }

        return null;
    }

    /// <summary>
    /// Helper class to track available combinations and quick step information.
    /// </summary>
    private class AvailableCombo
    {
        public string comboName;
        public Combination combination;
        public float weight;
        public bool requiresQuickStep;
        public float quickStepDistance;
    }
}