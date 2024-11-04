using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BearAI : MonoBehaviour
{
    public Animator animator;
    public float speed = 2.0f;
    public float runSpeed = 4.0f;
    public float slopeLimit = 30.0f; // Maximum slope angle the goat can move up
    public LayerMask terrainLayer; // Layer to detect terrain
    public float terrainCheckDistance = 2.0f; // Distance to check terrain ahead
    public float gravity = -9.81f; // Gravity force
    public float stuckThreshold = 0.1f; // Distance threshold to consider the goat stuck
    public float stuckTime = 2.0f; // Time threshold to consider the goat stuck
    public float directionChangeInterval = 3.0f; // Time interval for random direction changes
    public float preyDetectionRange = 10f;

    public float health = 100f;    // New Health variable
    public float hunger = 100f;    // New Hunger variable
    public float hungerDecreaseRate = 1f;    // Hunger reduction rate
    public float healthDecreaseRate = 2f;    // Health reduction rate when hunger is low
    public bool isEating = false;
    public bool isMoving = true;
    public bool isChasing = false;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private float stuckTimer;
    private float directionChangeTimer;

    private enum State { Idle, Walk, Run }
    private State currentState;
    private int previousStateIndex = -1;

    // Weights for each state (Idle, Walk, Run)
    private float[] stateWeights = { 1.0f, 1.0f, 1.0f };

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = slopeLimit;
        lastPosition = transform.position;
        StartCoroutine(ChangeStatePeriodically(5.0f));
    }

    void Update()
    {
        MoveBasedOnState();
        CheckIfStuck();
        UpdateRandomDirection();
        UpdateHungerAndHealth(); 
    }

    IEnumerator ChangeStatePeriodically(float interval)
    {
        while (true)
        {
            ChooseRandomState();
            yield return new WaitForSeconds(interval);
        }
    }

    bool IsPreyNearby(out Vector3 preyDirection)
    {
        preyDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, preyDetectionRange);
        float closestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Prey"))
            {
                Vector3 directionToprey = hitCollider.transform.position - transform.position;
                float distance = directionToprey.magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    preyDirection = directionToprey;
                }
            }
        }

        return preyDirection != Vector3.zero;
    }

    void ChooseRandomState()
    {
        Vector3 preyDirection;
        if (IsPreyNearby(out preyDirection))
        {
            currentState = State.Run;
        }
        else
        {
            State[] states = { State.Idle, State.Walk, State.Run };
            int chosenStateIndex = WeightedRandomChoice(stateWeights);
            currentState = states[chosenStateIndex];
            UpdateStateWeights(chosenStateIndex);
            previousStateIndex = chosenStateIndex;
        }
        UpdateAnimatorState();
    }

    void MoveBasedOnState()
    {
        Vector3 movement = Vector3.zero;

        if (currentState != State.Idle)
        {
            Vector3 forwardDirection = transform.forward;
            Vector3 preyDirection;
            float currentSpeed = (currentState == State.Walk) ? speed : runSpeed;

            if (IsPreyNearby(out preyDirection))
            {
                forwardDirection = preyDirection.normalized;
                currentSpeed = runSpeed;
                Quaternion targetRotation = Quaternion.LookRotation(forwardDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.5f * Time.deltaTime);
                isChasing=true;
            }
            else
            {
                forwardDirection = transform.forward;
                
            }
            if (CanMoveForward(forwardDirection, terrainCheckDistance))
            {
                // Move forward
                movement = forwardDirection * currentSpeed;
            }
            else
            {
                // Change direction if a steep slope is detected
                ChangeDirection();
            }
            
        }

        // Apply gravity
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;

        // Combine horizontal movement with vertical velocity
        Vector3 finalMovement = movement + new Vector3(0, velocity.y, 0);

        // Move the character
        controller.Move(finalMovement * Time.deltaTime);

        // Update animation based on actual movement
        bool isMoving = movement.magnitude > 0.1f;
        animator.SetBool("walk", isMoving && currentState != State.Run);
        animator.SetBool("Run", isMoving && currentState == State.Run);
    }

    void UpdateAnimatorState()
    {
        // Reset all movement states
        animator.SetBool("walk", false);
        animator.SetBool("Run", false);
        animator.SetBool("isEating", false);

        if (isEating)
        {
            animator.SetBool("isEating", true);
        }
        else{
        // Set the 'isMoving' flag for walking or running states
        if (isMoving)
        {
            if (currentState == State.Walk)
            {
                animator.SetBool("walk", true); // Walk is true
            }
            else if (currentState == State.Run)
            {
                animator.SetBool("Run", true); // Run is true
            }
        }
        }
    }

    bool CanMoveForward(Vector3 direction, float checkDistance)
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, direction, out hit, checkDistance, terrainLayer))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            return slopeAngle <= slopeLimit;
        }
        return true; // No obstacle detected, can move forward
    }

    void ChangeDirection(bool majorChange = false)
    {
        if(!isChasing){
            float randomAngle;
            if (majorChange)
            {
                randomAngle = Random.Range(-180f, 180f);
            }
            else
            {
                randomAngle = Random.Range(-10f, 10f);
            }
            transform.Rotate(Vector3.up, randomAngle);
        }
    }

    void CheckIfStuck()
    {
        if (currentState != State.Idle)
        {
            if (Vector3.Distance(transform.position, lastPosition) < stuckThreshold)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= stuckTime)
                {
                    ChangeDirection(true); // Force a major direction change when stuck
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        lastPosition = transform.position;
    }

    void UpdateRandomDirection()
    {
        directionChangeTimer += Time.deltaTime;
        if (directionChangeTimer >= directionChangeInterval)
        {
            if (currentState != State.Idle && !isEating)
            {
                bool majorChange = Random.value < 0.2f; // 20% chance for a major direction change
                ChangeDirection(majorChange);
            }
            directionChangeTimer = 0f;
        }
    }

    int WeightedRandomChoice(float[] weights)
    {
        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }

        float randomPoint = Random.value * totalWeight;

        for (int i = 0; i < weights.Length; i++)
        {
            if (randomPoint < weights[i])
            {
                return i;
            }
            else
            {
                randomPoint -= weights[i];
            }
        }
        return weights.Length - 1;
    }

    void UpdateStateWeights(int chosenIndex)
    {
        for (int i = 0; i < stateWeights.Length; i++)
        {
            stateWeights[i] = 1.0f;
        }

        if (previousStateIndex != -1)
        {
            stateWeights[previousStateIndex] = 0.2f;
        }
    }
    void UpdateHungerAndHealth()
    {
        hunger -= hungerDecreaseRate * Time.deltaTime;

        if (hunger < 30)
        {
            isEating = true;
            isMoving = false;
            hunger += 50;
        }
        else
        {
            isEating = false;
            isMoving = true;
        }

        if (hunger < 10)
        {
            health -= healthDecreaseRate * Time.deltaTime;
        }

        if (isEating && hunger < 100 && hunger > 40)
        {
            hunger += hungerDecreaseRate * 2 * Time.deltaTime;  // Eating restores hunger faster
        }
    }
}
