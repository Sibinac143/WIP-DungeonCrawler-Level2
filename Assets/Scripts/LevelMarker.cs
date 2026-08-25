using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum LevelMarkerType
{
    PlayerSpawn, MainGate, GuardianSpawn, Trigger, VillageCenter,
    KeyMakerShop, KeyBox, Letter, CastleEntrance, PrisonCell,
    KeyMakerNPC, BossArena, AirPatrol, EscapePoint
}

[ExecuteAlways]
public class LevelMarker : MonoBehaviour
{
    public LevelMarkerType markerType;
    public Color markerColor = Color.white;
    public float markerSize = 2f;
    [TextArea] public string description;

    private void OnDrawGizmos()
    {
        Gizmos.color = markerColor;

        switch (markerType)
        {
            case LevelMarkerType.MainGate:
            case LevelMarkerType.KeyMakerShop:
            case LevelMarkerType.CastleEntrance:
            case LevelMarkerType.PrisonCell:
                Gizmos.DrawWireCube(
                    transform.position + Vector3.up * markerSize * 0.5f,
                    new Vector3(markerSize * 1.8f, markerSize, markerSize * 0.8f)
                );
                break;

            case LevelMarkerType.Trigger:
                Gizmos.DrawWireCube(
                    transform.position + Vector3.up,
                    new Vector3(markerSize * 2f, 2f, markerSize * 2f)
                );
                break;

            default:
                Gizmos.DrawWireSphere(transform.position, markerSize);
                break;
        }

#if UNITY_EDITOR
        Handles.color = markerColor;
        Handles.Label(
            transform.position + Vector3.up * (markerSize + 0.8f),
            string.IsNullOrWhiteSpace(description)
                ? gameObject.name
                : $"{gameObject.name}\n{description}"
        );
#endif
    }
}
