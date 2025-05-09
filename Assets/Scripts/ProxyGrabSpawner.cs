using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ProxyGrabSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject prefabToSpawn;

    private XRGrabInteractable grabInteractable;


    private IXRSelectInteractor lastInteractor;
    private XRInteractionManager interactionManager;
    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        interactionManager = grabInteractable.interactionManager;
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        base.OnDestroy();
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        lastInteractor = args.interactorObject;
        interactionManager.SelectExit(lastInteractor, grabInteractable);
        if (prefabToSpawn == null) return;

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        SpawnServerRpc(spawnPos, spawnRot);

        
    }

    [Rpc(SendTo.Server)]
    void SpawnServerRpc(Vector3 pos, Quaternion rot)
    {
        var spawned = Instantiate(prefabToSpawn, pos, rot);
        spawned.Spawn();
        NotifyClientGrabClientRpc(spawned.NetworkObjectId);

    }

    [ClientRpc]
    private void NotifyClientGrabClientRpc(ulong objectId)
    {

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj)) return;

        if (lastInteractor == null) return;

        if (netObj.TryGetComponent(out XRGrabInteractable newInteractable))
        {
            interactionManager.SelectEnter(lastInteractor, newInteractable);
        }
    }
}
