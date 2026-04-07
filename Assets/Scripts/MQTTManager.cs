using System;
using System.Text;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

/// <summary>
/// MQTTManager — Handles MQTT connection for AR Smart Parking System.
/// Uses M2MqttUnity library. Attach to an empty GameObject.
/// </summary>
public class MQTTManager : MonoBehaviour
{
    // ─── Broker Settings ────────────────────────────────────
    [Header("MQTT Broker Settings")]
    [SerializeField] private string brokerAddress = "broker.hivemq.com";
    [SerializeField] private int brokerPort = 1883;
    [SerializeField] private string clientId = "Unity_SmartParking";

    // ─── Subscribe Topics (ESP32 → Unity) ───────────────────
    [Header("Subscribe Topics (ESP32 -> Unity)")]
    [SerializeField] private string topicDistance   = "parking/distance";
    [SerializeField] private string topicSlotStatus = "parking/slot_status";
    [SerializeField] private string topicGate       = "parking/gate";
    [SerializeField] private string topicTouch      = "parking/touch";

    // ─── Publish Topics (Unity → ESP32) ─────────────────────
    [Header("Publish Topics (Unity -> ESP32)")]
    [SerializeField] private string topicControlGate = "parking/control/gate";

    // ─── References ─────────────────────────────────────────
    [Header("Controller References")]
    [SerializeField] private ParkingUIController uiController;
    [SerializeField] private SmartParking.ParkingAnimationController animController;

    // ─── Private State ──────────────────────────────────────
    private MqttClient mqttClient;
    private bool isConnected = false;
    public bool IsConnected => isConnected;

    // Thread-safe queue — MQTT callbacks run on background thread
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> mainThreadActions
        = new System.Collections.Concurrent.ConcurrentQueue<Action>();

    // ═════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═════════════════════════════════════════════════════════

    void Start() { Connect(); }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
            action?.Invoke();
    }

    void OnDestroy() { Disconnect(); }
    void OnApplicationQuit() { Disconnect(); }

    // ═════════════════════════════════════════════════════════
    //  CONNECTION MANAGEMENT
    // ═════════════════════════════════════════════════════════

    /// <summary>Connect to MQTT broker and subscribe topics</summary>
    public void Connect()
    {
        try
        {
            Debug.Log($"[MQTT] Connecting to {brokerAddress}:{brokerPort}...");
            string uid = clientId + "_" + UnityEngine.Random.Range(1000, 9999);

            mqttClient = new MqttClient(brokerAddress, brokerPort, false, null, null, MqttSslProtocols.None);
            mqttClient.MqttMsgPublishReceived += OnMessageReceived;
            mqttClient.ConnectionClosed += (s, e) =>
            {
                isConnected = false;
                mainThreadActions.Enqueue(() => uiController?.OnMQTTDisconnected("Connection lost"));
            };

            mqttClient.Connect(uid);

            if (mqttClient.IsConnected)
            {
                isConnected = true;
                Debug.Log("[MQTT] Connected!");
                mqttClient.Subscribe(
                    new[] { topicDistance, topicSlotStatus, topicGate, topicTouch },
                    new byte[] { 0, 0, 0, 0 });
                mainThreadActions.Enqueue(() => uiController?.OnMQTTConnected());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MQTT] Failed: {ex.Message}");
            isConnected = false;
            mainThreadActions.Enqueue(() => uiController?.OnMQTTDisconnected(ex.Message));
        }
    }

    /// <summary>Disconnect from broker</summary>
    public void Disconnect()
    {
        if (mqttClient != null && mqttClient.IsConnected)
        {
            try { mqttClient.Disconnect(); } catch { }
        }
        isConnected = false;
    }

    /// <summary>Reconnect to broker</summary>
    public void Reconnect() { Disconnect(); Connect(); }

    // ═════════════════════════════════════════════════════════
    //  MESSAGE ROUTING
    // ═════════════════════════════════════════════════════════

    /// <summary>Route incoming MQTT messages to controllers (on main thread)</summary>
    private void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string topic = e.Topic;
        string msg = Encoding.UTF8.GetString(e.Message);

        mainThreadActions.Enqueue(() =>
        {
            // Route ke UI Controller
            if (uiController != null)
            {
                if (topic == topicDistance && int.TryParse(msg, out int d))
                    uiController.UpdateDistance(d);
                else if (topic == topicSlotStatus)
                    uiController.UpdateSlotStatus(msg);
                else if (topic == topicGate)
                    uiController.UpdateGateStatus(msg);
                else if (topic == topicTouch)
                    uiController.OnTouchEvent(msg);
            }

            // Route ke Animation Controller
            if (animController != null)
            {
                if (topic == topicDistance && int.TryParse(msg, out int da))
                    animController.SetDistance(da);
                else if (topic == topicSlotStatus)
                    animController.SetSlotStatus(msg == "OCCUPIED");
                else if (topic == topicGate)
                    animController.SetGateStatus(msg);
                else if (topic == topicTouch)
                    animController.SetTouchEvent(msg);
            }
        });
    }

    // ═════════════════════════════════════════════════════════
    //  PUBLISH — Gate Control (Unity → ESP32)
    // ═════════════════════════════════════════════════════════

    private void Publish(string topic, string message)
    {
        if (mqttClient != null && mqttClient.IsConnected)
        {
            mqttClient.Publish(topic, Encoding.UTF8.GetBytes(message), 0, false);
            Debug.Log($"[MQTT] Published → {topic}: {message}");
        }
        else
        {
            Debug.LogWarning("[MQTT] Cannot publish — not connected!");
        }
    }

    /// <summary>Publish "OPEN" ke topic parking/control/gate</summary>
    public void PublishGateOpen() => Publish(topicControlGate, "OPEN");

    /// <summary>Publish "CLOSE" ke topic parking/control/gate</summary>
    public void PublishGateClose() => Publish(topicControlGate, "CLOSE");
}
