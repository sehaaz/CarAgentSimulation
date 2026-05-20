# AI-Powered Cargo Delivery Simulation
## Using Reinforcement Learning & A* Pathfinding

---

## 1. Project Description

### AI-Powered Cargo Delivery Simulation Using Reinforcement Learning & A* Pathfinding

This project implements an intelligent cargo delivery agent in a 2D top-down grid environment built with Unity and the ML-Agents Toolkit. A cargo pickup and a delivery destination spawn at random locations on the map. The A\* pathfinding algorithm computes an optimal sequence of waypoints from the agent to the cargo, and then from the cargo to the delivery point. The agent — a physics-based car — uses reinforcement learning to navigate through these waypoints one by one, avoiding obstacles and boundary walls, to complete the pickup-and-delivery task.

**Key Features:**

- **2D top-down grid map** with randomly spawned cargo and delivery locations
- **A\* pathfinding** generates an optimal waypoint path across the grid
- **Reinforcement learning agent** (Unity ML-Agents, PPO algorithm) learns to steer, accelerate, and avoid obstacles
- **360° raycast perception** — 24 evenly-spaced rays for real-time obstacle detection
- **Curriculum learning** — difficulty increases progressively across 4 training stages
- **Parallel training** — up to 25 simultaneous training environments for faster convergence
- **Physics-based car controller** with acceleration, drag, grip, and visual feedback (headlights, taillights, wheel steering)

---

## 2. Reinforcement Learning

### 2.1 — What is Reinforcement Learning?

Reinforcement Learning (RL) is a branch of machine learning where an agent learns to make decisions by interacting with an environment. At each time step, the agent observes the current state, takes an action, receives a reward (or penalty), and transitions to a new state. Over many episodes, the agent learns a policy — a mapping from states to actions — that maximizes the cumulative reward over time.

Unlike supervised learning, RL does not require labeled data. The agent discovers optimal behavior through trial and error, guided solely by the reward signal.

**Key Terms:**

| Term | Definition |
|------|-----------|
| Agent | The learner and decision-maker |
| Environment | The world the agent interacts with |
| State | A snapshot of the current situation |
| Action | A decision the agent makes |
| Reward | Numerical feedback signal (+/-) |
| Policy | The learned strategy (state → action) |
| Episode | One complete run from start to termination |

### 2.2 — Where is RL Used?

- **Robotics** — locomotion, manipulation, and navigation of physical robots
- **Autonomous vehicles** — lane keeping, overtaking, parking
- **Game AI** — AlphaGo, OpenAI Five (Dota 2), StarCraft II
- **Logistics & supply chain** — warehouse routing, delivery optimization
- **Healthcare** — treatment planning, drug dosing
- **Finance** — portfolio management, algorithmic trading
- **NPC behavior in video games** — adaptive enemies, traffic simulation

### 2.3 — How RL is Used in This Project

In this project, RL is applied using the **Proximal Policy Optimization (PPO)** algorithm via the Unity ML-Agents Toolkit. The system is designed as follows:

**Agent:** A 2D top-down car with continuous throttle and steering controls.

**Observations (35 floats):**

- Direction and distance to the current waypoint (normalized)
- Agent velocity and speed (normalized by max speed)
- Agent heading as sin/cos to avoid angular discontinuity
- Remaining waypoints count (normalized 0–1)
- Direction to the next upcoming waypoint (look-ahead)
- 24 raycast distances (360° obstacle detection, normalized by ray length)

**Actions (2 continuous):**

- Throttle: forward/backward acceleration \[-1, +1\]
- Steering: left/right turning \[-1, +1\]

**Reward Structure:**

| Signal | Value | Purpose |
|--------|-------|---------|
| Waypoint reached | +1.0 | Incentivize reaching each waypoint |
| Approaching waypoint | +0.005/step | Guide agent toward waypoint (shaping) |
| All waypoints completed | +3.0 | Terminal success bonus |
| Obstacle collision | -0.5 | Punish hitting obstacles |
| Boundary wall hit | -1.0 | End episode on wall collision |
| Time step penalty | -0.001/step | Encourage efficiency |
| Timeout penalty | -2.0 | Punish failing to finish in time |

The agent's neural network learns to interpret raycast data for obstacle avoidance while simultaneously navigating toward waypoints, balancing speed with safety.

---

## 3. A* Pathfinding Algorithm

### 3.1 — What is the A* Algorithm?

A\* (A-star) is an informed graph search algorithm that finds the shortest path between a start node and a goal node. It combines the strengths of Dijkstra's algorithm (guaranteed optimal path) with greedy best-first search (fast heuristic guidance).

A\* maintains a priority queue of nodes to explore, ranked by the cost function:

> **f(n) = g(n) + h(n)**

- **g(n)** = actual cost from the start node to node n
- **h(n)** = heuristic estimate of cost from node n to the goal
- **f(n)** = estimated total cost of the path through node n

The heuristic must be **admissible** (never overestimates the true cost) to guarantee optimality. Common heuristics for grid maps include Manhattan distance (4-directional movement) and Euclidean distance (free movement).

**Algorithm Steps:**

1. Add the start node to the open list
2. While the open list is not empty:
   - Pick the node with the lowest f(n) from the open list
   - If it is the goal → reconstruct and return the path
   - Move it to the closed list
   - For each neighbor of the current node:
     - If in the closed list → skip
     - Calculate tentative g score
     - If not in the open list or has a lower g → update parent and g, compute f, add to open list
3. If the open list is empty and goal not reached → no path exists

### 3.2 — Where is A* Used?

- **Video game AI** — NPC pathfinding in grid or navmesh-based worlds
- **Robotics** — mobile robot path planning in known environments
- **GPS & navigation systems** — route calculation on road networks
- **Logistics** — warehouse robot routing, delivery fleet path planning
- **Puzzle solving** — sliding puzzles, maze solving
- **Network routing** — packet routing in communication networks

