using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using TMPro;
using System;


// add penalties?



public class LevelStats
{
    public int levelNumber;
    public float finalTime;
    public bool didWin;
}

public class PlayerManager : NetworkBehaviour
{
    [SerializeField]
    private ParticleSystem confettiHost;
    [SerializeField]
    private ParticleSystem confettiClient;
    public string playerId;
    private Dictionary<int, LevelStats> levelStats = new();
    public static PlayerManager LocalPlayer;

    public Transform avatarRoot;
    public Transform xrRigHead;
    private XROrigin _XrOrigin;
    private Transform _cameraTransform;

    public int currentInstructionIndex = 0;
    private Instruction currentInstruction;

    public InstructionToolbar instructionToolbar;
    public ProgressBar progressBar;
    public GameObject levelStartPanel;
    public GameObject levelCompletePanel;

    private float currentProgress = 0f;

    public List<bool> levelComplete = new List<bool>();
    public List<bool> levelWon = new List<bool>();
    public List<float> levelTimes = new List<float>();

    public TextMeshProUGUI hostTimerUI;
    public TextMeshProUGUI clientTimerUI;
    private TextMeshProUGUI localTimerUI;
    private bool localTimerRunning = false;


    public DrawLineToObj pathVisualizer;
    public DrawLineToObjClient pathVisualizerClient;
    private bool isExecutingInstruction = false;

    void Awake()
    {
        confettiHost   = GameObject.FindWithTag("ConfettiHost")  
                         ?.GetComponent<ParticleSystem>();
        confettiClient = GameObject.FindWithTag("ConfettiClient")
                         ?.GetComponent<ParticleSystem>();
    }

    void Update()
    {
        //if (GameManager.Instance.GetCurrentInstruction(currentInstructionIndex).type == InstructionType.WayFind)
        //{
        //    MockWayfind();
        //}
        Debug.Log($"[Timer] IsOwner: {IsOwner}, localTimerRunning: {localTimerRunning}, UI null: {localTimerUI == null}");
        if (!IsOwner || !localTimerRunning || localTimerUI == null)
            return;

        float matchTime = GameManager.Instance.syncedGameTime.Value;
        int minutes = Mathf.FloorToInt(matchTime / 60);
        int seconds = Mathf.FloorToInt(matchTime % 60);
        localTimerUI.text = $"{minutes}:{seconds:00}";

        // This is a logic executed once when a new instruction shows up. Used to initialize or set up the scene for the task
        Debug.Log($"[PlayerManager] Update called. isExecutingInstruction={isExecutingInstruction}");

        if (isExecutingInstruction) return;

        currentInstruction = GameManager.Instance.GetCurrentInstruction(currentInstructionIndex);
        Debug.Log($"currentInstructionIndex={currentInstructionIndex}");

        switch (currentInstruction.type)
        {

            case InstructionType.WayFind:
                StartCoroutine(HandleWayfindingInstruction(currentInstruction));

                break;
                // Other instruction types will be handled externally (e.g., Grab/Salt via object interaction)
        }

        isExecutingInstruction = true;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        string timerTag = IsHost ? "HostTimerUI" : "ClientTimerUI";
        GameObject timerObj = GameObject.FindGameObjectWithTag(timerTag);
        if (timerObj != null)
        {
            localTimerUI = timerObj.GetComponentInChildren<TextMeshProUGUI>();
            Debug.Log($"TIMER FOUND FOR {timerTag}");
        }
        else
        {
            Debug.Log($"could not find timer for {timerTag}");
        }

        if (!IsOwner)
            return;

        Debug.Log($"[PlayerManager.OnNetworkSpawn IsServer:{IsServer}, IsOwner:{IsOwner}, IsClient:{IsClient}] Object: {gameObject.name}, InstanceID: {GetInstanceID()}, NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}, NetworkManager.LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        Debug.Log($"[PlayerManager.OnNetworkSpawn CLIENT-SIDE IsOwner EXECUTION] For my PlayerManager InstanceID: {GetInstanceID()}, My NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}, My LocalClientId is: {NetworkManager.Singleton.LocalClientId}.");
        LocalPlayer = this;
        _XrOrigin = FindObjectOfType<XROrigin>();
        _cameraTransform = _XrOrigin.Camera.transform;
        if (_XrOrigin == null)
        {
            Debug.LogError("[Player Manager] No XROrigin found in scene!");
        }
        instructionToolbar = _XrOrigin.Camera.GetComponentInChildren<InstructionToolbar>(true);
        if (instructionToolbar == null)
        {
            Debug.LogError("Couldn't find InstructionToolbar in scene!");
        }
        else
        {
            Debug.Log($"TOOLBAR FOUND IN SCENE FOR NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}");
        }

        progressBar = _XrOrigin.Camera.GetComponentInChildren<ProgressBar>(true);
        if (progressBar == null)
        {
            Debug.LogError("Couldn't find progress bar in scene!");
        }
        else
        {
            Debug.Log($"Progress BAR FOUND IN SCENE FOR NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}");
        }

        pathVisualizer = FindObjectOfType<DrawLineToObj>();
        //Debug.Log($"[PlayerManager] Automatically found pathVisualizer: {pathVisualizer?.gameObject.name}");
        if (pathVisualizer == null)
        {
            Debug.LogError("Couldn't find DrawLineToObj!");
        }

        pathVisualizerClient = FindObjectOfType<DrawLineToObjClient>();
        //Debug.Log($"[PlayerManager] Automatically found pathVisualizerClient: {pathVisualizer?.gameObject.name}");
        if (pathVisualizerClient == null)
        {
            Debug.LogError("Couldn't find DrawLineToObjClient!");
        }
    }


    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[PlayerManager OwnerId: {OwnerClientId}][ClientRpc] TeleportClientRpc received. Target position: {position}. IsOwner: {IsOwner}.");

