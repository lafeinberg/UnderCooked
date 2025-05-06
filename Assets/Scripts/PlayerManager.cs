using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;
using Unity.Netcode;
using Unity.XR.CoreUtils;



// add penalties??
public class LevelStats
{
    public int levelNumber;
    public int score;
    public float finalTime;
}

public class PlayerManager : NetworkBehaviour
{
    public string playerId;
    private Dictionary<int, LevelStats> levelStats = new();
    private GameManager gameManager;
    private XRINetworkPlayer networkPlayer;
    public static PlayerManager LocalPlayer;

    public Transform avatarRoot;     // Reference to AvatarRoot on PlayerPrefab
    public Transform xrRigHead;      // Reference to local XR rig's head (camera)


    private ulong clientId;

    void Update()
    {
        if (avatarRoot != null)
        {
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                xrRigHead = xrOrigin.Camera.transform;
            }
            // Sync networked avatar to local XR head position + rotation
            //avatarRoot.position = xrRigHead.position;
            //avatarRoot.rotation = xrRigHead.rotation;
        }
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        //if (!IsOwner) return;
        Debug.Log($"[Player {OwnerClientId}] Received teleport RPC to {position}");

        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            xrOrigin.transform.position = position;
            xrOrigin.transform.rotation = rotation;
            Debug.Log($"Teleported player {OwnerClientId} to {position}");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (NetworkGameLogicManager.Instance != null)
            {
                NetworkGameLogicManager.Instance.RegisterPlayer(this);
                Debug.Log($"[PlayerManager] Registered with GameLogicManager: {OwnerClientId}");
            }
            else
            {
                Debug.LogError("NetworkGameLogicManager.Instance is NULL!");
            }
        }
    }


    public void StartLevel(int levelNumber)
    {
        if (!levelStats.ContainsKey(levelNumber))
        {
            levelStats[levelNumber] = new LevelStats
            {
                levelNumber = levelNumber,
                score = 0,
                finalTime = 0f,
            };
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ingredient")
        {
            Debug.Log("Player touched ingredient!");
            NotifyStepCompleted();
        }
    }

    void NotifyStepCompleted()
    {
        GameManager.Instance.UpdatePlayerProgress(clientId, 1);
    }

    public void AddScore(int levelNumber, int amount)
    {
        if (levelStats.ContainsKey(levelNumber))
        {
            levelStats[levelNumber].score += amount;
        }
    }

    public void RecordFinalTime(int levelNumber, float time)
    {
        if (levelStats.ContainsKey(levelNumber))
        {
            levelStats[levelNumber].finalTime = time;
        }
    }

    public LevelStats GetLevelStats(int levelNumber)
    {
        if (levelStats.ContainsKey(levelNumber))
        {
            return levelStats[levelNumber];
        }

        return null;
    }
}

