# Unity ML-Agents 2D Top-Down Car Agent

A reinforcement learning project where a 2D top-down car agent learns to navigate toward waypoints while avoiding obstacles, using Unity ML-Agents.

---

## Prerequisites

- **Unity**: Version **2022.3 LTS** or newer (2021.3+ should also work)
- **Python**: Version 3.10.x (required by ML-Agents)
- **pip**: Comes with Python

---

## Step 1: Install the ML-Agents Unity Package

1. Open your Unity project.
2. Go to **Window → Package Manager**.
3. Click the **+** button in the top-left corner → **Add package from git URL...**
4. Paste: `com.unity.ml-agents` and click **Add**.
   - Alternatively, paste: `https://github.com/Unity-Technologies/ml-agents.git?path=com.unity.ml-agents#release_21`
5. Wait for the package to install. You should see **ML Agents** in the Package Manager list.

> **Note:** If you see errors about `com.unity.barracuda`, it should be installed automatically as a dependency. If not, add it via git URL: `com.unity.barracuda`

---

## Step 2: Create Required Layers and Tags

### Layers

1. Go to **Edit → Project Settings → Tags and Layers**.
2. In the **Layers** section, add the following to empty User Layer slots:
   - Layer 6 (or any free slot): `Obstacle`
   - Layer 7 (or any free slot): `Boundary`
   - Layer 8 (or any free slot): `Agent`

### Tags

1. In the same **Tags and Layers** window, expand the **Tags** section.
2. Click **+** and add a new tag: `Waypoint`

---

## Step 3: Configure Physics2D Layer Collision Matrix

1. Go to **Edit → Project Settings → Physics 2D**.
2. Scroll down to the **Layer Collision Matrix**.
3. Configure it so that:
   - **Agent ↔ Obstacle**: ✅ Enabled (checked)
   - **Agent ↔ Boundary**: ✅ Enabled (checked)
   - **Agent ↔ Agent**: ❌ Disabled (unchecked)
   - All other Agent collisions: ❌ Disabled (unchecked)
4. Leave all other layer collisions at their defaults.

---

## Step 4: Build the Waypoint Prefab

1. In the **Hierarchy**, right-click → **2D Object → Sprites → Circle** (or Square).
2. Name it `Waypoint`.
3. Set its **Tag** to `Waypoint` (the tag you created in Step 2).
4. Set the **Scale** to `(1, 1, 1)`.
5. Set the SpriteRenderer **Color** to something visible (e.g., bright yellow).
6. **Remove any Collider** components if present (waypoints are non-physical).
7. Drag the `Waypoint` GameObject from the Hierarchy into `Assets/Prefabs/` to create the prefab.
8. Delete the `Waypoint` object from the Hierarchy.

---

## Step 5: Build the CarAgent Prefab

### 5.1 Create the Car Body

1. In the **Hierarchy**, right-click → **2D Object → Sprites → Square**.
2. Name it `CarAgent`.
3. Set the **Layer** to `Agent`.
4. Set **Scale** to `(1, 2, 1)` to create a rectangular car shape (wider on X, longer on Y since Y is forward in 2D top-down).
5. Set the SpriteRenderer **Color** to your preferred car color (e.g., blue).

### 5.2 Add Components to CarAgent

1. Select the `CarAgent` GameObject.
2. The **SpriteRenderer** is already there.
3. Add **Box Collider 2D** (Component → Physics 2D → Box Collider 2D). Size will auto-fit.
4. Add **Rigidbody 2D** (Component → Physics 2D → Rigidbody 2D):
   - Set **Body Type**: Dynamic
   - Set **Gravity Scale**: 0
   - Under **Constraints**, check **Freeze Rotation Z** ✅
5. Add the **Car Controller** script (Component → Scripts → CarController). It will auto-add Rigidbody2D if missing.
6. Add the **Car Agent** script (Component → Scripts → CarAgent).
7. Add **Behavior Parameters** (Component → ML Agents → Behavior Parameters):
   - **Behavior Name**: `CarAgent`
   - **Vector Observation Size**: `35` (that is 11 + 24 raycasts — adjust if you change rayCastCount)
   - **Stacked Vectors**: `1`
   - **Actions → Continuous Actions**: `2`
   - **Actions → Discrete Branches**: leave empty
8. Add **Decision Requester** (Component → ML Agents → Decision Requester):
   - **Decision Period**: `5` (agent decides every 5 physics steps; lower = more responsive but slower training)

### 5.3 Create Wheel Child Objects

Create 4 child GameObjects under `CarAgent` for the wheels:

| Name | Position (local) | Scale | Notes |
|------|------------------|-------|-------|
| `WheelFrontLeft` | `(-0.4, 0.7, 0)` | `(0.2, 0.4, 1)` | Front-left |
| `WheelFrontRight` | `(0.4, 0.7, 0)` | `(0.2, 0.4, 1)` | Front-right |
| `WheelRearLeft` | `(-0.4, -0.7, 0)` | `(0.2, 0.4, 1)` | Rear-left |
| `WheelRearRight` | `(0.4, -0.7, 0)` | `(0.2, 0.4, 1)` | Rear-right |

