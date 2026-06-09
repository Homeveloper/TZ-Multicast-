using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleManager : MonoBehaviour
{
    [SerializeField] private PuzzleLevel[] levels;

    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private RectTransform trayRoot;
    [SerializeField] private PuzzlePiece piecePrefab;
    [SerializeField] private Image slotPrefab;

    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject completePanel;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button hintButton;

    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private RectTransform levelButtonsRoot;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private Button openLevelSelectButton;
    [SerializeField] private Button closeLevelSelectButton;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip snapClip;
    [SerializeField] private AudioClip completeClip;
    [SerializeField] private AudioClip buttonClip;

    [SerializeField] private FireworksVfx fireworksVfx;

    [SerializeField] private float snapDistance = 120f;
    [SerializeField] private float boardGap = 8f;
    [SerializeField] private float trayGap = 8f;

    private readonly List<RectTransform> slots = new List<RectTransform>();
    private readonly List<PuzzlePiece> pieces = new List<PuzzlePiece>();

    private int currentLevelIndex;
    private int placedCount;
    private Coroutine hintRoutine;

    private struct TrayLayout
    {
        public int columns;
        public int rows;

        public TrayLayout(int columns, int rows)
        {
            this.columns = columns;
            this.rows = rows;
        }
    }

    private void Start()
    {
        nextButton.onClick.AddListener(PlayButtonSound);
        nextButton.onClick.AddListener(LoadNextLevel);

        hintButton.onClick.AddListener(PlayButtonSound);
        hintButton.onClick.AddListener(ShowHint);

        openLevelSelectButton.onClick.AddListener(PlayButtonSound);
        openLevelSelectButton.onClick.AddListener(OpenLevelSelect);

        closeLevelSelectButton.onClick.AddListener(PlayButtonSound);
        closeLevelSelectButton.onClick.AddListener(CloseLevelSelect);

        levelSelectPanel.SetActive(false);

        CreateLevelSelectButtons();
        LoadLevel(0);
    }

    private void CreateLevelSelectButtons()
    {
        foreach (Transform child in levelButtonsRoot)
            Destroy(child.gameObject);

        for (int i = 0; i < levels.Length; i++)
        {
            int levelIndex = i;

            Button button = Instantiate(levelButtonPrefab, levelButtonsRoot);
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
                buttonText.text = (i + 1) + ". " + levels[i].levelName;

            button.onClick.AddListener(PlayButtonSound);
            button.onClick.AddListener(() => SelectLevel(levelIndex));
        }
    }

    private void OpenLevelSelect()
    {
        levelSelectPanel.SetActive(true);
        levelSelectPanel.transform.SetAsLastSibling();
    }

    private void CloseLevelSelect()
    {
        levelSelectPanel.SetActive(false);
    }

    private void SelectLevel(int levelIndex)
    {
        CloseLevelSelect();
        LoadLevel(levelIndex);
    }

    private void LoadLevel(int levelIndex)
    {
        if (levels == null || levels.Length == 0)
            return;

        currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
        placedCount = 0;

        ClearLevel();
        Canvas.ForceUpdateCanvases();

        PuzzleLevel level = levels[currentLevelIndex];

        levelText.text = level.levelName;
        completePanel.SetActive(false);

        GeneratePuzzle(level);
        UpdateProgress();
    }

    private void ClearLevel()
    {
        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
        }

        foreach (Transform child in boardRoot)
            Destroy(child.gameObject);

        foreach (Transform child in trayRoot)
            Destroy(child.gameObject);

        slots.Clear();
        pieces.Clear();
    }

    private void GeneratePuzzle(PuzzleLevel level)
    {
        int totalPieces = level.rows * level.columns;

        List<Sprite> sprites = CreateSprites(level.image, level.rows, level.columns);
        TrayLayout trayLayout = GetBestTrayLayout(totalPieces);

        float boardPieceSize = GetBoardPieceSize(level.rows, level.columns);
        float trayPieceSize = GetTrayPieceSize(trayLayout);

        CreateSlots(level, sprites, boardPieceSize);
        CreatePieces(sprites, trayPieceSize);
        ShufflePiecesInTray(trayLayout, trayPieceSize);
    }

    private float GetBoardPieceSize(int rows, int columns)
    {
        float availableWidth = boardRoot.rect.width - boardGap * (columns - 1);
        float availableHeight = boardRoot.rect.height - boardGap * (rows - 1);

        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;

        return Mathf.Min(cellWidth, cellHeight);
    }

    private float GetTrayPieceSize(TrayLayout layout)
    {
        float availableWidth = trayRoot.rect.width - trayGap * (layout.columns - 1);
        float availableHeight = trayRoot.rect.height - trayGap * (layout.rows - 1);

        float cellWidth = availableWidth / layout.columns;
        float cellHeight = availableHeight / layout.rows;

        return Mathf.Min(cellWidth, cellHeight);
    }

    private TrayLayout GetBestTrayLayout(int totalPieces)
    {
        if (totalPieces <= 1)
            return new TrayLayout(1, 1);

        if (totalPieces == 2)
            return new TrayLayout(2, 1);

        if (totalPieces == 3)
            return new TrayLayout(3, 1);

        if (totalPieces == 4)
            return new TrayLayout(2, 2);

        if (totalPieces <= 6)
            return new TrayLayout(3, 2);

        if (totalPieces <= 8)
            return new TrayLayout(4, 2);

        return new TrayLayout(4, Mathf.CeilToInt(totalPieces / 4f));
    }

    private void CreateSlots(PuzzleLevel level, List<Sprite> sprites, float pieceSize)
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            int row = i / level.columns;
            int column = i % level.columns;

            Image slot = Instantiate(slotPrefab, boardRoot);
            RectTransform slotRect = slot.GetComponent<RectTransform>();

            SetupPuzzleRect(slotRect, pieceSize);
            slotRect.anchoredPosition = GetGridPosition(row, column, level.rows, level.columns, pieceSize, boardGap);

            slot.sprite = sprites[i];
            slot.preserveAspect = true;
            slot.raycastTarget = false;
            slot.color = new Color(0f, 0f, 0f, 0.22f);

            slots.Add(slotRect);
        }
    }

    private void CreatePieces(List<Sprite> sprites, float pieceSize)
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            PuzzlePiece piece = Instantiate(piecePrefab, trayRoot);
            RectTransform pieceRect = piece.GetComponent<RectTransform>();

            SetupPuzzleRect(pieceRect, pieceSize);

            piece.Init(this, canvas, sprites[i], i);
            pieces.Add(piece);
        }
    }

    private void SetupPuzzleRect(RectTransform rect, float size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
    }

    private Vector2 GetGridPosition(int row, int column, int rows, int columns, float size, float gap)
    {
        float step = size + gap;

        float startX = -step * (columns - 1) * 0.5f;
        float startY = step * (rows - 1) * 0.5f;

        return new Vector2(startX + column * step, startY - row * step);
    }

    private void ShufflePiecesInTray(TrayLayout layout, float pieceSize)
    {
        List<PuzzlePiece> shuffledPieces = GetShuffledPieces();

        for (int i = 0; i < shuffledPieces.Count; i++)
        {
            int row = i / layout.columns;
            int column = i % layout.columns;

            int piecesInRow = GetPiecesCountInRow(row, layout.columns, shuffledPieces.Count);
            Vector2 position = GetCenteredTrayPosition(row, column, layout.rows, piecesInRow, pieceSize);

            shuffledPieces[i].SetStartPosition(position);
        }
    }

    private List<PuzzlePiece> GetShuffledPieces()
    {
        List<PuzzlePiece> shuffledPieces = new List<PuzzlePiece>(pieces);

        for (int attempt = 0; attempt < 12; attempt++)
        {
            ShuffleList(shuffledPieces);

            if (!HasPiecesInOriginalVisualPlaces(shuffledPieces))
                return shuffledPieces;
        }

        if (shuffledPieces.Count > 1)
        {
            PuzzlePiece firstPiece = shuffledPieces[0];
            shuffledPieces.RemoveAt(0);
            shuffledPieces.Add(firstPiece);
        }

        return shuffledPieces;
    }

    private void ShuffleList(List<PuzzlePiece> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            PuzzlePiece temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private bool HasPiecesInOriginalVisualPlaces(List<PuzzlePiece> shuffledPieces)
    {
        if (shuffledPieces.Count <= 1)
            return false;

        for (int i = 0; i < shuffledPieces.Count; i++)
        {
            if (shuffledPieces[i].CorrectIndex == i)
                return true;
        }

        return false;
    }

    private int GetPiecesCountInRow(int row, int columns, int totalPieces)
    {
        int usedBeforeRow = row * columns;
        int remaining = totalPieces - usedBeforeRow;

        return Mathf.Clamp(remaining, 0, columns);
    }

    private Vector2 GetCenteredTrayPosition(int row, int column, int rows, int columnsInRow, float size)
    {
        float step = size + trayGap;

        float startX = -step * (columnsInRow - 1) * 0.5f;
        float startY = step * (rows - 1) * 0.5f;

        return new Vector2(startX + column * step, startY - row * step);
    }

    private List<Sprite> CreateSprites(Texture2D texture, int rows, int columns)
    {
        List<Sprite> result = new List<Sprite>();

        int pieceWidth = texture.width / columns;
        int pieceHeight = texture.height / rows;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Rect rect = new Rect(
                    column * pieceWidth,
                    texture.height - (row + 1) * pieceHeight,
                    pieceWidth,
                    pieceHeight
                );

                Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
                result.Add(sprite);
            }
        }

        return result;
    }

    public void TryPlacePiece(PuzzlePiece piece)
    {
        RectTransform pieceRect = piece.GetComponent<RectTransform>();
        RectTransform correctSlot = slots[piece.CorrectIndex];

        float distance = Vector2.Distance(pieceRect.position, correctSlot.position);

        if (distance > snapDistance)
        {
            piece.ReturnToStart();
            return;
        }

        piece.SnapTo(correctSlot);
        placedCount++;

        PlaySound(snapClip);
        UpdateProgress();

        if (placedCount == pieces.Count)
            CompleteLevel();
    }

    private void UpdateProgress()
    {
        progressText.text = placedCount + "/" + pieces.Count;
    }

    private void CompleteLevel()
    {
        completePanel.SetActive(true);

        PlaySound(completeClip);

        if (fireworksVfx != null)
            fireworksVfx.Play();
    }

    private void LoadNextLevel()
    {
        int nextLevelIndex = currentLevelIndex + 1;

        if (nextLevelIndex >= levels.Length)
            nextLevelIndex = 0;

        LoadLevel(nextLevelIndex);
    }

    private void ShowHint()
    {
        if (hintRoutine != null)
            return;

        PuzzlePiece piece = GetFirstUnlockedPiece();

        if (piece == null)
            return;

        RectTransform slot = slots[piece.CorrectIndex];
        RectTransform pieceRect = piece.GetComponent<RectTransform>();

        hintRoutine = StartCoroutine(PlayHint(slot, pieceRect));
    }

    private PuzzlePiece GetFirstUnlockedPiece()
    {
        foreach (PuzzlePiece piece in pieces)
        {
            if (!piece.IsLocked)
                return piece;
        }

        return null;
    }

    private IEnumerator PlayHint(RectTransform slot, RectTransform piece)
    {
        Vector3 slotStartScale = slot.localScale;
        Vector3 pieceStartScale = piece.localScale;

        Image slotImage = slot.GetComponent<Image>();
        Color slotStartColor = slotImage.color;

        slotImage.color = new Color(1f, 0.85f, 0.1f, 0.55f);

        for (int i = 0; i < 3; i++)
        {
            slot.localScale = slotStartScale * 1.18f;
            piece.localScale = pieceStartScale * 1.18f;

            yield return new WaitForSeconds(0.15f);

            slot.localScale = slotStartScale;
            piece.localScale = pieceStartScale;

            yield return new WaitForSeconds(0.15f);
        }

        slotImage.color = slotStartColor;
        hintRoutine = null;
    }

    private void PlayButtonSound()
    {
        PlaySound(buttonClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}