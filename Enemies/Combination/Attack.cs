using System.Collections;
using UnityEngine;

[System.Serializable]
public class EnemyAttack
{
    public string name;
    public string animationTrigger;
    public bool hardParry = false;
    public GameObject hitboxPrefab;
    public float hitboxDuration = 0.2f;
    public float delayAfter = 0.5f;
    public float hitboxDelay = 0.3f;
    public IEnumerator ExecuteAttack(Animator animator, System.Func<bool> isInterrupted, EnemyCombat executor)
    {
        executor.currentAttack = this;
        if (isInterrupted())yield break;
        if(hardParry)
        {
            executor.gameObject.GetComponent<Enemy>().flashHardParry();
        }
        // Trigger animation
        if (!string.IsNullOrEmpty(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }
        EnemyAudioManager audioManager = executor.gameObject.GetComponent<EnemyAI>().audioManager;
        // Play sound
        if (audioManager != null)
        {
            audioManager.getInstance().playAttackSound(name);
        }
        // Activate hitbox
        if (hitboxPrefab != null)
        {
            yield return new WaitForSeconds(hitboxDelay);
            GameObject hitbox = GameObject.Instantiate(hitboxPrefab, executor.transform.position, Quaternion.identity, executor.transform);
            float elapsed = 0f;
            while (elapsed < hitboxDuration)
            {
                if (isInterrupted()) break;
                elapsed += Time.deltaTime;
                yield return null;
            }
            GameObject.Destroy(hitbox);
        }

        // Delay before next attack
        float delay = 0f;
        while (delay < delayAfter)
        {
            if (isInterrupted()) yield break;
            delay += Time.deltaTime;
            yield return null;
        }
    }
}