using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NightStormController : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private Light moonLight;
    [SerializeField] private Light flashLight;
    [SerializeField] private Transform moonTransform;

    [Header("Storm timing")]
    [SerializeField, Min(0.2f)] private float minDelayBetweenStrikes = 3.5f;
    [SerializeField, Min(0.3f)] private float maxDelayBetweenStrikes = 8f;
    [SerializeField, Range(0f, 1f)] private float groundStrikeChance = 0.45f;
    [SerializeField, Min(1)] private int maxBurstCount = 3;

    [Header("Lightning shape")]
    [SerializeField, Min(10f)] private float stormRadius = 90f;
    [SerializeField, Min(10f)] private float skyHeight = 55f;
    [SerializeField, Min(0.02f)] private float boltWidth = 0.16f;
    [SerializeField, Min(3)] private int boltSegments = 9;
    [SerializeField, Min(0.05f)] private float boltLifetime = 0.28f;
    [SerializeField, Min(0f)] private float skyStrikeMinLength = 8f;
    [SerializeField, Min(0f)] private float skyStrikeMaxLength = 18f;

    [Header("Lightning flash")]
    [SerializeField, Min(0f)] private float flashIntensity = 1.4f;
    [SerializeField, Min(0f)] private float flashDuration = 0.08f;
    [SerializeField, Min(0f)] private float flashRecovery = 0.18f;

    [Header("Ground strike impact")]
    [SerializeField, Min(0f)] private float impactLightIntensity = 7f;
    [SerializeField, Min(0f)] private float impactLightRange = 18f;
    [SerializeField, Min(0f)] private float impactLightDuration = 0.2f;

    private Material lightningMaterial;
    private Color boltColor = new Color(0.84f, 0.92f, 1f, 1f);
    private Color flashColor = new Color(0.72f, 0.82f, 1f, 1f);
    private float flashBaseIntensity;
    private float moonBaseIntensity;
    private Coroutine stormRoutine;

    public void Configure(
        Transform target,
        Light moon,
        Light flash,
        Transform moonObject)
    {
        followTarget = target;
        moonLight = moon;
        flashLight = flash;
        moonTransform = moonObject;
    }

    private void Awake()
    {
        EnsureMaterial();

        if (flashLight != null)
        {
            flashBaseIntensity = flashLight.intensity;
            flashLight.intensity = 0f;
        }

        if (moonLight != null)
            moonBaseIntensity = moonLight.intensity;
    }

    private void OnEnable()
    {
        if (stormRoutine == null)
            stormRoutine = StartCoroutine(StormLoop());
    }

    private void OnDisable()
    {
        if (stormRoutine != null)
        {
            StopCoroutine(stormRoutine);
            stormRoutine = null;
        }
    }

    private IEnumerator StormLoop()
    {
        yield return new WaitForSeconds(1f);

        while (enabled)
        {
            float wait = Random.Range(
                minDelayBetweenStrikes,
                Mathf.Max(minDelayBetweenStrikes + 0.1f, maxDelayBetweenStrikes));

            yield return new WaitForSeconds(wait);

            int burstCount =
                Random.Range(1, Mathf.Max(2, maxBurstCount + 1));

            for (int index = 0; index < burstCount; index++)
            {
                bool groundStrike = Random.value < groundStrikeChance;
                SpawnLightningStrike(groundStrike);

                yield return new WaitForSeconds(
                    Random.Range(0.12f, 0.4f));
            }
        }
    }

    private void SpawnLightningStrike(bool groundStrike)
    {
        EnsureMaterial();

        Vector3 center = GetStormCenter();
        Vector2 offset2D = Random.insideUnitCircle * stormRadius;

        Vector3 start = new Vector3(
            center.x + offset2D.x,
            GetGroundHeight(center) + skyHeight + Random.Range(6f, 14f),
            center.z + offset2D.y);

        Vector3 end;

        if (groundStrike)
        {
            Vector3 desired = new Vector3(
                center.x + offset2D.x * 0.8f,
                0f,
                center.z + offset2D.y * 0.8f);

            end = SampleGround(desired);
        }
        else
        {
            Vector3 drift = new Vector3(
                Random.Range(-8f, 8f),
                -Random.Range(skyStrikeMinLength, skyStrikeMaxLength),
                Random.Range(-8f, 8f));

            end = start + drift;
        }

        GameObject boltObject = new GameObject(
            groundStrike ? "Lightning Ground Strike" : "Lightning Sky Strike");

        boltObject.transform.SetParent(transform, false);

        LineRenderer line = boltObject.AddComponent<LineRenderer>();
        line.material = lightningMaterial;
        line.useWorldSpace = true;
        line.positionCount = boltSegments;
        line.startWidth = boltWidth;
        line.endWidth = boltWidth * 0.42f;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 1;
        line.numCapVertices = 1;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.alignment = LineAlignment.View;
        line.startColor = boltColor;
        line.endColor = new Color(boltColor.r, boltColor.g, boltColor.b, 0.55f);

        for (int i = 0; i < boltSegments; i++)
        {
            float t = boltSegments <= 1
                ? 1f
                : i / (float)(boltSegments - 1);

            Vector3 point = Vector3.Lerp(start, end, t);

            if (i != 0 && i != boltSegments - 1)
            {
                float jitter = Mathf.Lerp(3.4f, 0.4f, t);
                point += new Vector3(
                    Random.Range(-jitter, jitter),
                    Random.Range(-0.9f, 0.9f),
                    Random.Range(-jitter, jitter));
            }

            line.SetPosition(i, point);
        }

        StartCoroutine(FlashRoutine(end, groundStrike));
        StartCoroutine(DestroyAfterDelay(boltObject, boltLifetime));
    }

    private IEnumerator FlashRoutine(Vector3 impactPoint, bool groundStrike)
    {
        Light impactLight = null;

        if (groundStrike)
        {
            GameObject lightObject = new GameObject("Lightning Impact Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.position = impactPoint + Vector3.up * 1.2f;

            impactLight = lightObject.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.color = flashColor;
            impactLight.intensity = impactLightIntensity;
            impactLight.range = impactLightRange;
            impactLight.shadows = LightShadows.None;
        }

        if (flashLight != null)
        {
            flashLight.color = flashColor;
            flashLight.intensity = flashBaseIntensity + flashIntensity;
        }

        if (moonLight != null)
            moonLight.intensity = moonBaseIntensity + flashIntensity * 0.18f;

        yield return new WaitForSeconds(flashDuration);

        if (flashLight != null)
            flashLight.intensity = flashBaseIntensity;

        if (moonLight != null)
            moonLight.intensity = moonBaseIntensity;

        if (impactLight != null)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, impactLightDuration);
            float startIntensity = impactLight.intensity;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                impactLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
                yield return null;
            }

            Destroy(impactLight.gameObject);
        }
        else
        {
            yield return new WaitForSeconds(flashRecovery);
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target != null)
            Destroy(target);
    }

    private Vector3 GetStormCenter()
    {
        if (followTarget != null)
            return followTarget.position;

        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 size = terrain.terrainData.size;
            return terrain.transform.position + size * 0.5f;
        }

        return transform.position;
    }

    private Vector3 SampleGround(Vector3 desired)
    {
        float y = GetGroundHeight(desired);
        return new Vector3(desired.x, y, desired.z);
    }

    private float GetGroundHeight(Vector3 position)
    {
        if (Physics.Raycast(
                position + Vector3.up * 200f,
                Vector3.down,
                out RaycastHit hit,
                500f,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null && terrain.terrainData != null)
            return terrain.SampleHeight(position) + terrain.transform.position.y;

        return position.y;
    }

    private void EnsureMaterial()
    {
        if (lightningMaterial != null)
            return;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        lightningMaterial = new Material(shader);
        lightningMaterial.name = "Runtime Lightning Material";

        if (lightningMaterial.HasProperty("_BaseColor"))
            lightningMaterial.SetColor("_BaseColor", boltColor);

        if (lightningMaterial.HasProperty("_Color"))
            lightningMaterial.SetColor("_Color", boltColor);
    }
}
