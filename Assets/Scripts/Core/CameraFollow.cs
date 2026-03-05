using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("O Transform do Player que a câmera deve seguir.")]
    public Transform target;
    
    [Tooltip("Posição da câmera em relação ao alvo (Padrão: 0, 20, -15).")]
    public Vector3 offset = new Vector3(0, 20f, -15f);

    [Header("Movement Settings")]
    [Tooltip("Tempo aproximado para a câmera alcançar o alvo. Valores menores deixam a câmera mais ágil.")]
    public float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero; // Usado internamente pelo SmoothDamp

    [Header("Grid Bounds")]
    [Tooltip("Ative para impedir que a câmera siga o jogador para fora dos limites do grid.")]
    public bool useGridBounds = true;
    
    [Tooltip("Limites mínimos do grid no mundo (X, Z).")]
    public Vector2 minBounds = new Vector2(0, 0);
    
    [Tooltip("Limites máximos do grid no mundo (X, Z). Para um grid 100x100, use 99, 99.")]
    public Vector2 maxBounds = new Vector2(99, 99);

    [Header("Collision Handling")]
    [Tooltip("Ative para impedir que a câmera atravesse paredes ou cenários.")]
    public bool avoidClipping = true;
    
    [Tooltip("A Layer que representa os obstáculos (ex: Walls).")]
    public LayerMask obstacleLayer;
    
    [Tooltip("Distância mínima que a câmera deve manter da parede ao colidir.")]
    public float collisionOffset = 0.5f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Pega a posição base do alvo (Player)
        Vector3 focusPosition = target.position;

        // 2. Aplica os limites do Grid (Bounds) para a posição de foco
        if (useGridBounds)
        {
            focusPosition.x = Mathf.Clamp(focusPosition.x, minBounds.x, maxBounds.x);
            focusPosition.z = Mathf.Clamp(focusPosition.z, minBounds.y, maxBounds.y);
        }

        // 3. Calcula a posição ideal da câmera com o offset
        Vector3 desiredPosition = focusPosition + offset;

        // 4. Prevenção de Clipping (Colisão) com obstáculos
        if (avoidClipping)
        {
            RaycastHit hit;
            // Lança um raio do jogador (foco) até onde a câmera quer ir
            if (Physics.Linecast(focusPosition, desiredPosition, out hit, obstacleLayer))
            {
                // Se bater em uma parede, a posição desejada passa a ser o ponto de impacto,
                // afastado levemente na direção do jogador para não entrar na malha do 3D
                desiredPosition = hit.point + (focusPosition - desiredPosition).normalized * collisionOffset;
            }
        }

        // 5. Move a câmera suavemente para a posição final usando SmoothDamp
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, 
            smoothTime
        );
    }
}