using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;

public class CookNotifier : MonoBehaviour
{
    public void OnCookButtonPressed()
    {
        Debug.Log("Cook Item Notified.");
        PlayerManager closestPlayer = GameManager.Instance.FindLocalPlayer();
        closestPlayer.PlayerNotifyActionCompleted(InstructionType.CookItem);
    }
}
