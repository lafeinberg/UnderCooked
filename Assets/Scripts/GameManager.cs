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
    [SerializeField]
    public GameObject winUI;
    [SerializeField]
    public GameObject loseUI;

    [Header("Next Level UI")]
    public GameObject nextLevelUI;
    public TextMeshProUGUI levelEndIntroText;
    public TextMeshProUGUI levelEndStats;

    [Header("Timer")]
    private float timer = 0f;


    [Header("Network Variables")]
    private NetworkVariable<bool> showReadyUI = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> readyCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> winner = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server); // -1 = no one, 0 = host, 1 = client
    //private NetworkVariable<int> nextLevelReadyCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> syncedGameTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Dictionary<ulong, float> playerProgress = new();  // clientId -> progress
    public List<PlayerManager> players = new List<PlayerManager>();
    private bool localReady = false;
    private bool uiShown = false;

    [Header("Instruction Logic")]
    public List<InstructionSet> allLevelInstructionSets;
    public InstructionSet currentLevelInstructions;
    public InstructionProgressPanel instructionProgressPanel;


    [Header("Level State Manager")]
    public int currentLevel = 1;
    private bool matchRunning = false;
    private bool winnerDetected = false;

    public ProgressBar player1ProgressBar;
    public ProgressBar player2ProgressBar;

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
        //winUI.SetActive(false);
        //loseUI.SetActive(false);
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
            syncedGameTime.Value = timer;
        }

    }

    public void RegisterPlayerLevelComplete(PlayerManager player)
    {
        player.gameComplete = true;
        player.gameTime = timer;

        if (!winnerDetected)
        {
            winnerDetected = true;
            winner.Value = (int)player.OwnerClientId;
            player.gameWon = true;
        }

        ShowPlayerResultClientRpc(player.OwnerClientId, (ulong)winner.Value);

        if (players[0].gameComplete && players[1].gameComplete)
        {
            EndLevelClientRpc();
        }

    }

    [ClientRpc]
    public void ShowPlayerResultClientRpc(ulong finishedPlayerId, ulong winnerId)
    {
        Debug.Log("SHOW PLAYER RESULT");
        if (NetworkManager.Singleton.LocalClientId == finishedPlayerId)
        {
            if (finishedPlayerId == winnerId)
                winUI.SetActive(true);
            else
                loseUI.SetActive(true);
        }
    }

    public void ActivateWinUI()
    {
        Debug.Log("activating win UI");
        winUI.SetActive(true);
    }

    public void ActivateLoseUI()
    {
        Debug.Log("activating lose UI");
        loseUI.SetActive(true);
    }

    public void StartLevel(int currentLevel)
    {
        timer = 0f;
        matchRunning = true;
        winner.Value = -1;
        winnerDetected = false;
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

        ShowLevelClientRpc($"Ready... Set...", 4f);
        foreach (var player in players)
        {
            player.StartLevelClientRpc(currentLevel, 6f);
        }
    }

    [ClientRpc]
    public void EndLevelClientRpc()
    {
        Debug.Log("Level ended -- showing next level UI");
        matchRunning = false;
        //nextLevelUI.SetActive(true);

        levelEndIntroText.text = $"Game {currentLevel} Complete!";
        int player1EndMinutes = Mathf.FloorToInt(players[0].gameTime / 60);
        int player1EndSeconds = Mathf.FloorToInt(players[0].gameTime % 60);
        string player1Time = string.Format("{0}:{1:00}", player1EndMinutes, player1EndSeconds);

        int player2EndMinutes = Mathf.FloorToInt(players[1].gameTime / 60);
        int player2EndSeconds = Mathf.FloorToInt(players[1].gameTime % 60);
        string player2Time = string.Format("{0}:{1:00}", player2EndMinutes, player2EndSeconds);
        levelEndStats.text = $"Player 1 Time: {player1Time}\nPlayer 2 Time: {player2Time}";
    }

    // OLD LEVEL MANAGEMENT CODE
    /*
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
    */


    [ClientRpc]
    void ShowLevelClientRpc(string levelName, float duration)
    {
        {
            StartCoroutine(ShowOverlay(levelName, duration));
        }
    }

    private System.Collections.IEnumerator ShowOverlay(string levelName, float duration)
    {
        Debug.Log("SHOWING START LEVEL UI");

        startLevelPanel.SetActive(true);
        startLevelText.text = levelName;
        yield return new WaitForSeconds(duration);
        startLevelText.text = "Start Cooking!";
        yield return new WaitForSeconds(2);
        startLevelPanel.SetActive(false);
    }

    [ServerRpc]
    public void UpdatePlayerProgressServerRpc(ulong clientId, float progress)
    {
        Debug.Log($"player {clientId} completed instruction");

        UpdateLeaderboardClientRpc(clientId, progress);

    }

    [ClientRpc]
    void UpdateLeaderboardClientRpc(ulong clientId, float progress)
    {
        Debug.Log("updating leaderboard");

        if (clientId == 0)
        {
            player1ProgressBar.SetProgress(progress);
        }
        else
        {
            player2ProgressBar.SetProgress(progress);
        }

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

    public PlayerManager FindLocalPlayer()
    {

        PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();
        foreach (var player in allPlayers)
        {
            if (player.IsOwner)
            {
                Debug.Log($"Local player found: {player.OwnerClientId}, name: {player.name}");
                return player;
            }
        }

        Debug.LogWarning("Local player not found!");
        return null;
    }

}

