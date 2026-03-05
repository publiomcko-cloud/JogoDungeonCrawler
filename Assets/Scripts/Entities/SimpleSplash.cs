using UnityEngine;

public class SimpleSplash : MonoBehaviour
{
    public float lifetime = 0.4f;
    public Vector3 maxScale = new Vector3(1.5f, 1.5f, 1.5f);
    
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;
    private float timer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        
        // Fica de frente para a inclinação da câmera isométrica
        transform.rotation = Quaternion.Euler(53f, 0f, 0f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.Lerp(originalScale, maxScale, progress);

        if (spriteRenderer != null)
        {
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, progress);
            spriteRenderer.color = newColor;
        }
    }
}