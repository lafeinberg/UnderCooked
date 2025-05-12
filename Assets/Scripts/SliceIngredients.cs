using UnityEngine;
using XRMultiplayer;

[RequireComponent(typeof(Collider))]
public class SliceIngredients : MonoBehaviour
{
    [SerializeField] private GameObject slicedTomatoPrefab;
    [SerializeField] private int hitsToSlice = 3;
    [SerializeField] private ParticleSystem cutParticlesPrefab;

    private int hitCount = 0;
    private bool isCollidingWithKnife = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Knife") || isCollidingWithKnife)
            return;

        isCollidingWithKnife = true;
        hitCount++;

        if (cutParticlesPrefab != null)
            SpawnCutParticles(collision.GetContact(0));

        if (hitCount >= hitsToSlice)
            SliceTomato();
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

    private void SliceTomato()
    {
        PlayerManager closestPlayer = GameManager.Instance.FindPlayerByObject();
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

        Instantiate(
            slicedTomatoPrefab,
            transform.position,
            transform.rotation,
            transform.parent
        );
        Destroy(gameObject);
    }
}
