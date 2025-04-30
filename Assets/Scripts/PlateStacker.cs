using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Collider))]
public class PlateStacker : MonoBehaviour
{
    [Tooltip("Empty child where stacked items get parented")]
    public Transform stackRoot;

    // how tall the pile already is, in local plate-space
    private float currentLocalHeight = 0f;

    void Awake()
    {
        // make sure this collider is just the trigger
        var trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // only catch ingredients that aren’t already stacked and aren’t being grabbed
        var grab = other.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (!other.CompareTag("Ingredient") || grab.isSelected) return;

        // measure its world height (bounds.size.y) before we reparent
        var col    = other.GetComponent<Collider>();
        float h    = col.bounds.size.y;

        // parent _without_ preserving world-pos
        other.transform.SetParent(stackRoot, worldPositionStays: false);

        // position it so its bottom sits exactly on the current pile
        // pivot is at center, so local Y = half-height + currentLocalHeight
        other.transform.localPosition = new Vector3(
            0f,
            currentLocalHeight + (h * 0.5f),
            0f
        );

        // lock it down
        var rb = other.GetComponent<Rigidbody>();
        rb.isKinematic            = true;
        grab.enabled              = false;
        col.enabled               = false;   // turn off collider so it can't push itself up

        // bump the pile height for the next ingredient
        currentLocalHeight += h;
    }
}
