using UnityEngine;
using Unity.Netcode;

public class PlayerStats : NetworkBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxMana = 50f;

    public NetworkVariable<float> Health = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> Mana = new NetworkVariable<float>(
        50f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Level = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Health.Value = maxHealth;
            Mana.Value = maxMana;
            Level.Value = 1;
        }

        Health.OnValueChanged += OnHealthChanged;
        Mana.OnValueChanged += OnManaChanged;
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
        Mana.OnValueChanged -= OnManaChanged;
        base.OnNetworkDespawn();
    }

    private void OnHealthChanged(float oldVal, float newVal)
    {
        Debug.Log($"[MMO] Health: {oldVal} -> {newVal}");
    }

    private void OnManaChanged(float oldVal, float newVal)
    {
        Debug.Log($"[MMO] Mana: {oldVal} -> {newVal}");
    }

    // Replaced [Server] attribute with IsServer check instead
    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        Health.Value = Mathf.Clamp(Health.Value - amount, 0f, maxHealth);
        if (Health.Value <= 0f) OnDeath();
    }

    public void UseMana(float amount)
    {
        if (!IsServer) return;
        Mana.Value = Mathf.Clamp(Mana.Value - amount, 0f, maxMana);
    }

    public void Heal(float amount)
    {
        if (!IsServer) return;
        Health.Value = Mathf.Clamp(Health.Value + amount, 0f, maxHealth);
    }

    private void OnDeath()
    {
        Debug.Log($"[MMO] Player {OwnerClientId} died");
    }

    private void OnGUI()
    {
        if (!IsOwner) return;

        float barWidth = 200f;
        float barHeight = 18f;
        float x = 10f;
        float y = Screen.height - 90f;
        float hpPercent = Health.Value / maxHealth;
        float mpPercent = Mana.Value / maxMana;

        // --- HP Bar ---
        // Background
        GUI.color = new Color(0.2f, 0f, 0f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);
        // Fill
        GUI.color = new Color(0.9f, 0.1f, 0.1f);
        GUI.DrawTexture(new Rect(x, y, barWidth * hpPercent, barHeight), Texture2D.whiteTexture);
        // Label
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 5, y + 1, barWidth, barHeight), $"HP  {Health.Value:F0} / {maxHealth}");

        // --- MP Bar ---
        y += barHeight + 4f;
        // Background
        GUI.color = new Color(0f, 0f, 0.2f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);
        // Fill
        GUI.color = new Color(0.1f, 0.3f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, barWidth * mpPercent, barHeight), Texture2D.whiteTexture);
        // Label
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 5, y + 1, barWidth, barHeight), $"MP  {Mana.Value:F0} / {maxMana}");

        // --- Level ---
        y += barHeight + 4f;
        GUI.color = Color.yellow;
        GUI.Label(new Rect(x, y, barWidth, barHeight), $"Level  {Level.Value}");

        // Reset color
        GUI.color = Color.white;
    }
}