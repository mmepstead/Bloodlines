using System;
using UnityEngine;
using UnityEngine.UI;
public abstract class EnemyAudioManager : MonoBehaviour
{
    public EnemyAudioManager getInstance()
    {
        return this;
    }
    public abstract void PlayWalk();
    public abstract void StopWalk();
    public abstract void PlayAlert();
    public abstract void StopAlert();
    public abstract void PlayHurt();
    public abstract void StopHurt();
    public abstract void PlayDeath();
    public abstract void StopDeath();
    public abstract void playAttackSound(string name);

}