using UnityEngine;


[RequireComponent(typeof(Collider), typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class StackIngredient : MonoBehaviour
{
    [Tooltip("Tag used by the plate and by any placed ingredient.")]
    public string validTargetTag = "IngredientSurface";

    [Tooltip("How ‘upward’ the collision normal must be (1 = perfectly up).")]
    [Range(0f, 1f)]
    public float upwardDotThreshold = 0.7f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    Rigidbody         _rb;
    MeshCollider      _meshCol;
    bool              _isLocked = false;

    void Awake()
    {
        _grab    = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        _rb      = GetComponent<Rigidbody>();
        // assume your real geometry lives in a MeshCollider on a child called "Visuals"
        _meshCol = GetComponentInChildren<MeshCollider>();
        if (_meshCol == null)
            Debug.LogError($"[{name}] No MeshCollider found in children!");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_isLocked) return;

        var hitCol = collision.collider;
        // only snap to things tagged "IngredientSurface"
        // (plate root and any ingredient you retag after snapping)
        if (!hitCol.CompareTag(validTargetTag))
            return;

        // require at least one contact from above
        bool fromAbove = false;
        foreach (var contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) >= upwardDotThreshold)
            {
                fromAbove = true;
                break;
            }
        }
        if (!fromAbove) return;

        // 1) calculate the exact Y where bottom of this mesh should sit
        float targetTopY = hitCol.bounds.max.y;
        float myHalfH    = _meshCol.bounds.extents.y;

        Vector3 newPos = transform.position;
        newPos.y = targetTopY + myHalfH;
        transform.position = newPos;  // only adjust height

        // 2) **always** parent to the **plate root** so everything moves together
        //    transform.root returns the top‐most GameObject in this hierarchy
        var plateRoot = hitCol.transform.root;
        transform.SetParent(plateRoot, worldPositionStays: true);

        // 3) lock it down
        _grab.enabled = false;
        _rb.isKinematic = true;
        gameObject.tag  = validTargetTag;  // so the next ingredient can land on you
        _isLocked       = true;
    }
}
