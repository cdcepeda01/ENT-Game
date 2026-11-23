using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Escena de celebración tras superar los 100 puntos en S3_JuegoPrincipal.</summary>
public class CelebracionVictoria : MonoBehaviour
{
    [Header("Escena y temporizador")]
    [Min(1f)] public float segundos = 10f;
    public string escenaMenu = "S1_Inicio";

    [Header("Referencias de escena (opcionales, se crean si faltan)")]
    public Transform canonObjeto;
    public Transform grupoLuces;
    public Light[] lucesColores;
    public TMP_Text textoTitulo;
    public TMP_Text textoCuenta;

    [Header("Recursos")]
    public GameObject cannonPrefab;
    public TMP_FontAsset fuente;
    public Material materialBase;
    public AudioClip sonidoVictoria;

    static readonly Color[] PaletaFiesta =
    {
        new Color(1f, 0.18f, 0.48f),  // Magenta / rojo festivo
        new Color(0.12f, 0.78f, 1f),  // Cian brillante
        new Color(1f, 0.82f, 0.14f),  // Dorado
        new Color(0.28f, 1f, 0.58f),  // Verde esmeralda
        new Color(0.72f, 0.32f, 1f),  // Púrpura
        new Color(1f, 0.45f, 0.12f),  // Naranja cálido
    };

