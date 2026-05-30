using UnityEngine;

public class DissolveEffect : MonoBehaviour
{
    public Material dissolveMaterial;
    public float dissolveSpeed = 1.0f;
    private float dissolveAmount = 0;

    void Update()
    {
        dissolveAmount += Time.deltaTime * dissolveSpeed;
        dissolveMaterial.SetFloat("_BurnProgress", dissolveAmount);

        if (dissolveAmount >= 0.85f)
        {
            Destroy(gameObject); // Remove sprite when fully dissolved
        }
    }
}