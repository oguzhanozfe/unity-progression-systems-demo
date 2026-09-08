using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OguzhanOzdemir.ProgressionDemo
{
    public sealed class ProgressionDemoController : MonoBehaviour
    {
        private const string SaveKey = "progression-demo-v1";
        private static readonly Color Background = new Color32(8, 13, 28, 255);
        private static readonly Color Surface = new Color32(17, 25, 48, 255);
        private static readonly Color Muted = new Color32(126, 143, 178, 255);
        private static readonly Color Cyan = new Color32(52, 219, 210, 255);
        private static readonly Color Violet = new Color32(150, 92, 255, 255);

        private readonly List<RectTransform> _waypoints = new List<RectTransform>();
        private ProgressionState _progression;
        private NavigationQueue _navigation;
        private RectTransform _player;
        private Image _xpFill;
        private Text _levelText;
        private Text _xpText;
        private Text _queueText;
        private Text _statusText;
        private bool _animating;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDemoExists()
        {
            if (FindObjectOfType<ProgressionDemoController>() == null)
                new GameObject("Progression Demo").AddComponent<ProgressionDemoController>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            ProgressionSnapshot snapshot = JsonUtility.FromJson<ProgressionSnapshot>(
                PlayerPrefs.GetString(SaveKey, "{\"level\":1,\"xp\":0}"));
            _progression = new ProgressionState(new ProgressionCurve(), snapshot);
            _navigation = new NavigationQueue(7);
            BuildInterface();
            RefreshProgression();
            StartCoroutine(CaptureScreenshotIfRequested());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) QueueMove(NavigationIntent.Advance);
            if (Input.GetKeyDown(KeyCode.F)) QueueMove(NavigationIntent.Finish);
            if (Input.GetKeyDown(KeyCode.X)) GainXp();
        }

        private void BuildInterface()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform);
            }

            Canvas canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.transform.SetParent(transform);
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1180, 720);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = Panel(canvas.transform, "Background", Background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform content = Panel(root, "Content", Color.clear, new Vector2(.07f, .07f), new Vector2(.93f, .93f), Vector2.zero, Vector2.zero);
            Text(content, "ENGINEERING DEMO", 14, Cyan, TextAnchor.MiddleLeft, new Vector2(0, .91f), new Vector2(.55f, 1));
            Text(content, "Progression Systems Demo", 42, Color.white, TextAnchor.MiddleLeft, new Vector2(0, .77f), new Vector2(.72f, .93f), FontStyle.Bold);
            Text(content, "A deterministic command queue and durable XP model, shown through a compact Unity interface.", 18, Muted, TextAnchor.UpperLeft, new Vector2(0, .67f), new Vector2(.72f, .78f));

            RectTransform pathCard = Panel(content, "Path Card", Surface, new Vector2(0, .22f), new Vector2(.72f, .64f), Vector2.zero, Vector2.zero);
            Text(pathCard, "COMMAND PATH", 13, Muted, TextAnchor.MiddleLeft, new Vector2(.045f, .84f), new Vector2(.5f, .97f), FontStyle.Bold);
            CreatePath(pathCard);

            RectTransform side = Panel(content, "Progress Card", new Color32(21, 30, 58, 255), new Vector2(.75f, .22f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            Text(side, "PLAYER STATE", 13, Muted, TextAnchor.MiddleLeft, new Vector2(.08f, .89f), new Vector2(.92f, .98f), FontStyle.Bold);
            _levelText = Text(side, "", 34, Color.white, TextAnchor.MiddleLeft, new Vector2(.08f, .72f), new Vector2(.92f, .89f), FontStyle.Bold);
            _xpText = Text(side, "", 15, Muted, TextAnchor.MiddleLeft, new Vector2(.08f, .64f), new Vector2(.92f, .74f));
            RectTransform bar = Panel(side, "XP Bar", new Color32(39, 51, 82, 255), new Vector2(.08f, .57f), new Vector2(.92f, .61f), Vector2.zero, Vector2.zero);
            RectTransform fill = Panel(bar, "Fill", Cyan, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.anchorMax = new Vector2(0, 1);
            _xpFill = fill.GetComponent<Image>();
            Button(side, "+350 XP", new Vector2(.08f, .43f), new Vector2(.92f, .53f), Cyan, GainXp);
            Button(side, "Reset saved state", new Vector2(.08f, .31f), new Vector2(.92f, .40f), new Color32(49, 61, 95, 255), ResetProgress);
            _statusText = Text(side, "", 14, Muted, TextAnchor.UpperLeft, new Vector2(.08f, .08f), new Vector2(.92f, .27f));

            RectTransform controls = Panel(content, "Controls", Color.clear, new Vector2(0, 0), new Vector2(.72f, .18f), Vector2.zero, Vector2.zero);
            Button(controls, "Advance", new Vector2(0, .28f), new Vector2(.29f, .92f), Violet, () => QueueMove(NavigationIntent.Advance));
            Button(controls, "Finish path", new Vector2(.31f, .28f), new Vector2(.60f, .92f), new Color32(49, 61, 95, 255), () => QueueMove(NavigationIntent.Finish));
            _queueText = Text(controls, "", 14, Muted, TextAnchor.MiddleRight, new Vector2(.62f, .28f), new Vector2(1, .92f));
        }

        private void CreatePath(RectTransform parent)
        {
            Vector2[] positions = {
                new Vector2(-330, -45), new Vector2(-220, 30), new Vector2(-110, -20), new Vector2(0, 52),
                new Vector2(110, -10), new Vector2(220, 38), new Vector2(330, -35)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                RectTransform node = Panel(parent, "Waypoint " + (i + 1), i == positions.Length - 1 ? Violet : new Color32(49, 61, 95, 255),
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), positions[i], new Vector2(34, 34));
                node.GetComponent<Image>().sprite = CircleSprite();
                _waypoints.Add(node);
            }
            _player = Panel(parent, "Player", Cyan, new Vector2(.5f, .5f), new Vector2(.5f, .5f), positions[0], new Vector2(22, 22));
            _player.GetComponent<Image>().sprite = CircleSprite();
            Outline outline = _player.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, .25f);
            outline.effectDistance = new Vector2(7, -7);
        }

        private void QueueMove(NavigationIntent intent)
        {
            _navigation.Request(intent);
            _queueText.text = _navigation.PendingCount + " queued";
            if (!_animating) StartCoroutine(RunQueue());
        }

        private IEnumerator RunQueue()
        {
            _animating = true;
            while (_navigation.TryBeginNext(out NavigationMove move))
            {
                if (move.From == move.To)
                {
                    _statusText.text = "Path complete. Finish requests are idempotent.";
                    continue;
                }
                Vector2 start = _player.anchoredPosition;
                Vector2 end = _waypoints[move.To].anchoredPosition;
                float elapsed = 0;
                const float duration = .38f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                    _player.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
                    yield return null;
                }
                _player.anchoredPosition = end;
                _navigation.Complete(move);
                _queueText.text = _navigation.PendingCount + " queued";
                _statusText.text = move.Intent == NavigationIntent.Finish
                    ? "Finish superseded earlier pending commands."
                    : "Advance completed in FIFO order.";
            }
            _animating = false;
        }

        private void GainXp()
        {
            ProgressionChange change = _progression.AddXp(350);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_progression.Snapshot()));
            PlayerPrefs.Save();
            _statusText.text = change.LevelsGained > 0
                ? "Level-up applied with overflow XP preserved."
                : change.ReachedMaximum ? "Maximum level reached." : "XP saved locally.";
            RefreshProgression();
        }

        private void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            _progression = new ProgressionState(new ProgressionCurve(), new ProgressionSnapshot(1, 0));
            _statusText.text = "Saved progression reset.";
            RefreshProgression();
        }

        private void RefreshProgression()
        {
            _levelText.text = "Level " + _progression.Level;
            _xpText.text = _progression.IsMaximumLevel ? "MAX LEVEL" : _progression.Xp + " / " + _progression.RequiredXp + " XP";
            RectTransform fill = _xpFill.rectTransform;
            fill.anchorMax = new Vector2(_progression.NormalizedProgress, 1);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        private static IEnumerator CaptureScreenshotIfRequested()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(arguments, "-capturePath");
            if (flag < 0 || flag + 1 >= arguments.Length)
                yield break;

            string capturePath = Path.GetFullPath(arguments[flag + 1]);
            string directory = Path.GetDirectoryName(capturePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            yield return new WaitForSecondsRealtime(1f);
            yield return new WaitForEndOfFrame();
            if (File.Exists(capturePath))
                File.Delete(capturePath);
            ScreenCapture.CaptureScreenshot(capturePath);

            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!File.Exists(capturePath) || new FileInfo(capturePath).Length == 0) && Time.realtimeSinceStartup < deadline)
                yield return null;

            Application.Quit();
        }

        private static RectTransform Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            value.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text Text(Transform parent, string value, int size, Color color, TextAnchor alignment, Vector2 min, Vector2 max, FontStyle style = FontStyle.Normal)
        {
            GameObject valueObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            valueObject.transform.SetParent(parent, false);
            RectTransform rect = valueObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            Text text = valueObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Button(Transform parent, string label, Vector2 min, Vector2 max, Color color, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Panel(parent, label, color, min, max, Vector2.zero, Vector2.zero);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, .14f);
            colors.pressedColor = Color.Lerp(color, Color.black, .18f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Text text = Text(rect, label, 16, color == Cyan ? Background : Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, FontStyle.Bold);
            text.raycastTarget = false;
        }

        private static Sprite CircleSprite()
        {
            const int size = 48;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Runtime Circle" };
            Color32[] pixels = new Color32[size * size];
            float radius = (size - 1) * .5f;
            Vector2 center = new Vector2(radius, radius);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[(y * size) + x] = Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        }
    }
}
