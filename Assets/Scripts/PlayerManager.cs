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
    public float finalTime;
    public bool didWin;
}

public class PlayerManager : NetworkBehaviour
{
    public string playerId;
    private Dictionary<int, LevelStats> levelStats = new();
    public static PlayerManager LocalPlayer;

    public Transform avatarRoot;
    public Transform xrRigHead;

    public int currentInstructionIndex = 0;
    private Instruction currentInstruction;

    public InstructionToolbar instructionToolbar;
    public GameObject levelStartPanel;
    public GameObject levelCompletePanel;

    public List<bool> levelComplete = new List<bool>();
    public List<bool> levelWon = new List<bool>();
    public List<float> levelTimes = new List<float>();


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
        if (NetworkGameLogicManager.Instance != null)
        {
            NetworkGameLogicManager.Instance.RegisterPlayer(this);
            Debug.Log($"[PlayerManager] Registered with GameLogicManager: {OwnerClientId}");
            instructionToolbar = GetComponentInChildren<InstructionToolbar>(true);
            if (instructionToolbar)
            {
                Debug.Log("TOOLBAR FOUND");
            }
        }
        else
        {
            Debug.LogError("NetworkGameLogicManager.Instance is NULL!");
        }
    }


    [ClientRpc]
    public void StartLevelClientRpc(int levelNumber)
    {
        Debug.Log("Starting level on client");
        NetworkGameLogicManager.Instance.currentLevelInstructions = NetworkGameLogicManager.Instance.allLevelInstructionSets[levelNumber - 1];
        instructionToolbar.ActivateInstructionToolbar(NetworkGameLogicManager.Instance.GetCurrentInstruction(currentInstructionIndex));

    }

    // DUMMY METHOD FOR TESTING 
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ingredient")
        {
            Debug.Log("Player touched ingredient!");
            PlayerStepCompleted();
        }
    }

    void PlayerStepCompleted()
    {
        if (currentInstructionIndex < NetworkGameLogicManager.Instance.GetInstructionCount())
        {
            currentInstructionIndex++;
            NetworkGameLogicManager.Instance.UpdatePlayerProgress(this);
            instructionToolbar.ShowInstruction(NetworkGameLogicManager.Instance.GetCurrentInstruction(currentInstructionIndex));
        }
        else
        {
            RegisterPlayerLevelComplete();
        }
    }

    public void RecordFinalTime(int levelNumber, float time)
    {
        levelTimes[levelNumber] = time;
    }

    public void RegisterPlayerLevelComplete()
    {
        int currentLevel = NetworkGameLogicManager.Instance.GetCurrentLevel();
        levelComplete[currentLevel] = true;
        NetworkGameLogicManager.Instance.RegisterPlayerLevelComplete(this);
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

