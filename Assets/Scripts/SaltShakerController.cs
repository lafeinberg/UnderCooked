using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SaltShakerController : MonoBehaviour
{
    public float verticalSpeedThreshold = 0.3f; // shaking threshold
    public ParticleSystem saltParticleSystem;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    public Transform saltCheckPoint;
    public float saltRange = 0.1f;

    public AudioSource saltAudioSource;
    public AudioClip saltShakeClip;



    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (saltParticleSystem == null)
        {
            Debug.LogWarning("Salt particle system not assigned!");
        }

        if (saltAudioSource != null)
        {
            saltAudioSource.clip = saltShakeClip;
            saltAudioSource.loop = true;
            saltAudioSource.playOnAwake = false;
            saltAudioSource.spatialBlend = 1f; // 3D audio for realism
        }
    }
    

    void Update()
    {
        if (grabInteractable.isSelected && saltParticleSystem != null)
        {
            float verticalSpeed = Mathf.Abs(rb.velocity.y);

            if (verticalSpeed > verticalSpeedThreshold)
            {
                if (!saltParticleSystem.isPlaying)
                {
                    saltParticleSystem.Play();
                    TrySaltTarget();
                }

                if (saltAudioSource != null && !saltAudioSource.isPlaying)
                {
                    saltAudioSource.Play();
                }
            }
            else
            {
                if (saltParticleSystem.isPlaying)
                    saltParticleSystem.Stop();

                if (saltAudioSource != null && saltAudioSource.isPlaying)
                    saltAudioSource.Stop();
            }
        }
        else
        {
            if (saltParticleSystem != null && saltParticleSystem.isPlaying)
                saltParticleSystem.Stop();

            if (saltAudioSource != null && saltAudioSource.isPlaying)
                saltAudioSource.Stop();
        }
    }


    void TrySaltTarget()
    {
        Debug.Log($"TrySalt called!");
        Collider[] hits = Physics.OverlapSphere(saltCheckPoint.position, saltRange);

        foreach (var hit in hits)
        {
            Debug.Log($"[SaltShaker] Overlap hit: {hit.name}");
            if (hit.TryGetComponent(out SaltReceiver receiver))
            {
                receiver.AddSalt(1f); 
            }
        }
    }
}
