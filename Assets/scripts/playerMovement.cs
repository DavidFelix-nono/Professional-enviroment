using UnityEngine;
using Unity.Netcode;

public class PlayerMovement2D : NetworkBehaviour
{
    // Velocidade do personagem
    public float speed = 5f;
    private Vector2 movement;

    void Update()
    {
        if (!IsOwner)
            return;
        // Input.GetAxisRaw("Horizontal") pega:
        // A = -1
        // D = 1
        // nada = 0
        float moveX = Input.GetAxisRaw("Horizontal");

        // Input.GetAxisRaw("Vertical") pega:
        // S = -1
        // W = 1
        // nada = 0
        float moveY = Input.GetAxisRaw("Vertical");

        // Criamos um Vector2 com esses dois valores
        // x = horizontal
        // y = vertical
        movement = new Vector2(moveX, moveY);
        // normalized "ajeita" o vetor para ele ter tamanho 1,
        // mantendo só a direção.
        movement = movement.normalized;
    }


    void FixedUpdate()
    {
        if (!IsOwner)
            return;
        // Envia o movimento para o servidor
        MovePlayerServerRpc(movement, speed);
    }

    [ServerRpc]
    void MovePlayerServerRpc(Vector2 m, float s)
    {
        GameManager.Instance.PlayerMoved(transform, m, s);
    }

    // O método movePlayer() não é mais necessário
}