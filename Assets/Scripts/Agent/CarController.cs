using UnityEngine;

/// <summary>
/// Physics-based 2D top-down car movement controller.
/// Handles acceleration, steering, drag, wheel visuals, and light colors.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour
{
    [Header("Movement")]

    [Tooltip("Maximum speed in world units per second. Also used to normalize velocity observations for the agent — do not change during a training run or observation scale will be inconsistent. Increasing allows faster movement but may make control harder. Decreasing limits top speed. Recommended: 8–15.")]
    [SerializeField] private float maxSpeed = 10f;

    [Tooltip("Forward force applied to Rigidbody2D per physics step when throttle is non-zero. Increasing makes the car accelerate faster, which can make control less stable. Decreasing makes acceleration sluggish. Recommended: 15–40.")]
    [SerializeField] private float accelerationForce = 25f;

    [Tooltip("Rotation speed in degrees per second at maximum steer input. Increasing makes the car turn sharper and faster, which can cause spinning. Decreasing makes turns wide and slow. Recommended: 100–250.")]
    [SerializeField] private float turnSpeed = 180f;

    [Tooltip("Linear drag applied each FixedUpdate to simulate road friction. Increasing slows the car more quickly when not accelerating, giving tighter control. Decreasing lets the car coast further, making it feel slippery. Recommended: 2.0–6.0.")]
    [SerializeField] private float dragCoefficient = 3.5f;

    [Tooltip("How quickly the car's velocity realigns to its facing direction after turning. Higher values make turns sharper and more responsive — the car goes where it points. Lower values make the car slide and drift through turns. Recommended: 5–15 for tight control, 1–3 for drifty feel.")]
    [SerializeField] private float gripFactor = 10f;

    [Header("Wheel Visuals")]

    [Tooltip("Maximum rotation angle in degrees applied to front wheel sprites at full steer input. Increasing makes wheel turn visually more dramatic. Decreasing makes it subtle. This is purely visual and does not affect physics. Recommended: 25–45.")]
    [Range(0f, 90f)]
    [SerializeField] private float maxWheelAngle = 35f;

    [Header("Wheel References")]

    [Tooltip("Transform of the front-left wheel child object. Rotated on local Z axis based on steer input.")]
    [SerializeField] private Transform wheelFrontLeft;

    [Tooltip("Transform of the front-right wheel child object. Rotated on local Z axis based on steer input.")]
    [SerializeField] private Transform wheelFrontRight;

    [Tooltip("Transform of the rear-left wheel child object. Does not rotate.")]
    [SerializeField] private Transform wheelRearLeft;

    [Tooltip("Transform of the rear-right wheel child object. Does not rotate.")]
    [SerializeField] private Transform wheelRearRight;

    [Header("Light References")]

    [Tooltip("SpriteRenderer of the front-left headlight.")]
    [SerializeField] private SpriteRenderer headlightLeftRenderer;

    [Tooltip("SpriteRenderer of the front-right headlight.")]
    [SerializeField] private SpriteRenderer headlightRightRenderer;

    [Tooltip("SpriteRenderer of the rear-left taillight.")]
    [SerializeField] private SpriteRenderer taillightLeftRenderer;

    [Tooltip("SpriteRenderer of the rear-right taillight.")]
    [SerializeField] private SpriteRenderer taillightRightRenderer;

    [Header("Light Colors")]

    [Tooltip("Color of headlights when the car is moving forward (throttle > 0). Recommended: bright white or yellow.")]
    [SerializeField] private Color headlightBrightColor = Color.white;

    [Tooltip("Color of headlights when the car is idle or reversing. Recommended: dim gray or dark yellow.")]
    [SerializeField] private Color headlightDimColor = new Color(0.4f, 0.4f, 0.3f);

    [Tooltip("Color of taillights when the car is moving forward or idle (brake lights). Recommended: bright red.")]
    [SerializeField] private Color taillightBrakeColor = Color.red;

    [Tooltip("Color of taillights when the car is reversing (reverse lights). Recommended: white.")]
    [SerializeField] private Color taillightReverseColor = Color.white;

    [Tooltip("Color of taillights when the car is idle. Recommended: dim red or dark red.")]
    [SerializeField] private Color taillightDimColor = new Color(0.4f, 0.1f, 0.1f);

    private Rigidbody2D rb;
    private float currentThrottle;
    private float currentSteer;

    /// <summary>
    /// Current velocity of the car as a Vector2.
    /// </summary>
    public Vector2 Velocity => rb.velocity;

    /// <summary>
    /// Current speed (magnitude of velocity) of the car.
    /// </summary>
    public float Speed => rb.velocity.magnitude;

    /// <summary>
    /// Maximum speed value, used for normalizing observations.
    /// </summary>
    public float MaxSpeed => maxSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    /// <summary>
    /// Apply throttle and steering inputs. Called by the agent each step.
    /// </summary>
    /// <param name="throttle">Forward/backward input in range [-1, 1].</param>
    /// <param name="steer">Left/right steering input in range [-1, 1].</param>
    public void Move(float throttle, float steer)
    {
        currentThrottle = Mathf.Clamp(throttle, -1f, 1f);
        currentSteer = Mathf.Clamp(steer, -1f, 1f);
    }

    /// <summary>
    /// Resets the car's velocity and angular velocity to zero.
    /// </summary>
    public void ResetPhysics()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        currentThrottle = 0f;
        currentSteer = 0f;
    }

    private void FixedUpdate()
    {
        // Rotate first so force is applied in the new direction
        if (Speed > 0.01f)
        {
            float rotationAmount = -currentSteer * turnSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation + rotationAmount);
        }

        // Apply throttle in current facing direction
        Vector2 forward = transform.up;
        rb.AddForce(forward * currentThrottle * accelerationForce);

        // Realign velocity toward facing direction to prevent sliding
        if (rb.velocity.magnitude > 0.01f)
        {
            float forwardSpeed = Vector2.Dot(rb.velocity, forward);
            Vector2 alignedVelocity = forward * forwardSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, alignedVelocity, gripFactor * Time.fixedDeltaTime);
        }

        rb.velocity *= (1f - dragCoefficient * Time.fixedDeltaTime);

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        UpdateWheelVisuals();
        UpdateLightColors();
    }

    private void UpdateWheelVisuals()
    {
        float wheelAngle = currentSteer * maxWheelAngle;

        if (wheelFrontLeft != null)
            wheelFrontLeft.localRotation = Quaternion.Euler(0f, 0f, -wheelAngle);
        if (wheelFrontRight != null)
            wheelFrontRight.localRotation = Quaternion.Euler(0f, 0f, -wheelAngle);
    }

    private void UpdateLightColors()
    {
        Color headColor;
        Color tailColor;

        if (currentThrottle > 0f)
        {
            headColor = headlightBrightColor;
            tailColor = taillightBrakeColor;
        }
        else if (currentThrottle < 0f)
        {
            headColor = headlightDimColor;
            tailColor = taillightReverseColor;
        }
        else
        {
            headColor = headlightDimColor;
            tailColor = taillightDimColor;
        }

        if (headlightLeftRenderer != null) headlightLeftRenderer.color = headColor;
        if (headlightRightRenderer != null) headlightRightRenderer.color = headColor;
        if (taillightLeftRenderer != null) taillightLeftRenderer.color = tailColor;
        if (taillightRightRenderer != null) taillightRightRenderer.color = tailColor;
    }
}
