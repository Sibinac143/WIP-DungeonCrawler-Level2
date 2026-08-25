using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KeyRewardPopup : MonoBehaviour
{
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Camera displayCamera;
    [SerializeField, Min(0.5f)] private float displayDuration = 3f;
    [SerializeField] private Vector3 localPosition =
        new Vector3(0f, -0.12f, 1.35f);
    [SerializeField] private Vector3 localEuler =
        new Vector3(12f, 20f, 90f);
    [SerializeField, Min(0.1f)] private float displayScale = 5f;
    [SerializeField, Min(0f)] private float spinSpeed = 85f;

    private Coroutine routine;

    public void Configure(
        GameObject prefab,
        Camera cameraReference)
    {
        keyPrefab = prefab;
        displayCamera = cameraReference;
    }

    public void Show()
    {
        if (keyPrefab == null)
            return;

        if (displayCamera == null)
            displayCamera = Camera.main;

        if (displayCamera == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        GameObject instance =
            Instantiate(
                keyPrefab,
                displayCamera.transform);

        instance.name = "Main Gate Key Reward Popup";
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation =
            Quaternion.Euler(localEuler);
        instance.transform.localScale =
            Vector3.one * displayScale;

        foreach (Collider collider in
                 instance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        float elapsed = 0f;

        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;

            instance.transform.Rotate(
                Vector3.up,
                spinSpeed * Time.deltaTime,
                Space.Self);

            yield return null;
        }

        Destroy(instance);
        routine = null;
    }
}
