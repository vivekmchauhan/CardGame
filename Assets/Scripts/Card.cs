using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class Card : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image frontImage;
    public Image backImage;

    [Header("Card Data")]
    public int cardId; // id to match
    public int index; // position in grid

    [Header("Flip Settings")]
    public float flipDuration = 0.35f;
    public bool startFaceDown = true;

    // Internal state
    public bool isFaceUp { get; private set; }
    public bool isMatched { get; private set; }
    public bool isBusy { get; private set; }

    private CanvasGroup cg;

    public event Action<Card> OnFlippedToFaceUp;
    public event Action<Card> OnMatched;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        isFaceUp = !startFaceDown;
        SetVisualImmediate();
    }

    public void Initialize(int id, Sprite frontSprite, int idx)
    {
        cardId = id;
        index = idx;
        frontImage.sprite = frontSprite;
        isMatched = false;
        isBusy = false;
        isFaceUp = false;
        SetVisualImmediate();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryFlip();
    }

    public void TryFlip()
    {
        if (isBusy || isMatched) return;
        StartCoroutine(DoFlip(!isFaceUp));
    }

    public IEnumerator DoFlip(bool showFace)
    {
        isBusy = true;
        float elapsed = 0f;
        float half = flipDuration / 2f;

        // first half: scale X to 0
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            float scale = Mathf.Lerp(1f, 0f, t);
            transform.localScale = new Vector3(scale, 1f, 1f);
            yield return null;
        }

        // swap visuals
        isFaceUp = showFace;
        SetVisualImmediate();

        // second half: scale back to 1
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            float scale = Mathf.Lerp(0f, 1f, t);
            transform.localScale = new Vector3(scale, 1f, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        isBusy = false;

        if (isFaceUp) OnFlippedToFaceUp?.Invoke(this);
    }

    public void MarkMatched()
    {
        isMatched = true;
        // optional: disable any interaction
        isBusy = true;
        OnMatched?.Invoke(this);
    }

    public void ForceFlipDownImmediate()
    {
        isFaceUp = false;
        SetVisualImmediate();
        isBusy = false;
    }

    private void SetVisualImmediate()
    {
        if (frontImage == null || backImage == null) return;
        frontImage.gameObject.SetActive(isFaceUp);
        backImage.gameObject.SetActive(!isFaceUp);
    }
}
