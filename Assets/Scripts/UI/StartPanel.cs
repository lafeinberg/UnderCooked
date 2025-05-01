using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class StartPanel : NetworkBehaviour
{
    public GameObject startPanel;
    public Button startButton;
    public TMP_Text statusText;

    private NetworkVariable<bool> player1Ready = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> player2Ready = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private ulong localClientId;

    public static StartPanel Instance;

    private bool showPanel;
    private bool gameStarted;

    void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        localClientId = NetworkManager.Singleton.LocalClientId;

        gameStarted = false;
        showPanel = false;
    }
    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (showPanel)
        {
            startPanel.SetActive(true);
            if (BothPlayersReady() && !gameStarted)
            {
                gameStarted = true;
                StartCoroutine(StartGame());
            }
            else
            {
                statusText.text = GetStatusText();
            }
        }
        else
        {
            startPanel.SetActive(false);
        }

    }

    void OnStartButtonClicked()
    {
        if (IsServer)
        {
            SetPlayerReady(localClientId);
        }
        else
        {
            SubmitReadyRequestServerRpc();
        }
    }

    public void ShowStartPanel()
    {
        showPanel = true;
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitReadyRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        SetPlayerReady(rpcParams.Receive.SenderClientId);
    }

    private void SetPlayerReady(ulong clientId)
    {
        if (clientId == 0)
            player1Ready.Value = true;
        else
            player2Ready.Value = true;
    }

    private bool BothPlayersReady()
    {
        return player1Ready.Value && player2Ready.Value;
    }

    private string GetStatusText()
    {
        if ((localClientId == 0 && !player1Ready.Value) || (localClientId == 1 && !player2Ready.Value))
        {
            return "Waiting for you to hit Start...";
        }
        else
        {
            return "Waiting for other player...";
        }
    }

    IEnumerator StartGame()
    {
        statusText.text = "Starting game...";
        yield return new WaitForSeconds(1f);
        startPanel.SetActive(false);

        if (IsServer)
        {
            // Call your game start logic here
            Debug.Log("Both players ready, starting game!");
            // Example: GameManager.Instance.StartLevel();
        }
    }
}
