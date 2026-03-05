using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("O Transform do Player que a câmera deve seguir.")]
    public Transform target;
    
    [Tooltip("Posição base da câmera em relação ao alvo (Padrão: 0, 20, -15).")]
    public Vector3 offset = new Vector3(0, 20f, -15f);

    [Header("Movement Settings")]
    [Tooltip("Tempo aproximado para a câmera alcançar o alvo. Valores menores deixam a câmera mais ágil.")]
    public float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero;

    [Header("Zoom Settings")]
    [Tooltip("Ativa/Desativa o controle de zoom pelo scroll do mouse.")]
    public bool enableZoom = true;
    [Tooltip("Velocidade com que o scroll aproxima ou afasta a câmera.")]
    public float zoomSpeed = 2f;
    [Tooltip("Multiplicador mínimo (o quão perto a câmera pode chegar). 0.5 = Metade da distância.")]
    public float minZoom = 0.4f;
    [Tooltip("Multiplicador máximo (o quão longe a câmera pode ir). 2.0 = Dobro da distância.")]
    public float maxZoom = 1.5f;
    
    // Variável interna que guarda o nível de zoom atual (1.0 = offset original)
    private float currentZoomMultiplier = 1.0f;

    [Header("Grid Bounds")]
    [Tooltip("Ative para impedir que a câmera siga o jogador para fora dos limites do grid.")]
    public bool useGridBounds = true;
    public Vector2 minBounds = new Vector2(0, 0);
    public Vector2 maxBounds = new Vector2(99, 99);

    [Header("Collision Handling")]
    [Tooltip("Ative para impedir que a câmera atravesse paredes ou cenários.")]
    public bool avoidClipping = true;
    public LayerMask obstacleLayer;
    public float collisionOffset = 0.5f;

    void LateUpdate()
    {
        if (target == null) return;

        // --- Lógica de Zoom (Scroll do Mouse) ---
        if (enableZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                // Subtrai porque rolar a roda do rato para "frente" (positivo) aproxima (zoom menor)
                currentZoomMultiplier -= scroll * zoomSpeed;
                currentZoomMultiplier = Mathf.Clamp(currentZoomMultiplier, minZoom, maxZoom);
            }
        }

        // 1. Pega a posição base do alvo (Player)
        Vector3 focusPosition = target.position;

        // 2. Aplica os limites do Grid (Bounds) para a posição de foco
        if (useGridBounds)
        {
            focusPosition.x = Mathf.Clamp(focusPosition.x, minBounds.x, maxBounds.x);
            focusPosition.z = Mathf.Clamp(focusPosition.z, minBounds.y, maxBounds.y);
        }

        // 3. Calcula a posição ideal com o offset afetado pelo multiplicador de zoom
        Vector3 zoomedOffset = offset * currentZoomMultiplier;
        Vector3 desiredPosition = focusPosition + zoomedOffset;

        // 4. Prevenção de Clipping (Colisão)
        if (avoidClipping)
        {
            RaycastHit hit;
            if (Physics.Linecast(focusPosition, desiredPosition, out hit, obstacleLayer))
            {
                desiredPosition = hit.point + (focusPosition - desiredPosition).normalized * collisionOffset;
            }
        }

        // 5. Move a câmera suavemente
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, 
            smoothTime
        );
    }
}