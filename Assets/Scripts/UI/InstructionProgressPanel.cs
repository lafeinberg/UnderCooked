using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InstructionProgressPanel : MonoBehaviour
{
    public GameObject instructionItemPrefab;
    public Transform contentArea;

    private List<GameObject> instructionItems = new();

    public void SetupInstructions(List<Instruction> instructions)
    {
        foreach (var item in instructionItems)
            Destroy(item);
        instructionItems.Clear();

        for (int i = 0; i < instructions.Count; i++)
        {
            var item = Instantiate(instructionItemPrefab, contentArea);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            text.text = $"{i + 1}. {instructions[i].description}";
            instructionItems.Add(item);
        }
    }

    // true = me, false = opponent
    public void MarkInstructionComplete(int index, bool isSelf)
    {
        if (index >= 0 && index < instructionItems.Count)
        {
            var item = instructionItems[index];

            if (isSelf)
            {
                var selfIcon = item.transform.Find("PlayerCheck").GetComponent<Image>();
                selfIcon.color = Color.green;
            }
            else
            {
                var oppIcon = item.transform.Find("OpponentCheck").GetComponent<Image>();
                oppIcon.color = Color.green;
            }
        }
    }
}