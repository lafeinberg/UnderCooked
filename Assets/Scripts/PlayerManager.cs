using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using TMPro;

public class PlayerManager : NetworkBehaviour
{
    [Header("Player Rig Components")]
    public Transform avatarRoot;
    public Transform xrRigHead;
    private XROrigin _XrOrigin;
    private Transform _cameraTransform;

    [Header("Instruction Controllers")]
    public int currentInstructionIndex = 0;
    private Instruction currentInstruction;

    public InstructionToolbar instructionToolbar;
    public ProgressBar progressBar;
    public GameObject levelStartPanel;
    public GameObject levelCompletePanel;

    [Header("Player Stats")]
    public bool gameComplete;
    public bool gameWon;
    public float gameTime;

    [Header("Timer UI")]
    public TextMeshProUGUI hostTimerUI;
    public TextMeshProUGUI clientTimerUI;
    private TextMeshProUGUI localTimerUI;
    private bool localTimerRunning = false;

    [Header("Wayfinding")]
    public DrawLineToObj pathVisualizer;
    public DrawLineToObjClient pathVisualizerClient;
    private bool isExecutingInstruction = false;

    void Update()
    {
        if (!IsOwner || !localTimerRunning || localTimerUI == null)
            return;

        // update local player timer UI
        float matchTime = GameManager.Instance.syncedGameTime.Value;
        int minutes = Mathf.FloorToInt(matchTime / 60);
        int seconds = Mathf.FloorToInt(matchTime % 60);
        localTimerUI.text = $"{minutes}:{seconds:00}";

        if (isExecutingInstruction) return;

        currentInstruction = GameManager.Instance.GetCurrentInstruction(currentInstructionIndex);
        Debug.Log($"currentInstructionIndex={currentInstructionIndex}");

        switch (currentInstruction.type)
        {

            case InstructionType.WayFind:
                StartCoroutine(HandleWayfindingInstruction(currentInstruction));

                break;
                // other instruction types will be handled externally (e.g., Grab/Salt via object interaction)
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

        // get xrorigin object
        _XrOrigin = FindObjectOfType<XROrigin>();
        _cameraTransform = _XrOrigin.Camera.transform;
        if (_XrOrigin == null)
        {
            Debug.LogError("No XROrigin found in scene!");
        }

        // get toolbar
        instructionToolbar = _XrOrigin.Camera.GetComponentInChildren<InstructionToolbar>(true);
        if (instructionToolbar == null)
        {
            Debug.LogError("Couldn't find InstructionToolbar in scene!");
        }
        else
        {
            Debug.Log($"TOOLBAR FOUND IN SCENE FOR NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}");
        }

        // get progress bar
        progressBar = _XrOrigin.Camera.GetComponentInChildren<ProgressBar>(true);
        if (progressBar == null)
        {
            Debug.LogError("Couldn't find progress bar in scene!");
        }
        else
        {
            Debug.Log($"Progress BAR FOUND IN SCENE FOR NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}");
        }

        // wayfinding path visualizers
        pathVisualizer = FindObjectOfType<DrawLineToObj>();
        if (pathVisualizer == null)
        {
            Debug.LogError("Couldn't find DrawLineToObj!");
        }

        pathVisualizerClient = FindObjectOfType<DrawLineToObjClient>();
        if (pathVisualizerClient == null)
        {
            Debug.LogError("Couldn't find DrawLineToObjClient!");
        }
    }


    /* Teleports client to start position in game */
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

    /* Start level for every player */
    [ClientRpc]
    public void StartLevelClientRpc(int levelNumber, float overlayDuration)
    {
        StartCoroutine(ActivateToolbarAfterDelay(levelNumber, overlayDuration));
        StartLocalTimer();
    }

    /* Activate tooltip with instruction for every player */
    private IEnumerator ActivateToolbarAfterDelay(int levelNumber, float delay)
    {
        yield return new WaitForSeconds(delay);
        var instruction = GameManager.Instance.GetCurrentInstruction(currentInstructionIndex);
        instructionToolbar.ShowInstruction(instruction);
        int instructionCount = GameManager.Instance.GetInstructionCount();
        progressBar.SetProgress((float)currentInstructionIndex / instructionCount);
    }

    private void StartLocalTimer()
    {
        localTimerRunning = true;
    }

    private void StopLocalTimer()
    {
        gameTime = GameManager.Instance.syncedGameTime.Value;
        localTimerRunning = false;
    }


    /* Player */
    public void PlayerNotifyActionCompleted(InstructionType type)
    {
        Debug.Log("Player action completed");
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
        progressBar.SetProgress((float)currentInstructionIndex / instructionCount);

        if (currentInstructionIndex < instructionCount)
        {
            SubmitProgressToServerRpc();
            currentInstructionIndex++;
            instructionToolbar.ShowInstruction(GameManager.Instance.GetCurrentInstruction(currentInstructionIndex));
        }

        //RegisterPlayerLevelComplete();

    }

    public void RegisterPlayerLevelComplete()
    {
        int currentLevel = GameManager.Instance.GetCurrentLevel();
        gameComplete = true;
        StopLocalTimer();
        GameManager.Instance.RegisterPlayerLevelComplete(this);
    }

    [ServerRpc]
    void SubmitProgressToServerRpc()
    {
        float progress = (float)currentInstructionIndex / GameManager.Instance.GetInstructionCount() * 100;
        GameManager.Instance.UpdatePlayerProgressServerRpc(OwnerClientId, progress);
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

