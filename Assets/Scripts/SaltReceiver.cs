using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaltReceiver : MonoBehaviour
{
    public float saltAmount = 0f;
    public bool canReceiveSalt = true;

    public GameObject saltBallPrefab;
    public Transform saltContainer;

    private InstructionType instructionType = InstructionType.AddCondiment;

    public void AddSalt(float amount)
    {
        Debug.Log($"AddSalt called!");
        if (!canReceiveSalt) return;

        saltAmount += amount;
        for (int i = 0; i < amount * 5; i++)
        {
            Vector3 localPos = new Vector3(
                Random.Range(-0.3f, 0.3f),
                0.1f,
                Random.Range(-0.3f, 0.3f)
            );

            GameObject salt = Instantiate(saltBallPrefab, saltContainer);
            salt.transform.localPosition = localPos;
        }
        Debug.Log($"{name} received salt! Total: {saltAmount}");

        if (saltAmount >= 3)
        {
            Debug.Log($"[SaltReceiver] Finished salting target: {name}");

            PlayerManager closestPlayer = GameManager.Instance.FindLocalPlayer();
            closestPlayer.PlayerNotifyActionCompleted(instructionType);
        }

    }
}
