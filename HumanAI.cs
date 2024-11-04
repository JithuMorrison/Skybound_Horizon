using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State { Idle, Exploring, Interacting, Fighting, Fleeing, SearchingFood, Building, Grouping, Sleeping }

public class HumanAI : MonoBehaviour
{
    // Personality traits and basic stats
    public Animator animator;
    [Range(0f, 1f)] public float bravery, sociability, aggression, curiosity, trustworthiness, needFactor;
    [Range(0f, 1f)] public float calmness, love, lethargy, happiness, creativity, socialFactor, tiredness;
    [Range(0f, 1f)] public float braveFactor, calmFactor, happyFactor, lethargyFactor, loveFactor;
    public float health, hunger, strength, defense, movementSpeed;
    private State currentState;
    
    // Environmental factors
    private Transform nearestThreat, nearestFoodSource, nearestItemOfInterest, nearestFriend;
    private List<HumanAI> groupMembers = new List<HumanAI>();
    private Transform homeBase;
    [SerializeField] private float hungerThreshold = 40.0f;
    private bool hasHouse = false;
    private float wanderChangeTime = 0f;
    private float nextWanderChange = 0f;
    private bool isExploringItem = false;
    private bool startexplore = false;
    private bool isEating = false;
    private float explorationTimer = 0.0f;
    private float explorationTime;
    private float stateChangeTimer = 0f;
    private bool hasRotatedToItem = false;

    [SerializeField] private List<Transform> favoriteFoods = new List<Transform>();
    [SerializeField] private List<Transform> closeFriends = new List<Transform>();
    [SerializeField] private List<Transform> friends = new List<Transform>();
    [SerializeField] private List<Transform> dislikedPeople = new List<Transform>();
    [SerializeField] private List<string> favoriteActions = new List<string> { "Build", "Explore", "Group" };
    private List<string> pastMistakes = new List<string>();
    private List<string> pastSuccesses = new List<string>();

    private bool hasComplexGoals = false;
    private Transform currentGoal;  
    
    // Settings for time-based actions
    private float timeOfDay;
    private bool isDaytime;
    [SerializeField] private float dangerRadius = 10.0f;
    [SerializeField] private float interactionRadius = 25.0f;

    void Start()
    {
        InitializeTraitsAndStats();
        currentState = State.Idle;
        nextWanderChange = Random.Range(2f, 5f);
    }

    void Update()
    {
        stateChangeTimer += Time.deltaTime;

        if (stateChangeTimer >= 1.0f)
        {
            PerceiveEnvironment();
            DecideAction();
            stateChangeTimer = 0f; // Reset timer after changing state
        }
        Act();
    }

    void InitializeTraitsAndStats()
    {
        bravery = Random.Range(0.0f, 1.0f);
        braveFactor = Random.Range(0.0f, 1.0f);
        sociability = Random.Range(0.0f, 1.0f);
        socialFactor = Random.Range(0.0f, 1.0f);
        aggression = Random.Range(0.0f, 1.0f);
        calmFactor = Random.Range(0.0f, 1.0f);
        curiosity = Random.Range(0.0f, 1.0f);
        trustworthiness = Random.Range(0.0f, 1.0f);
        calmness = Random.Range(0.1f, 1.0f);
        loveFactor = Random.Range(0.1f, 1.0f);
        lethargy = Random.Range(0.1f, 1.0f);
        happiness = Random.Range(0.1f, 1.0f);
        creativity = Random.Range(0.1f, 1.0f);
        socialFactor = Random.Range(0.1f, 1.0f);
        needFactor = 0f;

        health = Random.Range(50.0f, 100.0f);
        hunger = 100.0f;
        strength = Random.Range(10.0f, 50.0f);
        defense = Random.Range(25.0f, 55.0f);
        movementSpeed = Random.Range(2.0f, 5.0f);
    }

    void UpdateTimeOfDay()
    {
        timeOfDay += Time.deltaTime;
        isDaytime = (timeOfDay % 24) < 18;
    }

    void PerceiveEnvironment()
    {
        nearestThreat = FindNearestThreat();
        nearestFoodSource = FindNearestFood();
        nearestItemOfInterest = FindNearestItemOfInterest();
        nearestFriend = FindNearestFriend();
    }

