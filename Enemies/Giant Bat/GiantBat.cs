using System;
using UnityEngine;
using UnityEngine.Serialization;
public class GiantBat : Enemy {
    void Awake()
    {
        health = 10;
    }
    void Update()
    {
        if(health <= 0)
        {
            Destroy(GetComponent<ChasePlayer>());
            enemyDeath();
        }
        
    }
    public override void dropItems()
    {
        // Configure item drop rates
    }
}
