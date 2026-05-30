using UnityEngine;

public class Expand : MonoBehaviour
{
    public float speed = 1.01f;
    public float timer = 300;

    void Update()
    {
        gameObject.transform.localScale = gameObject.transform.localScale*speed;
        timer -=1;
        if(timer == 0)
        {
            Destroy(gameObject);
        }
    }
}