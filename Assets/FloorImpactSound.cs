using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FloorImpactSound : MonoBehaviour
{
    public AudioClip bangClip;
    public float impactThreshold = 2f;

    private AudioSource audioSource;

    // Tags to react to
    private readonly string[] allowedTags = { "Ingredient", "Pan", "Knife", "Tool" };

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only react if the tag matches
        string tag = collision.gameObject.tag;
        if (!System.Array.Exists(allowedTags, t => t == tag))
            return;

        // Require a Rigidbody (and not something huge or static)
        Rigidbody rb = collision.rigidbody;
        if (rb == null || rb.isKinematic)
            return;

        if (collision.relativeVelocity.magnitude < impactThreshold)
            return;

        // Play the sound
        if (bangClip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f); // optional variation
            audioSource.PlayOneShot(bangClip);
        }
    }
}
