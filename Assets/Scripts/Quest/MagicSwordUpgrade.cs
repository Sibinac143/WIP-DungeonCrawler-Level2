using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MagicSwordUpgrade : MonoBehaviour
{
    [SerializeField] private SwordDamageDealer damageDealer;
    [SerializeField] private GameObject originalSwordVisualRoot;
    [SerializeField] private GameObject magicSwordVisual;

    [Header("Magic sword combat")]
    [SerializeField, Min(0f)] private float lightDamage = 70f;
    [SerializeField, Min(0f)] private float heavyDamage = 115f;
    [SerializeField, Min(0.2f)] private float attackRange = 2.8f;
    [SerializeField, Min(0.05f)] private float attackRadius = 0.95f;

    private Renderer[] originalRenderers;
    private Collider[] originalColliders;

    public bool IsEquipped =>
        magicSwordVisual != null &&
        magicSwordVisual.activeSelf;

    public void Configure(
        SwordDamageDealer dealer,
        GameObject oldVisual,
        GameObject newVisual)
    {
        damageDealer = dealer;
        originalSwordVisualRoot = oldVisual;
        magicSwordVisual = newVisual;

        CacheOriginalParts();
        SetEquipped(false);
    }

    private void Awake()
    {
        CacheOriginalParts();
    }

    public void SetEquipped(bool equipped)
    {
        CacheOriginalParts();

        if (originalRenderers != null)
        {
            foreach (Renderer renderer in
                     originalRenderers)
            {
                if (renderer != null)
                    renderer.enabled = !equipped;
            }
        }

        if (originalColliders != null)
        {
            foreach (Collider collider in
                     originalColliders)
            {
                if (collider != null)
                    collider.enabled = !equipped;
            }
        }

        if (magicSwordVisual != null)
            magicSwordVisual.SetActive(equipped);

        if (damageDealer == null)
            return;

        if (equipped)
        {
            damageDealer.SetDamageValues(
                lightDamage,
                heavyDamage,
                attackRange,
                attackRadius);
        }
        else
        {
            damageDealer.RestoreDefaultDamageValues();
        }
    }

    private void CacheOriginalParts()
    {
        if (originalSwordVisualRoot == null)
            return;

        List<Renderer> renderers =
            new List<Renderer>();

        foreach (Renderer renderer in
                 originalSwordVisualRoot.GetComponentsInChildren<Renderer>(
                     true))
        {
            if (magicSwordVisual != null &&
                renderer.transform.IsChildOf(
                    magicSwordVisual.transform))
            {
                continue;
            }

            renderers.Add(renderer);
        }

        List<Collider> colliders =
            new List<Collider>();

        foreach (Collider collider in
                 originalSwordVisualRoot.GetComponentsInChildren<Collider>(
                     true))
        {
            if (magicSwordVisual != null &&
                collider.transform.IsChildOf(
                    magicSwordVisual.transform))
            {
                continue;
            }

            colliders.Add(collider);
        }

        originalRenderers = renderers.ToArray();
        originalColliders = colliders.ToArray();
    }
}
