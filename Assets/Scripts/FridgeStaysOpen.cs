using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
public class FridgeStaysOpen : MonoBehaviour
{
    [Tooltip("Angle (°) at which the door locks open")]
    [SerializeField] private float openHoldAngle = 90f;
    [Tooltip("Speed (°/s) the door is driven closed when below hold angle")]
    [SerializeField] private float closeSpeed = 120f;
    [Tooltip("Max torque the motor can apply when holding or closing")]
    [SerializeField] private float motorForce = 1000f;

    private HingeJoint _hinge;
    private JointMotor _motor;

    void Awake()
    {
        _hinge = GetComponent<HingeJoint>();

        // clamp between 0 and openHoldAngle
        _hinge.useLimits = true;
        var lim = _hinge.limits;
        lim.min = 0f;
        lim.max = openHoldAngle;
        _hinge.limits = lim;

        // prepare the motor
        _motor = _hinge.motor;
        _hinge.useMotor = true;
    }

    void FixedUpdate()
    {
        float angle = _hinge.angle;  
        // above the hold threshold → lock in place
        if (angle >= openHoldAngle - 0.1f)
        {
            _motor.targetVelocity = 0f;
            _motor.force = motorForce;
        }
        // below the hold threshold → drive it closed
        else
        {
            _motor.targetVelocity = -closeSpeed;
            _motor.force = motorForce;
        }
        _hinge.motor = _motor;
    }
}
