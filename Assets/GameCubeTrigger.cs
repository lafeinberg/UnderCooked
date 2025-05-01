using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameCubeTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int cubeOwnerId;

    private bool platePlaced = false;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Cube] Collision detected with: {collision.gameObject.name}, tag: {collision.gameObject.tag}");
        if (platePlaced) return;
        if (!NetworkManager.Singleton.IsServer)

        {
            Debug.Log("[Cube] Ignored because not running on server.");
            return; }

        if (collision.gameObject.CompareTag("Ingredient"))
        {
            platePlaced = true;
            Debug.Log($"[Server] Plate placed on cube owned by {cubeOwnerId}");

            NetworkGameLogicManager logic = FindAnyObjectByType<NetworkGameLogicManager>();
            if (logic != null)
            {
                logic.RegisterWinner(cubeOwnerId);
            }
        }
    }
}
