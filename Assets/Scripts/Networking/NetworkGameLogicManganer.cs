// ================================
// 🎮 Multiplayer Plate Placement Game (Step 1: Ready System)
// ================================

using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class NetworkGameLogicManager : NetworkBehaviour
{
    [Header("Ready UI")]
    public GameObject readyUI; // In-body world-space UI visible to both
    public GameObject readyButton; // Ready button
    public TextMeshProUGUI readyCountText;

    [Header("Win/Lose UI")]
    public GameObject winUI;
    public GameObject loseUI;

    private NetworkVariable<bool> showReadyUI = new NetworkVariable<bool>(false,NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> readyCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    private NetworkVariable<int> winner = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server); // -1 = no one, 0 = host, 1 = client

    private bool localReady = false;
    private bool uiShown = false;

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
            Debug.Log($"[Client {OwnerClientId}] showReadyUI={showReadyUI.Value}, uiShown={uiShown}, gameStarted={gameStarted.Value}");
        }
        if (showReadyUI.Value && !uiShown && !gameStarted.Value)
        {
            readyUI.SetActive(true);
            uiShown = true;
        }

        if (gameStarted.Value)
        {
            readyUI.SetActive(false);
            // Game logic activation comes here in Step 2
        }

        if (uiShown && !gameStarted.Value)
        {
            readyCountText.text = $"Players Ready: {readyCount.Value} / 2";
        }

        // Optional debug UI
        if (gameStarted.Value && (winner.Value == 0 && IsServer || winner.Value == 1 && !IsServer))
        {
            winUI.SetActive(true);
        }
        else if(gameStarted.Value && winner.Value != -1)
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

}
