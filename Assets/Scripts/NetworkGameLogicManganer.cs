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


public class NetworkGameLogicManager : NetworkBehaviour
{

    [Header("Start Level UI")]
    public GameObject overlayPanel;
    public TextMeshProUGUI overlayText;
    public GameObject readyUI;
    public GameObject readyButton;
    public TextMeshProUGUI readyCountText;

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

    private NetworkVariable<bool> showReadyUI = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> readyCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> winner = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server); // -1 = no one, 0 = host, 1 = client

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

    public Transform[] spawnPoints;

    public static NetworkGameLogicManager Instance;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }


    public void RegisterPlayer(PlayerManager player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"Registered player {player.OwnerClientId}");
        }
    }

    void Start()
    {
        winUI.SetActive(false);
        loseUI.SetActive(false);
        timerObject.SetActive(false);
        readyUI.SetActive(true);
        nextLevelUI.SetActive(true);
        uiShown = true;
        readyCountText.text = "Players Ready: 0 / 2";
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
    void SubmitReadyServerRpc()
    {
        readyCount.Value++;
        if (readyCount.Value >= 2)
        {
            gameStarted.Value = true;
            int index = 0;
            foreach (var player in players)
            {
                var spawnPoint = spawnPoints[index % spawnPoints.Length];
                index++;
            }
            StartLevel(currentLevel);
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
    }

    public void RegisterPlayerLevelComplete(PlayerManager player)
    {
        if (NetworkManager.Singleton.LocalClientId == player.OwnerClientId)
        {
            if (!levelWinnerDetected)
            {
                winUI.SetActive(true);
                player.levelWon[currentLevel] = true;
                player.levelTimes[currentLevel] = timer;
                player.levelComplete[currentLevel] = true;
                timerObject.SetActive(false);
            }
            else
            {
                loseUI.SetActive(true);
                player.levelWon[currentLevel] = false;
                player.levelTimes[currentLevel] = timer;
                player.levelComplete[currentLevel] = true;
                timerObject.SetActive(false);
            }
        }
        if (players[0].levelComplete[currentLevel] && players[1].levelComplete[currentLevel])
        {
            EndLevelClientRpc();
        }
    }

    public void StartLevel(int currentLevel)
    {
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

        ShowLevelClientRpc($"Level {currentLevel}", 4f);
        foreach (var player in players)
        {
            player.StartLevelClientRpc(currentLevel);
            matchRunning = true;
        }
    }

    [ClientRpc]
    public void EndLevelClientRpc()
    {
        matchRunning = false;
        timer = 0f;
        nextLevelUI.SetActive(true);

        levelEndIntroText.text = $"Level {currentLevel} Complete!";
        int player1EndMinutes = Mathf.FloorToInt(players[0].levelTimes[currentLevel] / 60);
        int player1EndSeconds = Mathf.FloorToInt(players[0].levelTimes[currentLevel] % 60);
        string player1Time = string.Format("{0}:{1:00}", player1EndMinutes, player1EndSeconds);

        int player2EndMinutes = Mathf.FloorToInt(players[0].levelTimes[currentLevel] / 60);
        int player2EndSeconds = Mathf.FloorToInt(players[0].levelTimes[currentLevel] % 60);
        string player2Time = string.Format("{0}:{1:00}", player2EndMinutes, player2EndSeconds);
        levelEndStats.text = $"Player 1 Time: {player1Time}\nPlayer 2 Time: {player2Time}";
    }

    public void OnNextLevel()
    {
        currentLevel++;
        nextLevelUI.SetActive(false);
        StartLevel(currentLevel);
    }


    [ClientRpc]
    void ShowLevelClientRpc(string levelName, float duration)
    {
        {
            timerObject.SetActive(true);
            StartCoroutine(ShowOverlay(levelName, duration));
        }
    }

    private System.Collections.IEnumerator ShowOverlay(string levelName, float duration)
    {
        overlayPanel.SetActive(true);
        overlayText.text = levelName;
        yield return new WaitForSeconds(duration);
        overlayPanel.SetActive(false);
        yield return new WaitForSeconds(2);
    }

    // only for the progress Ui on the fridge
    public void UpdatePlayerProgress(PlayerManager player)
    {
        Debug.Log($"player {player.OwnerClientId} completed instruction {player.currentInstructionIndex}");

        if (player.currentInstructionIndex >= currentLevelInstructions.instructions.Count)
        {
            Debug.Log($"Player {player.OwnerClientId} finished all instructions!");
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

}
