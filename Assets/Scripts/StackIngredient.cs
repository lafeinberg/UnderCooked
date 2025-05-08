using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class IngredientStacker : MonoBehaviour
{
    public Transform snapPoint;
    public LayerMask stackableLayer;
    public float snapRadius = 0.05f;

    XRGrabInteractable grabInteractable;
    Rigidbody rb;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs args)
        => TrySnap();

    void TrySnap()
    {
        Collider[] hits = Physics.OverlapSphere(snapPoint.position, snapRadius, stackableLayer);
        foreach (var col in hits)
        {
            var other = col.GetComponentInParent<IngredientStacker>();
            if (other != null && other != this)
            {
                SnapOnto(other);
                return;
            }
        }
    }

    void SnapOnto(IngredientStacker target)
    {
        var joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = target.rb;
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;

        transform.position = target.snapPoint.position;
        transform.rotation = target.snapPoint.rotation;

        grabInteractable.enabled = false;

        // Ensure the root ALWAYS has an XRGrabInteractable and no others do.
        var root = target.GetStackRoot();
        foreach (var ing in root.GetComponentsInChildren<IngredientStacker>())
        {
            if (ing != root && ing.GetComponent<XRGrabInteractable>() != null)
            {
                Destroy(ing.GetComponent<XRGrabInteractable>());
            }
        }
        if (root.GetComponent<XRGrabInteractable>() == null)
        {
            root.gameObject.AddComponent<XRGrabInteractable>();
        }
    }
}

public static class StackExtensions
{
    public static IngredientStacker GetStackRoot(this IngredientStacker ing)
    {
        var visited = new HashSet<IngredientStacker>();
        var current = ing;
        while (true)
        {
            bool foundParent = false;
            foreach (var joint in current.GetComponents<FixedJoint>())
            {
                var parentIng = joint.connectedBody.GetComponent<IngredientStacker>();
                if (parentIng != null && visited.Add(parentIng))
                {
                    current = parentIng;
                    foundParent = true;
                    break;
                }
            }
            if (!foundParent) break;
        }
        return current;
    }
}