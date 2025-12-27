using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    public ColourType[] tubesFlat;
    public int tubeCount = 8;
    public int capacity = 4;

    public ColourType[][] GetTubes()
    { 
        ColourType[][] result = new ColourType[tubeCount][];
        int index = 0;
        for(int i = 0; i < tubeCount; i++)
        {
            result[i] = new ColourType[capacity];
            for(int j = 0; j < capacity; j++)
            {
                if(index < tubesFlat.Length)
                    result[i][j] = tubesFlat[index++];
                else
                    result[i][j] = ColourType.None;
            }
        }
        return result;
    }
}
