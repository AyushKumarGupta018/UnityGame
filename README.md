# 🌊 Beacon of the Flooded Ruins
> **A 3D Atmospheric Exploration & Rescue Game — Built Entirely by Voice using Wispr Flow**

[![Built with Wispr Flow](https://img.shields.io/badge/Built%20with-Wispr%20Flow-059669?style=for-the-badge&logo=microphone)](https://ref.wisprflow.ai/hhg)
[![Unity 6](https://img.shields.io/badge/Engine-Unity%206%20URP-000000?style=for-the-badge&logo=unity)](https://unity.com)
[![WebGL Compatible](https://img.shields.io/badge/Platform-WebGL%20Browser-blue?style=for-the-badge&logo=webgl)](https://itch.io)

---

## 📖 Overview
**Beacon of the Flooded Ruins** is an atmospheric 3D adventure game created as part of the **Wispr Flow Shortlisting Task**. 

The goal was to demonstrate how voice-driven development through **Wispr Flow** can be used to architect, write, debug, and deploy a complete, functional 3D game without manually typing code.

### 🎮 Gameplay Loop
1. **Spawn & Explore**: You awaken as a stranded survivor on a dilapidated railway bridge overlooking a vast flooded town.
2. **Submerged Retrieval**: Traverse murky wetlands and submerged villas (experiencing realistic water drag physics) to recover **2 Submerged Power Cells** (Alpha & Beta).
3. **Ignite the Beacon**: Deliver the cells to the emergency beacon mast situated at the water pavilion. The 23-meter pole powers up, cycling vibrant laser red and electric blue light across the misty waters.
4. **Cinematic Rescue**: The beacon alerts a search-and-rescue team. A heavy extraction dropship sweeps through the clouds with searchlights, comms radio chatter, and a **★ MISSION ACCOMPLISHED ★** extraction sequence.
5. **Scenic Exploration**: Press `[C]` anytime to activate a multi-angle cinematic fly-through tour of the landscape.

---

## 🎙️ Voice-Driven Development with Wispr Flow
Every component of this project—from gameplay logic to shader bugfixes and WebGL player configurations—was orchestrated through voice dictation with **Wispr Flow**:

- **Player Controller & Dynamic Water Drag**: Dictated fluid movement calculations, character animations, and underwater resistance in [`FloodedPlayerController.cs`](Assets/Flooded_Grounds/Scripts/FloodedPlayerController.cs).
- **Emergency Beacon Alternating Cycle**: Programmed the unlit dormant state, power cell insertion logic, and 1-second HDR Red/Blue alternating cycle in [`FloodedBeaconPole.cs`](Assets/Flooded_Grounds/Scripts/FloodedBeaconPole.cs).
- **Cinematic Director & Comms System**: Implemented multi-shot spline camera movements, dropship flight paths, and Canvas victory overlays in [`FloodedCinematicTour.cs`](Assets/Flooded_Grounds/Scripts/FloodedCinematicTour.cs).
- **WebGL Build Automation**: Dictated custom editor build pipelines to strip conflicting third-party assemblies, configure 512MB WebAssembly heap allocations, and automate WebGL export in [`FloodedWebGlBuilder.cs`](Assets/Flooded_Grounds/Scripts/Editor/FloodedWebGlBuilder.cs).

---

## 🕹️ Controls
| Action | Key / Input |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` |
| **Sprint** | `Left Shift` |
| **Jump** | `Space` |
| **Interact / Insert Cells** | `E` |
| **Camera Perspective** | `V` (Toggle 1st / 3rd Person) |
| **Cinematic Scenic Tour** | `C` (Toggle Tour) |
| **Restart Mission** | `R` |

---

## 🚀 How to Run

### Play in Browser (WebGL)
1. Navigate to the `Builds/WebGL` directory.
2. Serve via any local HTTP server:
   ```bash
   python -m http.server 8000 --directory Builds/WebGL
   ```
3. Open `http://localhost:8000` in any modern web browser (Chrome, Edge, Firefox, Safari).

### Open in Unity Editor
- **Unity Version:** Unity 6 (`6000.0.4f1` or later)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Main Scene:** `Assets/Scenes/FloodedGrounds_Complete.unity`

---

## 🏆 Acknowledgments & Wispr Flow
- Developed for the **Wispr Flow Shortlisting Task** (October 2026).
- Account created via official referral: [https://ref.wisprflow.ai/hhg](https://ref.wisprflow.ai/hhg)
