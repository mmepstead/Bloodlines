using UnityEngine;

public class Flash : MonoBehaviour
{ 
    private Material matInstance;

    private static readonly string flashProperty = "_Flash";
    private SpriteRenderer sr;
    private Color originalColor;
    public float flashDuration = 0.1f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    // Clone the material instance so we don't overwrite the shared one
        matInstance = Instantiate(sr.material);
        sr.material = matInstance;
    }

    public void flash(Color customColour)
    {
        StopAllCoroutines();
        StartCoroutine(doFlash(customColour));
    }

    public void whiteFlash(float duration = 0.1f)
    {
        StopAllCoroutines();
        StartCoroutine(doWhiteFlash(duration));
    }

    private System.Collections.IEnumerator doWhiteFlash(float duration)
    {
        matInstance.SetFloat(flashProperty, 1f);
        yield return new WaitForSeconds(flashDuration);
        matInstance.SetFloat(flashProperty, 0f);
    } 

    private System.Collections.IEnumerator doFlash(Color customColour)
    {
        sr.color = customColour;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
    }
}