For each wheel:
1. Right-click `CarAgent` → **2D Object → Sprites → Square**.
2. Name it according to the table above.
3. Set **local position** and **scale** as shown.
4. Set SpriteRenderer **Color** to dark gray or black.
5. Set **Sorting Order** to 1 (so wheels render above/below the car body as desired).

### 5.4 Create Light Child Objects

Create 4 child GameObjects under `CarAgent` for the lights:

| Name | Position (local) | Scale | Color |
|------|------------------|-------|-------|
| `HeadlightLeft` | `(-0.35, 1.0, 0)` | `(0.15, 0.15, 1)` | White |
| `HeadlightRight` | `(0.35, 1.0, 0)` | `(0.15, 0.15, 1)` | White |
| `TaillightLeft` | `(-0.35, -1.0, 0)` | `(0.15, 0.15, 1)` | Red |
| `TaillightRight` | `(0.35, -1.0, 0)` | `(0.15, 0.15, 1)` | Red |

For each light:
1. Right-click `CarAgent` → **2D Object → Sprites → Circle**.
2. Name it according to the table above.
3. Set **local position**, **scale**, and **color** as shown.
4. Set **Sorting Order** to 1.

### 5.5 Assign CarController References

1. Select the `CarAgent` GameObject.
2. In the **Car Controller** component:
   - Drag `WheelFrontLeft` → **Wheel Front Left**
   - Drag `WheelFrontRight` → **Wheel Front Right**
   - Drag `WheelRearLeft` → **Wheel Rear Left**
   - Drag `WheelRearRight` → **Wheel Rear Right**
   - Drag `HeadlightLeft`'s SpriteRenderer → **Headlight Left Renderer**
   - Drag `HeadlightRight`'s SpriteRenderer → **Headlight Right Renderer**
   - Drag `TaillightLeft`'s SpriteRenderer → **Taillight Left Renderer**
   - Drag `TaillightRight`'s SpriteRenderer → **Taillight Right Renderer**

### 5.6 Assign CarAgent References

1. In the **Car Agent** component:
   - **Car Controller**: drag the `CarAgent` GameObject itself (it has CarController)
   - **Waypoint Manager**: leave empty for now (will be assigned in the TrainingArea prefab)
   - **Training Area**: leave empty for now (will be assigned in the TrainingArea prefab)
   - **Obstacle Layer**: set to include both `Obstacle` and `Boundary` layers

### 5.7 Save the Prefab

1. Drag the `CarAgent` from the Hierarchy into `Assets/Prefabs/` to create the prefab.
2. Delete the `CarAgent` from the Hierarchy.

---

## Step 6: Build the TrainingArea Prefab

### 6.1 Create the Root

1. In the Hierarchy, right-click → **Create Empty**.
2. Name it `TrainingArea`.
3. Reset its Transform to position `(0, 0, 0)`.

### 6.2 Add the WaypointManager

1. Right-click `TrainingArea` → **Create Empty**.
2. Name it `WaypointManager`.
3. Add the **Waypoint Manager** script to it.
4. Assign:
   - **Waypoint Prefab**: drag in `Assets/Prefabs/Waypoint.prefab`

### 6.3 Create ObstaclesRoot

1. Right-click `TrainingArea` → **Create Empty**.
2. Name it `ObstaclesRoot`.
3. Inside `ObstaclesRoot`, create obstacle GameObjects:
   - Right-click `ObstaclesRoot` → **2D Object → Sprites → Square**
   - Name each one `Obstacle_1`, `Obstacle_2`, etc.
   - Set each obstacle's **Layer** to `Obstacle`
   - Add **Box Collider 2D** to each
   - Position and scale them within the area bounds (remember area is 40×40 by default, centered on the TrainingArea position)
   - Add a few sparse obstacles for Stage 3 and more for Stage 4 (you can duplicate and reposition later)

### 6.4 Add the CarAgent

1. Drag `Assets/Prefabs/CarAgent.prefab` into the `TrainingArea` hierarchy as a child.

### 6.5 Add the TrainingArea Script

1. Select the `TrainingArea` root GameObject.
2. Add the **Training Area** script.
3. Assign in the Inspector:
   - **Car Agent Prefab**: `Assets/Prefabs/CarAgent.prefab` (used if no agent child exists)
   - **Obstacles Root**: drag the `ObstaclesRoot` child

### 6.6 Assign CarAgent Cross-References

1. Select the `CarAgent` child inside TrainingArea.
2. In the **Car Agent** component:
   - **Waypoint Manager**: drag the `WaypointManager` child
   - **Training Area**: drag the `TrainingArea` root

### 6.7 Save the Prefab

1. Drag `TrainingArea` from the Hierarchy into `Assets/Prefabs/` to create the prefab.
2. Delete `TrainingArea` from the Hierarchy.

