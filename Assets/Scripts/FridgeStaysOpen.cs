using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
public class FridgeStaysOpen : MonoBehaviour
{
    [SerializeField] private float brakeForce = 10f;

    private HingeJoint _hinge;

    void Awake()
    {
        _hinge = GetComponent<HingeJoint>();

        var m = _hinge.motor;
        m.force = brakeForce;
        m.targetVelocity = 0f;
        m.freeSpin = false;
        _hinge.motor = m;
        _hinge.useMotor = true;
    }

}
