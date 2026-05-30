using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MovingPlatform : MonoBehaviour
{
    public List<Waypoint> waypoints;
    public int waypointIndex = 0;
    public float speed = 2;
    public int direction = 1;
    private bool stopped = false;

    void Start()
    {

    }

    void FixedUpdate()
    {
        if(!stopped)
        {
            if(transform.position != waypoints[waypointIndex].position) 
            {
                Vector3 movement = (waypoints[waypointIndex].position - transform.position).normalized * speed;
                this.transform.position += movement;
                if(GameObject.Find("Player").GetComponent<Movement>().groundedOn == this.gameObject)
                {
                    GameObject.Find("Player").GetComponent<Movement>().movePlayer(movement);
                }
            }
            else 
            {
                StartCoroutine(waitForStopTime());
            }
        }
    }

    public IEnumerator waitForStopTime() 
    {
        stopped = true;
        yield return new WaitForSeconds(waypoints[waypointIndex].stopTime);
        if((waypointIndex + direction == waypoints.Count) || (waypointIndex + direction < 0)) 
        {
            direction = -direction;
        }
        waypointIndex += direction;
        stopped = false;
    }
}