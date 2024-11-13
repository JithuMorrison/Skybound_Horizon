using System;
using System.Collections.Generic;
using UnityEngine;

public class ClimateConditionController : MonoBehaviour
{
    private const int MAX_TEMPERATURE = 50;
    private const int MIN_TEMPERATURE = -30;
    private const int MAX_HUMIDITY = 100;
    private const int MIN_HUMIDITY = 0;
    private const int MAX_RAINFALL = 200;
    private const int MIN_RAINFALL = 0;

    private const int SEARCH_DEPTH = 2;

    private float timePassed = 0f;
    private const float updateInterval = 10f; // 10 minutes in seconds

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

    private Dictionary<RegionType, ClimateCondition> initialConditions = new Dictionary<RegionType, ClimateCondition>
    {
        { RegionType.Desert, new ClimateCondition(40, 20, 5) },
        { RegionType.Forest, new ClimateCondition(20, 80, 100) },
        { RegionType.Tundra, new ClimateCondition(-15, 60, 20) },
        { RegionType.Grassland, new ClimateCondition(25, 50, 50) }
    };

    private int EvaluateClimate(ClimateCondition climate, RegionType region)
    {
        var initial = initialConditions[region];
        int score = 0;

        score -= Mathf.Abs(climate.Temperature - initial.Temperature);
        score -= Mathf.Abs(climate.Humidity - initial.Humidity);
        score -= Mathf.Abs(climate.Rainfall - initial.Rainfall);

        return score;
    }

    private int Minimax(ClimateCondition climate, RegionType region, int depth, bool isMaximizing)
    {
        if (depth == 0)
            return EvaluateClimate(climate, region);

        int bestScore = isMaximizing ? int.MinValue : int.MaxValue;
        List<ClimateCondition> possibleConditions = GeneratePossibleConditions(climate);

        foreach (var condition in possibleConditions)
        {
            int eval = Minimax(condition, region, depth - 1, !isMaximizing);
            bestScore = isMaximizing ? Mathf.Max(bestScore, eval) : Mathf.Min(bestScore, eval);
        }

        return bestScore;
    }

    private List<ClimateCondition> GeneratePossibleConditions(ClimateCondition climate)
    {
        List<ClimateCondition> conditions = new List<ClimateCondition>();

        int[] temperatureOptions = { Mathf.Clamp(climate.Temperature - 5, MIN_TEMPERATURE, MAX_TEMPERATURE),
                                     climate.Temperature,
                                     Mathf.Clamp(climate.Temperature + 5, MIN_TEMPERATURE, MAX_TEMPERATURE) };

        int[] humidityOptions = { Mathf.Clamp(climate.Humidity - 10, MIN_HUMIDITY, MAX_HUMIDITY),
                                  climate.Humidity,
                                  Mathf.Clamp(climate.Humidity + 10, MIN_HUMIDITY, MAX_HUMIDITY) };

        int[] rainfallOptions = { Mathf.Clamp(climate.Rainfall - 20, MIN_RAINFALL, MAX_RAINFALL),
                                  climate.Rainfall,
                                  Mathf.Clamp(climate.Rainfall + 20, MIN_RAINFALL, MAX_RAINFALL) };

        foreach (var temp in temperatureOptions)
        {
            foreach (var hum in humidityOptions)
            {
                foreach (var rain in rainfallOptions)
                {
                    conditions.Add(new ClimateCondition(temp, hum, rain));
                }
            }
        }

        return conditions;
    }

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

    public void RandomizeClimateForRegions()
    {
        foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
        {
            ClimateCondition randomizedClimate;

            switch (region)
            {
                case RegionType.Desert:
                    randomizedClimate = new ClimateCondition(
                        UnityEngine.Random.Range(30, 50), 
                        UnityEngine.Random.Range(10, 30), 
                        UnityEngine.Random.Range(0, 20)  
                    );
                    break;

                case RegionType.Forest:
                    randomizedClimate = new ClimateCondition(
                        UnityEngine.Random.Range(10, 30), 
                        UnityEngine.Random.Range(60, 100),
                        UnityEngine.Random.Range(50, 200) 
                    );
                    break;

                case RegionType.Tundra:
                    randomizedClimate = new ClimateCondition(
                        UnityEngine.Random.Range(-30, 0),  
                        UnityEngine.Random.Range(50, 80),  
                        UnityEngine.Random.Range(10, 50)  
                    );
                    break;

                case RegionType.Grassland:
                    randomizedClimate = new ClimateCondition(
                        UnityEngine.Random.Range(20, 35),  
                        UnityEngine.Random.Range(30, 70), 
                        UnityEngine.Random.Range(20, 100)  
                    );
                    break;

                default:
                    randomizedClimate = new ClimateCondition(
                        UnityEngine.Random.Range(MIN_TEMPERATURE, MAX_TEMPERATURE),
                        UnityEngine.Random.Range(MIN_HUMIDITY, MAX_HUMIDITY),
                        UnityEngine.Random.Range(MIN_RAINFALL, MAX_RAINFALL)
                    );
                    break;
            }

            // Find the best climate after randomizing the values
            ClimateCondition bestClimate = FindBestClimateAfterRandomChange(region, randomizedClimate);

            Debug.Log($"Best Climate for {region}: Temperature: {bestClimate.Temperature}°C, " +
                    $"Humidity: {bestClimate.Humidity}%, Rainfall: {bestClimate.Rainfall}mm");
        }
    }

    private ClimateCondition FindBestClimateAfterRandomChange(RegionType region, ClimateCondition randomizedClimate)
    {
        int bestScore = int.MinValue;
        ClimateCondition bestCondition = randomizedClimate;
    
        List<ClimateCondition> possibleConditions = GeneratePossibleConditions(randomizedClimate);
    
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

    void Start()
    {
        foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
        {
            ClimateCondition bestClimate = GenerateClimateCondition(region);
            Debug.Log($"Best Climate for {region}: Temperature: {bestClimate.Temperature}°C, " +
                      $"Humidity: {bestClimate.Humidity}%, Rainfall: {bestClimate.Rainfall}mm");
        }
    }

    void Update()
    {
        timePassed += Time.deltaTime;

        if (timePassed >= updateInterval)
        {
            timePassed = 0f;  
            RandomizeClimateForRegions();
        }
    }
}
