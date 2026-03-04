using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public bool playerTurn = true;

    public delegate void OnEnemyTurn();
    public event OnEnemyTurn EnemyTurnEvent;

    private void Awake()
    {
        Instance = this;
    }

    public void EndPlayerTurn()
    {
        playerTurn = false;
        EnemyTurnEvent?.Invoke();
        Invoke(nameof(StartPlayerTurn), 0.2f);
    }

    void StartPlayerTurn()
    {
        playerTurn = true;
    }
}