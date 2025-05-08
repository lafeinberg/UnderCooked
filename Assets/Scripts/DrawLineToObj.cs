using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class DrawLineToObj : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navSurface;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform[] targets;
    [SerializeField] private LineRenderer lineRenderer;

    private NavMeshPath navPath;

    private IEnumerator Start()
    {
        if (navSurface != null)
        {
            Debug.Log("built NavMesh");
        }
        else
        {
            Debug.LogError("no NavMeshSurface assigned");
        }
        yield return null;
    }

    private void Update() {
        if (targets == null || targets.Length == 0 || targets[0] == null) {
            return;
        }
        DrawPathToTarget(targets[0]);  
    }

    private void DrawPathToTarget(Transform target)
    {
        if (!NavMesh.SamplePosition(playerTransform.position, out var startHit, 1f, NavMesh.AllAreas))
        {
            Debug.LogError("player start off NavMesh");
            return;
        }

        if (!NavMesh.SamplePosition(target.position, out var endHit, 1f, NavMesh.AllAreas))
        {
            Debug.LogError("Target is off the NavMesh");
            return;
        }

        navPath = new NavMeshPath();
        bool pathFound = NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, navPath);
        if (!pathFound || navPath.corners.Length < 2)
        {
            Debug.LogError("no valid path to target");
            lineRenderer.positionCount = 0;
            return;
        }

        // draw it
        lineRenderer.positionCount = navPath.corners.Length;
        for (int i = 0; i < navPath.corners.Length; i++)
            lineRenderer.SetPosition(i, navPath.corners[i] + Vector3.up * 0.02f);

        Debug.Log($"Drew path to {target.name} with {navPath.corners.Length} corners");
    }
}
