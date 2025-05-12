using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabNotifier : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("GrabbableNotifier requires XRGrabInteractable.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnDropped);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnDropped);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"[GrabbableNotifier] Object {gameObject.name} grabbed!");

        PlayerManager closestPlayer = GameManager.Instance.FindPlayerByObject();

        if (closestPlayer == null)
            return;

        var expected = closestPlayer.GetCurrentTargetObjectName();
        if (gameObject.name.ToLower().Contains(expected.ToLower()))
        {
            Debug.Log($"[GrabbableNotifier] Grabbed correct target: {gameObject.name}");
            closestPlayer.PlayerNotifyActionCompleted(InstructionType.GrabItem);
        }
        else
        {
            Debug.Log($"[GrabbableNotifier] Grabbed WRONG object: {gameObject.name}, expected: {expected}");
        }
    }

    private void OnDropped(SelectExitEventArgs args)
    {
        Debug.Log($"[GrabbableNotifier] Object {gameObject.name} dropped!");
        
        PlayerManager closestPlayer = GameManager.Instance.FindPlayerByObject();

        if (closestPlayer == null)
            return;

        var expected = closestPlayer.GetCurrentTargetObjectName();
        if (gameObject.name.ToLower().Contains(expected.ToLower()))
        {
            Debug.Log($"[GrabbableNotifier] Dropped correct target (contains match): {gameObject.name}");
            closestPlayer.PlayerNotifyActionCompleted(InstructionType.DropItem);
        }
        else
        {
            Debug.Log($"[GrabbableNotifier] Dropped WRONG object: {gameObject.name}, expected keyword: {expected}");
        }
    }
}
