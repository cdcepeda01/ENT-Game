using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Aplica el diseño a los textos existentes; conserva las referencias de la partida.</summary>
public static class CannonHudLayout
{
    [MenuItem("Tools/Simulation/Style cannon HUD %#F11")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        var game = Object.FindAnyObjectByType<ControlPartida>();
        if (game == null || game.textoTiempo == null || game.textoPuntos == null) return;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/MedievalTimes-AL7l6 SDF.asset");
        Style(game.textoTiempo, "PanelTiempo", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -32), new Vector2(320, 144), font, TextAlignmentOptions.Center,
            "<size=26><color=#D7B77C>Tiempo restante</color></size>\n01:00");
        game.textoTiempo.fontSize = 60;
        game.textoTiempo.fontSizeMax = 60;
        var medieval = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/MedievalTimes-AL7l6 SDF.asset");
        Style(game.textoPuntos, "PanelPuntos", Vector2.zero, Vector2.zero,
            new Vector2(32, 32), new Vector2(360, 192), medieval, TextAlignmentOptions.MidlineLeft,
            "<color=#A51D2D><size=28>Puntos</size>\n0</color>");
        game.textoPuntos.fontSize = 100;
        game.textoPuntos.fontSizeMax = 100;
        var scorePanel = game.textoPuntos.transform.parent;
        scorePanel.GetComponent<Image>().enabled = false;
        scorePanel.Find("Acento").gameObject.SetActive(false);
        var shadow = game.textoPuntos.GetComponent<Shadow>();
        if (shadow == null) shadow = game.textoPuntos.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.025f, 0.015f, 0.9f);
        shadow.effectDistance = new Vector2(2, -2);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        Debug.Log("[HUD] Tiempo centrado arriba; puntos abajo a la izquierda. Escena guardada.");
    }

    static void Style(TMP_Text text, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, TMP_FontAsset font, TextAlignmentOptions alignment, string preview)
    {
        var canvas = text.GetComponentInParent<Canvas>();
        var panel = canvas.transform.Find(name) as RectTransform;
        if (panel == null)
        {
            panel = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Crear panel HUD");
            panel.SetParent(canvas.transform, false);
        }
        panel.anchorMin = panel.anchorMax = anchor;
        panel.pivot = pivot;
        panel.anchoredPosition = position;
        panel.sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(0.045f, 0.065f, 0.085f, 0.88f);
        panel.GetComponent<Image>().raycastTarget = false;
        if (panel.Find("Acento") == null)
        {
            var accent = new GameObject("Acento", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(panel, false);
            var rect = (RectTransform)accent.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 3);
            rect.anchoredPosition = Vector2.zero;
            accent.GetComponent<Image>().color = new Color(0.84f, 0.72f, 0.49f);
            accent.GetComponent<Image>().raycastTarget = false;
        }
        Undo.RecordObject(text, "Estilo de texto HUD");
        Undo.SetTransformParent(text.transform, panel, "Posición del HUD");
        var t = text.rectTransform;
        t.anchorMin = Vector2.zero;
        t.anchorMax = Vector2.one;
        t.offsetMin = new Vector2(22, 10);
        t.offsetMax = new Vector2(-22, -10);
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.fontSize = 42;
        text.enableAutoSizing = true;
        text.fontSizeMin = 24;
        text.fontSizeMax = 42;
        text.alignment = alignment;
        text.color = new Color(1, 0.96f, 0.87f);
        text.richText = true;
        text.raycastTarget = false;
        text.text = preview;
        EditorUtility.SetDirty(text);
    }
}
