using UnityEngine;

public class SlideInPrefabSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("Prefab to spawn. Must have a public 'string descriptionText' field or property.")]
    public GameObject prefabToSpawn;

    [Header("Slide Settings")]
    [Tooltip("How far to the right (in local space) the prefab starts before sliding in.")]
    public float startOffsetX = 5f;

    [Tooltip("How fast the prefab slides toward the parent position.")]
    public float slideSpeed = 5f;

    private Transform parentTransform;
    private GameObject tmp;
    public GameObject instance;
    private string description;
    private void Awake()
    {
        parentTransform = transform;
    }

    /// <summary>
    /// Instantiates the prefab to the right of the parent and slides it in.
    /// </summary>
    /// <param name="description">The string to assign to the prefab's descriptionText field.</param>
    public void SpawnAndSlideIn(string description)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Prefab not assigned on " + name);
            return;
        }

        // Instantiate as a child of the same parent as this object
        instance = Instantiate(prefabToSpawn, parentTransform);
        tmp = instance.GetComponent<PickupBox>().textObject;
        this.description = description;
        // Start position: to the right of parent in local space
        instance.transform.localPosition = new Vector3(startOffsetX, 0f, 0f);

        // Start coroutine to slide in
        StartCoroutine(SlideIn(instance.transform));
    }

    private System.Collections.IEnumerator SlideIn(Transform target)
    {
        Vector3 targetPos = Vector3.zero;
        while (Vector3.Distance(target.localPosition, targetPos) > 0.01f)
        {
            target.localPosition = Vector3.MoveTowards(
                target.localPosition,
                targetPos,
                slideSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Snap exactly to 0,0,0 at the end
        tmp.active = true;
        tmp.GetComponent<TMPro.TMP_Text>().text = description;
        target.localPosition = targetPos;
    }

    public System.Collections.IEnumerator SlideOut()
    {
       Vector3 targetPos = instance.transform.localPosition + new Vector3(startOffsetX, 0f, 0f);
        while (Vector3.Distance(instance.transform.localPosition, targetPos) > 0.01f)
        {
            tmp.transform.localPosition += 200 * slideSpeed * Time.deltaTime * Vector3.right;
            instance.transform.localPosition = Vector3.MoveTowards(
                instance.transform.localPosition,
                targetPos,
                slideSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Snap exactly to 0,0,0 at the end
        Destroy(instance);
        Destroy(tmp);
    }
}
