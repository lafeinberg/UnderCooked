using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class SliceIngredients : NetworkBehaviour
{
    [SerializeField] private NetworkObject slicedTomatoPrefab;
    [SerializeField] private int hitsToSlice = 3;
    [SerializeField] private ParticleSystem cutParticlesPrefab;

    [SerializeField] private AudioSource sliceAudioSource;
    [SerializeField] private AudioClip sliceSoundClip;


    private int hitCount = 0;
    private bool isCollidingWithKnife = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Knife") || isCollidingWithKnife)
            return;

        isCollidingWithKnife = true;
        hitCount++;

        if (sliceAudioSource != null && sliceSoundClip != null)
        {
            sliceAudioSource.pitch = Random.Range(0.95f, 1.05f); // slight variation
            sliceAudioSource.PlayOneShot(sliceSoundClip);
        }


        if (cutParticlesPrefab != null)
            SpawnCutParticles(collision.GetContact(0));

        if (hitCount >= hitsToSlice)
        {
            // Clients must ask the server to perform the slice
            if (IsOwner || IsClient) // Ensures only one request
            {
                RequestSliceServerRpc();
                PlayerManager closestPlayer = GameManager.Instance.FindLocalPlayer();
                string expected = closestPlayer.GetCurrentTargetObjectName();

                if (gameObject.name.ToLower().Contains(expected.ToLower()))
                {
                    Debug.Log($"[SliceIngredients] Successfully sliced correct ingredient: {gameObject.name}");
                    closestPlayer.PlayerNotifyActionCompleted(InstructionType.ChopItem);
                }
                else
                {
                    Debug.Log($"[SliceIngredients] Sliced object: {gameObject.name}, but expected: {expected}. Ignored.");
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Knife"))
            isCollidingWithKnife = false;
    }

    private void SpawnCutParticles(ContactPoint contact)
    {
        var ps = Instantiate(
            cutParticlesPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );

        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSliceServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsHost) return;

        

        var sliced = Instantiate(
            slicedTomatoPrefab,
            transform.position,
            transform.rotation,
            transform.parent
        );

        sliced.Spawn(); // Makes it appear on all clients

        Destroy(gameObject);
    }
}
