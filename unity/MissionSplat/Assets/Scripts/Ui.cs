namespace MissionSplat.Player
{

using UnityEngine;
using UnityEngine.UI;

internal static class Ui
{
    public static Font Font { get; } = LoadFont();

    public static Sprite Pixel { get; } = WhitePixel();

    public static Sprite Circle { get; } = CircleSprite(64);

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
    {
        var rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : Pixel;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Text Label(string name, Transform parent, int size, Color color, TextAnchor align)
    {
        var rect = Rect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    public static Button Button(string name, Transform parent, string caption, Color fill)
    {
        var image = Image(name, parent, fill);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        var nav = Navigation.defaultNavigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        var label = Label("Label", image.transform, 22, Color.white, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform);
        label.text = caption;
        return button;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Anchored(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void Clear(Transform parent, int keepLeading = 0)
    {
        for (var i = parent.childCount - 1; i >= keepLeading; i--)
        {
            var child = parent.GetChild(i).gameObject;
            // Destroy waits until end of frame and would still take a click. DestroyImmediate is edit-mode only.
            child.SetActive(false);
            Object.Destroy(child);
        }
    }

    private static Font LoadFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Helvetica", 16);
    }

    private static Sprite WhitePixel()
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite CircleSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = (size - 1) / 2f;
        var radius = center;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var inside = (dx * dx) + (dy * dy) <= radius * radius;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
}
