using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportOnConnection : MonoBehaviour
{
    // Start is called before the first frame update

    [Header("XR Rig to Teleport")]
    public Transform xrRig;

    [Header("Target World Position")]
    public Vector3 teleportPosition = Vector3.zero;

    void OnEnable()
    {
        Invoke(nameof(Teleport), 0.1f);
    }

    void Teleport()
    {
        if (xrRig == null)
        {
            Debug.LogWarning("XR Rig not assigned!");
            return;
        }

        xrRig.position = teleportPosition;
    }
}