    void DecideAction()
    {
        float randomFactor = Random.Range(0.0f, 1.0f);

        if (hunger < hungerThreshold)
        {
            currentState = State.SearchingFood;
            return;
        }
        else{
            if (!isDaytime && (tiredness > 0.6f || lethargy > 0.7f))
            {
                if(lethargy<0.3f && nearestThreat!=null && randomFactor>0.8f){
                    if(strength>20f && bravery>0.6f){
                        currentState=State.Fighting;
                    }
                    else{
                        currentState=State.Fleeing;
                    }
                }
                else{
                    currentState = State.Sleeping;
                }
                return;
            }
            else{
                if (nearestThreat != null && Vector3.Distance(transform.position, nearestThreat.position) < dangerRadius)
                {
                    currentState = (bravery < 0.3f || (calmness < 0.5f && strength < 15f)) ? State.Fleeing : 
                                (aggression > 0.6f && strength > 20.0f) ? State.Fighting : State.Idle;
                    return;
                }
                else{
                    if (nearestFriend != null && Vector3.Distance(transform.position, nearestFriend.position) < interactionRadius && sociability > 0.5f && loveFactor > 0.4f && randomFactor < socialFactor)
                    {
                        currentState = State.Interacting;
                        return;
                    }

                    if (nearestItemOfInterest != null && (curiosity > 0.5f || creativity > 0.5f))
                    {
                        currentState = State.Exploring;
                        return;
                    }

                    if (groupMembers.Count == 0 && sociability > 0.5f && nearestFriend != null)
                    {
                        currentState = State.Grouping;
                        return;
                    }

                    if (!hasHouse && needFactor > 0.6f)
                    {
                        currentState = State.Building;
                        return;
                    }
                    if(randomFactor>0.3f){
                        currentState = State.Exploring;
                    }
                    else{
                        currentState = State.Idle;
                    }
                }
            }
        }
    }

    void Act()
    {
        switch (currentState)
        {
            case State.Idle: Nothing(); break;
            case State.SearchingFood: SearchForFood();Debug.Log("Food"); break;
            case State.Building: BuildHouse();Debug.Log("Build"); break;
            case State.Grouping: FormGroup();Debug.Log("Form"); break;
            case State.Sleeping: Sleep();Debug.Log("Sleep"); break;
            case State.Exploring: Explore(); break;
            case State.Interacting: Interact();Debug.Log("int"); break;
            case State.Fighting: Attack(nearestThreat);Debug.Log("att"); break;
            case State.Fleeing: Flee(nearestThreat);Debug.Log("threat"); break;
        }
    }

    void Nothing(){}

