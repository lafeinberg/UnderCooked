using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TomatoSliceOnHits : MonoBehaviour
{
    [Header("Assign via Inspector")]
    [Tooltip("Drag your Knife GameObject here")]
    [SerializeField] private GameObject knifeObject;

    [Tooltip("Drag your Sliced Tomato prefab here")]
    [SerializeField] private GameObject slicedTomatoPrefab;

    [Tooltip("How many knife-hits before slicing")]
    [SerializeField] private int hitsToSlice = 3;

    private int hitCount = 0;
    private bool isCollidingWithKnife = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == knifeObject && !isCollidingWithKnife)
        {
            isCollidingWithKnife = true;
            hitCount++;

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
