using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class KillFeed : NetworkBehaviour
{
    private List<string> feedMessages = new List<string>();
    private float messageLifetime = 4f;
    private List<float> messageTimes = new List<float>();

    public static KillFeed Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void AddKill(ulong killerId, ulong victimId)
    {
        string msg = $"Player {killerId} killed Player {victimId}";
        feedMessages.Add(msg);
        messageTimes.Add(Time.time);

        if (feedMessages.Count > 5)
        {
            feedMessages.RemoveAt(0);
            messageTimes.RemoveAt(0);
        }
    }

    private void OnGUI()
    {
        float x = Screen.width - 220f;
        float y = 10f;

        for (int i = 0; i < feedMessages.Count; i++)
        {
            float age = Time.time - messageTimes[i];
            float alpha = Mathf.Clamp01(1f - (age / messageLifetime));

            GUI.color = new Color(1f, 0.3f, 0.3f, alpha);
            GUI.Label(new Rect(x, y + i * 22f, 210f, 20f),
                feedMessages[i]);
        }

        GUI.color = Color.white;
    }
}