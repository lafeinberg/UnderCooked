using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SliceIngredients : MonoBehaviour
{
    [SerializeField] private GameObject knifeObject;
    [SerializeField] private GameObject slicedTomatoPrefab;
    [SerializeField] private int hitsToSlice = 3;
    [Tooltip("Particle system to play on each registered cut")]
    [SerializeField] private ParticleSystem cutParticles;

    private int hitCount = 0;
    private bool isCollidingWithKnife = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == knifeObject && !isCollidingWithKnife)
        {
            isCollidingWithKnife = true;
            hitCount++;

            if (cutParticles != null)
                cutParticles.Play();

            if (hitCount >= hitsToSlice)
                SliceTomato();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == knifeObject)
            isCollidingWithKnife = false;
    }

    private void SliceTomato()
    {
        Instantiate(
            slicedTomatoPrefab,
            transform.position,
            transform.rotation,
            transform.parent
        );

        Destroy(gameObject);
    }
}
