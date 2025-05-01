using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    // progress = index of step a player is at
    // player 1 created lobby
    public NetworkVariable<int> player1Progress = new NetworkVariable<int>();
    // player 2 joined lobby
    public NetworkVariable<int> player2Progress = new NetworkVariable<int>();

    public NetworkVariable<bool> player1GameReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> player2GameReady = new NetworkVariable<bool>(false);


    public static GameManager Instance;

    // different for every round?
    public float matchDuration = 180f;
    private float timer = 0f;
    private bool matchRunning = false;

    public int currentLevel = 1;
    private Dictionary<ulong, PlayerManager> players = new();
    public InstructionSet currentLevelInstructions;
    public InstructionProgressPanel instructionProgressPanel;

    private Dictionary<ulong, int> clientIdToPlayerNumber = new();
    private int nextPlayerNumber = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (matchRunning)
        {
            timer += Time.deltaTime;
            if (timer >= matchDuration)
            {
                EndMatch();
            }
        }
        else
        {
            timer = 0f;
        }
    }

    public override void OnNetworkSpawn()
    {
        // begin tracking player progress
        if (IsServer)
        {
            player1Progress.Value = 0;
            player2Progress.Value = 0;
        }
    }

    void CheckPlayersReady()
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            NotifyClientsStartGameClientRpc();
        }
    }

    [ClientRpc]
    void NotifyClientsStartGameClientRpc()
    {
        StartPanel.Instance.ShowStartPanel();
    }

    public void RegisterPlayer(ulong playerId)
    {
        if (!clientIdToPlayerNumber.ContainsKey(playerId))
        {
            clientIdToPlayerNumber[playerId] = nextPlayerNumber;
            Debug.Log($"registered client {playerId} as player {nextPlayerNumber}");
            nextPlayerNumber++;
        }
    }

    public void UpdatePlayerProgress(ulong playerId, int progress)
    {
        // only server can update progress
        if (!IsServer) return;

        if (clientIdToPlayerNumber[playerId] == 1)
        {
            player1Progress.Value = progress;
            Debug.Log("player 1 progress");
        }
        else if (clientIdToPlayerNumber[playerId] == 2)
            player2Progress.Value = progress;
    }

    public void AddScore(ulong playerId, int amount) =>
        players[playerId].AddScore(currentLevel, amount);

    [ServerRpc(RequireOwnership = false)]
    public void SubmitReadyServerRpc(ulong playerId)
    {
        if (clientIdToPlayerNumber[playerId] == 1)
            player1GameReady.Value = true;
        else if (clientIdToPlayerNumber[playerId] == 2)
            player2GameReady.Value = true;
    }

    public void StartMatch()
    {

        foreach (var player in players.Values)
        {
            player.StartLevel(currentLevel);
        }
        if (instructionProgressPanel != null && currentLevelInstructions != null)
        {
            instructionProgressPanel.SetupInstructions(currentLevelInstructions.instructions);
        }

        Debug.Log($"Level {currentLevel} started!");
    }

    public void EndMatch()
    {
        matchRunning = false;

        foreach (var player in players.Values)
        {
            player.RecordFinalTime(currentLevel, timer);
        }

        Debug.Log($"Level {currentLevel} complete!");
        foreach (var player in players)
        {
            var stats = player.Value.GetLevelStats(currentLevel);
            Debug.Log($"{player.Key} - Score: {stats.score}, Time: {stats.finalTime}");
        }
        currentLevel++;
    }

    public Instruction GetInstruction(int index)
    {
        return currentLevelInstructions.instructions[index];
    }

    public int GetInstructionCount()
    {
        return currentLevelInstructions.instructions.Count;
    }
}
