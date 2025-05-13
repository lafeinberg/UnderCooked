using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Collider))]
public class PlateStacker : MonoBehaviour
{
    [Tooltip("Ingredient stack order from bottom to top")]
    public List<string> validBurgerTagOrder = new List<string>
    {
        "bun_bottom", "cheese", "bun_top"
    };

    //public GameManager gameManager;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = false;
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        var other = collision.collider;
        var ing = other.GetComponent<IngredientStacker>();
        if (ing == null) return;

        var root = ing.GetStackRoot();
        var stack = new List<IngredientStacker>();
        CollectStackFromRoot(root, stack);

        Debug.Log($"Burger on plate with {stack.Count} ingredients.");
        if (stack.Count >= 4) {
            PlayerManager winningPlayer = GameManager.Instance.FindLocalPlayer();
            winningPlayer.WinGame();
        }

        if (IsValidBurger(stack))
        {
            Debug.Log("Interface for Game Manager: ✅ Valid burger submitted!");
            return;

        }
        else
        {
            Debug.LogWarning("❌ Invalid burger.");
        }
    }

    void CollectStackFromRoot(IngredientStacker root, List<IngredientStacker> result)
    {
        result.Clear();

        var current = root;
        while (current != null)
        {
            result.Add(current);

            FixedJoint nextJoint = null;
            foreach (var joint in FindObjectsOfType<FixedJoint>())
            {
                if (joint.connectedBody == current.rb)
                {
                    nextJoint = joint;
                    break;
                }
            }

            if (nextJoint != null)
            {
                current = nextJoint.GetComponent<IngredientStacker>();
            }
            else
            {
                break;
            }
        }
    }

    bool IsValidBurger(List<IngredientStacker> stack)
    {
        if (stack.Count != validBurgerTagOrder.Count)
            return false;

        for (int i = 0; i < stack.Count; i++)
        {
            string expected = validBurgerTagOrder[i];
            string actualName = stack[i].name;

            if (!actualName.Contains(expected))
            {
                Debug.LogWarning($"Expected '{expected}', but got '{actualName}'");
                return false;
            }
        }

        return true;
    }
}
