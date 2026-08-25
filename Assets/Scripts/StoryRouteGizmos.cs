using UnityEngine;

[ExecuteAlways]
public class StoryRouteGizmos : MonoBehaviour
{
    public Transform[] discoveryRoute;
    public Transform[] rescueRoute;
    public Transform[] escapeRoute;

    private void OnDrawGizmos()
    {
        DrawRoute(discoveryRoute, new Color(1f, 0.8f, 0.1f));
        DrawRoute(rescueRoute, new Color(0.8f, 0.25f, 1f));
        DrawRoute(escapeRoute, new Color(0.1f, 1f, 0.45f));
    }

    private static void DrawRoute(Transform[] points, Color color)
    {
        if (points == null || points.Length < 2)
            return;

        Gizmos.color = color;

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] == null || points[i + 1] == null)
                continue;

            Gizmos.DrawLine(
                points[i].position + Vector3.up * 0.15f,
                points[i + 1].position + Vector3.up * 0.15f
            );
        }
    }
}
