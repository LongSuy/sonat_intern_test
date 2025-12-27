using System.Collections.Generic;
using UnityEngine;


public struct GeneratedLevel
{
    public int tubeCount;
    public int capacity;
    public ColourType[][] tubes;
}
public static class LevelGenerator
{
    private static readonly ColourType[] AvailableColours =
    {
        ColourType.Yellow,
        ColourType.Purple,
        ColourType.Red,
        ColourType.Blue,
        ColourType.Green,
        ColourType.Orange,
    };

    public static GeneratedLevel Generate(Difficulty difficulty)
    {
        int capacity = 4;
        int coloursCount;
        int emptyTubes;

        switch (difficulty)
        {
            case Difficulty.Easy:
                coloursCount = 4;
                emptyTubes = 2;
                break;

            case Difficulty.Medium:
                coloursCount = 5;
                emptyTubes = 2;
                break;

            case Difficulty.Hard:

            default:
                coloursCount = 6;
                emptyTubes = 2;
                break;
        }
        return GenerateLevel(capacity, coloursCount, emptyTubes, 8);
    }

    public static GeneratedLevel GenerateLevel(int capacity, int coloursCount, int emptyTubes, int minTubes = 8)
    {
        coloursCount = Mathf.Clamp(coloursCount, 1, AvailableColours.Length);

        int colouredTubes = coloursCount;
        int tubeCount = colouredTubes + emptyTubes;

        if(tubeCount < minTubes)
        {
            int extraEmpty = minTubes - tubeCount;
            emptyTubes += extraEmpty;
            tubeCount = colouredTubes + emptyTubes;
        }

        ColourType[][] tubes = new ColourType[tubeCount][];
        for(int i = 0; i < tubeCount; i++)
        {
            tubes[i] = new ColourType[capacity];
        }

        List<ColourType> pool = new List<ColourType>();
        for(int c = 0; c < coloursCount; c++)
        {
            for(int k = 0; k < capacity; k++)
            {
                pool.Add(AvailableColours[c]);
            }
        }

        for (int i = 0; i < pool.Count; i++)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }


        int index = 0;
        for(int t = 0; t < colouredTubes; t++)
        {
            for(int s = 0; s < capacity; s++)
            {
                tubes[t][s] = pool[index++];
            }
        }

        for(int t = colouredTubes; t < tubeCount; t++)
        {
            for(int s = 0; s < capacity; s++)
            {
                tubes[t][s] = ColourType.None;
            }
        }

        if (IsSolved(tubes))
        {
            return GenerateLevel(capacity, coloursCount, emptyTubes, minTubes);
        }

        return new GeneratedLevel
        {
            tubeCount = tubeCount,
            capacity = capacity,
            tubes = tubes
        };
    }

    private static bool IsSolved(ColourType[][] tubes)
    {
        foreach(var tube in tubes)
        {
            int nonNoneCount = 0;   
            ColourType first = ColourType.None;

            for(int i = 0; i < tube.Length; i++)
            {
                if (tube[i] == ColourType.None) continue;

                if(first == ColourType.None)
                    first = tube[i];

                if (tube[i] != first)
                    return false;

                nonNoneCount++;
            }
            if(nonNoneCount > 0 && nonNoneCount < tube.Length)
            {
                return false;
            }
        }
        return true;
    }
}
