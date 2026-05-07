using UnityEngine;
using System;
using Unity.Netcode;

public class GameManager : NetworkBehaviour // aaaaaa verdadeeeeeeeeeeeeee verdadeiraaaaaaaaa
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Garante que só exista um
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); //persiste entre cenas
    }
    
    public void PlayerMoved(Transform transform, Vector2 movement, float speed)
    {
        // Só o servidor pode mover
        if (!IsServer) return;
        transform.position += (Vector3)(movement * speed * Time.fixedDeltaTime);
    }
    
}