---

## Step 7: Set Up the Training Scene

1. Open `Assets/Scenes/SampleScene.unity` (or create a new scene: **File → New Scene → Basic 2D**). Save it as `Assets/Scenes/TrainingScene.unity`.
2. Delete the default Main Camera if you plan to use your own, or keep it.
3. In the Hierarchy, right-click → **Create Empty**.
4. Name it `TrainingManager`.
5. Add the **Training Area Spawner** script to it.
6. Assign:
   - **Training Area Prefab**: `Assets/Prefabs/TrainingArea.prefab`
   - **Area Count X**: `3` (start small — 3×3 = 9 parallel environments)
   - **Area Count Y**: `3`
   - **Area Padding**: `10`
7. Save the scene.

---

## Step 8: Create the Training Configuration YAML

Create a file named `car_training.yaml` in your project root (next to `Assets/`):

```yaml
behaviors:
  CarAgent:
    trainer_type: ppo
    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
      learning_rate_schedule: linear
    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 5000000
    time_horizon: 64
    summary_freq: 10000
```

> **Important:** The `vector_observation_size` is set in the Unity Inspector on the Behavior Parameters component, not in this YAML file. Make sure it matches `11 + rayCastCount` (default: 11 + 24 = 35).

---

## Step 9: Install ML-Agents Python Package

1. Open a terminal / command prompt.
2. (Recommended) Create a virtual environment:
   ```bash
   python -m venv mlagents-env
   ```
   Activate it:
   - **Windows**: `mlagents-env\Scripts\activate`
   - **Mac/Linux**: `source mlagents-env/bin/activate`
3. Install ML-Agents:
   ```bash
   pip install mlagents==1.1.0
   ```
   > Use version 1.1.0 for compatibility with Release 21. Check [ML-Agents releases](https://github.com/Unity-Technologies/ml-agents/releases) for version matching.

4. Verify the installation:
   ```bash
   mlagents-learn --help
   ```

---

## Step 10: Run Training

1. Make sure your Unity scene is saved and the Editor is open with the TrainingScene loaded.
2. In the terminal (with your virtual environment activated), navigate to the project root folder:
   ```bash
   cd D:\unity\projeler\Aihomework
   ```
3. Start training:
   ```bash
   mlagents-learn car_training.yaml --run-id=CarAgent_Run1
   ```
4. When you see `Start training by pressing the Play button in the Unity Editor`, press **Play** in Unity.
5. Training will begin. You'll see the agents moving in parallel across all training areas.

### Curriculum Training

To train with curriculum stages:
1. Start with `curriculumStage = 1` on all TrainingArea instances.
2. Train until the agent consistently reaches the single waypoint (monitor via TensorBoard).
3. Stop training, change `curriculumStage` to `2`, and resume:
   ```bash
   mlagents-learn car_training.yaml --run-id=CarAgent_Run1 --resume
   ```
4. Repeat for stages 3 and 4.

> **Tip:** You can also use ML-Agents' built-in curriculum feature by modifying the YAML. See the [ML-Agents Curriculum docs](https://unity-technologies.github.io/ml-agents/Training-ML-Agents/).

---

## Step 11: Monitor Training with TensorBoard

1. In a separate terminal (with the virtual environment activated):
   ```bash
   tensorboard --logdir results
   ```
2. Open a web browser and go to `http://localhost:6006`
3. You'll see graphs for:
   - **Cumulative Reward**: should increase over time
   - **Episode Length**: should decrease as the agent gets faster
   - **Policy Loss / Value Loss**: training stability indicators

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Agent doesn't move | Check that CarController has Rigidbody2D, gravity = 0, and the agent has a Decision Requester |
| Agent ignores walls | Verify `obstacleLayer` mask on CarAgent includes both `Obstacle` and `Boundary` layers |
| `vector_observation_size` mismatch error | Ensure Behavior Parameters → Vector Observation Size = 11 + rayCastCount |
| Waypoints not appearing | Check that WaypointManager has the Waypoint prefab assigned |
| Boundaries not working | Verify boundary walls are on the `Boundary` layer (check Step 2) |
| Training doesn't start | Make sure the YAML behavior name `CarAgent` matches the name in Behavior Parameters |
| `No module named mlagents` | Activate your Python virtual environment before running mlagents-learn |

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Agent/
│   │   ├── CarAgent.cs          — ML-Agents agent with observations, actions, rewards
│   │   └── CarController.cs     — Physics-based 2D car movement
│   ├── Environment/
│   │   ├── TrainingArea.cs      — Episode management, rewards, curriculum
│   │   └── WaypointManager.cs   — Waypoint generation and tracking
│   └── Utils/
│       └── TrainingAreaSpawner.cs — Grid instantiation of training areas
├── Prefabs/
│   ├── CarAgent.prefab
│   ├── TrainingArea.prefab
│   └── Waypoint.prefab
└── Scenes/
    └── TrainingScene.unity
```
