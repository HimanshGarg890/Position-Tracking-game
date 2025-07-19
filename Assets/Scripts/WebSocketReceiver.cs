using UnityEngine;
using NativeWebSocket;

public class WebSocketReceiver : MonoBehaviour
{
    private WebSocket websocket;
    public Animator animator; // Assign in Inspector

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:8080");

        websocket.OnOpen += () => Debug.Log("WebSocket connected");
        websocket.OnError += (e) => Debug.Log("Error: " + e);
        websocket.OnClose += (e) => Debug.Log("WebSocket closed");

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + message);

            if (message == "punch") animator.SetTrigger("Punch");
            else if (message == "kick") animator.SetTrigger("Kick");
            else if (message == "jump") animator.SetTrigger("Jump");
        };

        await websocket.Connect();

    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}