using UnityEngine;
using Unity.Netcode;

public class PlayerNameplate : NetworkBehaviour
{
    private void OnGUI()
    {
        if (Camera.main == null) return;

        // Project 3D position to screen position
        Vector3 worldPos = transform.position + Vector3.up * 1.5f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // Don't render if behind camera
        if (screenPos.z < 0) return;

        // Flip Y axis (Unity GUI is top-left origin)
        screenPos.y = Screen.height - screenPos.y;

        float barWidth = 80f;
        float barHeight = 8f;
        float x = screenPos.x - barWidth / 2f;
        float y = screenPos.y - 30f;

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;

        float hpPercent = stats.Health.Value / 100f;

        // Name label
        GUI.color = IsOwner ? Color.green : Color.red;
        GUI.Label(new Rect(x, y - 16f, barWidth, 16f),
            IsOwner ? "▶ You" : $"Player {OwnerClientId}");

        // HP bar background
        GUI.color = new Color(0.2f, 0f, 0f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight),
            Texture2D.whiteTexture);

        // HP bar fill
        GUI.color = IsOwner
            ? new Color(0.1f, 0.9f, 0.1f)
            : new Color(0.9f, 0.1f, 0.1f);
        GUI.DrawTexture(new Rect(x, y, barWidth * hpPercent, barHeight),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}