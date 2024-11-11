using System;
using System.Collections.Generic;
using UnityEngine;

public class ClimateConditionGenerator : MonoBehaviour
{
    // Define constants for climate attributes
    private const int MAX_TEMPERATURE = 50;
    private const int MIN_TEMPERATURE = -30;
    private const int MAX_HUMIDITY = 100;
    private const int MIN_HUMIDITY = 0;
    private const int MAX_RAINFALL = 200;
    private const int MIN_RAINFALL = 0;

    // Depth for minimax search
    private const int SEARCH_DEPTH = 2;

    // Define possible climate regions
    public enum RegionType { Desert, Forest, Tundra, Grassland }

    [System.Serializable]
    public struct ClimateCondition
    {
        public int Temperature;
        public int Humidity;
        public int Rainfall;

        public ClimateCondition(int temp, int hum, int rain)
        {
            Temperature = temp;
            Humidity = hum;
            Rainfall = rain;
        }
    }

    // Define initial climate conditions for each region type
    private Dictionary<RegionType, ClimateCondition> initialConditions = new Dictionary<RegionType, ClimateCondition>
    {
        { RegionType.Desert, new ClimateCondition(40, 20, 5) },
        { RegionType.Forest, new ClimateCondition(20, 80, 100) },
        { RegionType.Tundra, new ClimateCondition(-15, 60, 20) },
        { RegionType.Grassland, new ClimateCondition(25, 50, 50) }
    };

    // Evaluate a climate condition based on a given region
    private int EvaluateClimate(ClimateCondition climate, RegionType region)
    {
        var initial = initialConditions[region];
        int score = 0;

        // Score is calculated based on how close the condition is to the initial "ideal" conditions
        score -= Mathf.Abs(climate.Temperature - initial.Temperature);
        score -= Mathf.Abs(climate.Humidity - initial.Humidity);
        score -= Mathf.Abs(climate.Rainfall - initial.Rainfall);

        return score;
    }

    // Minimax function for generating climate conditions
    private int Minimax(ClimateCondition climate, RegionType region, int depth, bool isMaximizing)
    {
        if (depth == 0)
            return EvaluateClimate(climate, region);

        int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

        // Generate possible changes in climate attributes
        List<ClimateCondition> possibleConditions = GeneratePossibleConditions(climate);

        foreach (var condition in possibleConditions)
        {
            int eval = Minimax(condition, region, depth - 1, !isMaximizing);
            bestScore = isMaximizing ? Mathf.Max(bestScore, eval) : Mathf.Min(bestScore, eval);
        }

        return bestScore;
    }

    // Generate possible variations of a climate condition
    private List<ClimateCondition> GeneratePossibleConditions(ClimateCondition climate)
    {
        List<ClimateCondition> conditions = new List<ClimateCondition>();

        // Slightly adjust temperature, humidity, and rainfall
        int[] temperatureOptions = { climate.Temperature - 5, climate.Temperature, climate.Temperature + 5 };
        int[] humidityOptions = { climate.Humidity - 10, climate.Humidity, climate.Humidity + 10 };
        int[] rainfallOptions = { climate.Rainfall - 20, climate.Rainfall, climate.Rainfall + 20 };

        foreach (var temp in temperatureOptions)
        {
            foreach (var hum in humidityOptions)
            {
                foreach (var rain in rainfallOptions)
                {
                    int tempCapped = Mathf.Clamp(temp, MIN_TEMPERATURE, MAX_TEMPERATURE);
                    int humCapped = Mathf.Clamp(hum, MIN_HUMIDITY, MAX_HUMIDITY);
                    int rainCapped = Mathf.Clamp(rain, MIN_RAINFALL, MAX_RAINFALL);

                    conditions.Add(new ClimateCondition(tempCapped, humCapped, rainCapped));
                }
            }
        }

        return conditions;
    }

    // Public method to find the best climate condition for a given region
    public ClimateCondition GenerateClimateCondition(RegionType region)
    {
        ClimateCondition initialClimate = initialConditions[region];
        int bestScore = int.MinValue;
        ClimateCondition bestCondition = initialClimate;

        List<ClimateCondition> possibleConditions = GeneratePossibleConditions(initialClimate);

        foreach (var condition in possibleConditions)
        {
            int score = Minimax(condition, region, SEARCH_DEPTH, true);
            if (score > bestScore)
            {
                bestScore = score;
                bestCondition = condition;
            }
        }

        return bestCondition;
    }

    // Example Unity usage to display the best climate condition for each region
    private void Start()
    {
        // Example to generate climate for each region
        foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
        {
            ClimateCondition bestClimate = GenerateClimateCondition(region);
            Debug.Log($"Best Climate for {region}: Temperature: {bestClimate.Temperature}°C, " +
                      $"Humidity: {bestClimate.Humidity}%, Rainfall: {bestClimate.Rainfall}mm");
        }
    }
}
