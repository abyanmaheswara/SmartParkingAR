/*
 * ============================================================
 *  ParkingAnimationController.cs — AR Smart Parking System
 * ============================================================
 *  Menangani semua animasi dan UI update secara smooth:
 *    - Smooth distance counter (Mathf.Lerp)
 *    - Distance bar dengan gradient warna
 *    - Animasi rotasi palang gate (Coroutine + EaseInOut)
 *    - Transisi warna LED (Color.Lerp)
 *    - Button press animation (Scale punch + Color flash)
 *
 *  Setup:
 *    1. Attach script ini ke GameObject di scene
 *    2. Assign semua referensi UI via Inspector
 *    3. Panggil public methods dari MQTTManager.cs
 * ============================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SmartParking
{
    public class ParkingAnimationController : MonoBehaviour
    {
        // ─── UI References (assign via Inspector) ───────────
        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI slotStatusText;
        [SerializeField] private TextMeshProUGUI gateStatusText;



        [Header("Dashboard Panel Texts")]
        [SerializeField] private TextMeshProUGUI ledStatusText;
        [SerializeField] private TextMeshProUGUI touchStatusText;

        // ─── Button References (untuk animasi tekan) ────────
        [Header("Control Buttons")]
        [Tooltip("Tombol BUKA PALANG — akan diberi animasi tekan")]
        [SerializeField] private Button btnBukaPalang;
        [Tooltip("Teks di dalam tombol BUKA PALANG")]
        [SerializeField] private TextMeshProUGUI textBtnBuka;

        [Tooltip("Tombol TUTUP PALANG — akan diberi animasi tekan")]
        [SerializeField] private Button btnTutupPalang;
        [Tooltip("Teks di dalam tombol TUTUP PALANG")]
        [SerializeField] private TextMeshProUGUI textBtnTutup;

        // ─── Distance Smoothing ─────────────────────────────
        // targetDistance diset dari MQTT, displayedDistance
        // di-lerp setiap frame agar angka bergerak smooth
        private float targetDistance = 0f;
        private float displayedDistance = 0f;
        private const float DISTANCE_LERP_SPEED = 8f;
        private const float MAX_DISTANCE = 30f;



        // ─── Button Animation Settings ──────────────────────
        private const float BTN_PUNCH_SCALE    = 0.85f;  // scale minimum saat ditekan
        private const float BTN_PUNCH_DURATION = 0.15f;  // durasi shrink
        private const float BTN_BOUNCE_DURATION = 0.2f;   // durasi bounce back
        private const float BTN_BOUNCE_OVERSHOOT = 1.08f; // overshoot saat bounce
        private const float BTN_FLASH_DURATION = 0.3f;    // durasi color flash
        private readonly Color BTN_FLASH_COLOR_OPEN  = new Color(0.4f, 1f, 0.5f, 0.5f);   // hijau terang
        private readonly Color BTN_FLASH_COLOR_CLOSE = new Color(1f, 0.4f, 0.4f, 0.5f);   // merah terang

        // ─── Status Color Hex Codes ─────────────────────────
        private readonly string HEX_OCCUPIED = "#E24B4A";  // Merah
        private readonly string HEX_AVAILABLE = "#1D9E75";  // Hijau
        private readonly string HEX_GATE_OPEN = "#378ADD";  // Biru
        private readonly string HEX_GATE_CLOSED = "#E24B4A"; // Merah
        private readonly string HEX_LED_GREEN = "#1D9E75";
        private readonly string HEX_LED_RED = "#E24B4A";
        private readonly string HEX_TOUCH_ACTIVE = "#FFD700"; // Gold

        // ═════════════════════════════════════════════════════
        //  INITIALIZATION
        // ═════════════════════════════════════════════════════

        private void Start()
        {
            // Null check untuk semua SerializeField
            ValidateReferences();



            // Wire button animations
            SetupButtonAnimations();
        }

        /// <summary>
        /// Wire animasi tekan ke tombol Buka/Tutup Palang.
        /// Animasi berjalan independen dari fungsi MQTT di ParkingUIController.
        /// </summary>
        private void SetupButtonAnimations()
        {
            if (btnBukaPalang != null)
                btnBukaPalang.onClick.AddListener(() =>
                    StartCoroutine(AnimateButtonPress(btnBukaPalang, textBtnBuka, BTN_FLASH_COLOR_OPEN, "BUKA")));

            if (btnTutupPalang != null)
                btnTutupPalang.onClick.AddListener(() =>
                    StartCoroutine(AnimateButtonPress(btnTutupPalang, textBtnTutup, BTN_FLASH_COLOR_CLOSE, "TUTUP")));
        }

        /// <summary>
        /// Validasi semua referensi Inspector.
        /// Log warning untuk setiap referensi yang belum di-assign.
        /// </summary>
        private void ValidateReferences()
        {
            if (distanceText == null)
                Debug.LogWarning("[ParkingAnim] distanceText belum di-assign di Inspector!");
            if (slotStatusText == null)
                Debug.LogWarning("[ParkingAnim] slotStatusText belum di-assign di Inspector!");
            if (gateStatusText == null)
                Debug.LogWarning("[ParkingAnim] gateStatusText belum di-assign di Inspector!");

        }

        // ═════════════════════════════════════════════════════
        //  UPDATE LOOP — Smooth animations every frame
        // ═════════════════════════════════════════════════════

        private void Update()
        {
            UpdateDistanceSmooth();
        }

        // ─── 1. Smooth Distance Counter ─────────────────────
        // Lerp displayedDistance menuju targetDistance setiap frame
        // sehingga angka di UI bergerak halus, bukan melompat

        private void UpdateDistanceSmooth()
        {
            displayedDistance = Mathf.Lerp(displayedDistance, targetDistance, Time.deltaTime * DISTANCE_LERP_SPEED);

            if (distanceText != null)
                distanceText.text = Mathf.RoundToInt(displayedDistance) + " cm";
        }



        // ═════════════════════════════════════════════════════
        //  BUTTON PRESS ANIMATION — Scale Punch + Color Flash
        // ═════════════════════════════════════════════════════

        /// <summary>
        /// Animasi tombol saat ditekan: scale punch (mengecil → bounce back)
        /// + color flash (warna terang → fade kembali).
        /// </summary>
        /// <param name="button">Button yang di-animasi</param>
        /// <param name="btnText">Text di dalam button (diubah lewat script agar pasti ketemu)</param>
        /// <param name="flashColor">Warna flash highlight</param>
        /// <param name="originalTextString">Teks asli tombol untuk dikembalikan (misal BUKA PALANG)</param>
        private IEnumerator AnimateButtonPress(Button button, TextMeshProUGUI btnText, Color flashColor, string originalTextString)
        {
            if (button == null) yield break;

            RectTransform rect = button.GetComponent<RectTransform>();
            Image btnImage = button.GetComponent<Image>();
            if (rect == null) yield break;

            Vector3 originalScale = Vector3.one;
            Color originalColor = btnImage != null ? btnImage.color : Color.white;
            
            // Teks dikunci permanen ke BUKA / TUTUP, gak pake ganti ke MEMPROSES
            if (btnText != null) btnText.text = originalTextString;

            // ── Phase 1: Scale Punch (shrink) ──────────────
            float elapsed = 0f;
            while (elapsed < BTN_PUNCH_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / BTN_PUNCH_DURATION);
                // EaseOutQuad — mempercepat di awal
                t = 1f - (1f - t) * (1f - t);

                float scale = Mathf.Lerp(1f, BTN_PUNCH_SCALE, t);
                rect.localScale = new Vector3(scale, scale, 1f);

                // Color flash mulai bersamaan
                if (btnImage != null)
                    btnImage.color = Color.Lerp(originalColor, flashColor, t);

                yield return null;
            }

            // ── Phase 2: Bounce Back (overshoot → settle) ──
            elapsed = 0f;
            while (elapsed < BTN_BOUNCE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / BTN_BOUNCE_DURATION);

                // EaseOutBack — bounce overshoot lalu settle
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float eased = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

                float scale = Mathf.Lerp(BTN_PUNCH_SCALE, 1f, eased);
                // Clamp overshoot agar tidak terlalu besar
                scale = Mathf.Clamp(scale, BTN_PUNCH_SCALE, BTN_BOUNCE_OVERSHOOT);
                rect.localScale = new Vector3(scale, scale, 1f);

                // Color flash fade back
                if (btnImage != null)
                    btnImage.color = Color.Lerp(flashColor, originalColor, t);

                yield return null;
            }

            // ── Ensure reset ke state awal ──────────────────
            rect.localScale = originalScale;
            if (btnImage != null)
                btnImage.color = originalColor;
        }

        // ═════════════════════════════════════════════════════
        //  5. PUBLIC METHODS — Dipanggil dari MQTTManager.cs
        // ═════════════════════════════════════════════════════

        /// <summary>
        /// Set jarak target dari sensor ultrasonik.
        /// displayedDistance akan smooth-lerp menuju nilai ini.
        /// </summary>
        /// <param name="cm">Jarak dalam centimeter dari MQTT</param>
        public void SetDistance(float cm)
        {
            targetDistance = cm;
        }

        /// <summary>
        /// Update status slot parkir dan warna LED.
        /// </summary>
        /// <param name="occupied">true jika slot terisi, false jika kosong</param>
        public void SetSlotStatus(bool occupied)
        {

            // Update teks status slot dengan warna yang sesuai
            if (slotStatusText != null)
            {
                if (occupied)
                {
                    slotStatusText.text = "TERISI";
                    if (ColorUtility.TryParseHtmlString(HEX_OCCUPIED, out Color occColor))
                        slotStatusText.color = occColor;
                }
                else
                {
                    slotStatusText.text = "KOSONG";
                    if (ColorUtility.TryParseHtmlString(HEX_AVAILABLE, out Color avlColor))
                        slotStatusText.color = avlColor;
                }
            }
        }

        /// <summary>
        /// Update status gate dan jalankan animasi palang.
        /// </summary>
        /// <param name="status">"OPEN" atau "CLOSED" dari MQTT</param>
        public void SetGateStatus(string status)
        {

            // Update teks status gate dengan warna yang sesuai
            if (gateStatusText != null)
            {
                gateStatusText.text = (status == "OPEN") ? "BUKA" : "TUTUP";

                string hexColor = (status == "OPEN") ? HEX_GATE_OPEN : HEX_GATE_CLOSED;
                if (ColorUtility.TryParseHtmlString(hexColor, out Color gateColor))
                    gateStatusText.color = gateColor;
            }

            // Update LED panel — ikut status gate
            SetLedFromGate(status);


        }

        // ═════════════════════════════════════════════════════
        //  6. LED, TOUCH, GATE CONTROL PANELS
        // ═════════════════════════════════════════════════════

        /// <summary>Update LED status text berdasarkan gate state</summary>
        private void SetLedFromGate(string gateStatus)
        {
            if (ledStatusText == null) return;

            if (gateStatus == "OPEN")
            {
                ledStatusText.text = "HIJAU";
                if (ColorUtility.TryParseHtmlString(HEX_LED_GREEN, out Color green))
                    ledStatusText.color = green;
            }
            else
            {
                ledStatusText.text = "MERAH";
                if (ColorUtility.TryParseHtmlString(HEX_LED_RED, out Color red))
                    ledStatusText.color = red;
            }
        }

        /// <summary>Handle touch sensor event dari MQTT</summary>
        public void SetTouchEvent(string message)
        {
            if (touchStatusText != null)
            {
                touchStatusText.text = "DITEKAN!";
                if (ColorUtility.TryParseHtmlString(HEX_TOUCH_ACTIVE, out Color gold))
                    touchStatusText.color = gold;
            }

            // Reset ke "Idle" setelah 2 detik
            StartCoroutine(ResetTouchText());
        }

        private IEnumerator ResetTouchText()
        {
            yield return new WaitForSeconds(2f);
            if (touchStatusText != null)
            {
                touchStatusText.text = "SIAGA";
                touchStatusText.color = Color.white;
            }
        }
    }
}
