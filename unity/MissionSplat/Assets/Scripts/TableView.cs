namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class TableView : MonoBehaviour
{
    private RectTransform _root;
    private RectTransform _safe;
    private RectTransform _play;
    private RectTransform _setup;
    private Rect _seenSafeArea;
    private int _seenScreenWidth;
    private int _seenScreenHeight;
    private ScreenOrientation _seenOrientation;
    private bool _seenSafeAreaFrame;
    private TablePlayView _playView;
    private TableSetupView _setupView;
    private TableSnapshot _rendered;

    public event Action<int, int> Tapped;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;
    public event Action<TableStart> Started;

    private void Awake()
    {
        PaintCamera();
        BuildCanvas();
        ShowSetup(TableStart.PassAndPlayDraft());
    }

    // The overlay canvas does not track Screen.safeArea, so the inset is reapplied when it changes.
    // Lattices keep pixel sizes from the last Render, so that snapshot is painted again. Setup has none and stays put.
    private void Update()
    {
        if (_safe != null && SafeAreaChanged())
        {
            ApplySafeArea();
        }
    }

    private static void PaintCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SplatPalette.Cream;
    }

    public void ShowSetup(TableStart draft)
    {
        _rendered = null;
        _play.gameObject.SetActive(false);
        _setup.gameObject.SetActive(true);
        _setupView.Show(draft);
    }

    public void Render(TableSnapshot snapshot)
    {
        _rendered = snapshot;
        _setup.gameObject.SetActive(false);
        _play.gameObject.SetActive(true);
        _playView.Render(snapshot);
    }

    private void BuildCanvas()
    {
        EnsureEventSystem();
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _root = canvasGo.GetComponent<RectTransform>();
        var background = Ui.Image("Background", _root, SplatPalette.Cream);
        Ui.Stretch(background.rectTransform);

        _safe = Ui.Rect("SafeArea", _root);
        ApplySafeArea();
        _play = Ui.Rect("Play", _safe);
        Ui.Stretch(_play);
        _setup = Ui.Rect("Setup", _safe);
        Ui.Stretch(_setup);
        _playView = new TablePlayView(_play);
        _setupView = new TableSetupView(_setup);
        _playView.Tapped += (tileX, tileY) => Tapped?.Invoke(tileX, tileY);
        _playView.Confirmed += () => Confirmed?.Invoke();
        _playView.QuarterTurnsSelected += turns => QuarterTurnsSelected?.Invoke(turns);
        _setupView.Started += draft => Started?.Invoke(draft);
        _play.gameObject.SetActive(false);
    }

    private bool SafeAreaChanged() =>
        !_seenSafeAreaFrame
        || Screen.safeArea != _seenSafeArea
        || Screen.width != _seenScreenWidth
        || Screen.height != _seenScreenHeight
        || Screen.orientation != _seenOrientation;

    private void ApplySafeArea()
    {
        var safe = Screen.safeArea;
        var width = Screen.width;
        var height = Screen.height;
        _seenSafeArea = safe;
        _seenScreenWidth = width;
        _seenScreenHeight = height;
        _seenOrientation = Screen.orientation;
        _seenSafeAreaFrame = true;
        if (width <= 0 || height <= 0 || safe.width <= 0f || safe.height <= 0f)
        {
            Ui.Stretch(_safe);
        }
        else
        {
            var min = new Vector2(Mathf.Clamp01(safe.xMin / width), Mathf.Clamp01(safe.yMin / height));
            var max = new Vector2(Mathf.Clamp01(safe.xMax / width), Mathf.Clamp01(safe.yMax / height));
            if (max.x <= min.x || max.y <= min.y)
            {
                Ui.Stretch(_safe);
            }
            else
            {
                Ui.Anchored(_safe, min, max, Vector2.zero, Vector2.zero);
            }
        }

        RenderCurrentSnapshot();
    }

    private void RenderCurrentSnapshot()
    {
        if (_rendered == null || _play == null || !_play.gameObject.activeSelf)
        {
            return;
        }

        // Anchor changes do not update child rects until the canvas lays out. The board measures those rects.
        Canvas.ForceUpdateCanvases();
        _playView.Render(_rendered);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        // Input System 1.19 assigns DefaultInputActions from OnEnable when no actions are set, including touch.
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.transform.SetParent(transform, false);
    }
}
}
