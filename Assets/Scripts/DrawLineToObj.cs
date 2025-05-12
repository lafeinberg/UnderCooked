using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
public class DrawLineToObj : NetworkBehaviour
{
    [SerializeField] private NavMeshSurface navSurface;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform[] targets;
    [SerializeField] private LineRenderer lineRenderer;
    public int current = -1;

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

    private void Update()
    {
        if (!IsHost)
        {
            return;
        }
        if (current < 0) return;
        if (targets == null || targets.Length == 0 || targets[0] == null)
        {
            return;
        }

        DrawPathToTarget(targets[current]);
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
        //lineRenderer.positionCount = navPath.corners.Length;
        //for (int i = 0; i < navPath.corners.Length; i++)
        //    lineRenderer.SetPosition(i, navPath.corners[i] + Vector3.up * 0.02f);

        List<Vector3> smoothPath = GetSmoothPath(navPath.corners);
        lineRenderer.positionCount = smoothPath.Count;
        lineRenderer.SetPositions(smoothPath.ToArray());
    }

    List<Vector3> GetSmoothPath(Vector3[] corners)
    {
        List<Vector3> smooth = new List<Vector3>();

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 p0 = i > 0 ? corners[i - 1] : corners[i];
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[i + 1];
            Vector3 p3 = (i + 2 < corners.Length) ? corners[i + 2] : p2;

            for (int j = 0; j <= 10; j++) // 每段插值 10 个点
            {
                float t = j / 10f;
                Vector3 pos = 0.5f * (
                    2 * p1 +
                    (-p0 + p2) * t +
                    (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t +
                    (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t
                );
                pos += Vector3.up * 0.02f;
                smooth.Add(pos);
            }
        }
        return smooth;
    }

    public void SetTarget(int i)
    {
        if (i < 0 || i > 2) return;
        current = i;
        Debug.Log($"[DrawLineToObj] SetTarget called with index {i}");
    }
    public void ClearPath()
    {
        Debug.Log("[DrawLineToObj] ClearPath called");
        current = -1;
        lineRenderer.positionCount = 0;
    }

    public bool ReachedTarget(float threshold = 1f)
    {
        if (current < 0 || targets.Length <= current)
            return false;

        bool reach = Vector3.Distance(playerTransform.position, targets[current].position) <= threshold;
        Debug.Log($"[DrawLineToObj] Isreach: {reach}");
        return reach;
    }
}
