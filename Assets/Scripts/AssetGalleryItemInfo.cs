using UnityEngine;

public sealed class AssetGalleryItemInfo : MonoBehaviour
{
    [TextArea(2, 4)] public string sourcePath;
    public string category;
    public Vector3 measuredWorldSize;
    public int rendererCount;
    public int colliderCount;
    public int animatorCount;
    public int missingScriptCount;
    public int brokenMaterialSlotCount;

    private void OnDrawGizmosSelected()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