    readonly List<Object> recursos = new List<Object>();
    Vector3 escalaTituloBase = Vector3.one;
    float inicio;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        inicio = Time.unscaledTime;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.22f, 0.34f);
        RenderSettings.fog = false;

        AsegurarCamara();
        AsegurarEscenarioYCanon();
        AsegurarLucesDeColores();
        CrearSerpentinas();
        AsegurarInterfaz();
        ReproducirSonidoVictoria();

        if (textoTitulo != null) escalaTituloBase = textoTitulo.rectTransform.localScale;
        ActualizarCuenta();
        StartCoroutine(Regresar());
    }

    void AsegurarCamara()
    {
        if (Camera.main != null) return;
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(3.2f, 2.2f, 4.4f);
        cam.transform.LookAt(new Vector3(0f, 0.95f, 0.15f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.045f, 0.11f);
        cam.fieldOfView = 42f;
    }

    void AsegurarEscenarioYCanon()
    {
        if (GameObject.Find("Podio") == null)
        {
            var podium = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            podium.name = "Podio";
            podium.transform.position = new Vector3(0f, 0.125f, 0f);
            podium.transform.localScale = new Vector3(4.8f, 0.125f, 4.8f);
            podium.GetComponent<Renderer>().sharedMaterial = CrearMaterial(new Color(0.09f, 0.12f, 0.22f), 0.45f, Color.black);
        }

        if (canonObjeto == null && cannonPrefab != null)
        {
            var inst = Instantiate(cannonPrefab, new Vector3(0f, 0.25f, 0f), Quaternion.Euler(0f, 35f, 0f));
            inst.name = "Cannon";
            canonObjeto = inst.transform;
        }

        if (canonObjeto != null)
        {
            var renderers = canonObjeto.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                canonObjeto.position += Vector3.up * (0.25f - bounds.min.y);
            }
        }
    }

    void AsegurarLucesDeColores()
    {
        if (grupoLuces == null)
        {
            var existente = GameObject.Find("Luces de Colores");
            grupoLuces = existente != null ? existente.transform : new GameObject("Luces de Colores").transform;
        }

        if (lucesColores == null || lucesColores.Length == 0)
            lucesColores = grupoLuces.GetComponentsInChildren<Light>(true);

        if (lucesColores == null || lucesColores.Length == 0)
        {
            lucesColores = new Light[PaletaFiesta.Length];
            float radio = 2.7f;
            for (int i = 0; i < PaletaFiesta.Length; i++)
            {
                float ang = i * Mathf.PI * 2f / PaletaFiesta.Length;
                var go = new GameObject($"Luz Color {i + 1}", typeof(Light));
                go.transform.SetParent(grupoLuces, false);
                go.transform.localPosition = new Vector3(Mathf.Cos(ang) * radio, i % 2 == 0 ? 2.1f : 1.35f, Mathf.Sin(ang) * radio);
                var light = go.GetComponent<Light>();
                light.type = LightType.Point;
                light.range = 12f;
                light.intensity = 22f;
                light.color = PaletaFiesta[i];
                lucesColores[i] = light;
            }
        }

        // Pequeñas esferas luminosas en cada foco de color para dar brillo festivo visible.
        for (int i = 0; i < lucesColores.Length; i++)
        {
            var light = lucesColores[i];
            if (light == null || light.transform.Find("Foco") != null) continue;
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Foco";
            Destroy(orb.GetComponent<Collider>());
            orb.transform.SetParent(light.transform, false);
            orb.transform.localScale = Vector3.one * 0.16f;
            var r = orb.GetComponent<Renderer>();
            r.sharedMaterial = CrearMaterial(light.color, 0.8f, light.color * 2.4f);
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    void CrearSerpentinas()
    {
        var contenedor = new GameObject("Serpentinas").transform;
        var malla = CrearMallaSerpentina();
        recursos.Add(malla);

        for (int i = 0; i < PaletaFiesta.Length; i++)
        {
            var ps = new GameObject($"Serpentinas_{i + 1}", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            ps.transform.SetParent(contenedor, false);
            ps.transform.position = new Vector3(0f, 6.5f, 0f);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.duration = 6f;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.1f);
            main.startColor = PaletaFiesta[i];
            main.maxParticles = 90;
            main.gravityModifier = 0.14f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.rateOverTime = 14f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 0.4f, 8f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
            rot.y = new ParticleSystem.MinMaxCurve(-3.2f, 3.2f);
            rot.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.35f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = malla;
            var mat = CrearMaterial(PaletaFiesta[i], 0.55f, PaletaFiesta[i] * 0.45f, dobleCara: true);
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
        }
    }

    static Mesh CrearMallaSerpentina()
    {
        const int segmentos = 10;
        var mesh = new Mesh { name = "Cinta de serpentina" };
        var vertices = new Vector3[(segmentos + 1) * 2];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[segmentos * 6];

        for (int j = 0; j <= segmentos; j++)
        {
            float t = (float)j / segmentos;
            float y = (t - 0.5f) * 0.85f;
            float ondaX = Mathf.Sin(t * Mathf.PI * 3f) * 0.09f;
            float ondaZ = Mathf.Cos(t * Mathf.PI * 3f) * 0.05f;
            vertices[j * 2] = new Vector3(ondaX - 0.04f, y, ondaZ);
            vertices[j * 2 + 1] = new Vector3(ondaX + 0.04f, y, ondaZ);
            uvs[j * 2] = new Vector2(0f, t);
            uvs[j * 2 + 1] = new Vector2(1f, t);

            if (j == segmentos) continue;
            int idx = j * 6;
            int v = j * 2;
            triangles[idx] = v;
            triangles[idx + 1] = v + 2;
            triangles[idx + 2] = v + 1;
            triangles[idx + 3] = v + 1;
            triangles[idx + 4] = v + 2;
            triangles[idx + 5] = v + 3;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Material CrearMaterial(Color color, float smoothness, Color emision, bool dobleCara = false)
    {
        Material m;
        if (materialBase != null)
        {
            m = new Material(materialBase);
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
        }

        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        m.SetFloat("_Smoothness", smoothness);
        if (dobleCara) m.SetFloat("_Cull", 0f);
        if (emision.maxColorComponent > 0.001f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision);
        }
        recursos.Add(m);
        return m;
    }

    void AsegurarInterfaz()
    {
        if (textoTitulo != null && textoCuenta != null) return;

        var canvasGo = new GameObject("Celebracion UI", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (textoTitulo == null)
            textoTitulo = CrearTexto(canvas.transform, "¡Felicidades!", 112, new Vector2(0f, -70f), arriba: true, new Color(1f, 0.86f, 0.32f));
        CrearTexto(canvas.transform, "¡Has superado los 100 puntos!", 42, new Vector2(0f, -200f), arriba: true, new Color(1f, 0.96f, 0.86f));
        if (textoCuenta == null)
            textoCuenta = CrearTexto(canvas.transform, "", 34, new Vector2(0f, 65f), arriba: false, new Color(0.9f, 0.95f, 1f));
    }

    TMP_Text CrearTexto(Transform parent, string contenido, int tamano, Vector2 posicion, bool arriba, Color color)
    {
        var text = new GameObject(contenido, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        if (fuente != null) text.font = fuente;
        text.text = contenido;
        text.fontSize = tamano;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, arriba ? 1f : 0f);
        text.rectTransform.pivot = new Vector2(0.5f, arriba ? 1f : 0f);
        text.rectTransform.sizeDelta = new Vector2(1600f, 150f);
        text.rectTransform.anchoredPosition = posicion;
        return text;
    }

    void ReproducirSonidoVictoria()
    {
        if (sonidoVictoria == null) return;
        var audio = GetComponent<AudioSource>();
        if (audio == null) audio = gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        audio.PlayOneShot(sonidoVictoria, 0.85f);
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (grupoLuces != null)
            grupoLuces.Rotate(Vector3.up, 20f * Time.unscaledDeltaTime, Space.World);

        if (lucesColores != null)
        {
            for (int i = 0; i < lucesColores.Length; i++)
            {
                if (lucesColores[i] == null) continue;
                lucesColores[i].intensity = 22f + Mathf.Sin(t * 2.4f + i * 1.05f) * 8f;
            }
        }

        if (textoTitulo != null)
        {
            float pulso = 1f + Mathf.Sin(t * 2.6f) * 0.035f;
            textoTitulo.rectTransform.localScale = escalaTituloBase * pulso;
        }

        ActualizarCuenta();
    }

    void ActualizarCuenta()
    {
        if (textoCuenta == null) return;
        int restantes = Mathf.CeilToInt(Mathf.Max(0f, segundos - (Time.unscaledTime - inicio)));
        textoCuenta.text = $"Volviendo al menú en {restantes} s";
    }

    IEnumerator Regresar()
    {
        yield return new WaitForSecondsRealtime(segundos);
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.IrAlMenu();
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(escenaMenu);
        }
    }

    void OnDestroy()
    {
        foreach (var recurso in recursos)
            if (recurso != null) Destroy(recurso);
    }
}
