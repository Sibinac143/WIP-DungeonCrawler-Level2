using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatWorldHealthBar : MonoBehaviour
{
    [SerializeField] private CombatHealth health;
    [SerializeField] private bool alwaysVisible;
    [SerializeField, Min(1f)] private float maximumDistance = 70f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);

    private Renderer[] renderers;

    public void Configure(
        CombatHealth healthReference,
        bool showAlways)
    {
        health = healthReference;
        alwaysVisible = showAlways;
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnGUI()
    {
        if (health == null ||
            health.IsDead ||
            (!alwaysVisible &&
             health.CurrentHealth >= health.MaxHealth))
        {
            return;
        }

        Camera camera = Camera.main;

        if (camera == null)
            return;

        Vector3 worldPosition =
            CalculateTopPosition() + worldOffset;

        if (Vector3.Distance(
                camera.transform.position,
                worldPosition) > maximumDistance)
        {
            return;
        }

        Vector3 screen =
            camera.WorldToScreenPoint(worldPosition);

        if (screen.z <= 0f)
            return;

        const float width = 120f;
        const float height = 12f;

        Rect background = new Rect(
            screen.x - width * 0.5f,
            Screen.height - screen.y,
            width,
            height);

        Color original = GUI.color;

        GUI.color = new Color(0.15f, 0.02f, 0.02f, 0.9f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        GUI.color = new Color(0.15f, 0.8f, 0.2f, 0.95f);
        GUI.DrawTexture(
            new Rect(
                background.x,
                background.y,
                background.width * health.NormalizedHealth,
                background.height),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(
            new Rect(
                background.x,
                background.y - 20f,
                background.width,
                20f),
            gameObject.name);

        GUI.color = original;
    }

    private Vector3 CalculateTopPosition()
    {
        if (renderers == null || renderers.Length == 0)
            return transform.position + Vector3.up * 2f;

        Bounds bounds = renderers[0].bounds;

        for (int index = 1;
             index < renderers.Length;
             index++)
        {
            if (renderers[index] != null)
                bounds.Encapsulate(renderers[index].bounds);
        }

        return new Vector3(
            bounds.center.x,
            bounds.max.y,
            bounds.center.z);
    }
}