    void Wander()
    {
        if(!startexplore  && !isEating){
            wanderChangeTime += Time.deltaTime;
            Debug.Log("Wandering");
            if (IsSteepSlope())
            {
                float randomAngle = Random.Range(-110f, 110f);
                transform.Rotate(Vector3.up, randomAngle);
            }
            else if (wanderChangeTime >= nextWanderChange)
            {
                float randomAngle = Random.Range(-45f, 45f);
                transform.Rotate(Vector3.up, randomAngle);
                wanderChangeTime = 0f;
                nextWanderChange = Random.Range(2f, 4f);
                hunger -= 5;
            }

            transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);
        }
    }

    bool IsSteepSlope()
    {
        float terrainCheckDistance = 1.5f;
        LayerMask terrainLayer = LayerMask.GetMask("Ground");
        float slopeLimit = 25f;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, terrainCheckDistance, terrainLayer))
        {
            return Vector3.Angle(hit.normal, Vector3.up) > slopeLimit;
        }
        return false;
    }

    void SearchForFood()
    {
        if (nearestFoodSource != null)
        {
            isEating=true;
            transform.position = Vector3.MoveTowards(transform.position, nearestFoodSource.position, movementSpeed * Time.deltaTime);
            Vector3 movementDirection = transform.position;

            if (movementDirection.magnitude > 0.01f)
            {
                RotateToMovementDirection(movementDirection);
            }

            if (Vector3.Distance(transform.position, nearestFoodSource.position) < 2.5f)
            {
                animator.SetBool("eat",true);
                hunger += 80.0f;
                hunger = Mathf.Clamp(hunger, 0f, 100f);
                Debug.Log("Ate food and reduced hunger");
                isEating=false;
                StartCoroutine(WaitAndExecute());
                animator.SetBool("eat",false);
            }
        }
        else
        {
            Wander();
        }
    }

    private IEnumerator WaitAndExecute()
    {
        yield return new WaitForSeconds(4f);
        Debug.Log("Waited for 4 seconds!");
        
    }

    void BuildHouse()
    {
        if (!hasHouse)
        {
            homeBase = new GameObject("House").transform;
            homeBase.position = transform.position;
            hasHouse = true;
            Debug.Log("Built a house!");
        }
    }

    void FormGroup()
    {
        Collider[] nearbyHumans = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (Collider human in nearbyHumans)
        {
            if (human.TryGetComponent(out HumanAI otherHuman) && otherHuman != this && Random.value < sociability)
            {
                groupMembers.Add(otherHuman);
                otherHuman.groupMembers.Add(this);
                Debug.Log("Formed a group with another human");
            }
        }
    }

    void Sleep()
    {
        if (homeBase != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, homeBase.position, movementSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, homeBase.position) < 1.0f)
            {
                Debug.Log("Sleeping at home");
                tiredness -= Time.deltaTime * 0.1f;
                tiredness = Mathf.Clamp(tiredness, 0f, 1f);
            }
        }
    }

    void Explore()
    {
        Debug.Log("explore");
        if (nearestItemOfInterest != null)
        {
            if (!isExploringItem)
            {
                Vector3 directionToItem = nearestItemOfInterest.position - transform.position;
                startexplore=true;
                float distance = Vector3.Distance(transform.position, nearestItemOfInterest.position);
                if (distance > 1.5f)
                {
                    if (!hasRotatedToItem)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(directionToItem);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 0.5f);
                        hasRotatedToItem = true;
                    }
                    transform.position = Vector3.MoveTowards(transform.position, nearestItemOfInterest.position, movementSpeed * Time.deltaTime);
                }
                else
                {
                    isExploringItem = true;
                    explorationTimer = 0.0f;
                    explorationTime = Random.Range(5.0f, 7.0f);
                    hasRotatedToItem=false;
                }
            }
            else
            {
                explorationTimer += Time.deltaTime;
                if (explorationTimer >= explorationTime)
                {
                    if(!hasRotatedToItem){
                        float randomAngle = Random.Range(160f, 210f);
                        transform.Rotate(Vector3.up, randomAngle);
                        hasRotatedToItem=true;
                    }
                    Vector3 directionAway = (transform.position - nearestItemOfInterest.position).normalized;
                    Vector3 targetPosition = transform.position + directionAway * 10f;
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
                    startexplore=false;
                    if(explorationTimer >= 2*explorationTime){
                        isExploringItem = false;
                        nearestItemOfInterest = null;
                    }
                }
            }
        }
        else{
            Wander();
        }
    }

    void Interact()
    {
        if (nearestFriend != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, nearestFriend.position, movementSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, nearestFriend.position) < 1.0f)
            {
                Debug.Log("Interacting with friend!");
                happiness += 0.1f;
                happiness = Mathf.Clamp(happiness, 0f, 1f);
            }
        }
    }

    void Attack(Transform target)
    {
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, movementSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.position) < 1.0f)
            {
                Debug.Log("Attacking target!");
                // Implement attack logic here
            }
        }
    }

    void Flee(Transform target)
    {
        if (target != null)
        {
            Vector3 directionAway = transform.position - target.position;
            transform.Translate(directionAway.normalized * movementSpeed * Time.deltaTime);
        }
    }

    Transform FindNearestThreat()
    {
        return FindNearestWithTag("Predator", dangerRadius);
    }

    Transform FindNearestFood()
    {
        return FindNearestWithTag("Food", interactionRadius);
    }

    Transform FindNearestItemOfInterest()
    {
        return FindNearestWithTag("Item", interactionRadius);
    }

    Transform FindNearestFriend()
    {
        return FindNearestWithTag("Human", interactionRadius);
    }

    Transform FindNearestWithTag(string tag, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        Transform nearest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag(tag) && col.transform != transform)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearest = col.transform;
                }
            }
        }

        return nearest;
    }
    private void RotateToMovementDirection(Vector3 direction)
    {
        direction.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.5f * Time.deltaTime);
    }
}