        if (IsOwner)
        {
            if (_XrOrigin != null)
            {
                Debug.Log($"[PlayerManager OwnerId: {OwnerClientId}] Teleporting own XROrigin from current: {_XrOrigin.transform.position} to target: {position}");

                _XrOrigin.MoveCameraToWorldLocation(position);
                if (!IsHost)
                {
                    _XrOrigin.RotateAroundCameraUsingOriginUp(180f);
                    Debug.Log("Rotated 180");
                }

                //pathVisualizer = FindObjectOfType<DrawLineToObj>();
                //Debug.Log($"[PlayerManager] Automatically found pathVisualizer: {pathVisualizer?.gameObject.name} for {NetworkObject.OwnerClientId}");
                //if (pathVisualizer == null)
                //{
                //    Debug.LogError("Couldn't find DrawLineToObj!");
                //}

                //pathVisualizerClient = FindObjectOfType<DrawLineToObjClient>();
                //Debug.Log($"[PlayerManager] Automatically found pathVisualizerClient: {pathVisualizer?.gameObject.name} for {NetworkObject.OwnerClientId}");
                //if (pathVisualizerClient == null)
                //{
                //    Debug.LogError("Couldn't find DrawLineToObjClient!");
                //}

                Debug.Log($"[PlayerManager OwnerId: {OwnerClientId}] XROrigin teleported. New position: {_XrOrigin.transform.position}");
            }
            else
            {
                Debug.LogError($"[PlayerManager OwnerId: {OwnerClientId}] _XrOrigin is null. Cannot execute teleport.");

            }
        }
        else
        {
            Debug.LogWarning($"[PlayerManager OwnerId: {OwnerClientId}][ClientRpc] TeleportClientRpc received, but IsOwner is false. Ignoring teleport for this instance.");
        }
    }

    [ClientRpc]
    public void StartLevelClientRpc(int levelNumber, float overlayDuration)
    {
        StartCoroutine(ActivateToolbarAfterDelay(levelNumber, overlayDuration));
        StartLocalTimer();
    }

    private IEnumerator ActivateToolbarAfterDelay(int levelNumber, float delay)
    {
        yield return new WaitForSeconds(delay);
        var instruction = GameManager.Instance.GetCurrentInstruction(currentInstructionIndex);
        instructionToolbar.ShowInstruction(instruction);
        progressBar.SetProgress(currentProgress);
    }


    private void StartLocalTimer()
    {
        localTimerRunning = true;
    }

    private void StopLocalTimer()
    {
        localTimerRunning = false;
    }


    // CALL THIS FOR STEP MANAGEMENT
    public void PlayerNotifyActionCompleted(InstructionType type)
    {
        Debug.Log("player action completed");
        Debug.Log($"Checking instruction type, recieved: {type}");
        if (type == GameManager.Instance.GetCurrentInstruction(currentInstructionIndex).type)
        {
            isExecutingInstruction = false;
            PlayerStepCompleted();
        }

    }

    void PlayerStepCompleted()
    {
        Debug.Log("current step completed");
        int instructionCount = GameManager.Instance.GetInstructionCount();
        progressBar.SetProgress(currentInstructionIndex / instructionCount);

        if (currentInstructionIndex < instructionCount)
        {
            currentInstructionIndex++;
            //GameManager.Instance.UpdatePlayerProgress(this);
            instructionToolbar.ShowInstruction(GameManager.Instance.GetCurrentInstruction(currentInstructionIndex));
        }
        else
        {
            RegisterPlayerLevelComplete();
        }
    }

    public void RegisterPlayerLevelComplete()
    {
        int currentLevel = GameManager.Instance.GetCurrentLevel();
        levelComplete[currentLevel] = true;
        StopLocalTimer();
        GameManager.Instance.RegisterPlayerLevelComplete(this);
    }

    public void WinGame()
    {
        // host instance → play host confetti
        if (IsHost)
        {
            if (confettiHost != null)
                confettiHost.Play();
        }
        // all other clients → play client confetti
        else
        {
            if (confettiClient != null)
                confettiClient.Play();
        }
    }


    private IEnumerator HandleWayfindingInstruction(Instruction instruction)
    {
        int targetIndex = instruction.targetObject switch
        {
            "Fridge" => 0,
            "Stove" => 1,
            "Table" => 2,
            "Assembly" => 3,
            _ => -1
        };

        Debug.Log($"[PlayerManager IsHost:{IsHost} ,targetIndex {targetIndex}");

        if (IsHost)
        {
            pathVisualizer.SetTarget(targetIndex);
        }
        else
        {
            pathVisualizerClient.SetTarget(targetIndex);
        }

        while (!pathVisualizer.ReachedTarget(1f) && !pathVisualizerClient.ReachedTarget(1f))
        {
            yield return null;
        }

        if (IsHost)
        {
            pathVisualizer.ClearPath();
        }
        else
        {
            pathVisualizerClient.ClearPath();
        }
        PlayerNotifyActionCompleted(InstructionType.WayFind);
        isExecutingInstruction = false;
    }

    public string GetCurrentTargetObjectName()
    {
        return GameManager.Instance.GetCurrentInstruction(currentInstructionIndex).targetObject;
    }
}

