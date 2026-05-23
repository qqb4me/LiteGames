using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PuzzleSceneManager : MonoBehaviour
{
    public bool InputLocked { get; private set; }

    private readonly List<PuzzlePiece> _pieces = new List<PuzzlePiece>();
    private readonly Dictionary<PuzzlePiece, List<Vector2Int>> _occupiedByPiece = new Dictionary<PuzzlePiece, List<Vector2Int>>();
    private readonly Dictionary<Vector2Int, PuzzlePiece> _cellOwner = new Dictionary<Vector2Int, PuzzlePiece>();
    private readonly HashSet<Vector2Int> _targetCells = new HashSet<Vector2Int>();

    private PuzzleField _field;
    private Canvas _canvas;
    private Button _resetButton;
    private Button _submitButton;
    private GameObject _victoryPanel;
    private bool _initialized;

    public static PuzzleSceneManager GetOrCreate()
    {
        PuzzleSceneManager existing = FindAnyObjectByType<PuzzleSceneManager>();
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject("PuzzleSceneManager");
        return go.AddComponent<PuzzleSceneManager>();
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    public void RegisterPiece(PuzzlePiece piece)
    {
        if (piece == null || _pieces.Contains(piece))
        {
            return;
        }

        _pieces.Add(piece);
        EnsureInitialized();
        RefreshSubmitButton();
    }

    public void UnregisterPiece(PuzzlePiece piece)
    {
        if (piece == null)
        {
            return;
        }

        DetachPiece(piece);
        _pieces.Remove(piece);
        RefreshSubmitButton();
    }

    public void TryPlacePiece(PuzzlePiece piece)
    {
        if (piece == null)
        {
            return;
        }

        DetachPiece(piece);

        if (_field == null || _targetCells.Count == 0)
        {
            piece.ResetToInventory();
            return;
        }

        if (TryFindBestPlacement(piece, out Vector2Int anchor, out Vector3 snappedWorld, out List<Vector2Int> occupiedCells))
        {
            for (int i = 0; i < occupiedCells.Count; i++)
            {
                _cellOwner[occupiedCells[i]] = piece;
            }

            _occupiedByPiece[piece] = occupiedCells;
            piece.MarkPlaced(anchor, snappedWorld);
            RefreshSubmitButton();
            CheckVictory();
            return;
        }

        piece.ResetToInventory();
        RefreshSubmitButton();
    }

    public void DetachPiece(PuzzlePiece piece)
    {
        if (piece == null)
        {
            return;
        }

        if (_occupiedByPiece.TryGetValue(piece, out List<Vector2Int> cells))
        {
            for (int i = 0; i < cells.Count; i++)
            {
                _cellOwner.Remove(cells[i]);
            }

            _occupiedByPiece.Remove(piece);
        }

        piece.MarkDetached();
    }

    public void NotifyPieceTransformed(PuzzlePiece piece)
    {
        if (piece == null)
        {
            return;
        }

        if (piece.IsPlaced)
        {
            TryPlacePiece(piece);
        }
    }

    public bool IsFull()
    {
        return _targetCells.Count > 0 && _cellOwner.Count == _targetCells.Count;
    }

    public void ResetPuzzle()
    {
        _cellOwner.Clear();
        _occupiedByPiece.Clear();

        for (int i = 0; i < _pieces.Count; i++)
        {
            if (_pieces[i] != null)
            {
                _pieces[i].ResetToInventory();
            }
        }

        InputLocked = false;
        HideVictoryScreen();
        RefreshSubmitButton();
    }

    public void SubmitPotion()
    {
        CheckVictory(force: true);
    }

    public float GetFieldCellSize()
    {
        EnsureInitialized();
        return _field != null ? Mathf.Max(0.01f, _field.cellSize) : 0f;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _field = FindAnyObjectByType<PuzzleField>();
        if (_field == null)
        {
            Debug.LogError("[PuzzleSceneManager] No PuzzleField found. Put PuzzleField on the white field sprite.");
            return;
        }

        _field.Bake();
        _targetCells.Clear();
        foreach (Vector2Int cell in _field.TargetCells)
        {
            _targetCells.Add(cell);
        }

        BuildRuntimeUiIfNeeded();
        RefreshSubmitButton();
        _initialized = true;
    }

    private bool TryFindBestPlacement(PuzzlePiece piece, out Vector2Int anchor, out Vector3 snappedWorld, out List<Vector2Int> occupiedCells)
    {
        anchor = default;
        snappedWorld = default;
        occupiedCells = null;

        Vector2Int[] shape = piece.GetRotatedShapeCells();
        if (shape == null || shape.Length == 0)
        {
            return false;
        }

        Vector2Int preferredAnchor = _field.WorldToGridCell(piece.GetAnchorReferenceWorld());
        float bestAnchorCellDistance = float.MaxValue;
        float bestWorldDistance = float.MaxValue;
        bool found = false;

        foreach (Vector2Int targetCell in _targetCells)
        {
            for (int i = 0; i < shape.Length; i++)
            {
                Vector2Int candidateAnchor = targetCell - shape[i];

                if (!CanPlaceAt(candidateAnchor, shape, out List<Vector2Int> candidateCells))
                {
                    continue;
                }

                Vector3 candidateAnchorWorld = _field.GridCellToWorld(candidateAnchor);
                Vector3 candidateWorld = piece.GetSnappedWorldFromAnchor(candidateAnchorWorld);
                float anchorCellDistance = (candidateAnchor - preferredAnchor).sqrMagnitude;
                float worldDistance = (piece.transform.position - candidateWorld).sqrMagnitude;
                if (!found || anchorCellDistance < bestAnchorCellDistance ||
                    (Mathf.Approximately(anchorCellDistance, bestAnchorCellDistance) && worldDistance < bestWorldDistance))
                {
                    found = true;
                    bestAnchorCellDistance = anchorCellDistance;
                    bestWorldDistance = worldDistance;
                    anchor = candidateAnchor;
                    snappedWorld = candidateWorld;
                    occupiedCells = candidateCells;
                }
            }
        }

        return found;
    }

    private bool CanPlaceAt(Vector2Int anchor, Vector2Int[] shape, out List<Vector2Int> occupiedCells)
    {
        occupiedCells = new List<Vector2Int>(shape.Length);

        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int cell = anchor + shape[i];
            if (!_targetCells.Contains(cell) || _cellOwner.ContainsKey(cell))
            {
                return false;
            }

            occupiedCells.Add(cell);
        }

        return true;
    }

    private void CheckVictory(bool force = false)
    {
        if (!force && !IsFull())
        {
            RefreshSubmitButton();
            return;
        }

        if (IsFull())
        {
            ShowVictoryScreen();
        }
        else if (force)
        {
            RefreshSubmitButton();
        }
    }

    private void BuildRuntimeUiIfNeeded()
    {
        if (_canvas != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasGo = new GameObject("PuzzleRuntimeUI");
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        _resetButton = CreateButton("Сброс", new Vector2(110f, 40f), new Vector2(110f, 40f), ResetPuzzle);
        _submitButton = CreateButton("Победа", new Vector2(-130f, 40f), new Vector2(220f, 40f), SubmitPotion);
        _submitButton.interactable = false;

        _victoryPanel = CreateVictoryPanel();
        _victoryPanel.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private Button CreateButton(string title, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction click)
    {
        GameObject btnGo = new GameObject(title + "Button");
        btnGo.transform.SetParent(_canvas.transform, false);

        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchoredPos.x < 0 ? 1f : 0f, 0f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = new Vector2(anchoredPos.x < 0 ? 1f : 0f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.35f, 0.2f, 0.95f);

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(click);

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        Text txt = txtGo.AddComponent<Text>();
        txt.text = title;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return button;
    }

    private GameObject CreateVictoryPanel()
    {
        GameObject panel = new GameObject("VictoryPanel");
        panel.transform.SetParent(_canvas.transform, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(panel.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.15f, 0.4f);
        textRt.anchorMax = new Vector2(0.85f, 0.75f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 42;
        text.color = Color.white;
        text.text = "Зелье готово!";

        Button close = CreateFixedSizeButton(panel.transform, "Продолжить", new Vector2(0.5f, 0.2f), 320f, 85f, () =>
        {
            SceneManager.LoadScene("AlchemistHomeAfterHeal");
        });
        close.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;

        return panel;
    }

    private Button CreateFixedSizeButton(Transform parent, string title, Vector2 anchor, float width, float height, UnityEngine.Events.UnityAction click)
    {
        GameObject btnGo = new GameObject(title + "Btn");
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, height);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.28f, 0.45f, 0.28f, 1f);

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(click);

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        Text txt = txtGo.AddComponent<Text>();
        txt.text = title;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = Mathf.RoundToInt(height * 0.48f);

        return button;
    }

    private void ShowVictoryScreen()
    {
        if (_victoryPanel == null)
        {
            return;
        }

        InputLocked = true;
        _victoryPanel.SetActive(true);
    }

    private void HideVictoryScreen()
    {
        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(false);
        }
    }

    private void RefreshSubmitButton()
    {
        if (_submitButton != null)
        {
            _submitButton.interactable = IsFull();
            Image img = _submitButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = _submitButton.interactable ? new Color(0.2f, 0.48f, 0.2f, 0.95f) : new Color(0.4f, 0.4f, 0.4f, 0.95f);
            }
        }
    }
}
