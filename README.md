# 🚗 AR Smart Parking System

> Sistem parkir cerdas berbasis Augmented Reality menggunakan Unity + Vuforia, ESP32, MQTT, dan Node-RED.
> Proyek UTS Mata Kuliah Sistem Intelijen — Politeknik Manufaktur Bandung

![Unity](https://img.shields.io/badge/Unity-6.3-black?logo=unity)
![Vuforia](https://img.shields.io/badge/Vuforia-11.4.4-red)
![ESP32](https://img.shields.io/badge/ESP32-Arduino-blue)
![MQTT](https://img.shields.io/badge/MQTT-HiveMQ-orange)
![Node--RED](https://img.shields.io/badge/Node--RED-MySQL-darkred)

---

## 📋 Daftar Isi

- [Deskripsi](#deskripsi)
- [Arsitektur Sistem](#arsitektur-sistem)
- [Komponen Hardware](#komponen-hardware)
- [Struktur File](#struktur-file)
- [Langkah Pembuatan](#langkah-pembuatan)
- [MQTT Topic Map](#mqtt-topic-map)
- [Cara Menjalankan](#cara-menjalankan)

---

## 📌 Deskripsi

AR Smart Parking System adalah sistem monitoring dan kontrol parkir yang menampilkan data sensor secara real-time melalui Augmented Reality. Kamera smartphone diarahkan ke image target, kemudian dashboard AR muncul menampilkan:

- **Jarak** kendaraan dari sensor ultrasonik
- **Status slot** parkir (AVAILABLE / OCCUPIED)
- **Status palang** (OPEN / CLOSED)
- **Touch sensor** status
- **Kontrol manual** palang langsung dari AR

---

## 🏗️ Arsitektur Sistem

```
ESP32 (Sensor) ──MQTT──► HiveMQ Broker ──MQTT──► Unity AR App
     │                        │
     │                   Node-RED
     │                        │
     └──────── Subscribe ◄────┘
                               │
                            MySQL DB
```

---

## 🔧 Komponen Hardware

| Komponen | GPIO | Fungsi |
|---|---|---|
| HC-SR04 (Trig) | GPIO 5 | Trigger sensor ultrasonik |
| HC-SR04 (Echo) | GPIO 18 | Echo sensor ultrasonik |
| LED Merah | GPIO 26 | Indikator slot terisi |
| LED Hijau | GPIO 27 | Indikator slot kosong |
| Servo Motor | GPIO 13 | Palang parkir |
| Touch Sensor | GPIO 4 | Override manual |

**Power:** Semua komponen menggunakan 5V (VIN ESP32)

---

## 📁 Struktur File

```
AR-Smart-Parking/
├── ESP32/
│   └── SmartParking/
│       └── SmartParking.ino          ← Firmware ESP32
├── NodeRED/
│   └── flows.json                    ← Node-RED flow (import via menu)
├── Unity/
│   └── Scripts/
│       ├── MQTTManager.cs            ← Koneksi MQTT di Unity
│       ├── ParkingUIController.cs    ← UI Controller Canvas AR
│       └── ParkingAnimationController.cs ← Animasi smooth
├── Assets/
│   ├── dashboard_dark.png            ← Texture dashboard AR
│   └── parking_target.png           ← Vuforia image target
└── README.md
```

---

## 🛠️ Langkah Pembuatan

### 1. Setup ESP32

1. Install library berikut di Arduino IDE:
   - `PubSubClient` by Nick O'Leary
   - `ESP32Servo` by Kevin Harrington
   - `WiFi` (built-in ESP32)

2. Buka `ESP32/SmartParking/SmartParking.ino`

3. Ganti konfigurasi WiFi:
```cpp
const char* WIFI_SSID     = "YOUR_WIFI_SSID";
const char* WIFI_PASSWORD = "YOUR_WIFI_PASSWORD";
```

4. Upload ke ESP32

5. Buka Serial Monitor (115200 baud) — pastikan muncul `[MQTT] Connected!`

---

### 2. Setup Node-RED

1. Install Node-RED dependency:
```bash
npm install node-red-node-mysql
```

2. Buka Node-RED → Menu (☰) → **Import** → paste isi `NodeRED/flows.json`

3. Buat database MySQL di phpMyAdmin:
```sql
CREATE DATABASE smart_parking;
USE smart_parking;
CREATE TABLE sensor_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    distance FLOAT,
    slot_status VARCHAR(20),
    gate_status VARCHAR(20),
    touch VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

4. Klik **Deploy**

---

### 3. Setup Unity AR

#### 3.1 Persiapan Project
1. Buat project Unity baru (3D Core, Unity 2021.3+)
2. Install Vuforia Engine via Package Manager:
   - Window → Package Manager → Add by name: `com.ptc.vuforia.engine`
3. Set build target ke Android: File → Build Settings → Android → Switch Platform

#### 3.2 Setup Vuforia
1. Daftar di [developer.vuforia.com](https://developer.vuforia.com)
2. Buat license baru → copy License Key
3. Klik ARCamera di Hierarchy → Inspector → paste License Key di **App License Key**
4. Upload `parking_target.png` ke Vuforia Target Manager
5. Download database `.unitypackage` → import ke Unity

#### 3.3 Setup Scene
1. Hapus Main Camera → tambah AR Camera (GameObject → Vuforia Engine → AR Camera)
2. Tambah Image Target (GameObject → Vuforia Engine → Image Target)
3. Set database: `SmartParkingDB`, target: `ParkingTarget`
4. Import `dashboard_dark.png` → buat Material → Shader: Unlit/Texture
5. Buat Quad sebagai child ImageTarget → assign material → Rotation X:90, Scale X:0.3 Y:0.2

#### 3.4 Import M2Mqtt
1. Download ZIP dari [github.com/gpvigano/M2MqttUnity](https://github.com/gpvigano/M2MqttUnity)
2. Copy folder `M2Mqtt` dan `M2MqttUnity` ke `Assets/`

#### 3.5 Setup Canvas & Script
1. Copy 3 script ke `Assets/Scripts/`
2. Buat Canvas (World Space) sebagai child ImageTarget:
   - Scale: X:0.001, Y:0.001, Z:0.001
   - Width: 1000, Height: 680
   - Rotation X:90
3. Tambah TextMeshPro untuk: `DistanceText`, `SlotStatusText`, `GateStatusText`, `LEDText`, `TouchText`
4. Tambah 2 Button: `BtnBukaPalang`, `BtnTutupPalang`
5. Buat empty GameObject → attach `MQTTManager.cs`
6. Wire semua referensi di Inspector
7. Build APK

---

## 📡 MQTT Topic Map

| Topic | Arah | Isi |
|---|---|---|
| `parking/distance` | ESP32 → Unity/Node-RED | Jarak dalam cm |
| `parking/slot_status` | ESP32 → Unity/Node-RED | `AVAILABLE` / `OCCUPIED` |
| `parking/gate` | ESP32 → Unity/Node-RED | `OPEN` / `CLOSED` |
| `parking/touch` | ESP32 → Node-RED | `PRESSED` |
| `parking/control/gate` | Unity → ESP32 | `OPEN` / `CLOSE` |
| `parking/control/led` | Unity → ESP32 | `RED` / `GREEN` / `AUTO` |

**Broker:** `broker.hivemq.com:1883`

---

## ▶️ Cara Menjalankan

1. Nyalakan ESP32 — pastikan konek WiFi dan MQTT (`[MQTT] Connected!` di Serial Monitor)
2. Jalankan XAMPP → Start MySQL
3. Buka Node-RED → pastikan flow aktif
4. Buka aplikasi **SmartParkingAR** di HP
5. Izinkan akses kamera
6. Arahkan kamera ke image target yang sudah diprint (min. 10x10cm)
7. Dashboard AR muncul dengan data real-time dari ESP32

---

## 👨‍💻 Author

**Abyan Maheswara** — Politeknik Manufaktur Bandung  
Program Studi Teknologi Manufaktur & IIoT  
UTS Mata Kuliah Sistem Intelijen — 2026
