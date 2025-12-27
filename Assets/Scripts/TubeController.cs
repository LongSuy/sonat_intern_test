using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public class TubeController : MonoBehaviour
{
    public int tubeIndex;
    public int capacity = 4;
    public Transform slotsParent;
    public float bottomY = -1.7f;
    public float stepY = 1.1f;
    public GameObject waterPiecePrefab;

    [HideInInspector]
    public List<ColourType> colours = new List<ColourType>();

    private GameManager _gameManager;

    private Vector3 _originalScale;
    private Coroutine _selectCo;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }
    
    public void StartSelectAnim()
    {
        if(_selectCo != null)
            StopCoroutine(_selectCo);
        _selectCo = StartCoroutine(SelectAnimCoroutine());
    }

    IEnumerator SelectAnimCoroutine()
    {
        float t = 0f;
        Vector3 target = _originalScale * 1.1f;

        while(t < 1f)
        {
            t += Time.deltaTime * 6f;
            transform.localScale = Vector3.Lerp(_originalScale, target, t);
            yield return null;
        }
    }

    public void ResetScale()
    {
        if(_selectCo != null)
            StopCoroutine(_selectCo);
        transform.localScale = _originalScale;
    }

    private void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        RefreshVisual();
    }

    private void OnMouseDown()
    {
        Debug.Log($"Tube clicked: {tubeIndex}");
        if(_gameManager != null)
            _gameManager.OnTubeClicked(this);
    }

    public void Init(List<ColourType> initialColours)
    {
        colours = new List<ColourType>(initialColours);
        RefreshVisual();
    }

    public void PushColour(ColourType colour)
    {
        if (colours.Count >= capacity) return;
        colours.Add(colour);
        RefreshVisual();
    }

    public ColourType PeekTop()
    {
        if(colours.Count == 0) return ColourType.None;
        return colours[colours.Count - 1];
    }

    public ColourType PopColour()
    {
        if (colours.Count == 0) return ColourType.None;
        int lastIndex = colours.Count - 1;
        ColourType c = colours[lastIndex];
        colours.RemoveAt(lastIndex);
        RefreshVisual();
        return c;
    }

    public bool IsEmpty() => colours.Count == 0;
    public bool IsFull() => colours.Count >= capacity;

    public bool IsUniformAndFull()
    {
        if(colours.Count == 0) return false;
        if(colours.Count != capacity) return false;

        ColourType first = colours[0];
        if (first == ColourType.None) return false;

        for(int i = 1; i < colours.Count; i++)
        {
            if (colours[i] != first)
                return false;
        }
        return true ;
    }

    public void RefreshVisual()
    {

        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }

       
        for (int i = 0; i < colours.Count; i++)
        {
            var piece = Instantiate(waterPiecePrefab, slotsParent);
            float y = bottomY + i * stepY;
            piece.transform.localPosition = new Vector3(0f, y, 0f);
            piece.transform.localScale = Vector3.one;

            var sr = piece.GetComponent<SpriteRenderer>();
            sr.color = ColourTypeToColor(colours[i]);
        }
    }

    public static Color ColourTypeToColor(ColourType type)
    {
        switch (type)
        {
            case ColourType.Yellow:
                return new Color(1f, 0.92f, 0.3f);       
            case ColourType.Purple:
                return new Color(0.6f, 0.28f, 0.83f);    
            case ColourType.Red:
                return new Color(0.9f, 0.2f, 0.2f);
            case ColourType.Blue:
                return new Color(0.2f, 0.4f, 1f);
            case ColourType.Green:
                return new Color(0.2f, 0.8f, 0.3f);
            case ColourType.Orange:
                return new Color(1f, 0.6f, 0.2f);
            default:
                return Color.white;
        }
    }

    public Vector3 GetSlotWorldPosition(int index)
    {
        float y = bottomY + index * stepY;
        Vector3 localPos = new Vector3(0f, y, 0f);
        return slotsParent.TransformPoint(localPos);
    }


    public int CountTopSameColour()
    {
        if (colours.Count == 0) return 0;
        ColourType top = colours[colours.Count - 1];
        int count = 0;

        for(int i = colours.Count - 1; i >= 0; i--)
        {
            if (colours[i] == top)
                count++;
            else
                break;
        }
        return count;
    }

    public ColourType PopColourNoVisual()
    {
        if(colours.Count == 0) return ColourType.None;
        int lastIndex = colours.Count - 1;
        ColourType c = colours[lastIndex];
        colours.RemoveAt(lastIndex);
        return c;
    }

    public void PushColourNoVisual(ColourType colour)
    {
        if (colours.Count >= capacity) return;
        colours.Add(colour);
    }
}
