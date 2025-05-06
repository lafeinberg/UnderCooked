// ================================
// 🎮 Multiplayer Plate Placement Game (Step 1: Ready System)
// ================================
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;


public class NetworkGameLogicManager : NetworkBehaviour
{
    [Header("Ready UI")]
    public GameObject readyUI;
    public GameObject readyButton;
    public TextMeshProUGUI readyCountText;

    [Header("Start Level UI")]
    public GameObject overlayPanel;
    public TextMeshProUGUI overlayText;

    [Header("Win/Lose UI")]
    public GameObject winUI;
    public GameObject loseUI;

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

    public int currentLevel = 1;
    private float timer = 0f;
    private bool matchRunning = false;

    public InstructionSet currentLevelInstructions;
    public InstructionProgressPanel instructionProgressPanel;

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
        readyUI.SetActive(false);
        winUI.SetActive(false);
        loseUI.SetActive(false);
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
                player.TeleportClientRpc(spawnPoint.position, spawnPoint.rotation);
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
        if (IsOwner)
        {
            //Debug.Log($"[Client {OwnerClientId}] showReadyUI={showReadyUI.Value}, uiShown={uiShown}, gameStarted={gameStarted.Value}");

        }
        if (showReadyUI.Value && !uiShown && !gameStarted.Value)
        {
            readyUI.SetActive(true);
            uiShown = true;
        }

        if (gameStarted.Value)
        {
            readyUI.SetActive(false);
        }

        if (uiShown && !gameStarted.Value)
        {
            readyCountText.text = $"Players Ready: {readyCount.Value} / 2";
        }

        if (gameStarted.Value && (winner.Value == 0 && IsServer || winner.Value == 1 && !IsServer))
        {
            winUI.SetActive(true);
        }
        else if (gameStarted.Value && winner.Value != -1)
        {
            loseUI.SetActive(true);
        }
    }


    public void RegisterWinner(int winnerId)
    {
        if (!IsServer || winner.Value != -1) return;

        Debug.Log($"[Server] RegisterWinner({winnerId})");
        winner.Value = winnerId;
    }

    public void StartLevel(int currentLevel)
    {
        ShowLevel($"Level {currentLevel}", 4f);
    }

    public void ShowLevel(string levelName, float duration)
    {
        StartCoroutine(ShowOverlay(levelName, duration));
    }

    private System.Collections.IEnumerator ShowOverlay(string levelName, float duration)
    {
        overlayPanel.SetActive(true);
        overlayText.text = levelName;
        yield return new WaitForSeconds(duration);
        overlayPanel.SetActive(false);
    }

}
