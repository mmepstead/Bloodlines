using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
public class WallCling : MonoBehaviour {
    static readonly int IsHanging = Animator.StringToHash("Hanging");
    static readonly int IsAttacking = Animator.StringToHash("Attacking");
    public GameObject wallChipsPrefab;
    void OnTriggerEnter2D(Collider2D collision)
    {

        // Check for a match with the specified name on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "Grabbable Wall")
        {
            AudioManager.Instance.PlayPlayerWallStab();
            GameObject player = GameObject.Find("Player");
            if(player.GetComponent<Movement>().grounded)
            {
                return;
            }
            Vector3 collisionPoint = collision.ClosestPoint(player.transform.position);
            GameObject wallChips = Instantiate(wallChipsPrefab, collisionPoint, Quaternion.identity);
            var wallChipsParticlesShape = wallChips.GetComponent<ParticleSystem>().shape;
            wallChipsParticlesShape.rotation = new Vector3(0,player.transform.localScale.x > 0 ? 270 : 90, 0);
            Destroy(GameObject.Find("Attack(Clone)"), 0.5f);
            player.GetComponentInChildren<Animator>().SetBool(IsHanging, true);
            player.GetComponentInChildren<Animator>().SetBool(IsAttacking, false);
            Vector2 startHangingPosition;
            if(collision.gameObject.GetComponent<Tilemap>() == null)
            {
                // Set player
                player.GetComponent<Movement>().hangingOn = collision.gameObject;
                player.GetComponent<Movement>().hangingPoint = collisionPoint;
                startHangingPosition = new Vector2(collisionPoint.x + (player.transform.localScale.x > 0 ? -0.7f : 0.7f), collisionPoint.y);
                player.GetComponent<Movement>().hangingOn = collision.gameObject;
            }
            else 
            {
                Tilemap tilemap = collision.gameObject.GetComponent<Tilemap>();
                Vector3Int cellPosition = tilemap.WorldToCell(player.transform.position);
                while(tilemap.GetSprite(cellPosition) == null)
                {
                    cellPosition.x += player.transform.localScale.x > 0 ? 1 : -1;
                }
                Vector3 cellCenterWorld = tilemap.GetCellCenterWorld(cellPosition);
                startHangingPosition = new Vector2(cellCenterWorld.x + (player.transform.localScale.x > 0 ? -0.7f : 0.7f), cellCenterWorld.y);
                player.GetComponent<Movement>().hangingOn = null;
            }
            // Set player
            player.GetComponent<Movement>().hangingPoint = startHangingPosition;
            player.transform.position = startHangingPosition;
            return;
        }
        
    }
}
