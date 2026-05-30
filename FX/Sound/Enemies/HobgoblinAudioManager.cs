using System;
using UnityEngine;
using UnityEngine.UI;
public class HobgoblinAudioManager : EnemyAudioManager
{
    public AudioSource walkSource;
    public AudioSource alertSource;
    public AudioSource swipeSnortSource;
    public AudioSource slamSnortSource;
    public AudioSource hurtSource;
    public AudioSource deathSource;

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
    public override void PlayWalk() => PlaySound(walkSource);
    public override void StopWalk() => StopSound(walkSource);
    public override void PlayAlert() => PlaySound(alertSource);
    public override void StopAlert() => StopSound(alertSource);
    public void PlaySwipeSnort() => PlaySound(swipeSnortSource);
    public void StopSwipeSnort() => StopSound(swipeSnortSource);
    public void PlaySlamSnort() => PlaySound(slamSnortSource);
    public void StopSlamSnort() => StopSound(slamSnortSource);
    public override void PlayHurt() => PlaySound(hurtSource);
    public override void StopHurt() => StopSound(hurtSource);
    public override void PlayDeath() => PlaySound(deathSource);
    public override void StopDeath() => StopSound(deathSource);
    public override void playAttackSound(string name)
    {
        switch (name)
        {
            case "Swipe":
                PlaySwipeSnort();
                break;
            case "Downswing":
                PlaySlamSnort();
                break;
        }
    }
}