using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyCombat : MonoBehaviour
{
    public Animator animator;
    public AudioSource audioSource;
    public List<WeightedCombination> possibleCombinations;
    public EnemyAttack currentAttack;
    private Coroutine currentComboCoroutine;
    private bool isInterrupted;

    // Cached to avoid repeated GetComponent calls
    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    public void TriggerRandomAttackCombo()
    {
        if (currentComboCoroutine != null) return;
        if (isInterrupted)
        {
            enemyAI.attacking = false;
            isInterrupted = false; // Clear the flag so future combos aren't blocked
            return;
        }

        Combination selected = GetAvailableRandomCombo();
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
        currentComboCoroutine = StartCoroutine(ExecuteComboWrapper(combo));
    }

    public void InterruptCombo()
    {
        isInterrupted = true;
        currentAttack = null;

        // If a coroutine is running, stop it immediately and clean up state.
        // This fixes the case where the enemy gets stunned/dies mid-combo and
        // the coroutine never reaches the end, leaving attacking = true forever.
        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
            currentComboCoroutine = null;
            isInterrupted = false;      // Coroutine is gone — no need to keep the flag
            enemyAI.attacking = false;  // Guarantee the flag is cleared
        }
    }

    private IEnumerator ExecuteComboWrapper(Combination combo)
    {
        yield return combo.Execute(() => isInterrupted, animator, this);

        // Small delay to ensure proper state transition
        yield return new WaitForSeconds(1f);

        StartCoroutine(combo.startCooldown());

        isInterrupted         = false;
        currentComboCoroutine = null;

        Debug.Log("Combo finished");
        Debug.Log(enemyAI.attacking);

        enemyAI.attacking = false;

        Debug.Log(enemyAI.attacking);
    }

    private Combination GetAvailableRandomCombo()
    {
        List<WeightedCombination> available = possibleCombinations.FindAll(
            wc => wc.combination != null && !wc.combination.onCooldown);

        if (available.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var item in available)
            totalWeight += item.weight;

        float randomValue = Random.value * totalWeight;
        float current     = 0f;

        foreach (var item in available)
        {
            current += item.weight;
            if (randomValue <= current)
                return item.combination;
        }

        return null;
    }
}