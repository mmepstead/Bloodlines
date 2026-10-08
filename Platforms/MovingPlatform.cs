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

    void Start() { }

    void FixedUpdate()
    {
        if (!stopped)
        {
            Vector3 targetPos = waypoints[waypointIndex].position;
            float distanceToWaypoint = Vector3.Distance(transform.position, targetPos);
            float stepSize = speed * Time.fixedDeltaTime;

            if (distanceToWaypoint > stepSize)
            {
                Vector3 movement = (targetPos - transform.position).normalized * stepSize;
                transform.position += movement;

                if (GameObject.Find("Player").GetComponent<Movement>().groundedOn == this.gameObject)
                {
                    GameObject.Find("Player").GetComponent<Movement>().movePlayer(movement);
                }
            }
            else
            {
                // Snap exactly to the waypoint and carry the player with it
                Vector3 snapMovement = targetPos - transform.position;
                transform.position = targetPos;

                if (GameObject.Find("Player").GetComponent<Movement>().groundedOn == this.gameObject)
                {
                    GameObject.Find("Player").GetComponent<Movement>().movePlayer(snapMovement);
                }

                StartCoroutine(waitForStopTime());
            }
        }
    }

    public IEnumerator waitForStopTime()
    {
        stopped = true;
        yield return new WaitForSeconds(waypoints[waypointIndex].stopTime);

        if ((waypointIndex + direction == waypoints.Count) || (waypointIndex + direction < 0))
        {
            direction = -direction;
        }

        waypointIndex += direction;
        stopped = false;
    }
}