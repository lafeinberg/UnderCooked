// ================================
// 🎮 Multiplayer Plate Placement Game (Step 1: Ready System)
// ================================
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Services.Lobbies.Models;


public class GameManager : NetworkBehaviour
{
    [Header("Start Game UI")]
    public GameObject readyUI;
    public GameObject readyButton;
    public TextMeshProUGUI readyCountText;

    [Header("Start Level UI")]
    public GameObject startLevelPanel;
    public TextMeshProUGUI startLevelText;

    [Header("Win/Lose UI")]
    public GameObject winUI;
    public GameObject loseUI;

    [Header("Next Level UI")]
    public GameObject nextLevelUI;
    public TextMeshProUGUI levelEndIntroText;
    public TextMeshProUGUI levelEndStats;
    public GameObject nextLevelButton;
    public TextMeshProUGUI nextLevelReadyText;

    [Header("Timer")]
    public GameObject timerObject;
    private float timer = 0f;
    public TextMeshProUGUI timerText;


    [Header("Network Variables")]
    private NetworkVariable<bool> showReadyUI = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> readyCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> winner = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server); // -1 = no one, 0 = host, 1 = client
    private NetworkVariable<int> nextLevelReadyCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public List<PlayerManager> players = new List<PlayerManager>();
    private bool localReady = false;
    private bool uiShown = false;

    [Header("Instruction Logic")]
    public List<InstructionSet> allLevelInstructionSets;
    public InstructionSet currentLevelInstructions;
    public InstructionProgressPanel instructionProgressPanel;
    public InstructionToolbar instructionToolbar;

    [Header("Level State Manager")]
    public int currentLevel = 1;
    private bool matchRunning = false;
    private bool levelWinnerDetected = false;
    public float proximityThreshold = 2f;

    public Transform[] spawnPoints;
    public static GameManager Instance;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }


    public void RegisterPlayer(PlayerManager player)
    {
        if (players.Count < 2)
        {
            players.Add(player);
            Debug.Log($"Registered player {NetworkManager.Singleton.LocalClientId}");
        }
        Debug.Log($"PLAYERS LIST: {players}");
    }

    void Start()
    {
        winUI.SetActive(false);
        loseUI.SetActive(false);
        timerObject.SetActive(false);
        showReadyUI.Value = false;
        nextLevelUI.SetActive(false);
        uiShown = true;
        readyCountText.text = "Players Ready: 0 / 2";
        currentLevelInstructions = allLevelInstructionSets[0];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;
        // On the server, listen for every new client
        NetworkManager.Singleton.OnClientConnectedCallback += HandleNewClient;
    }

    private void HandleNewClient(ulong clientId)
    {
        // pull their spawned PlayerObject:
        var client = NetworkManager.Singleton.ConnectedClients[clientId];
        var playerObj = client.PlayerObject;
        if (playerObj == null)
        {
            Debug.LogError($"No PlayerObject for client {clientId}");
            return;
        }

        var pm = playerObj.GetComponent<PlayerManager>();
        if (pm == null)
        {
            Debug.LogError($"PlayerObject for {clientId} missing PlayerManager!");
            return;
        }

        RegisterPlayer(pm);
    }

    public void OnReadyClicked()
    {
        Debug.Log($"[UI Click] OnReadyClicked() called on client {NetworkManager.Singleton.LocalClientId}, isOwner={IsOwner}, isServer={IsServer}");
        if (!localReady)
        {
            localReady = true;
            SubmitReadyServerRpc();
            readyButton.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        readyCount.Value++;
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[Server] SubmitReadyServerRpc invoked by ClientId: {senderClientId}. Current readyCount: {readyCount.Value}");

        int expectedPlayers = 2;

        if (readyCount.Value >= expectedPlayers && !gameStarted.Value)
        {
            gameStarted.Value = true;
            Debug.Log($"[Server] All {expectedPlayers} players ready. Game is starting!");
            Debug.Log($"[Server] Current players list count: {players.Count}. Available spawn points: {spawnPoints.Length}");

            if (players.Count < expectedPlayers)
            {
                Debug.LogError($"[Server] CRITICAL: 'players' list count ({players.Count}) is less than expectedPlayers ({expectedPlayers}) even though readyCount was met. Aborting spawn logic.");
                gameStarted.Value = false; // Rollback
                return;
            }

            Debug.Log("[Server] --- BEGIN PLAYER SPAWN ASSIGNMENT ---");
            // Log the state of the 'players' list before iterating
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    Debug.Log($"[Server] Pre-Loop Check: players[{i}] -> OwnerClientId: {players[i].OwnerClientId}, InstanceID: {players[i].GetInstanceID()}");
                }
                else
                {
                    Debug.Log($"[Server] Pre-Loop Check: players[{i}] -> NULL PlayerManager instance!");
                }
            }

            int spawnPointIndex = 0; // This is the critical index for the spawnPoints array
            foreach (var playerManagerInstance in players)
            {
                Debug.Log($"[Server] Processing player: {playerManagerInstance?.OwnerClientId} (InstanceID: {playerManagerInstance?.GetInstanceID()}).");

                if (playerManagerInstance == null)
                {
                    Debug.LogWarning($"[Server] Skipping a NULL PlayerManager instance in the 'players' list.");
                    // If a null entry should still "consume" a spawn point index:
                    // spawnPointIndexToUse++;
                    continue; // Skip to the next playerManagerInstance
                }

                if (spawnPointIndex < spawnPoints.Length)
                {
                    Transform spawnPoint = spawnPoints[spawnPointIndex];
                    if (spawnPoint != null)
                    {
                        Debug.Log($"[Server] SUCCESS: Assigning Player OwnerClientId: {playerManagerInstance.OwnerClientId} to spawnPoints[{spawnPointIndex}] (Name: '{spawnPoint.name}', Position: {spawnPoint.position})");
                        playerManagerInstance.TeleportClientRpc(spawnPoint.position, spawnPoint.rotation);
                    }
                }
                else
                {
                    Debug.LogError($"[Server] ERROR: spawnPointIndexToUse ({spawnPointIndex}) is out of bounds for spawnPoints array (Length: {spawnPoints.Length}). Player {playerManagerInstance.OwnerClientId} will not get a unique spawn point via this index.");
                    // Fallback
                    if (spawnPoints.Length > 0 && spawnPoints[0] != null)
                    {
                        Debug.LogWarning($"[Server] Fallback: Teleporting Player {playerManagerInstance.OwnerClientId} to spawnPoints[0] due to index out of bounds.");
                        playerManagerInstance.TeleportClientRpc(spawnPoints[0].position, spawnPoints[0].rotation);
                    }
                    else
                    {
                        Debug.LogError($"[Server] Fallback failed: No spawnPoints[0] for Player {playerManagerInstance.OwnerClientId}.");
                    }
                }
                spawnPointIndex++;
            }

            StartLevel(currentLevel);
        }
        else if (gameStarted.Value)
        {
            Debug.LogWarning($"[Server] SubmitReadyServerRpc from ClientId {senderClientId}: Game already started. readyCount: {readyCount.Value}. Ignoring redundant ready signal.");
        }
        else
        {
            Debug.Log($"[Server] SubmitReadyServerRpc from ClientId {senderClientId}: Not enough players ready yet. readyCount: {readyCount.Value} / {expectedPlayers}");
        }
    }
    void Update()
    {
        if (IsServer && !showReadyUI.Value && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            showReadyUI.Value = true;
        }
        if (gameStarted.Value)
        {
            readyUI.SetActive(false);
        }
        if (showReadyUI.Value && !gameStarted.Value)
        {
            readyUI.SetActive(true);
            uiShown = true;
        }

        if (uiShown && !gameStarted.Value)
        {
            readyCountText.text = $"Players Ready: {readyCount.Value} / 2";
        }

        if (matchRunning)
        {
            timer += Time.deltaTime;
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);

            timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }

        nextLevelReadyText.text = $"Players Ready: {nextLevelReadyCount.Value} / 2";
    }

    public void RegisterPlayerLevelComplete(PlayerManager player)
    {
        player.levelComplete[currentLevel] = true;
        player.levelTimes[currentLevel] = timer;

        if (!levelWinnerDetected)
        {
            levelWinnerDetected = true;
            winner.Value = (int)player.OwnerClientId;
            player.levelWon[currentLevel] = true;
        }

        ShowPlayerResultClientRpc(player.OwnerClientId, (ulong)winner.Value);

        if (players[0].levelComplete[currentLevel] && players[1].levelComplete[currentLevel])
        {
            EndLevelClientRpc();
        }

    }

    [ClientRpc]
    void ShowPlayerResultClientRpc(ulong finishedPlayerId, ulong winnerId)
    {
        if (NetworkManager.Singleton.LocalClientId == finishedPlayerId)
        {
            if (finishedPlayerId == winnerId)
                winUI.SetActive(true);
            else
                loseUI.SetActive(true);
        }
    }


    public void StartLevel(int currentLevel)
    {
        winner.Value = -1;
        levelWinnerDetected = false;
        if (currentLevel <= allLevelInstructionSets.Count)
        {
            currentLevelInstructions = allLevelInstructionSets[currentLevel - 1];
            Debug.Log($"Loaded instructions for Level {currentLevel}");
        }
        else
        {
            Debug.LogError($"No instructions found for Level {currentLevel}");
            return;
        }
        foreach (var player in players)
        {
            ShowLevelClientRpc($"Level {currentLevel}", 4f);
            player.StartLevelClientRpc(currentLevel, 6f);
        }
    }

    [ClientRpc]
    public void EndLevelClientRpc()
    {
        Debug.Log("Level ended -- showing next level UI");
        matchRunning = false;
        timer = 0f;
        nextLevelUI.SetActive(true);

        levelEndIntroText.text = $"Level {currentLevel} Complete!";
        int player1EndMinutes = Mathf.FloorToInt(players[0].levelTimes[currentLevel] / 60);
        int player1EndSeconds = Mathf.FloorToInt(players[0].levelTimes[currentLevel] % 60);
        string player1Time = string.Format("{0}:{1:00}", player1EndMinutes, player1EndSeconds);

        int player2EndMinutes = Mathf.FloorToInt(players[1].levelTimes[currentLevel] / 60);
        int player2EndSeconds = Mathf.FloorToInt(players[1].levelTimes[currentLevel] % 60);
        string player2Time = string.Format("{0}:{1:00}", player2EndMinutes, player2EndSeconds);
        levelEndStats.text = $"Player 1 Time: {player1Time}\nPlayer 2 Time: {player2Time}";
    }

    public void OnNextLevel()
    {
        currentLevel++;
        nextLevelUI.SetActive(false);
        SubmitNextLevelReadyServerRpc();
    }


    [ServerRpc(RequireOwnership = false)]
    void SubmitNextLevelReadyServerRpc()
    {
        nextLevelReadyCount.Value++;
        if (nextLevelReadyCount.Value >= 2)
        {
            nextLevelReadyCount.Value = 0;
            StartLevel(currentLevel);
        }
    }


    [ClientRpc]
    void ShowLevelClientRpc(string levelName, float duration)
    {
        {
            StartCoroutine(ShowOverlay(levelName, duration));
            matchRunning = true;
            timerObject.SetActive(true);
        }
    }

    private System.Collections.IEnumerator ShowOverlay(string levelName, float duration)
    {
        Debug.Log("SHOWING START LEVEL UI");
        //startLevelPanel.transform.position =
        //Camera.main.transform.position;
        //startLevelPanel.transform.rotation =
        //uaternion.LookRotation(Camera.main.transform.forward);
        startLevelPanel.SetActive(true);
        startLevelText.text = levelName;
        yield return new WaitForSeconds(duration);
        startLevelText.text = "Start!";
        yield return new WaitForSeconds(2);
        startLevelPanel.SetActive(false);
    }

    // only for the progress Ui on the fridge
    /*
    public void UpdatePlayerProgress(PlayerManager player)
    {
        Debug.Log($"player {player.OwnerClientId} completed instruction {player.currentInstructionIndex}");

        if (player.currentInstructionIndex >= currentLevelInstructions.instructions.Count)
        {
            Debug.Log($"Player {player.OwnerClientId} finished all instructions!");
        }

    }
    */

    public PlayerManager FindPlayerByObject()
    {
        float closestDistanceSqr = float.MaxValue;
        PlayerManager closestPlayer = null;

        foreach (var player in players)
        {
            Vector3 playerPos = player.avatarRoot != null ? player.avatarRoot.position : player.transform.position;

            float distSqr = (playerPos - transform.position).sqrMagnitude;
            if (distSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distSqr;
                closestPlayer = player;
            }
        }

        if (closestPlayer != null && closestDistanceSqr <= proximityThreshold * proximityThreshold)
        {
            Debug.Log($"Closest player to object is {closestPlayer.OwnerClientId}");
        }
        else
        {
            Debug.LogWarning("No player found within proximity threshold!");
        }

        return closestPlayer;
    }

    public Instruction GetCurrentInstruction(int index)
    {
        return currentLevelInstructions.instructions[index];
    }

    public int GetInstructionCount()
    {
        return currentLevelInstructions.instructions.Count;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

}
