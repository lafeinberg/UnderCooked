using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class CookableSteak : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField] private string cookButtonTag = "Button";
    [SerializeField] private string panTag = "Pan";

    [Header("Prefabs & Thresholds")]
    [SerializeField] private ParticleSystem cookingParticlesPrefab;
    [SerializeField] private GameObject cookedSteakPrefab;
    [SerializeField] private GameObject burntSteakPrefab;
    [SerializeField] private float cookTimeThreshold = 3f;
    [SerializeField] private float burnTimeThreshold = 6f;

    [Header("Visual")]
    [SerializeField] private Gradient colorOverTime;

    private XRBaseInteractable cookButton;
    private Transform nearestPan;
    private Renderer rend;
    private Material matInstance;
    private float cookTimer;

    private InstructionType cookInstructionType = InstructionType.CookItem;
    private InstructionType dropInstructionType = InstructionType.DropItem;

    /* UI to implement later
    
    public GameObject cookTimerUI;
    public TextMeshProUGUI cookTimerText;

    */ 
    private bool isInPan;
    private bool isCooking;
    private ParticleSystem activeParticles;

    private void Awake()
    {
        // 1) Find *nearest* cook-button by tag
        var buttons = GameObject.FindGameObjectsWithTag(cookButtonTag);
        if (buttons.Length == 0)
        {
            Debug.LogError($"[CookableSteak] No GameObject found with tag '{cookButtonTag}'", this);
        }
        else
        {
            float minDist2 = float.MaxValue;
            GameObject best = null;
            foreach (var b in buttons)
            {
                float d2 = (b.transform.position - transform.position).sqrMagnitude;
                if (d2 < minDist2)
                {
                    minDist2 = d2;
                    best = b;
                }
            }
            cookButton = best.GetComponent<XRBaseInteractable>();
            Debug.Log($"[CookableSteak] Closest cookButton: {best.name}", this);
        }

        // 2) Find *nearest* pan by tag
        var pans = GameObject.FindGameObjectsWithTag(panTag);
        if (pans.Length == 0)
        {
            Debug.LogError($"[CookableSteak] No GameObject found with tag '{panTag}'", this);
        }
        else
        {
            float minDist2 = float.MaxValue;
            GameObject best = null;
            foreach (var p in pans)
            {
                float d2 = (p.transform.position - transform.position).sqrMagnitude;
                if (d2 < minDist2)
                {
                    minDist2 = d2;
                    best = p;
                }
            }
            nearestPan = best.transform;
            Debug.Log($"[CookableSteak] Closest pan: {best.name}", this);
        }

        // 3) Material instance for tinting
        rend = GetComponentInChildren<Renderer>();
        matInstance = Instantiate(rend.material);
        rend.material = matInstance;
    }

    private void OnEnable()
    {
        cookButton?.selectEntered.AddListener(OnSelectEntered);
        cookButton?.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        cookButton?.selectEntered.RemoveListener(OnSelectEntered);
        cookButton?.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        StartCooking();
    }

    private void StartCooking()
    {
        if (!isInPan || isCooking) return;

        isCooking = true;
        cookTimer = 0f;

        if (cookingParticlesPrefab != null)
        {
            activeParticles = Instantiate(
                cookingParticlesPrefab,
                transform.position,
                Quaternion.identity
            );
            activeParticles.Play();
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (isCooking) StopCookingAndFinish();
    }

    private void Update()
    {
        if (!isCooking)
        {
            // cookTimerUI.SetActive(false);
            cookTimer = 0f;
        }
        else
        {
            // cookTimerUI.SetActive(true);
            cookTimer += Time.deltaTime;
            // cookTimerText.text = $"{cookTimer}s";
            float t = Mathf.Clamp01(cookTimer / burnTimeThreshold);
            matInstance.color = colorOverTime.Evaluate(t);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // only count entering *the* nearest pan
        if (other.transform == nearestPan)
            isInPan = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform != nearestPan) return;
        isInPan = false;
        if (isCooking)
            StopCookingAndFinish();
    }

    private void StopCookingAndFinish()
    {
        isCooking = false;

        if (activeParticles != null)
        {
            activeParticles.Stop();
            Destroy(
              activeParticles.gameObject,
              activeParticles.main.duration + activeParticles.main.startLifetime.constantMax
            );
        }

        if (cookTimer >= cookTimeThreshold)
        {
            var prefab = cookTimer < burnTimeThreshold
                         ? cookedSteakPrefab
                         : burntSteakPrefab;

            Instantiate(prefab,
                        transform.position,
                        transform.rotation,
                        transform.parent);
            Destroy(gameObject);
        }
    }
}
