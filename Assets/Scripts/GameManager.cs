using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [Header("Level")]
    public LevelData levelData;

    [Header("Prefabs")]
    public GameObject tubePrefab;
    public GameObject waterPiecePrefab;

    [Header("Layout")]
    public Transform tubesParent;
    public float tubeSpacing = 2.5f;
    public int columns = 4; 
    public float rowSpacing = -3f;
    public float rowY = 1.5f;

    [Header("UI")]
    public GameObject winPanel;
    public GameObject losePanel;
    public Button restartButton;
    public Button nextLevelButton;

    [Header("Animation")]
    public float pourDuration = 0.25f;
    public float pourInterval = 0.02f;

    [Header("Level Mode")]
    public bool useGeneratedLevel = true;
    public Difficulty difficulty = Difficulty.Easy;

    private readonly List<TubeController> _tubes = new List<TubeController>();
    private TubeController _selectedTube;

    private bool isAnimating = false;   

    private void Start()
    {
        InitLevel();
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
        if(nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
    }

    private void InitLevel()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        foreach (Transform child in tubesParent)
        {
            Destroy(child.gameObject);
        }
        _tubes.Clear();
        _selectedTube = null;

        ColourType[][] tubesData;
        int tubeCount;
        int capacity;

        if (useGeneratedLevel)
        {
            GeneratedLevel genLevel = LevelGenerator.Generate(difficulty);
            tubesData = genLevel.tubes;
            tubeCount = genLevel.tubeCount;
            capacity = genLevel.capacity;
        }
        else
        {
            tubesData = levelData.GetTubes();
            tubeCount = tubesData.Length;
            capacity = levelData.capacity;
        }

            int cols = Mathf.Min(columns, tubeCount);
        float startX = -(cols - 1) * tubeSpacing * 0.5f;

        for (int i = 0; i < tubeCount; i++)
        {
            int row = i / columns;  
            int col = i % columns;

            var tubeObj = Instantiate(tubePrefab, tubesParent);

            float x = startX + col * tubeSpacing;
            float y = rowY + row * rowSpacing;
            tubeObj.transform.localPosition = new Vector3(x, y, 0);

            var tubeCtrl = tubeObj.GetComponent<TubeController>();
            tubeCtrl.tubeIndex = i;
            tubeCtrl.capacity = capacity;
            tubeCtrl.waterPiecePrefab = waterPiecePrefab;

            var listColors = new List<ColourType>();
            foreach (var c in tubesData[i])
            {
                if(c != ColourType.None)
                    listColors.Add(c);
            }

            tubeCtrl.Init(listColors);
            _tubes.Add(tubeCtrl);
        }
    }

    public void OnTubeClicked(TubeController tube)
    {
        if (isAnimating) return;
        if (winPanel != null && winPanel.activeSelf) return;
        if (losePanel != null && losePanel.activeSelf) return;

        
        if (_selectedTube == null)
        {
            if (!tube.IsEmpty())
            {
                _selectedTube = tube;
                HighlightTube(tube, true);
                AudioManager.Instance?.PlaySelect();
            }
            return;
        }

        
        if (tube == _selectedTube)
        {
            HighlightTube(_selectedTube, false);
            _selectedTube = null;
            AudioManager.Instance?.PlaySelect();
            return;
        }

        HighlightTube(_selectedTube, false);

        var from = _selectedTube;
        _selectedTube = null;

        if (!isAnimating)
        {
            StartCoroutine(PourAnimated(from, tube));
        }
    }

    private IEnumerator PourAnimated(TubeController from, TubeController to)
    {
        if (!CanPourBasic(from, to, out int moveCount))
        {
            AudioManager.Instance?.PlayInvalid();
            yield break;
        }

        isAnimating = true;
        AudioManager.Instance?.PlayPourStart();

        Vector3 fromEuler0 = from.transform.eulerAngles;
        Vector3 toEuler0 = to.transform.eulerAngles;

        float dirX = Mathf.Sign(to.transform.position.x - from.transform.position.x);
        if(dirX == 0) dirX = 1f;

        float tiltAngleFrom = 15f * dirX;
        float tiltAngleTo = 5f * dirX;

        Sequence seq = DOTween.Sequence();

        float tiltDuration = 0.15f;
        seq.Join(from.transform.DORotate(fromEuler0 + new Vector3(0, 0, tiltAngleFrom), tiltDuration).SetEase(Ease.OutQuad));
        seq.Join(to.transform.DORotate(toEuler0 + new Vector3(0, 0, tiltAngleTo), tiltDuration).SetEase(Ease.OutQuad));

        for (int i = 0; i < moveCount; i++)
        {
            int fromIndex = from.colours.Count - 1;
            int toIndex = to.colours.Count;

            Vector3 startPos = from.GetSlotWorldPosition(fromIndex);
            Vector3 endPos = to.GetSlotWorldPosition(toIndex);

            ColourType colour = from.PopColourNoVisual();
            to.PushColourNoVisual(colour);

            var piece = Instantiate(waterPiecePrefab, startPos, Quaternion.identity, tubesParent);
            var sr = piece.GetComponent<SpriteRenderer>();
            sr.color = TubeController.ColourTypeToColor(colour);

            GameObject pieceLocal = piece;

            seq.Append(pieceLocal.transform.DOMove(endPos, pourDuration).SetEase(Ease.InOutQuad));

            seq.AppendCallback(() =>
            {
                Destroy(pieceLocal);
                from.RefreshVisual();
                to.RefreshVisual();
                AudioManager.Instance?.PlayPourEnd();
            });

            seq.AppendInterval(pourInterval);
        }

        float backDuration = 0.15f;
        seq.Append(from.transform.DORotate(fromEuler0, backDuration).SetEase(Ease.OutQuad));
        seq.Join(to.transform.DORotate(toEuler0, backDuration).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            isAnimating = false;
            CheckWinOrLose();
        });
        yield return seq.WaitForCompletion();
    }


    private bool CanPourBasic(TubeController from, TubeController to, out int moveCount)
    {
        moveCount = 0;

        if (from == to) return false;
        if (from.IsEmpty()) return false;
        if (to.IsFull()) return false;

        ColourType colorFrom = from.PeekTop();
        ColourType colorTo = to.PeekTop();

        if (!to.IsEmpty() && colorFrom != colorTo)
            return false;

        int sameCountOnTop = from.CountTopSameColour();
        int freeSlots = to.capacity - to.colours.Count;

        moveCount = Mathf.Min(sameCountOnTop, freeSlots);
        return moveCount > 0;
    }

    private void HighlightTube(TubeController tube, bool on)
    {
        if(tube == null) return;
        if (on)
            tube.StartSelectAnim();
        else
            tube.ResetScale();
    }

    private void CheckWinOrLose()
    {
        if (IsWin())
        {
            Debug.Log("Win: All tubes solved.");
            AudioManager.Instance?.PlayWin();
            isAnimating = true;
            StartCoroutine(FadePannel(winPanel, true, 0.3f));
            
        }
        else if (IsStuck())
        {
            Debug.Log("Lose: No more valid moves.");
            AudioManager.Instance?.PlayLose();
            isAnimating = true;
            StartCoroutine(FadePannel(losePanel, true, 0.3f));

        }
    }

    private bool IsWin()
    {
        foreach (var tube in _tubes)
        {
            if (tube.IsEmpty())    
                continue;

            if (!tube.IsFull())    
                return false;

            if (!tube.IsUniformAndFull()) 
                return false;
        }

        return true;
    }

    private bool IsStuck()
    {
        if (IsWin()) return false;  

        int n = _tubes.Count;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;

                if (CanPour(_tubes[i], _tubes[j]))
                {
                    
                    return false;
                }
            }
        }

        return true; 
    }

    private bool CanPour(TubeController from, TubeController to)
    {
        if (from.IsEmpty()) return false;
        if (to.IsFull()) return false;

        ColourType fromTop = from.PeekTop();


        if (to.IsEmpty()) return true;
        ColourType toTop = to.PeekTop();

        return fromTop == toTop;
    }

    private IEnumerator FadePannel(GameObject panel, bool show, float duration = 0.3f)
    {
        if (panel == null) yield break;
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) 
            cg = panel.AddComponent<CanvasGroup>();

        panel.SetActive(true);
        
        float targetAlpha = show ? 1f : 0f;
        Tween t = cg.DOFade(targetAlpha, duration);

        yield return t.WaitForCompletion();

        if(!show)
            panel.SetActive(false);

        if(!show)
            isAnimating = false;
    }

    public void RestartLevel()
    {
        isAnimating = false;
        InitLevel();
    }

    public void OnNextLevelButtonClicked()
    {
        if (useGeneratedLevel)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    difficulty = Difficulty.Medium;
                    break;
                case Difficulty.Medium:
                    difficulty = Difficulty.Hard;
                    break;
                case Difficulty.Hard:
                default:
                    difficulty = Difficulty.Easy;
                    break;
            }
        }
        else
        {

        }
        RestartLevel();
    }
}
