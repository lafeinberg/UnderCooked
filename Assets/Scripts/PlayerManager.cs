using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;
using Unity.Netcode;



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

    private ulong clientId;


    void Awake()
    {
        networkPlayer = GetComponent<XRINetworkPlayer>();
    }


    public override void OnNetworkSpawn()
    {
        clientId = networkPlayer.NetworkObject.OwnerClientId;
        GameManager.Instance.RegisterPlayer(clientId);
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

