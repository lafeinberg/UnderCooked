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



    private void OnCollisionEnter(Collision collision)
    {
        var other = collision.collider;
        var ing = other.GetComponent<IngredientStacker>();
        if (ing == null) return;

        var root = ing.GetStackRoot();
        var stack = new HashSet<IngredientStacker>();
        //CollectStackFromRoot(root, stack);
        stack = CollectStackFromRoot(root);

        Debug.Log($"Burger on plate with {stack.Count} ingredients.");

        if (IsValidBurger(stack))
        {
            Debug.Log("Interface for Game Manager: ✅ Valid burger submitted!");
            //gameManager?.SubmitBurger(stack);
            PlayerManager closestPlayer = GameManager.Instance.FindLocalPlayer();
            closestPlayer.WinGame();
        }
        else
        {
            Debug.LogWarning("❌ Invalid burger.");
        }
    }

    HashSet<IngredientStacker> CollectStackFromRoot(IngredientStacker root)
    {
        var visited = new HashSet<IngredientStacker>();
        DFS(root, visited);
        return visited;
    }

    void DFS(IngredientStacker current, HashSet<IngredientStacker> visited)
    {
        if (current == null || !visited.Add(current))
            return;

        foreach (var joint in FindObjectsOfType<FixedJoint>())
        {
            if (joint.connectedBody == current.rb)
            {
                var child = joint.GetComponent<IngredientStacker>();
                DFS(child, visited);
            }
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

    bool IsValidBurger(HashSet<IngredientStacker> stack)
    {
        foreach (var requiredTag in validBurgerTagOrder)
        {
            bool found = false;

            foreach (var item in stack)
            {
                if (item.name.Contains(requiredTag))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"Missing required ingredient: {requiredTag}");
                return false;
            }
        }

        return true;
    }
}
