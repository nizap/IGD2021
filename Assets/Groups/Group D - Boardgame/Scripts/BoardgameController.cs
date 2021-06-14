using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class BoardgameController : MonoBehaviour
{
    public GameObject Minifig;
    // Constants.
    const float stickyTime = 0.05f;
    const float stickyForce = 9.6f;
    const float coyoteDelay = 0.1f;

    const float distanceEpsilon = 0.1f;
    const float angleEpsilon = 0.1f;

    public Vector3 tileCenterOffset;

    private bool animationCompleted = true;

    // Internal classes used to define targets when automatically animating.
    class MoveTarget
    {
        public Vector3 destination;
        public float minDistance;
        public Action onComplete;
        public float onCompleteDelay;
        public float moveDelay;
        public bool cancelSpecial;
        public float speedMultiplier;
        public float rotationSpeedMultiplier;
        public Vector3? turnToWhileCompleting;
    }

    class FollowTarget
    {
        public Transform target;
        public float minDistance;
        public Action onComplete;
        public float onCompleteDelay;
        public float followDelay;
        public bool cancelSpecial;
        public float speedMultiplier;
        public float rotationSpeedMultiplier;
        public Vector3? turnToWhileCompleting;
    }

    class TurnTarget
    {
        public Vector3 target;
        public float minAngle;
        public Action onComplete;
        public float onCompleteDelay;
        public float turnDelay;
        public bool cancelSpecial;
        public float rotationSpeedMultiplier;
    }

    // State used when automatically animating.
    enum State
    {
        Idle,
        Moving,
        CompletingMove,

        Turning,
        CompletingTurn,

        Following,
        CompletingFollow,
    }

    [Header("Movement")]

    public float maxForwardSpeed = 5f;
    [Range(4, 8)]
    public float maxBackwardSpeed = 4f;
    [Range(1, 60)]
    public float acceleration = 20.0f;
    // TODO Add sensible range when animations are fixed.
    [Range(0, 500)]
    public float maxRotateSpeed = 150f;
    [Range(0, 2500)]
    public float rotateAcceleration = 600f;
    public float jumpSpeed = 20f;
    public float gravity = 40f;
    private Vector2 _movement = new Vector2();

    [Header("Audio")]

    public List<AudioClip> stepAudioClips = new List<AudioClip>();
    public AudioClip jumpAudioClip;
    public AudioClip doubleJumpAudioClip;
    public AudioClip landAudioClip;
    public AudioClip explodeAudioClip;
    public AudioClip receiveItemAudioClip;

    [Header("Controls")]
    [SerializeField]
    bool inputEnabled = false;
    [SerializeField, Range(0, 10)]
    int maxJumpsInAir = 1;

    private bool inputEnabledCached = false; // used to fix a bug in SetState()

    public enum SpecialAnimation
    {
        AirGuitar = 0,
        Ballerina = 1,
        Crawl = 2,
        CrawlOnWallLeft = 3,
        CrawlOnWallRight = 4,
        Dance = 5,
        Flexing = 6,
        Flip_No_Y_Axis = 7,
        HatSwap = 8,
        HatSwap2 = 9,
        Idle_Light = 10,
        // Unused 11
        IdleHeavy = 12,
        IdleImpatient = 13,
        Jump = 14,
        Jump_GoingDown = 15,
        Jump_GoingUp = 16,
        Jump_LandingBounce = 17,
        Jump_Midair = 18,
        Jump_NoLanding_No_Y_Axis = 19,
        Jump_wBounce = 20,
        KickRightFoot = 21,
        Laughing = 22,
        LeftHandReaction = 23,
        LegReactive = 24,
        LookAbove = 25,
        LookingAround = 26,
        LookingDown = 27,
        LookingUp = 28,
        MoonWalk = 29,
        PantsSwap = 30,
        RightHandReactive = 31,
        Run = 32,
        // Unused 33 
        Skipping = 34,
        Sleeping = 35,
        Slide = 36,
        Sneezing = 37,
        Snoozing = 38,
        Spiderman = 39,
        Spin = 40,
        Stretching = 41,
        Superman_Flying = 42,
        Superman_Setoff = 43,
        Torso_Reactive = 44,
        Turn_90_Left = 45,
        Turn_90_Right = 46,
        Walk = 47,
        WalkBackwards = 48,
        Wave = 49
    }


    CharacterController controller;
    Animator animator;
    AudioSource audioSource;

    bool airborne;
    float airborneTime;
    int jumpsInAir;
    Vector3 directSpeed;
    bool exploded;
    bool stepped;

    List<MoveTarget> moves = new List<MoveTarget>();
    MoveTarget currentMove;
    FollowTarget currentFollowTarget;
    TurnTarget currentTurnTarget;
    State state;
    float waitedTime = 0.0f;

    float speed;
    float rotateSpeed;
    Vector3 moveDelta = Vector3.zero;
    bool stopSpecial;
    bool cancelSpecial;

    float externalRotation;
    Vector3 externalMotion;

    Transform groundedTransform;
    Vector3 groundedLocalPosition;
    Vector3 oldGroundedPosition;
    Quaternion oldGroundedRotation;

    int speedHash = Animator.StringToHash("Speed");
    int rotateSpeedHash = Animator.StringToHash("Rotate Speed");
    int groundedHash = Animator.StringToHash("Grounded");
    int jumpHash = Animator.StringToHash("Jump");
    int playSpecialHash = Animator.StringToHash("Play Special");
    int cancelSpecialHash = Animator.StringToHash("Cancel Special");
    int specialIdHash = Animator.StringToHash("Special Id");

    Action<bool> onSpecialComplete;

    void OnValidate()
    {
        maxForwardSpeed = Mathf.Clamp(maxForwardSpeed, 5, 30);
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = Minifig.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Initialise animation.
        animator.SetBool(groundedHash, true);

        // Make sure the Character Controller is grounded if starting on the ground.
        controller.Move(Vector3.down * 0.01f);
    }

    private void Start()
    {
        string controlScheme = GetComponent<PlayerInput>().defaultControlScheme;
        GetComponent<PlayerInput>().SwitchCurrentControlScheme(controlScheme, Keyboard.current);
        inputEnabledCached = inputEnabled;
    }

    void Update()
    {
        if (exploded)
        {
            return;
        }

        // Handle input.
        if (inputEnabled)
        {
            // Calculate direct speed and speed.
            var right = Vector3.right;
            var forward = Vector3.forward;
            if (Camera.main)
            {
                right = Camera.main.transform.right;
                right.y = 0.0f;
                right.Normalize();
                forward = Camera.main.transform.forward;
                forward.y = 0.0f;
                forward.Normalize();
            }

            var targetSpeed = right * _movement.x;
            targetSpeed += forward * _movement.y;
            if (targetSpeed.sqrMagnitude > 0.0f)
            {
                targetSpeed.Normalize();
            }
            targetSpeed *= maxForwardSpeed;

            var speedDiff = targetSpeed - directSpeed;
            if (speedDiff.sqrMagnitude < acceleration * acceleration * Time.deltaTime * Time.deltaTime)
            {
                directSpeed = targetSpeed;
            }
            else if (speedDiff.sqrMagnitude > 0.0f)
            {
                speedDiff.Normalize();

                directSpeed += speedDiff * acceleration * Time.deltaTime;
            }
            speed = directSpeed.magnitude;

            // Calculate rotation speed - ignore rotate acceleration.
            rotateSpeed = 0.0f;
            if (targetSpeed.sqrMagnitude > 0.0f)
            {
                var localTargetSpeed = transform.InverseTransformDirection(targetSpeed);
                var angleDiff = Vector3.SignedAngle(Vector3.forward, localTargetSpeed.normalized, Vector3.up);

                if (angleDiff > 0.0f)
                {
                    rotateSpeed = maxRotateSpeed;
                }
                else if (angleDiff < 0.0f)
                {
                    rotateSpeed = -maxRotateSpeed;
                }

                // Assumes that x > NaN is false - otherwise we need to guard against Time.deltaTime being zero.
                if (Mathf.Abs(rotateSpeed) > Mathf.Abs(angleDiff) / Time.deltaTime)
                {
                    rotateSpeed = angleDiff / Time.deltaTime;
                }
            }

            // Calculate move delta.
            moveDelta = new Vector3(directSpeed.x, moveDelta.y, directSpeed.z);



            // Check if player is grounded.
            if (!airborne)
            {
                jumpsInAir = maxJumpsInAir;
            }


            // Cancel special.
            cancelSpecial = !Mathf.Approximately(Input.GetAxis("Vertical"), 0) || !Mathf.Approximately(Input.GetAxis("Horizontal"), 0) || Input.GetButtonDown("Jump");

        }
        else
        {
            // Handle automatic animation.

            waitedTime += Time.deltaTime;

            switch (state)
            {
                case State.Idle:
                    {
                        // Stop moving.
                        MoveInDirection(transform.forward, 0.0f, false, 0.0f, 0.0f, 0.0f, false);
                        break;
                    }
                case State.Moving:
                    {
                        if (waitedTime > currentMove.moveDelay)
                        {
                            var direction = currentMove.destination - transform.position;

                            // Neutralize y component.
                            direction.y = 0.0f;

                            if (direction.magnitude > currentMove.minDistance + distanceEpsilon)
                            {
                                var shouldBreak = currentMove.onCompleteDelay > 0.0f || moves.Count == 1 || (moves.Count > 1 && moves[1].moveDelay > 0.0f);
                                MoveInDirection(direction, currentMove.minDistance, shouldBreak, currentMove.speedMultiplier, 0.0f, currentMove.rotationSpeedMultiplier, currentMove.cancelSpecial);
                            }
                            else
                            {
                                if (currentMove.onCompleteDelay > 0.0f)
                                {
                                    SetState(State.CompletingMove);
                                }
                                else
                                {
                                    CompleteMove();
                                }
                            }
                        }
                        else
                        {
                            // Set speed, move delta and rotation speed.
                            speed = 0.0f;
                            moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);
                            rotateSpeed = 0.0f;
                        }
                    }
                    break;
                case State.CompletingMove:
                    {
                        // Possibly turn to position.
                        if (currentMove.turnToWhileCompleting.HasValue)
                        {
                            var turnToDirection = currentMove.turnToWhileCompleting.Value - transform.position;
                            if (turnToDirection.magnitude > distanceEpsilon)
                            {
                                TurnToDirection(turnToDirection, 0.0f, currentMove.rotationSpeedMultiplier, currentMove.cancelSpecial);
                            }
                        }

                        if (waitedTime > currentMove.onCompleteDelay)
                        {
                            CompleteMove();
                        }
                    }
                    break;
                case State.Turning:
                    {
                        if (waitedTime > currentTurnTarget.turnDelay)
                        {
                            var direction = currentTurnTarget.target - transform.position;

                            // Neutralize y component.
                            direction.y = 0.0f;

                            if (direction.magnitude > distanceEpsilon)
                            {
                                TurnToDirection(direction, currentTurnTarget.minAngle, currentTurnTarget.rotationSpeedMultiplier, currentTurnTarget.cancelSpecial);
                            }

                            if (Vector3.Angle(transform.forward, direction) <= currentTurnTarget.minAngle + angleEpsilon)
                            {
                                if (currentTurnTarget.onCompleteDelay > 0.0f)
                                {
                                    SetState(State.CompletingTurn);
                                }
                                else
                                {
                                    CompleteTurn();
                                }
                            }
                        }
                        else
                        {
                            // Set speed, move delta and rotation speed.
                            speed = 0.0f;
                            moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);
                            rotateSpeed = 0.0f;
                        }
                        break;
                    }
                case State.CompletingTurn:
                    {
                        if (waitedTime > currentTurnTarget.onCompleteDelay)
                        {
                            CompleteTurn();
                        }
                        break;
                    }
                case State.Following:
                    {
                        if (waitedTime > currentFollowTarget.followDelay)
                        {
                            var direction = currentFollowTarget.target.position - transform.position;

                            // Neutralize y component.
                            direction.y = 0.0f;

                            if (direction.magnitude > currentFollowTarget.minDistance + distanceEpsilon)
                            {
                                var shouldBreak = currentFollowTarget.onCompleteDelay > 0.0f || (moves.Count > 0 && moves[0].moveDelay > 0.0f);
                                MoveInDirection(direction, currentFollowTarget.minDistance, shouldBreak, currentFollowTarget.speedMultiplier, 0.0f, currentFollowTarget.rotationSpeedMultiplier, currentFollowTarget.cancelSpecial);
                            }
                            else
                            {
                                if (currentFollowTarget.onCompleteDelay > 0.0f)
                                {
                                    SetState(State.CompletingFollow);
                                }
                                else
                                {
                                    CompleteFollow();
                                }
                            }
                        }
                        else
                        {
                            // Set speed, move delta and rotation speed.
                            speed = 0.0f;
                            moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);
                            rotateSpeed = 0.0f;
                        }
                        break;
                    }
                case State.CompletingFollow:
                    {
                        var direction = currentFollowTarget.target.position - transform.position;

                        // Neutralize y component.
                        direction.y = 0.0f;

                        if (direction.magnitude > currentFollowTarget.minDistance + distanceEpsilon)
                        {
                            // Start following again, with no delay.
                            SetState(State.Following, currentFollowTarget.followDelay);
                        }
                        else
                        {
                            // Possibly turn to position.
                            if (currentFollowTarget.turnToWhileCompleting.HasValue)
                            {
                                var turnToDirection = currentFollowTarget.turnToWhileCompleting.Value - transform.position;
                                if (turnToDirection.magnitude > distanceEpsilon)
                                {
                                    TurnToDirection(direction, 0.0f, currentFollowTarget.rotationSpeedMultiplier, currentFollowTarget.cancelSpecial);
                                }
                            }

                            if (waitedTime > currentFollowTarget.onCompleteDelay)
                            {
                                CompleteFollow();
                            }
                        }
                        break;
                    }
            }
        }

        // Handle external motion.
        externalMotion = Vector3.zero;
        externalRotation = 0.0f;

        var wasGrounded = controller.isGrounded;

        if (!controller.isGrounded)
        {
            // Apply gravity.
            moveDelta.y -= gravity * Time.deltaTime;

            groundedTransform = null;

            airborneTime += Time.deltaTime;
        }
        else
        {
            // Apply external motion and rotation.
            if (groundedTransform && Time.deltaTime > 0.0f)
            {
                var newGroundedPosition = groundedTransform.TransformPoint(groundedLocalPosition);
                externalMotion = (newGroundedPosition - oldGroundedPosition) / Time.deltaTime;
                oldGroundedPosition = newGroundedPosition;

                var newGroundedRotation = groundedTransform.rotation;
                // FIXME Breaks down if rotating more than 180 degrees per frame.
                var diffRotation = newGroundedRotation * Quaternion.Inverse(oldGroundedRotation);
                var rotatedRight = diffRotation * Vector3.right;
                rotatedRight.y = 0.0f;
                if (rotatedRight.magnitude > 0.0f)
                {
                    rotatedRight.Normalize();
                    externalRotation = Vector3.SignedAngle(Vector3.right, rotatedRight, Vector3.up) / Time.deltaTime;
                }
                oldGroundedRotation = newGroundedRotation;
            }
        }

        // Move minifig - check if game object was made inactive in some callback to avoid warnings from CharacterController.Move.
        if (gameObject.activeInHierarchy)
        {
            // Use a sticky move to make the minifig stay with moving platforms.
            var stickyMove = airborneTime < stickyTime ? Vector3.down * stickyForce * Time.deltaTime : Vector3.zero;
            controller.Move((moveDelta + externalMotion) * Time.deltaTime + stickyMove);
        }

        // If becoming grounded by this Move, reset y movement and airborne time.
        if (!wasGrounded && controller.isGrounded)
        {
            // Play landing sound if landing sufficiently hard.
            if (moveDelta.y < -5.0f)
            {
                if (landAudioClip)
                {
                    audioSource.PlayOneShot(landAudioClip);
                }
            }

            moveDelta.y = 0.0f;
            airborneTime = 0.0f;
        }

        // Update airborne state.
        airborne = airborneTime >= coyoteDelay;

        // Rotate minifig.
        transform.Rotate(0, (rotateSpeed + externalRotation) * Time.deltaTime, 0);

        // Stop special if requested.
        cancelSpecial |= stopSpecial;
        stopSpecial = false;

        // Update animation - delay airborne animation slightly to avoid flailing arms when falling a short distance.
        animator.SetBool(cancelSpecialHash, cancelSpecial);
        animator.SetFloat(speedHash, speed);
        animator.SetFloat(rotateSpeedHash, rotateSpeed);
        animator.SetBool(groundedHash, !airborne);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        inputEnabledCached = enabled; // also overwrite the cached value
    }

    public void PlayPickupSound()
    {
        audioSource.PlayOneShot(receiveItemAudioClip);
    }

    public void PlaySpecialAnimation(SpecialAnimation animation, AudioClip specialAudioClip = null, Action<bool> onSpecialComplete = null)
    {
        animator.SetBool(playSpecialHash, true);
        animator.SetInteger(specialIdHash, (int)animation);

        if (specialAudioClip)
        {
            audioSource.PlayOneShot(specialAudioClip);
        }

        this.onSpecialComplete = onSpecialComplete;
    }

    public void StopSpecialAnimation()
    {
        stopSpecial = true;
    }

    /// <summary>
    /// Do not call this directly.
    /// Called from SpecialAnimationBehaviour to signal that a special animation finished.
    /// </summary>
    public void SpecialAnimationFinished()
    {
        // Do callback.
        onSpecialComplete?.Invoke(animator.GetBool(cancelSpecialHash));
    }

    public void TeleportTo(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }



    public void MoveTo(Vector3 destination, float minDistance = 0.0f, Action onComplete = null, float onCompleteDelay = 0.0f,
        float moveDelay = 0.0f, bool cancelSpecial = true, float speedMultiplier = 1.0f, float rotationSpeedMultiplier = 1.0f, Vector3? turnToWhileCompleting = null)
    {
        MoveTarget move = new MoveTarget()
        {
            destination = destination,
            minDistance = minDistance,
            onComplete = onComplete,
            onCompleteDelay = onCompleteDelay,
            moveDelay = moveDelay,
            cancelSpecial = cancelSpecial,
            speedMultiplier = speedMultiplier,
            rotationSpeedMultiplier = rotationSpeedMultiplier,
            turnToWhileCompleting = turnToWhileCompleting
        };

        moves.Add(move);

        UpdateState();
    }

    public void ClearMoves()
    {
        moves.Clear();

        UpdateState();
    }

    public void TurnTo(Vector3 target, float minAngle = 0.0f, Action onComplete = null, float onCompleteDelay = 0.0f,
        float turnDelay = 0.0f, bool cancelSpecial = false, float rotationSpeedMultiplier = 1.0f)
    {
        TurnTarget turnTarget = new TurnTarget()
        {
            target = target,
            minAngle = minAngle,
            onComplete = onComplete,
            onCompleteDelay = onCompleteDelay,
            turnDelay = turnDelay,
            cancelSpecial = cancelSpecial,
            rotationSpeedMultiplier = rotationSpeedMultiplier
        };

        currentTurnTarget = turnTarget;

        UpdateState();
    }

    public void StopTurning()
    {
        currentTurnTarget = null;
        UpdateState();
    }

    public void Follow(Transform target, float minDistance = 0.0f, Action onComplete = null, float onCompleteDelay = 0.0f,
        float followDelay = 0.0f, bool cancelSpecial = true, float speedMultiplier = 1.0f, float rotationSpeedMultiplier = 1.0f, Vector3? turnToWhileCompleting = null)
    {
        FollowTarget followTarget = new FollowTarget()
        {
            target = target,
            minDistance = minDistance,
            onComplete = onComplete,
            onCompleteDelay = onCompleteDelay,
            followDelay = followDelay,
            cancelSpecial = cancelSpecial,
            speedMultiplier = speedMultiplier,
            rotationSpeedMultiplier = rotationSpeedMultiplier,
            turnToWhileCompleting = turnToWhileCompleting
        };

        currentFollowTarget = followTarget;

        UpdateState();
    }

    public void StopFollowing()
    {
        currentFollowTarget = null;
        UpdateState();
    }

    // Animation event.
    public void StepFoot()
    {
        if (!stepped)
        {
            if (stepAudioClips.Count > 0)
            {
                var stepAudioClip = stepAudioClips[UnityEngine.Random.Range(0, stepAudioClips.Count)];
                if (stepAudioClip)
                {
                    audioSource.PlayOneShot(stepAudioClip);
                }
            }
        }
        stepped = true;
    }

    // Animation event.
    public void LiftFoot()
    {
        stepped = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (controller.isGrounded)
        {
            RaycastHit raycastHit;
            if (Physics.SphereCast(transform.position + Vector3.up * 0.8f, 0.8f, Vector3.down, out raycastHit, 0.1f, -1, QueryTriggerInteraction.Ignore))
            {
                groundedTransform = raycastHit.collider.transform;
                oldGroundedPosition = raycastHit.point;
                groundedLocalPosition = groundedTransform.InverseTransformPoint(oldGroundedPosition);
                oldGroundedRotation = groundedTransform.rotation;
            }
        }
    }

    // Assumes that direction is not a zero vector.
    void MoveInDirection(Vector3 direction, float minDistance, bool breakBeforeTarget, float speedMultiplier, float minAngle, float rotationSpeedMultiplier, bool cancelSpecial)
    {
        // Distance required to stop current speed.
        var breakDistance = (speed * speed) / (2.0f * acceleration);

        // If breaking before target, only set target speed if still possible to break.
        var targetSpeed = 0.0f;
        if (!breakBeforeTarget || direction.magnitude - minDistance > breakDistance + distanceEpsilon)
        {
            targetSpeed = speedMultiplier * maxForwardSpeed;
        }

        // Adjust speed based on target speed.
        if (targetSpeed > speed)
        {
            speed = Mathf.Min(targetSpeed, speed + acceleration * Time.deltaTime);
        }
        else if (targetSpeed < speed)
        {
            speed = Mathf.Max(targetSpeed, speed - acceleration * Time.deltaTime);
        }

        // Calculate move delta - prevent overshoot by limiting speed.
        // Assumes that direction is not zero length as it will cause issues when Time.deltaTime is zero.
        var moveDirection = direction.normalized * Mathf.Min(speed, direction.magnitude / Time.deltaTime);
        moveDelta = new Vector3(moveDirection.x, moveDelta.y, moveDirection.z);

        // Angle required to stop current rotate speed.
        var breakAngle = (rotateSpeed * rotateSpeed) / (2.0f * rotateAcceleration);

        // Only set target rotate speed if still possible to break.
        var targetRotateSpeed = 0.0f;
        var angleDiff = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);

        if (Mathf.Abs(angleDiff) - minAngle > breakAngle + angleEpsilon)
        {
            targetRotateSpeed = Mathf.Clamp(angleDiff, -1.0f, 1.0f) * rotationSpeedMultiplier * maxRotateSpeed;
        }

        // Adjust rotate speed based on target rotate speed.
        if (targetRotateSpeed > rotateSpeed)
        {
            rotateSpeed = Mathf.Min(targetRotateSpeed, rotateSpeed + rotateAcceleration * Time.deltaTime);
        }
        else if (targetRotateSpeed < rotateSpeed)
        {
            rotateSpeed = Mathf.Max(targetRotateSpeed, rotateSpeed - rotateAcceleration * Time.deltaTime);
        }

        // Prevent overshoot by limiting rotateSpeed.
        if (angleDiff < 0.0f && rotateSpeed < 0.0f)
        {
            rotateSpeed = Mathf.Max(rotateSpeed, angleDiff / Time.deltaTime);
        }
        else if (angleDiff > 0.0f && rotateSpeed > 0.0f)
        {
            rotateSpeed = Mathf.Min(rotateSpeed, angleDiff / Time.deltaTime);
        }

        // Cancel special.
        this.cancelSpecial = cancelSpecial;
    }

    // Assumes the direction is not a zero vector.
    void TurnToDirection(Vector3 direction, float minAngle, float rotationSpeedMultiplier, bool cancelSpecial)
    {
        speed = Mathf.Max(0.0f, speed - acceleration * Time.deltaTime);

        // Calculate move delta - prevent overshoot by limiting speed.
        var moveDirection = transform.forward * speed;
        moveDelta = new Vector3(moveDirection.x, moveDelta.y, moveDirection.z);

        // Angle required to stop current rotate speed.
        var breakAngle = (rotateSpeed * rotateSpeed) / (2.0f * rotateAcceleration);

        // Only set target rotate speed if still possible to break.
        var targetRotateSpeed = 0.0f;
        var angleDiff = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);

        if (Mathf.Abs(angleDiff) - minAngle > breakAngle + angleEpsilon)
        {
            targetRotateSpeed = Mathf.Clamp(angleDiff, -1.0f, 1.0f) * rotationSpeedMultiplier * maxRotateSpeed;
        }

        // Adjust rotate speed based on target rotate speed.
        if (targetRotateSpeed > rotateSpeed)
        {
            rotateSpeed = Mathf.Min(targetRotateSpeed, rotateSpeed + rotateAcceleration * Time.deltaTime);
        }
        else if (targetRotateSpeed < rotateSpeed)
        {
            rotateSpeed = Mathf.Max(targetRotateSpeed, rotateSpeed - rotateAcceleration * Time.deltaTime);
        }

        // Prevent overshoot by limiting rotateSpeed.
        if (angleDiff < 0.0f && rotateSpeed < 0.0f)
        {
            rotateSpeed = Mathf.Max(rotateSpeed, angleDiff / Time.deltaTime);
        }
        else if (angleDiff > 0.0f && rotateSpeed > 0.0f)
        {
            rotateSpeed = Mathf.Min(rotateSpeed, angleDiff / Time.deltaTime);
        }

        // Cancel special.
        this.cancelSpecial = cancelSpecial;
    }

    void SetState(State newState, float initialWaitedTime = 0.0f)
    {
        waitedTime = initialWaitedTime;

        // Stop cancelling special.
        cancelSpecial = false;

        // Disable input if not idle.
        if (state == State.Idle && newState != State.Idle) {
            // store the old value (only the Idle state contains the "real" value)
            inputEnabledCached = inputEnabled;
        }
        if (newState != State.Idle)
        {
            inputEnabled = false;
        } 
        else 
        {
            // restore the old value
            inputEnabled = inputEnabledCached;
        }
        
        state = newState;

        switch (state)
        {
            case State.Moving:
                {
                    currentMove = moves[0];
                    break;
                }
            case State.CompletingMove:
                {
                    // Set speed and move delta.
                    speed = 0.0f;
                    moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);

                    break;
                }
            case State.Idle:
            case State.CompletingTurn:
            case State.CompletingFollow:
                {
                    // Set speed, move delta and rotation speed.
                    speed = 0.0f;
                    moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);
                    rotateSpeed = 0.0f;

                    break;
                }
        }
    }

    void UpdateState()
    {
        if (currentTurnTarget != null)
        {
            SetState(State.Turning);
        }
        else if (currentFollowTarget != null)
        {
            SetState(State.Following);
        }
        else if (moves.Count > 0)
        {
            SetState(State.Moving);
        }
        else
        {
            SetState(State.Idle);
        }
    }

    void CompleteMove()
    {
        // Remove move from queue.
        if (moves.Count > 0)
        {
            moves.RemoveAt(0);
        }

        // Do callbacks.
        currentMove.onComplete?.Invoke();

        UpdateState();
    }

    void CompleteTurn()
    {
        var completeFunc = currentTurnTarget.onComplete;
        currentTurnTarget = null;

        completeFunc?.Invoke();

        UpdateState();
    }

    void CompleteFollow()
    {
        var completeFunc = currentFollowTarget.onComplete;
        currentFollowTarget = null;

        completeFunc?.Invoke();

        UpdateState();
    }

    #region Input Handling
    // Input ------------------------------------------------------------------------------------------

    private void OnMove(InputValue value)
    {
        print("OnMove");
    }

    private void jump() {
        if (jumpAudioClip)
        {
            audioSource.PlayOneShot(jumpAudioClip);
        }

        moveDelta.y = jumpSpeed;
        animator.SetTrigger(jumpHash);

        airborne = true;
        airborneTime = coyoteDelay;
    }

    /// moves this player to the given tile (should be a neighboring tile, or the animation will be weird)
    /// must not be called when  animationDone() == false
    public void MoveToTile(Tile nextTile) { 
        animationCompleted = false;

        Vector3 nextPos = nextTile.getPosition() + tileCenterOffset;

        // Turn in the direction of the next tile. When done, move there.
        TurnTo(nextPos, 0.0f, () => {
            jump();
            MoveTo(nextPos, 0.0f, () => {
                animationCompleted = true;
            });
        });
    }

    /// returns whether any (movement) animation was completed
    public bool animationDone() {
        // TODO: extend to support animations other than moving around
        return !airborne && animationCompleted;
    }

    
    public Vector3 getPlayerPosition() {
        return transform.position;
    }


    #endregion
}


