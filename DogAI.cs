using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DogAI : MonoBehaviour
{
    public Animator animator;
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.0f;
    public float slopeLimit = 30.0f;
    public LayerMask terrainLayer;
    public float terrainCheckDistance = 2.0f;
    public float gravity = -9.81f;
    public float stuckThreshold = 0.1f;
    public float stuckTime = 2.0f;
    public float directionChangeInterval = 3.0f;
    public float predatorDetectionRange = 10f;
    private float predatorAttackCooldown = 10.0f;
    private float predatorTimer = 0f;
    private float dangerDistance = 1.5f;
    private Transform predator;

    public float health = 100f;   
    public float hunger = 100f;    
    public float hungerDecreaseRate = 1f; 
    public float healthDecreaseRate = 2f;  
    public bool isEating = false;
    public bool isMoving = true;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private float stuckTimer;
    private float directionChangeTimer;
    private List<Vector3> visitedPoints = new List<Vector3>(); 
    private Vector3? targetPoint = null;

    private enum State { Idle, Walk, Run, Sit, Stand, TurnLeft, TurnRight }
    private State currentState;
    private int previousStateIndex = -1;

    private float[] stateWeights = { 1.0f, 4.0f, 0.5f, 0.01f, 0.02f, 0.01f, 0.01f };

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

        if (currentState == State.Walk && Vector3.Distance(transform.position, lastPosition) > 1f)
        {
            visitedPoints.Add(transform.position);
            if (visitedPoints.Count > 10 && targetPoint == null)
            {
                targetPoint = visitedPoints[Random.Range(0, visitedPoints.Count)];
                StartCoroutine(FollowPathToTarget(targetPoint.Value));
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

    void ChooseRandomState()
    {
        Vector3 predatorDirection;
        if (IsPredatorNearby(out predatorDirection))
        {
            currentState = State.Run;
        }
        else
        {
            State[] states = { State.Idle, State.Walk, State.Run, State.Sit, State.Stand, State.TurnLeft, State.TurnRight };
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

        if (currentState != State.Idle && currentState != State.Sit && currentState != State.Stand && currentState != State.TurnLeft && currentState != State.TurnRight)
        {
            Vector3 moveDirection;
            Vector3 predatorDirection;
            float currentSpeed = (currentState == State.Run) ? runSpeed : walkSpeed;

            if (IsPredatorNearby(out predatorDirection))
            {
                moveDirection = -predatorDirection.normalized;
                currentSpeed = runSpeed;
            }
            else
            {
                moveDirection = transform.forward;
            }

            if (CanMoveForward(moveDirection, terrainCheckDistance) && !isEating)
            {
                movement = moveDirection * currentSpeed;
            }
            else
            {
                ChangeDirection(true);
                movement = transform.forward * currentSpeed;
            }
        }

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMovement = movement + new Vector3(0, velocity.y, 0);
        controller.Move(finalMovement * Time.deltaTime);

        if (movement != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(movement);
        }

        UpdateAnimatorState();
    }

    IEnumerator FollowPathToTarget(Vector3 target)
    {
        var path = CalculatePath(transform.position, target);
        foreach (var waypoint in path)
        {
            Vector3 direction = (waypoint - transform.position).normalized;
            while (Vector3.Distance(transform.position, waypoint) > 0.5f)
            {
                controller.Move(direction * walkSpeed * Time.deltaTime);
                yield return null;
            }
        }
        targetPoint = null;
    }

    List<Vector3> CalculatePath(Vector3 start, Vector3 end)
    {
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        Node startNode = new Node(start);
        Node endNode = new Node(end);

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            foreach (Node node in openList)
            {
                if (node.fCost < currentNode.fCost || (node.fCost == currentNode.fCost && node.hCost < currentNode.hCost))
                {
                    currentNode = node;
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            if (currentNode.position == endNode.position)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Vector3 direction in GetNeighborDirections())
            {
                Vector3 neighborPos = currentNode.position + direction;
                Node neighbor = new Node(neighborPos);

                if (closedList.Contains(neighbor) || !IsWalkable(neighbor.position))
                {
                    continue;
                }

                float newGCost = currentNode.gCost + Vector3.Distance(currentNode.position, neighbor.position);

                if (newGCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = Vector3.Distance(neighbor.position, endNode.position);
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }
        
        return new List<Vector3>();
    }

    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    List<Vector3> GetNeighborDirections()
    {
        return new List<Vector3>
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1)
        };
    }

    bool IsWalkable(Vector3 position)
    {
        return true;  
    }

    class Node
    {
        public Vector3 position;
        public float gCost;
        public float hCost;
        public Node parent;

        public float fCost
        {
            get { return gCost + hCost; }
        }

        public Node(Vector3 _position)
        {
            position = _position;
            gCost = float.MaxValue;
            hCost = 0;
            parent = null;
        }

        public override bool Equals(object obj)
        {
            return obj is Node node && node.position == this.position;
        }

        public override int GetHashCode()
        {
            return position.GetHashCode();
        }
    }

    void UpdateAnimatorState()
    {
        animator.SetBool("walk", false);
        animator.SetBool("Run", false);
        animator.SetBool("isEating", false);
        animator.SetBool("isSitting", false);
        animator.SetBool("isStanding", false);
        animator.SetBool("isTurningLeft", false);
        animator.SetBool("isTurningRight", false);

        if (isEating)
        {
            animator.SetBool("isEating", true);
        }
        else{
        if (isMoving)
        {
            if (currentState == State.Walk)
            {
                animator.SetBool("walk", true); 
            }
            else if (currentState == State.Run)
            {
                animator.SetBool("Run", true); 
            }
        }
        
        if (currentState == State.Sit)
        {
            animator.SetBool("isSitting", true);
        }
        else if (currentState == State.Stand)
        {
            animator.SetBool("isStanding", true);
        }

        if (currentState == State.TurnLeft)
        {
            animator.SetBool("isTurningLeft", true);
        }
        else if (currentState == State.TurnRight)
        {
            animator.SetBool("isTurningRight", true);
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
        return true;
    }

    void ChangeDirection(bool majorChange = false)
    {
        float randomAngle;
        if (majorChange)
        {
            randomAngle = Random.Range(-180f, 180f);
        }
        else
        {
            randomAngle = Random.Range(-2f, 2f);
        }
        transform.Rotate(Vector3.up, randomAngle);
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
            if (currentState != State.Idle && !isEating)
            {
                bool majorChange = Random.value < 0.1f;
                ChangeDirection(majorChange);
            }
            directionChangeTimer = 0f;
        }
    }

    bool IsPredatorNearby(out Vector3 predatorDirection)
    {
        predatorDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, predatorDetectionRange);
        float closestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Predator"))
            {
                Vector3 directionToPredator = hitCollider.transform.position - transform.position;
                float distance = directionToPredator.magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    predatorDirection = directionToPredator;
                }
            }
        }

        return predatorDirection != Vector3.zero;
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
            stateWeights[i] = (i == (int)State.Run) ? 0.5f : 1.0f;
        }

        if (previousStateIndex != -1 && previousStateIndex != (int)State.Run)
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
            hunger += hungerDecreaseRate * 2 * Time.deltaTime; 
        }
    }
}
