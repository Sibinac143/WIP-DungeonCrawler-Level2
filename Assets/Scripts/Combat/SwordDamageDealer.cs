using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class SwordDamageDealer : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private CombatHealth ownerHealth;

    [Header("Optional third-person melee origin")]
    [SerializeField] private Transform meleeOrigin;
    [SerializeField] private bool listenForInput = true;

    [Header("Light Attack")]
    [SerializeField, Min(0f)] private float lightDamage = 25f;
    [SerializeField, Min(0.05f)] private float lightCooldown = 0.55f;

    [Header("Heavy Attack")]
    [SerializeField, Min(0f)] private float heavyDamage = 45f;
    [SerializeField, Min(0.05f)] private float heavyCooldown = 1.05f;

    [Header("Hit Detection")]
    [SerializeField, Min(0.2f)] private float attackRange = 2.35f;
    [SerializeField, Min(0.05f)] private float attackRadius = 0.82f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private bool drawDebugGizmos = true;

    private float nextAttackTime;

    private float defaultLightDamage;
    private float defaultHeavyDamage;
    private float defaultAttackRange;
    private float defaultAttackRadius;
    private bool defaultsCached;

    private readonly HashSet<CombatHealth> hitTargets =
        new HashSet<CombatHealth>();

    public void Configure(
        Camera cameraReference,
        CombatHealth healthReference)
    {
        viewCamera = cameraReference;
        ownerHealth = healthReference;
    }

    public void ConfigureMeleeOrigin(
        Transform origin,
        bool shouldListenForInput)
    {
        meleeOrigin = origin;
        listenForInput = shouldListenForInput;
    }

    public void ClearMeleeOrigin(
        bool shouldListenForInput)
    {
        meleeOrigin = null;
        listenForInput = shouldListenForInput;
    }

    public void SetInputHandling(bool enabledState)
    {
        listenForInput = enabledState;
    }

    public void SetDamageValues(
        float newLightDamage,
        float newHeavyDamage,
        float newAttackRange,
        float newAttackRadius)
    {
        CacheDefaultDamageValues();

        lightDamage = Mathf.Max(0f, newLightDamage);
        heavyDamage = Mathf.Max(0f, newHeavyDamage);
        attackRange = Mathf.Max(0.2f, newAttackRange);
        attackRadius = Mathf.Max(0.05f, newAttackRadius);
    }

    public void RestoreDefaultDamageValues()
    {
        CacheDefaultDamageValues();

        lightDamage = defaultLightDamage;
        heavyDamage = defaultHeavyDamage;
        attackRange = defaultAttackRange;
        attackRadius = defaultAttackRadius;
    }

    private void CacheDefaultDamageValues()
    {
        if (defaultsCached)
            return;

        defaultLightDamage = lightDamage;
        defaultHeavyDamage = heavyDamage;
        defaultAttackRange = attackRange;
        defaultAttackRadius = attackRadius;
        defaultsCached = true;
    }

    private void Awake()
    {
        CacheDefaultDamageValues();

        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>(true);

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (ownerHealth == null)
            ownerHealth = GetComponent<CombatHealth>();
    }

    private void Update()
    {
        if (!listenForInput)
            return;

        if (ownerHealth != null && ownerHealth.IsDead)
            return;

#if ENABLE_INPUT_SYSTEM
        bool lightPressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        bool heavyPressed =
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;
#else
        bool lightPressed = Input.GetMouseButtonDown(0);
        bool heavyPressed = Input.GetKeyDown(KeyCode.E);
#endif

        if (lightPressed)
            TryLightAttack();

        if (heavyPressed)
            TryHeavyAttack();
    }

    public void TryLightAttack()
    {
        TryAttack(lightDamage, lightCooldown);
    }

    public void TryHeavyAttack()
    {
        TryAttack(heavyDamage, heavyCooldown);
    }

    private void TryAttack(float damage, float cooldown)
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + cooldown;
        hitTargets.Clear();

        if (meleeOrigin != null)
        {
            PerformThirdPersonMelee(damage);
            return;
        }

        PerformCameraMelee(damage);
    }

    private void PerformThirdPersonMelee(float damage)
    {
        Vector3 origin = meleeOrigin.position;
        Vector3 direction =
            Vector3.ProjectOnPlane(
                meleeOrigin.forward,
                Vector3.up).normalized;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        Vector3 end =
            origin + direction * attackRange;

        Collider[] hits =
            Physics.OverlapCapsule(
                origin,
                end,
                attackRadius,
                hitLayers,
                QueryTriggerInteraction.Ignore);

        foreach (Collider collider in hits)
            ProcessCollider(collider, damage);
    }

    private void PerformCameraMelee(float damage)
    {
        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera == null)
            return;

        Vector3 origin =
            viewCamera.transform.position;

        Vector3 direction =
            viewCamera.transform.forward;

        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                attackRadius,
                direction,
                attackRange,
                hitLayers,
                QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
            ProcessCollider(hit.collider, damage);

        Vector3 endPoint =
            origin + direction * attackRange;

        Collider[] endHits =
            Physics.OverlapSphere(
                endPoint,
                attackRadius,
                hitLayers,
                QueryTriggerInteraction.Ignore);

        foreach (Collider collider in endHits)
            ProcessCollider(collider, damage);
    }

    private void ProcessCollider(
        Collider collider,
        float damage)
    {
        if (collider == null)
            return;

        CombatHealth target =
            collider.GetComponentInParent<CombatHealth>();

        if (target == null ||
            target == ownerHealth ||
            hitTargets.Contains(target))
        {
            return;
        }

        hitTargets.Add(target);

        bool accepted =
            target.ApplyDamage(
                damage,
                gameObject);

        if (!accepted)
            return;

        CombatSignals.ReportHit(
            target.gameObject.name,
            damage,
            target.IsDead);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
            return;

        if (meleeOrigin != null)
        {
            Vector3 direction =
                Vector3.ProjectOnPlane(
                    meleeOrigin.forward,
                    Vector3.up).normalized;

            if (direction.sqrMagnitude < 0.001f)
                direction = transform.forward;

            Vector3 end =
                meleeOrigin.position +
                direction * attackRange;

            Gizmos.DrawWireSphere(
                meleeOrigin.position,
                attackRadius);

            Gizmos.DrawWireSphere(
                end,
                attackRadius);

            Gizmos.DrawLine(
                meleeOrigin.position,
                end);

            return;
        }

        Camera cameraReference =
            viewCamera != null
                ? viewCamera
                : GetComponentInChildren<Camera>(true);

        if (cameraReference == null)
            return;

        Vector3 endPoint =
            cameraReference.transform.position +
            cameraReference.transform.forward *
            attackRange;

        Gizmos.DrawWireSphere(
            endPoint,
            attackRadius);
    }
}
