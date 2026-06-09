using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image image;

    private PuzzleManager manager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 startPosition;
    private int correctIndex;
    private bool isLocked;

    public int CorrectIndex => correctIndex;
    public bool IsLocked => isLocked;

    public void Init(PuzzleManager owner, Canvas parentCanvas, Sprite sprite, int index)
    {
        manager = owner;
        canvas = parentCanvas;
        correctIndex = index;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (image == null)
            image = GetComponent<Image>();

        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        isLocked = false;
        canvasGroup.blocksRaycasts = true;
    }

    public void SetStartPosition(Vector2 position)
    {
        startPosition = position;
        rectTransform.anchoredPosition = position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked)
            return;

        startPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked)
            return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked)
            return;

        canvasGroup.blocksRaycasts = true;
        manager.TryPlacePiece(this);
    }

    public void SnapTo(RectTransform slot)
    {
        isLocked = true;

        rectTransform.SetParent(slot.parent, false);
        rectTransform.anchorMin = slot.anchorMin;
        rectTransform.anchorMax = slot.anchorMax;
        rectTransform.pivot = slot.pivot;
        rectTransform.sizeDelta = slot.sizeDelta;
        rectTransform.anchoredPosition = slot.anchoredPosition;

        canvasGroup.blocksRaycasts = false;
    }

    public void ReturnToStart()
    {
        rectTransform.anchoredPosition = startPosition;
    }
}