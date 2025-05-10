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
    private XROrigin _XrOrigin;
    private Transform _cameraTransform;

    public int currentInstructionIndex = 0;
    private Instruction currentInstruction;

    public InstructionToolbar instructionToolbar;
    public Vector3 toolbarOffset = new Vector3(0f, -0.5f, -1.5f);
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
                _cameraTransform = xrOrigin.Camera.transform;
            }
            // Sync networked avatar to local XR head position + rotation
            //avatarRoot.position = xrRigHead.position;
            //avatarRoot.rotation = xrRigHead.rotation;
        }

        if (GameManager.Instance.GetCurrentInstruction(currentInstructionIndex).type == InstructionType.WayFind)
        {
            MockWayfind();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[PlayerManager.OnNetworkSpawn IsServer:{IsServer}, IsOwner:{IsOwner}, IsClient:{IsClient}] Object: {gameObject.name}, InstanceID: {GetInstanceID()}, NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}, NetworkManager.LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        Debug.Log($"[PlayerManager.OnNetworkSpawn CLIENT-SIDE IsOwner EXECUTION] For my PlayerManager InstanceID: {GetInstanceID()}, My NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}, My LocalClientId is: {NetworkManager.Singleton.LocalClientId}.");
        LocalPlayer = this;
        _XrOrigin = FindObjectOfType<XROrigin>();
        if (_XrOrigin == null)
        {
            Debug.LogError("[Player Manager] No XROrigin found in scene!");
        }

        instructionToolbar = FindObjectOfType<InstructionToolbar>(true);
        if (instructionToolbar == null)
            Debug.LogError("Couldn't find InstructionToolbar in scene!");

    }

    void LateUpdate()
    {
        if (IsOwner && instructionToolbar != null && _cameraTransform != null)
        {
            Vector3 worldPos = _cameraTransform.TransformPoint(toolbarOffset);
            /*
            instructionToolbar.transform.position = worldPos;

            instructionToolbar.transform.rotation =
                Quaternion.LookRotation(instructionToolbar.transform.position - _cameraTransform.position);
            */
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
                _XrOrigin.MatchOriginUpCameraForward(rotation * Vector3.up, rotation * Vector3.forward); // Adjust if you want different up/forward alignment

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
    }

    private IEnumerator ActivateToolbarAfterDelay(int levelNumber, float delay)
    {
        yield return new WaitForSeconds(delay);
        var instruction = GameManager.Instance.GetCurrentInstruction(currentInstructionIndex);
        instructionToolbar.ShowInstruction(instruction);
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


    // CALL THIS FOR STEP MANAGEMENT
    public void PlayerNotifyActionCompleted(InstructionType type)
    {
        Debug.Log("player action completed");
        Debug.Log($"Checking instruction type, recieved: {type}");
        if (type == GameManager.Instance.GetCurrentInstruction(currentInstructionIndex).type)
        {
            PlayerStepCompleted();
        }

    }

    void PlayerStepCompleted()
    {
        Debug.Log("current step completed");
        if (currentInstructionIndex < GameManager.Instance.GetInstructionCount())
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
        GameManager.Instance.RegisterPlayerLevelComplete(this);
    }

    private System.Collections.IEnumerator MockWayfind()
    {
        yield return new WaitForSeconds(4);
        PlayerNotifyActionCompleted(InstructionType.WayFind);
    }
}

