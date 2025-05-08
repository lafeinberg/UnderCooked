using UnityEngine;

public class FollowXZ : MonoBehaviour
{
    [SerializeField] private Transform source;

    private void Update()
    {
        Vector3 p = transform.position;
        p.x = source.position.x;
        p.z = source.position.z;
        transform.position = p;
    }
}
