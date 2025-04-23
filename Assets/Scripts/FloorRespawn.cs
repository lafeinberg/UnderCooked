// attach to objects so they will respawn when hitting ground
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class RespawnOnGround : MonoBehaviour
{
    public string groundTag = "Floor";

    Vector3 _startPos;
    Quaternion _startRot;
    Rigidbody _rb;

    void Awake()
    {
        // cache start transform & rigidbody
        _startPos = transform.position;
        _startRot = transform.rotation;
        _rb       = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag(groundTag))
            ResetToStart();
    }

    void ResetToStart()
    {
        // stop all motion
        _rb.velocity        = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // teleport back
        transform.position = _startPos;
        transform.rotation = _startRot;
    }
}
