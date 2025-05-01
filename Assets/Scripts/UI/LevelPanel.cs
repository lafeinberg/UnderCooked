using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelPanel : MonoBehaviour
{
    public Transform head;
    public float distance = 6.0f;
    public Vector3 heightOffset = new Vector3(0, -0.2f, 0);

    void Update()
    {
        if (head == null) return;

        Vector3 targetPosition = head.position + (head.forward * distance);
        targetPosition.y = head.position.y;

        transform.position = targetPosition + heightOffset;

        transform.LookAt(new Vector3(head.position.x, transform.position.y, head.position.z));
        transform.Rotate(0, 180, 0);
    }
}
