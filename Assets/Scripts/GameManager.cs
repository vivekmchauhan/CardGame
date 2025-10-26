using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Board")]
    public RectTransform boardContainer;
    public GameObject cardPrefab;
    public int rows = 4;
    public int cols = 4;

    [Header("Card Faces")]
    public Sprite cardBackSprite;
    public List<Sprite> faceSprites = new List<Sprite>();

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text statusText;

    [Header("Rules")]
    public float mismatchRevealDuration = 1.0f;
    public int scorePerMatch = 100;
    public int scorePerMismatch = -10;

    // Internal
    private List<Card> cards = new List<Card>();
    private List<Card> faceUpUnmatched = new List<Card>();
    private int score = 0;
    private System.Random rng;
    private int shuffleSeed = 0;

    public Button restartButton;

    private string saveFileName => Path.Combine(Application.persistentDataPath, "cardGame.json");


    private void Start()
    {
        StartNewGame(rows, cols);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
    }

    #region Game setup
    public void StartNewGame(int r, int c)
    {
        rows = r;
        cols = c;
        shuffleSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        rng = new System.Random(shuffleSeed);
        score = 0;
        UpdateScoreUI();
        BuildBoard();
        statusText.text = "New Game";
    }

    private void BuildBoard()
    {
        // Clear old
        foreach (Transform t in boardContainer) Destroy(t.gameObject);
        cards.Clear();
        faceUpUnmatched.Clear();

        int total = rows * cols;
        if (total % 2 != 0)
        {
            Debug.LogWarning("Total cards should be even. Adding one to make it even.");
            total += 1;
            cols = total / rows;
        }

        // Validate faceSprites
        int neededPairs = total / 2;
        if (faceSprites.Count < neededPairs)
        {
            Debug.LogError($"Not enough face sprites. Need at least {neededPairs} unique faces.");
            return;
        }

        List<int> ids = new List<int>();
        for (int i = 0; i < neededPairs; i++)
        {
            ids.Add(i);
            ids.Add(i);
        }

        ids = ids.OrderBy(x => rng.Next()).ToList();

        for (int i = 0; i < total; i++)
        {
            GameObject go = Instantiate(cardPrefab, boardContainer);
            Card card = go.GetComponent<Card>();
            int id = ids[i];
            Sprite face = faceSprites[id];
            card.frontImage.sprite = face;
            card.backImage.sprite = cardBackSprite;
            card.Initialize(id, face, i);
            card.OnFlippedToFaceUp += OnCardFlippedToFaceUp;
            card.OnMatched += OnCardMatched;
            cards.Add(card);
        }

        var grid = boardContainer.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            float paddingX = grid.padding.left + grid.padding.right;
            float paddingY = grid.padding.top + grid.padding.bottom;
            float spacingX = grid.spacing.x * (cols - 1);
            float width = boardContainer.rect.width - paddingX - spacingX;
            float cellW = Mathf.Floor(width / cols);
            grid.cellSize = new Vector2(200, 300);
        }
    }
    #endregion

    #region Card events & matching
    private void OnCardFlippedToFaceUp(Card card)
    {
        if (card.isMatched) return;
        if (!faceUpUnmatched.Contains(card)) faceUpUnmatched.Add(card);

        var matching = faceUpUnmatched.Where(c => c != card && c.cardId == card.cardId && !c.isMatched).ToList();
        if (matching.Count > 0)
        {
            var other = matching[0];
            RegisterMatch(card, other);
            return;
        }

        if (faceUpUnmatched.Count >= 2)
        {
            var otherCandidates = faceUpUnmatched.Where(c => c != card && !c.isMatched).ToList();
            if (otherCandidates.Count > 0)
            {
                Card other = otherCandidates[otherCandidates.Count - 1];
                if (other.cardId != card.cardId)
                {
                    StartCoroutine(HandleMismatchFlipBack(card, other, mismatchRevealDuration));
                }
            }
        }
    }

    private IEnumerator HandleMismatchFlipBack(Card a, Card b, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!a.isMatched && a.isFaceUp)
        {
            a.TryFlip();
        }
        if (!b.isMatched && b.isFaceUp)
        {
            b.TryFlip();
        }

        yield return new WaitForSeconds(0.01f);
        faceUpUnmatched.RemoveAll(c => !c.isFaceUp || c.isMatched);

        score += scorePerMismatch;
        UpdateScoreUI();
    }

    private void RegisterMatch(Card a, Card b)
    {
        if (a == null || b == null) return;
        a.MarkMatched();
        b.MarkMatched();
        faceUpUnmatched.Remove(a);
        faceUpUnmatched.Remove(b);
        score += scorePerMatch;
        UpdateScoreUI();

        if (cards.All(c => c.isMatched))
        {
            OnGameOver();
        }
    }

    private void OnCardMatched(Card card)
    {
    }

    private void OnGameOver()
    {
        statusText.text = "You Win!";

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);
    }
    public void RestartGame()
    {
        if (File.Exists(saveFileName))
            File.Delete(saveFileName);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        statusText.text = "Restarting...";
        StartNewGame(rows, cols);
    }
    #endregion

    #region UI
    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }
    #endregion
}
