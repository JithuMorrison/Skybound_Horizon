void DecideAction()
{
    // Evaluate action scores based on environmental conditions
    UpdateActionScores();

    // Select action based on the highest score, accounting for additional conditions
    float highestScore = -1f;
    string bestAction = "Idle";

    foreach (var action in actionScores)
    {
        if (action.Value > highestScore)
        {
            highestScore = action.Value;
            bestAction = action.Key;
        }
    }

    // Set current state based on the best action, with added complexity
    if (highestScore < 0.5f)
    {
        currentState = State.Idle; // Default to idle if no action is deemed necessary
    }
    else
    {
        Enum.TryParse(bestAction, out currentState);
    }
}

void UpdateActionScores()
{
    // Reset scores to a baseline
    foreach (var action in actionScores.Keys.ToList())
    {
        actionScores[action] = 0.5f; // Start with a neutral score
    }

    // Modify scores based on various traits and environmental factors
    float hungerImpact = hunger < hungerThreshold ? 0.3f : -0.1f;
    actionScores["SearchingFood"] += hungerImpact;

    // Adjust scores based on social dynamics
    if (friends.Count > 0)
    {
        actionScores["Grouping"] += sociability * 0.2f;
    }
    else
    {
        actionScores["Grouping"] -= 0.1f; // Less likely to group if lonely
    }

    // Consider recent mistakes and successes for learning
    foreach (var mistake in pastMistakes)
    {
        if (mistake == "Failed to Build")
        {
            actionScores["Building"] -= 0.3f; // Penalize building action
        }
    }

    foreach (var success in pastSuccesses)
    {
        if (success == "Successfully Found Food")
        {
            actionScores["SearchingFood"] += 0.2f; // Reward successful food search
        }
    }

    // Environmental awareness
    if (nearestThreat != null && Vector3.Distance(transform.position, nearestThreat.position) < dangerRadius)
    {
        actionScores["Fleeing"] += bravery * 0.3f; // More likely to flee if threatened
    }

    // Adjust scores based on time of day
    if (timeOfDay < 6f || timeOfDay > 18f) // Night time
    {
        actionScores["Sleeping"] += 0.4f;
    }
    else // Day time
    {
        actionScores["Exploring"] += 0.2f;
    }

    // Scoring based on current activities
    if (isExploringItem)
    {
        actionScores["Exploring"] += curiosity * 0.5f;
        actionScores["Interacting"] -= 0.2f; // Less likely to interact while focused on exploring
    }

    // Adjust scores based on current health and hunger
    if (health < 50.0f)
    {
        actionScores["Fighting"] -= 0.2f; // Less likely to fight if low on health
        actionScores["SearchingFood"] += 0.3f; // More likely to search for food
    }
}

void Act()
{
    switch (currentState)
    {
        case State.Idle: Nothing(); break;
        case State.SearchingFood: SearchForFood(); break;
        case State.Building: BuildHouse(); break;
        case State.Grouping: FormGroup(); break;
        case State.Sleeping: Sleep(); break;
        case State.Exploring: Explore(); break;
        case State.Interacting: Interact(); break;
        case State.Fighting: Attack(nearestThreat); break;
        case State.Fleeing: Flee(nearestThreat); break;
    }

    // Adjust scores based on the success or failure of actions
    AdjustScoresOnActionOutcome(currentState);
}

void AdjustScoresOnActionOutcome(State actionState)
{
    // Adjust action scores based on the outcome of the action
    switch (actionState)
    {
        case State.SearchingFood:
            if (isEating)
            {
                actionScores["SearchingFood"] += 0.5f; // Reward for successful eating
                pastSuccesses.Add("Successfully Found Food");
            }
            else
            {
                actionScores["SearchingFood"] -= 0.2f; // Penalty for failing to find food
                pastMistakes.Add("Failed to Find Food");
            }
            break;

        case State.Building:
            if (hasHouse)
            {
                actionScores["Building"] += 0.4f; // Reward for successful building
                pastSuccesses.Add("Successfully Built House");
            }
            else
            {
                actionScores["Building"] -= 0.3f; // Penalty for failed building
                pastMistakes.Add("Failed to Build");
            }
            break;

        case State.Grouping:
            // If grouping successfully, increase the score
            if (groupMembers.Count > 1)
            {
                actionScores["Grouping"] += 0.5f; // Reward for forming a group
                pastSuccesses.Add("Successfully Formed Group");
            }
            else
            {
                actionScores["Grouping"] -= 0.1f; // Penalty for unsuccessful grouping
                pastMistakes.Add("Failed to Form Group");
            }
            break;

        case State.Sleeping:
            if (isDaytime)
            {
                actionScores["Sleeping"] -= 0.3f; // Penalty for sleeping during the day
                pastMistakes.Add("Slept During Day");
            }
            else
            {
                actionScores["Sleeping"] += 0.3f; // Reward for sleeping at night
                pastSuccesses.Add("Slept at Night");
            }
            break;

        case State.Exploring:
            if (isExploringItem)
            {
                actionScores["Exploring"] += 0.3f; // Reward for successful exploration
                pastSuccesses.Add("Successfully Explored Item");
            }
            else
            {
                actionScores["Exploring"] -= 0.1f; // Penalty for unsuccessful exploration
                pastMistakes.Add("Failed to Explore Item");
            }
            break;

        case State.Fighting:
            if (nearestThreat != null && health > 30f)
            {
                actionScores["Fighting"] += 0.4f; // Reward for successful fight
                pastSuccesses.Add("Successfully Defeated Threat");
            }
            else
            {
                actionScores["Fighting"] -= 0.3f; // Penalty for losing fight
                pastMistakes.Add("Failed in Combat");
            }
            break;

        case State.Fleeing:
            // Penalty for fleeing if not needed
            if (nearestThreat != null && Vector3.Distance(transform.position, nearestThreat.position) < dangerRadius)
            {
                actionScores["Fleeing"] += 0.4f; // Reward for successful fleeing
                pastSuccesses.Add("Successfully Fled from Threat");
            }
            else
            {
                actionScores["Fleeing"] -= 0.2f; // Penalty for unnecessary fleeing
                pastMistakes.Add("Fled Unnecessarily");
            }
            break;
    }

    // Decay scores over time to encourage variety in actions
    foreach (var action in actionScores.Keys.ToList())
    {
        actionScores[action] *= 0.95f; // Decay score slightly
    }
}
