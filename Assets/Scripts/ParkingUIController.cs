using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ParkingUIController — Manages all UI elements for AR Smart Parking System.
/// Attach to Canvas GameObject. Wire up UI references via Inspector.
/// </summary>
public class ParkingUIController : MonoBehaviour
{
    // ─── Status Texts (assign via Inspector) ────────────────
    [Header("Status Texts")]
    [SerializeField] private TextMeshProUGUI txtDistance;
    [SerializeField] private TextMeshProUGUI txtSlotStatus;
    [SerializeField] private TextMeshProUGUI txtGateStatus;

    // ─── Dashboard Panel Texts ──────────────────────────────
    [Header("Dashboard Panel Texts")]
    [SerializeField] private TextMeshProUGUI txtLedStatus;
    [SerializeField] private TextMeshProUGUI txtTouchStatus;

    // ─── Control Buttons ────────────────────────────────────
    [Header("Control Buttons")]
    [Tooltip("Tombol BUKA PALANG (BtnBukaPalang di scene)")]
    [SerializeField] private Button btnBukaPalang;

    [Tooltip("Tombol TUTUP PALANG (BtnTutupPalang di scene)")]
    [SerializeField] private Button btnTutupPalang;

    // ─── MQTT Manager Reference ─────────────────────────────
    [Header("MQTT Manager Reference")]
    [SerializeField] private MQTTManager mqttManager;

    // ─── State ──────────────────────────────────────────────
    private string currentGateStatus = "UNKNOWN";

    // ─── Colors ─────────────────────────────────────────────
    private readonly Color colorOccupied   = new Color(0.95f, 0.26f, 0.21f, 1f); // Red
    private readonly Color colorAvailable  = new Color(0.30f, 0.69f, 0.31f, 1f); // Green
    private readonly Color colorGateOpen   = new Color(0.30f, 0.69f, 0.31f, 1f); // Green
    private readonly Color colorGateClosed = new Color(0.95f, 0.26f, 0.21f, 1f); // Red
    private readonly Color colorLedRed     = new Color(0.95f, 0.26f, 0.21f, 1f);
    private readonly Color colorLedGreen   = new Color(0.30f, 0.69f, 0.31f, 1f);
    private readonly Color colorTouchActive = new Color(1f, 0.76f, 0.03f, 1f);   // Gold

    // ═════════════════════════════════════════════════════════
    //  INITIALIZATION
    // ═════════════════════════════════════════════════════════

    void Start()
    {
        SetupButtons();
        SetDefaultUI();
    }

    /// <summary>Wire button onClick events to MQTT publish methods</summary>
    private void SetupButtons()
    {
        if (btnBukaPalang != null)
            btnBukaPalang.onClick.AddListener(() => mqttManager?.PublishGateOpen());

        if (btnTutupPalang != null)
            btnTutupPalang.onClick.AddListener(() => mqttManager?.PublishGateClose());
    }

    /// <summary>Set initial UI state before data arrives</summary>
    private void SetDefaultUI()
    {
        SetText(txtDistance, "-- cm");
        SetText(txtSlotStatus, "MENUNGGU...");
        SetText(txtGateStatus, "MENUNGGU...");
        SetText(txtLedStatus, "AUTO");
        SetText(txtTouchStatus, "SIAGA");
    }

    // ═════════════════════════════════════════════════════════
    //  PUBLIC UPDATE METHODS (called by MQTTManager)
    // ═════════════════════════════════════════════════════════

    /// <summary>Update distance display</summary>
    public void UpdateDistance(int distanceCm)
    {
        SetText(txtDistance, $"{distanceCm} cm");
    }

    /// <summary>Update slot status: "OCCUPIED" or "AVAILABLE"</summary>
    public void UpdateSlotStatus(string status)
    {
        string displayStatus = (status == "OCCUPIED") ? "TERISI" : "KOSONG";
        SetText(txtSlotStatus, displayStatus);
        if (txtSlotStatus != null)
            txtSlotStatus.color = (status == "OCCUPIED") ? colorOccupied : colorAvailable;
    }

    /// <summary>Update gate status: "OPEN" or "CLOSED"</summary>
    public void UpdateGateStatus(string status)
    {
        currentGateStatus = status;
        string displayStatus = (status == "OPEN") ? "BUKA" : "TUTUP";
        SetText(txtGateStatus, displayStatus);
        if (txtGateStatus != null)
            txtGateStatus.color = (status == "OPEN") ? colorGateOpen : colorGateClosed;

        // Update LED status panel berdasarkan gate
        UpdateLedFromGate(status);


    }

    /// <summary>Handle touch sensor event</summary>
    public void OnTouchEvent(string message)
    {
        SetText(txtTouchStatus, "DITEKAN!");
        if (txtTouchStatus != null)
            txtTouchStatus.color = colorTouchActive;

        StartCoroutine(ResetTouchText());
    }

    /// <summary>Update LED status panel based on gate state</summary>
    private void UpdateLedFromGate(string gateStatus)
    {
        if (gateStatus == "OPEN")
        {
            SetText(txtLedStatus, "HIJAU");
            if (txtLedStatus != null)
                txtLedStatus.color = colorLedGreen;
        }
        else
        {
            SetText(txtLedStatus, "MERAH");
            if (txtLedStatus != null)
                txtLedStatus.color = colorLedRed;
        }
    }

    /// <summary>Get current gate status string</summary>
    public string GetCurrentGateStatus() => currentGateStatus;

    // ═════════════════════════════════════════════════════════
    //  CONNECTION CALLBACKS (called by MQTTManager)
    // ═════════════════════════════════════════════════════════

    public void OnMQTTConnected()
    {
        Debug.Log("[PARKING] MQTT Connected");
    }

    public void OnMQTTDisconnected(string reason)
    {
        Debug.Log($"[PARKING] MQTT Disconnected: {reason}");
    }

    // ═════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════

    private void SetText(TextMeshProUGUI textElement, string value)
    {
        if (textElement != null)
            textElement.text = value;
    }

    private System.Collections.IEnumerator ResetTouchText()
    {
        yield return new WaitForSeconds(2f);
        SetText(txtTouchStatus, "SIAGA");
        if (txtTouchStatus != null)
            txtTouchStatus.color = Color.white;
    }
}
