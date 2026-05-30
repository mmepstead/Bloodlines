using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCombination", menuName = "Combat/Combination")]
public class Combination : ScriptableObject
{
    public List<EnemyAttack> attacks;
    public float cooldown = 2f;
    [System.NonSerialized]
    public bool onCooldown = false;


    public IEnumerator Execute(System.Func<bool> isInterrupted, Animator animator,  EnemyCombat executor)
    {
        onCooldown = true;
        foreach (var attack in attacks)
        {
            if (isInterrupted()) break;
            yield return attack.ExecuteAttack(animator, isInterrupted, executor);
        }
    }

    public IEnumerator startCooldown()
    {
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }

    private static Combination GetRandomWeightedCombination(List<WeightedCombination> weightedCombinations)
    {
        float totalWeight = 0f;
        foreach (var item in weightedCombinations)
            totalWeight += item.weight;

        float randomValue = Random.value * totalWeight;
        float current = 0f;

        foreach (var item in weightedCombinations)
        {
            current += item.weight;
            if (randomValue <= current)
                return item.combination;
        }

        return null;
    }
}

[System.Serializable]
public class WeightedCombination
{
    public Combination combination;
    public float weight;
    public float range;
}