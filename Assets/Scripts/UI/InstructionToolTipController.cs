using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InstructionToolbar : MonoBehaviour
{
    public RectTransform panelRect;
    public TextMeshProUGUI instructionText;

    public float slideDuration = 0.4f;
    public float slideOffset = 200f;

    private Vector2 originalPos;
    private Coroutine currentAnim;

    private string[] testInstructions = {
    "Go to the fridge",
    "Grab the lettuce",
    "Toast the bun",
    "Assemble the burger"
    };


    void Awake()
    {
        originalPos = panelRect.anchoredPosition;
    }

    public void ActivateInstructionToolbar(Instruction firstInstruction)
    {
        Debug.Log("activating tooltip");
        gameObject.SetActive(true);
        ShowInstruction(firstInstruction);
    }

    /*
    * Shows new current instruction string and animates new instruction into scene
    */
    public void ShowInstruction(Instruction instruction)
    {
        gameObject.SetActive(true);
        panelRect.anchoredPosition = new Vector2(originalPos.x, originalPos.y + slideOffset);
        instructionText.text = instruction.description.ToString();

        Debug.Log($"Current instruction: {instructionText.text}");

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideIn());
    }

    public void HideInstruction()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideOut());
    }

    /* Helpers */

    private IEnumerator SlideIn()
    {
        Vector2 startPos = new Vector2(originalPos.x, originalPos.y + slideOffset);
        Vector2 endPos = originalPos;

        float t = 0f;
        panelRect.anchoredPosition = startPos;

        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        panelRect.anchoredPosition = endPos;
    }

    private IEnumerator SlideOut()
    {
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 endPos = new Vector2(originalPos.x, originalPos.y + slideOffset);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        panelRect.anchoredPosition = endPos;
    }

    /*
    private IEnumerator TestInstructionLoop()
    {
        while (true)
        {
            ShowInstruction(testInstructions[currentInstruction]);
            currentInstruction = (currentInstruction + 1) % testInstructions.Length;

            yield return new WaitForSeconds(5f);

            HideInstruction();

            yield return new WaitForSeconds(1f);
        }
    }
    */
}
