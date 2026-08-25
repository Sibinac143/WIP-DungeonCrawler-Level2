using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageFlash : MonoBehaviour
{
    [SerializeField] private CombatHealth health;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor =
        new Color(1f, 0.18f, 0.12f, 1f);

    private Renderer[] renderers;
    private Coroutine flashRoutine;
    private MaterialPropertyBlock block;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

        renderers =
            GetComponentsInChildren<Renderer>(true);

        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

        if (health != null)
            health.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Damaged -= HandleDamaged;

        ClearFlash();
    }

    private void HandleDamaged(
        CombatHealth damagedHealth,
        float amount,
        GameObject source)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine =
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, flashColor);
            block.SetColor(ColorId, flashColor);
            renderer.SetPropertyBlock(block);
        }

        yield return new WaitForSeconds(flashDuration);

        ClearFlash();
        flashRoutine = null;
    }

    private void ClearFlash()
    {
        if (renderers == null)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.SetPropertyBlock(null);
        }
    }
}
