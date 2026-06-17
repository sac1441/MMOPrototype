using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Phase 5 & 6 - Combat System with smooth death and respawn
/// Press F to attack nearest player within range.
/// Damage is server-authoritative.
/// </summary>
public class CombatSystem : NetworkBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float respawnDelay = 3f;

    private float lastAttackTime = -999f;
    private bool isDead = false;

    private void Update()
    {
        if (!IsOwner) return;
        if (isDead) return;

        if (Input.GetKeyDown(KeyCode.F))
            TryAttack();
    }

    // ─── CLIENT SIDE ──────────────────────────────────────────

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
        {
            float remaining = attackCooldown - (Time.time - lastAttackTime);
            Debug.Log($"[MMO] Attack on cooldown: {remaining:F1}s");
            return;
        }

        PlayerStats target = FindNearestPlayer();
        if (target == null)
        {
            Debug.Log("[MMO] No target in range");
            return;
        }

        lastAttackTime = Time.time;
        ShowAttackEffect(target.transform.position);
        RequestAttackServerRpc(target.GetComponent<NetworkObject>().NetworkObjectId);

        Debug.Log($"[MMO] Attacking player {target.OwnerClientId}");
    }

    private PlayerStats FindNearestPlayer()
    {
        PlayerStats nearest = null;
        float nearestDist = attackRange;

        foreach (var stats in FindObjectsOfType<PlayerStats>())
        {
            if (stats.OwnerClientId == OwnerClientId) continue;
            if (stats.Health.Value <= 0) continue;

            float dist = Vector3.Distance(
                transform.position,
                stats.transform.position
            );

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = stats;
            }
        }

        return nearest;
    }

    private void ShowAttackEffect(Vector3 targetPos)
    {
        Debug.DrawLine(
            transform.position + Vector3.up,
            targetPos + Vector3.up,
            Color.yellow, 0.5f
        );
    }

    // ─── SERVER SIDE ──────────────────────────────────────────

    [ServerRpc]
    private void RequestAttackServerRpc(ulong targetNetworkObjectId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects
            .TryGetValue(targetNetworkObjectId, out NetworkObject targetObj))
        {
            Debug.Log("[MMO] Server: Target not found");
            return;
        }

        PlayerStats targetStats = targetObj.GetComponent<PlayerStats>();
        if (targetStats == null) return;

        float distance = Vector3.Distance(
            transform.position,
            targetObj.transform.position
        );

        if (distance > attackRange * 1.5f)
        {
            Debug.Log($"[MMO] Server: Attack out of range ({distance:F1}m)");
            return;
        }

        targetStats.TakeDamage(attackDamage);
        Debug.Log($"[MMO] Server: Hit! {attackDamage} damage. " +
                  $"Target HP: {targetStats.Health.Value}");

        NotifyHitClientRpc(targetNetworkObjectId, attackDamage);

        if (targetStats.Health.Value <= 0)
            HandleDeathServerRpc(targetNetworkObjectId);
    }

    [ClientRpc]
    private void NotifyHitClientRpc(ulong targetId, float damage)
    {
        Debug.Log($"[MMO] Hit! {damage} damage dealt");
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleDeathServerRpc(ulong targetNetworkObjectId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects
            .TryGetValue(targetNetworkObjectId, out NetworkObject targetObj))
            return;

        // Add kill feed message
        if (KillFeed.Instance != null)
            KillFeed.Instance.AddKill(OwnerClientId, targetObj.OwnerClientId);

        CombatSystem targetCombat = targetObj.GetComponent<CombatSystem>();
        if (targetCombat != null)
            targetCombat.TriggerDeathClientRpc();
    }

    [ClientRpc]
    private void TriggerDeathClientRpc()
    {
        if (IsOwner)
            StartCoroutine(HandleDeath());
    }

    // ─── DEATH & RESPAWN ──────────────────────────────────────

    private System.Collections.IEnumerator HandleDeath()
    {
        isDead = true;
        Debug.Log("[MMO] You died! Respawning in 3 seconds...");

        Renderer renderer = GetComponent<Renderer>();
        CharacterController cc = GetComponent<CharacterController>();

        // Disable movement immediately
        cc.enabled = false;

        // Simple fade to grey then black
        renderer.material.color = Color.grey;
        yield return new WaitForSeconds(0.5f);

        renderer.material.color = Color.black;
        yield return new WaitForSeconds(0.5f);

        // Fully hide BEFORE moving anywhere
        renderer.enabled = false;

        // Wait while dead
        yield return new WaitForSeconds(2f);

        // Request respawn — player is invisible so no double image
        RespawnServerRpc();
    }

    [ServerRpc]
    private void RespawnServerRpc()
    {
        Vector3 respawnPos = new Vector3(
            Random.Range(-5f, 5f),
            1f,
            Random.Range(-5f, 5f)
        );

        transform.position = respawnPos;

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.Heal(100f);

        RespawnClientRpc(respawnPos);
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 position)
    {
        if (!IsOwner) return;
        StartCoroutine(SmoothRespawn(position));
    }

    private System.Collections.IEnumerator SmoothRespawn(Vector3 position)
    {
        Renderer renderer = GetComponent<Renderer>();
        CharacterController cc = GetComponent<CharacterController>();

        // Teleport while still invisible
        cc.enabled = false;
        transform.position = position;

        // Wait for position to sync across network
        yield return new WaitForSeconds(0.2f);

        // Appear at new position
        renderer.enabled = true;
        renderer.material.color = Color.white;
        yield return new WaitForSeconds(0.2f);

        renderer.material.color = Color.green;
        yield return new WaitForSeconds(0.2f);

        renderer.material.color = Color.white;
        yield return new WaitForSeconds(0.2f);

        // Fully restored
        renderer.material.color = Color.green;
        cc.enabled = true;
        isDead = false;

        Debug.Log("[MMO] Respawned!");
    }

    // Show attack range gizmo in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}