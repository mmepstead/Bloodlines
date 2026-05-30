using System;
using UnityEngine;
using UnityEngine.UI;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource playerWalkSource;
    public AudioSource playerParrySource;

    public AudioSource playerHitEnemySource;
    public AudioSource playerWallStabSource;

    public AudioSource playerHurtSource;
    public AudioSource playerHitShieldSource;
    public AudioSource playerSlideSource;
    public AudioSource playerChargingSource;
    public AudioSource playerChargedSource;
    public AudioSource playerChargeAttackSource;
    public AudioSource playerJumpSource;
    public AudioSource playerFallingSource;
    public AudioSource playerDashSource;
    public AudioSource playerThrowSource;
    public AudioSource playerStompSource;
    public AudioSource playerComboSource;
    public AudioSource playerDeathSource;
    public AudioSource playerLandingSource;
    // You can also use a Dictionary<string, AudioClip> for flexibility

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySound(AudioSource sound)
    {
        if (sound != null)
        {
            sound.Play();
        }
    }
    public void StopSound(AudioSource sound)
    {
        if (sound != null)
        {
            sound.Stop();
        }
    }
    public void PlayPlayerWalk() => PlaySound(playerWalkSource);
    public void StopPlayerWalk() => StopSound(playerWalkSource);
    public void PlayPlayerParry() => PlaySound(playerParrySource);
    public void StopPlayerParry() => StopSound(playerParrySource);
    public void PlayPlayerHitEnemy() => PlaySound(playerHitEnemySource);
    public void StopPlayerHitEnemy() => StopSound(playerHitEnemySource);
    public void PlayPlayerWallStab() => PlaySound(playerWallStabSource);
    public void StopPlayerWallStab() => StopSound(playerWallStabSource);
    public void PlayPlayerHitShield() => PlaySound(playerHitShieldSource);
    public void StopPlayerHitShield() => StopSound(playerHitShieldSource);
    public void PlayPlayerHurt() => PlaySound(playerHurtSource);
    public void StopPlayerHurt() => StopSound(playerHurtSource);
    public void PlayPlayerSlide() => PlaySound(playerSlideSource);
    public void StopPlayerSlide() => StopSound(playerSlideSource);
    public void PlayPlayerCharging() => PlaySound(playerChargingSource);
    public void StopPlayerCharging() => StopSound(playerChargingSource);
    public void PlayPlayerCharged() => PlaySound(playerChargedSource);
    public void StopPlayerCharged() => StopSound(playerChargedSource);
    public void PlayPlayerChargeAttack() => PlaySound(playerChargeAttackSource);
    public void StopPlayerChargeAttack() => StopSound(playerChargeAttackSource);
    public void PlayPlayerJump() => PlaySound(playerJumpSource);
    public void StopPlayerJump() => StopSound(playerJumpSource);
    public void PlayPlayerDash() => PlaySound(playerDashSource);
    public void StopPlayerDash() => StopSound(playerDashSource);
    public void PlayPlayerFalling() => PlaySound(playerFallingSource);
    public void StopPlayerFalling() => StopSound(playerFallingSource);
    public void PlayPlayerThrow() => PlaySound(playerThrowSource);
    public void StopPlayerThrow() => StopSound(playerThrowSource);
    public void PlayPlayerStomp() => PlaySound(playerStompSource);
    public void StopPlayerStomp() => StopSound(playerStompSource);
    public void PlayPlayerCombo() => PlaySound(playerComboSource);
    public void StopPlayerCombo() => StopSound(playerComboSource);
    public void PlayPlayerDeath() => PlaySound(playerDeathSource);
    public void StopPlayerDeath() => StopSound(playerDeathSource);
    public void PlayPlayerLanding() => PlaySound(playerLandingSource);
    public void StopPlayerLanding() => StopSound(playerLandingSource);
}
    