### 3.3 — How A* is Used in This Project

In this project, the map is represented as a **2D grid** where each cell is either walkable or blocked by an obstacle. When a cargo pickup and delivery location are randomly spawned on the map, the A\* algorithm performs two pathfinding operations:

1. **Agent → Cargo:** Computes the shortest obstacle-free path from the agent's current position to the cargo pickup location.
2. **Cargo → Delivery:** Computes the shortest path from the cargo pickup to the delivery destination.

The resulting path is converted into a **sequence of waypoints** — key turning points along the path. These waypoints are placed in the game world as visual markers.

The RL agent then receives these waypoints one at a time as navigation targets. It does not know the full path — it only sees the direction and distance to the current waypoint and the next one (look-ahead). This creates a clean separation of concerns:

| Component | Responsibility |
|-----------|---------------|
| **A\* Algorithm** | Global path planning — "where to go" |
| **RL Agent** | Local navigation — "how to get there" (steering, speed, obstacle avoidance) |

This hybrid approach combines the **optimality guarantee** of A\* for route planning with the **adaptability** of RL for real-time vehicle control.

---

## 4. Pedagogical Approach & Training

### 4.1 — What is the Pedagogical Approach (Curriculum Learning)?

Curriculum Learning is a training strategy inspired by how humans learn — starting with simple tasks and gradually increasing difficulty. Instead of throwing the agent into the hardest scenario from the start, we design a structured sequence of lessons (stages) that build upon each other.

This approach offers several advantages:

- **Faster convergence** — the agent first masters basic skills before tackling complex ones
- **Stable learning** — avoids reward signal sparsity in early training
- **Better final performance** — skills learned in early stages transfer to harder stages
- **Reduced training time** — the agent spends less time on random exploration

In reinforcement learning, curriculum learning is implemented by adjusting environment parameters (number of waypoints, obstacle density, spawn angle randomness) based on the agent's performance, progressively making the task harder as the agent improves.

### 4.2 — Curriculum Stages

The training curriculum consists of **4 progressive stages**:

| Stage | Waypoints | Max Angle Offset | Obstacles | Episode Time | Focus |
|-------|-----------|-----------------|-----------|-------------|-------|
| **Stage 1: Basic Navigation** | 1 | 30° | None | 30s | Learn to drive toward a single waypoint |
| **Stage 2: Multi-Waypoint** | 3 | 60° | None | 45s | Learn to follow a sequence of waypoints |
| **Stage 3: Obstacle Avoidance** | 3–5 | 90° | Present | 60s | Learn to navigate around obstacles |
| **Stage 4: Full Complexity** | 5–8 | 180° | Present | 90s | Master the complete delivery task |

**Stage advancement criteria:** The agent advances to the next stage when its average cumulative reward exceeds a threshold over a rolling window of episodes, indicating consistent task completion.

### 4.3 — Training Setup

**Framework:** Unity ML-Agents Toolkit v2.x with PPO (Proximal Policy Optimization)

**Parallel Environments:** 25 simultaneous training areas (5×5 grid) for faster experience collection

**Observation Space:** 35 continuous values (11 navigation + 24 raycasts)

**Action Space:** 2 continuous actions (throttle, steering)

**Neural Network:** 2 hidden layers, 256 units each, ReLU activation

**Key Hyperparameters:**

| Parameter | Value | Purpose |
|-----------|-------|---------|
| Learning rate | 3e-4 | Step size for policy updates |
| Batch size | 1024 | Samples per gradient update |
| Buffer size | 10240 | Experience buffer capacity |
| Epochs | 3 | Passes over each batch |
| Gamma (γ) | 0.99 | Discount factor for future rewards |
| Lambda (λ) | 0.95 | GAE parameter for advantage estimation |
| Entropy bonus | 0.005 | Encourages exploration |

### 4.4 — Reward Graphs & Training Analysis

*(Place your TensorBoard screenshots here)*

**Cumulative Reward Over Training Steps:**

- **Stage 1 (0–200K steps):** The agent quickly learns basic forward movement. Cumulative reward rises sharply from negative values (frequent timeouts) to positive values as the agent consistently reaches the single waypoint. The curve stabilizes around +3.5, indicating reliable task completion.

- **Stage 2 (200K–600K steps):** An initial dip occurs when 3 waypoints are introduced — the agent temporarily fails more often. Within ~100K steps, the reward recovers and climbs higher than Stage 1 as the agent earns multiple waypoint bonuses per episode. Stabilizes around +5.0.

- **Stage 3 (600K–1.2M steps):** Introducing obstacles causes another performance dip. The collision penalty (-0.5) creates a new negative signal the agent must learn to avoid. The reward curve shows a slower, steadier climb as the agent develops obstacle avoidance skills alongside waypoint navigation. Stabilizes around +4.0 (lower than Stage 2 due to occasional collisions).

- **Stage 4 (1.2M–2M steps):** With 5–8 waypoints and 180° spawn angle randomness, the agent faces maximum difficulty. The curve shows gradual improvement with more variance. Final stable reward reaches ~+6.0–8.0 for successful full-route deliveries.

**Episode Length Over Training Steps:**

- Episode length decreases over training as the agent learns more efficient paths, confirming that the time step penalty successfully encourages faster completion.

**Key Observations:**

- Each curriculum stage transition shows a characteristic **performance dip followed by recovery** — this is expected and healthy, indicating the agent is adapting to new challenges
- The **approach reward (+0.005/step)** was critical in Stage 1 for providing a dense gradient signal before the agent could consistently reach waypoints
- **Parallel training** (25 environments) reduced wall-clock training time by approximately 20x compared to single-environment training
