using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class LizardmanAI : MonoBehaviour
{
    public Animator animator;
    public float speed = 2.0f;
    public float runSpeed = 4.0f;
    public float slopeLimit = 30.0f;
    public LayerMask terrainLayer;
    public float terrainCheckDistance = 2.0f;
    public float gravity = -9.81f;
    public float stuckThreshold = 0.1f;
    public float stuckTime = 2.0f;
    public float directionChangeInterval = 3.0f;
    public float humanDetectionRange = 15f;
    public float attackCooldownTime = 3600f; 
    public float hungerDecreaseInterval = 5f; 
    public float villageDetectionRange = 100f;
    public float hungerThreshold = 50f; 
    private Transform targetVillage; 
    private float hunger = 100f; 
    private float attackCooldownTimer;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private float stuckTimer;
    private float directionChangeTimer;
    private Vector3[] currentPath; 
    private int currentWaypointIndex = 0; 
    private float interval=10.0f;
    public float attackDuration = 300f;

    private Vector3 homeBaseLocation = new Vector3(0, 0, 0);

    private bool isAttackingVillage = false;
    private float attackTimer = 0f;

    private enum State { Idle, Walk, Run, Attack }
    private State currentState;
    private int previousStateIndex = -1;
    private List<Transform> villages = new List<Transform>();

    private Transform targetHuman;

    private float[] stateWeights = { 1.0f, 50.0f, 10.0f };

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = slopeLimit;
        lastPosition = transform.position;
        homeBaseLocation = GetRandomPositionAroundHomeBase();
        StartCoroutine(ChangeStatePeriodically(interval));
        StartCoroutine(DecreaseHunger());
    }

    void Update()
    {
        MoveBasedOnState();
        CheckIfStuck();
        UpdateRandomDirection();
        UpdateAttackCooldown();
        UpdateHunger();

        if (hunger < hungerThreshold || attackCooldownTimer <= 0f)
        {
            if (villages.Count == 0)
            {
                interval += Time.deltaTime;
            }
            else
            {
                FindPathToNearestVillage();
            }
        }

        if (currentPath != null && currentPath.Length > 0 && currentWaypointIndex < currentPath.Length)
        {
            MoveTowardsWaypoint(currentPath[currentWaypointIndex]);
            if (Vector3.Distance(transform.position, currentPath[currentWaypointIndex]) < 1f)
            {
                currentWaypointIndex++;
            }
        }
        else if (targetVillage != null)
        {
            MoveTowardsWaypoint(targetVillage.position);
        }

        if (isAttackingVillage)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackDuration)
            {
                ExitVillageAfterAttack();
            }
        }
    }

    IEnumerator ChangeStatePeriodically(float interval)
    {
        while (true)
        {
            ChooseRandomState();
            yield return new WaitForSeconds(interval);
        }
    }

    Vector3 GetRandomPositionAroundHomeBase()
    {
        float radius = 50f;
        Vector3 homeBasePosition = transform.position;

        float randomAngle = Random.Range(0f, 2f * Mathf.PI); 
        float randomDistance = Random.Range(0f, radius);   

        float offsetX = randomDistance * Mathf.Cos(randomAngle);
        float offsetZ = randomDistance * Mathf.Sin(randomAngle);

        Vector3 randomPosition = new Vector3(homeBasePosition.x + offsetX, homeBasePosition.y, homeBasePosition.z + offsetZ);

        RaycastHit hit;
        if (Physics.Raycast(randomPosition + Vector3.up * 100f, Vector3.down, out hit, Mathf.Infinity))
        {
            randomPosition.y = hit.point.y;
        }

        return randomPosition;
    }

    void UpdateHunger()
    {
        if (hunger <= 0f && targetVillage != null)
        {
            currentState = State.Run;
            UpdateAnimatorState();
        }
    }

    IEnumerator DecreaseHunger()
    {
        while (true)
        {
            hunger -= 5f; 
            if (hunger < hungerThreshold && targetVillage != null)
            {
                currentState = State.Run;
                UpdateAnimatorState();
            }
            yield return new WaitForSeconds(hungerDecreaseInterval * 60f);
        }
    }

    void UpdateAttackCooldown()
    {
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    bool IsVillageNearby(out Vector3 villageDirection)
    {
        villageDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 100f);  

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Village"))  
            {
                Vector3 directionToVillage = hitCollider.transform.position - transform.position;
                villageDirection = directionToVillage;
                targetVillage = hitCollider.transform;
                return true;
            }
        }

        return false;
    }


    bool IsHumanNearby(out Vector3 humanDirection)
    {
        humanDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, humanDetectionRange);
        float closestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Human"))
            {
                Vector3 directionToHuman = hitCollider.transform.position - transform.position;
                float distance = directionToHuman.magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    humanDirection = directionToHuman;
                    targetHuman = hitCollider.transform;
                }
            }
        }

        return humanDirection != Vector3.zero;
    }

    void ChooseRandomState()
    {
        Vector3 humanDirection;
        if (IsHumanNearby(out humanDirection))
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
            Vector3 humanDirection;
            float currentSpeed = (currentState == State.Walk) ? speed : runSpeed;

            if (IsHumanNearby(out humanDirection))
            {
                currentState = State.Run;
                forwardDirection = humanDirection.normalized;
                currentSpeed = runSpeed;
                Quaternion targetRotation = Quaternion.LookRotation(forwardDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetHuman.position) <= 1.5f)
                {
                    currentState = State.Attack;
                    UpdateAnimatorState();
                }
                else
                {
                    movement = forwardDirection * currentSpeed;
                }
            }
            else
            {
                if (currentPath != null && currentWaypointIndex < currentPath.Length)
                {
                    Vector3 targetPosition = currentPath[currentWaypointIndex];
                    forwardDirection = (targetPosition - transform.position).normalized;

                    if (Vector3.Distance(transform.position, targetPosition) < 1f)
                    {
                        currentWaypointIndex++;
                    }

                    movement = forwardDirection * currentSpeed;
                }
            }

            if (CanMoveForward(forwardDirection, terrainCheckDistance))
            {
                movement = forwardDirection * currentSpeed;
            }
            else
            {
                ChangeDirection();
            }
        }

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMovement = movement + new Vector3(0, velocity.y, 0);

        controller.Move(finalMovement * Time.deltaTime);

        bool isMoving = movement.magnitude > 0.1f;
    }

    void UpdateAnimatorState()
    {
        animator.SetBool("walk", false);
        animator.SetBool("Run", false);
        animator.SetBool("Attack", false);

        if (currentState == State.Attack)
        {
            animator.SetBool("Attack", true);
        }
        else if (currentState == State.Walk)
        {
            animator.SetBool("walk", true);
        }
        else if (currentState == State.Run)
        {
            animator.SetBool("Run", true);
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
        return true;
    }

    void ChangeDirection(bool majorChange = false)
    {
        if (currentState != State.Attack)
        {
            float randomAngle = majorChange ? Random.Range(-180f, 180f) : Random.Range(-10f, 10f);
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
                    ChangeDirection(true);
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
            if (currentState != State.Idle && currentState != State.Attack)
            {
                bool majorChange = Random.value < 0.2f;
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
            randomPoint -= weights[i];
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
        stateWeights[chosenIndex] = 50.0f;
    }

    void CheckForVillages()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, villageDetectionRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Village") && !villages.Contains(hitCollider.transform))
            {
                villages.Add(hitCollider.transform);
            }
        }
    }

    void FindPathToNearestVillage()
    {
        if (villages.Count > 0)
        {
            targetVillage = villages.OrderBy(v => Vector3.Distance(transform.position, v.position)).First();
            currentPath = AStarPathfinding(transform.position, targetVillage.position);
            currentWaypointIndex = 0;
            currentState = State.Run;
            UpdateAnimatorState();
        }
    }

    void ExitVillageAfterAttack()
    {
        isAttackingVillage = false;
        attackTimer = 0f;
        attackCooldownTimer = attackCooldownTime;
        hunger = 100f;
        currentPath = AStarPathfinding(transform.position, homeBaseLocation);
        currentWaypointIndex = 0;
        currentState = State.Walk;
        UpdateAnimatorState();
    }

    Vector3[] AStarPathfinding(Vector3 start, Vector3 target)
    {
        List<Vector3> openList = new List<Vector3>();
        List<Vector3> closedList = new List<Vector3>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();

        openList.Add(start);

        while (openList.Count > 0)
        {
            Vector3 current = openList[0];
            foreach (Vector3 node in openList)
            {
                if (GetF(node, target) < GetF(current, target))
                {
                    current = node;
                }
            }

            openList.Remove(current);
            closedList.Add(current);

            if (current == target)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (Vector3 neighbor in GetNeighbors(current))
            {
                if (closedList.Contains(neighbor)) continue;

                if (!openList.Contains(neighbor))
                {
                    openList.Add(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        return new Vector3[] { };
    }

    float GetF(Vector3 node, Vector3 target)
    {
        return GetG(node) + GetH(node, target);
    }

    float GetG(Vector3 node)
    {
        return Vector3.Distance(transform.position, node); 
    }

    float GetH(Vector3 node, Vector3 target)
    {
        return Vector3.Distance(node, target); 
    }

    Vector3[] ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 current)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path.ToArray();
    }

    List<Vector3> GetNeighbors(Vector3 current)
    {
        List<Vector3> neighbors = new List<Vector3>();

        neighbors.Add(current + Vector3.forward);
        neighbors.Add(current + Vector3.back);
        neighbors.Add(current + Vector3.left);
        neighbors.Add(current + Vector3.right);

        return neighbors;
    }

    void MoveTowardsWaypoint(Vector3 waypoint)
    {
        Vector3 direction = (waypoint - transform.position).normalized;
        controller.Move(direction * speed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(direction);
    }
}
