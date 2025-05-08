using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class CookableSteak : MonoBehaviour
{
    [SerializeField] private XRBaseInteractable cookButton;
    [SerializeField] private string panTag = "Pan";
    [SerializeField] private float cookTimeThreshold = 3f;
    [SerializeField] private float burnTimeThreshold = 6f;
    [SerializeField] private GameObject cookedSteakPrefab;
    [SerializeField] private GameObject burntSteakPrefab;
    [SerializeField] private ParticleSystem cookingParticles;
    [SerializeField] private Gradient colorOverTime;

    private Renderer rend;
    private Material matInstance;
    private float cookTimer;
    private bool isInPan;
    private bool isCooking;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        matInstance = Instantiate(rend.material);
        rend.material = matInstance;
    }

    private void OnEnable()
    {
        cookButton.activated.AddListener(OnActivated);
        cookButton.deactivated.AddListener(OnDeactivated);
    }

    private void OnDisable()
    {
        cookButton.activated.AddListener(OnActivated);
        cookButton.deactivated.AddListener(OnDeactivated);
    }

    private void Update()
    {
        if (!isCooking) return;
        cookTimer += Time.deltaTime;
        float t = Mathf.Clamp01(cookTimer / burnTimeThreshold);
        matInstance.color = colorOverTime.Evaluate(t);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (isInPan && !isCooking)
        {
            isCooking = true;
            cookingParticles?.Play();
        }
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        if (isCooking)
        {
            isCooking = false;
            cookingParticles?.Stop();
            FinishCooking();
        }
    }

    private void FinishCooking()
    {
        if (cookTimer < cookTimeThreshold) return;
        GameObject toSpawn = cookTimer < burnTimeThreshold ? cookedSteakPrefab : burntSteakPrefab;
        Instantiate(toSpawn, transform.position, transform.rotation, transform.parent);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(panTag)) isInPan = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(panTag))
        {
            isInPan = false;
            if (isCooking)
            {
                isCooking = false;
                cookingParticles?.Stop();
                FinishCooking();
            }
        }
    }
}
