using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class TutorialTargetsSetup
{
    [MenuItem("Tools/Simulation/Install tutorial targets %#F5")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/S2_Tutorial.unity");
        var guide = Object.FindAnyObjectByType<TutorialGuiado>();
        var cannon = Object.FindAnyObjectByType<CannonController>();
        if (guide.dianasTutorial != null) { Adjust(guide, cannon); EditorSceneManager.SaveScene(scene); return; }
        const string folder = "Assets/Settings/TutorialTargets";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Settings", "TutorialTargets");
        Color[] colors = { new Color(.9f,.88f,.8f), new Color(.035f,.055f,.07f), new Color(.05f,.42f,.85f), new Color(.85f,.055f,.07f), new Color(1,.75f,.05f) };
        var materials = new Material[5];
        for (int i = 0; i < 5; i++)
        {
            materials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            materials[i].SetColor("_BaseColor", colors[i]);
            materials[i].SetFloat("_Smoothness", .15f);
            AssetDatabase.CreateAsset(materials[i], folder + "/Ring" + i + ".mat");
        }
        var wood = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wood.SetColor("_BaseColor", new Color(.25f,.12f,.055f));
        AssetDatabase.CreateAsset(wood, folder + "/Wood.mat");
        var group = new GameObject("DianasTutorial");
        var spawn = new SerializedObject(cannon).FindProperty("barrelSpawnLocation").objectReferenceValue as Transform;
        var forward = Vector3.ProjectOnPlane(cannon.transform.forward, Vector3.up).normalized;
        var right = Vector3.Cross(Vector3.up, forward);
        float[] distances = { 14, 20, 24 };
        float[] offsets = { -6, 5, -1 };
        for (int i = 0; i < 3; i++)
        {
            var target = new GameObject(i == 2 ? "Diana móvil horizontal" : "Diana fija " + (i + 1));
            target.transform.SetParent(group.transform);
            target.transform.position = spawn.position + forward * distances[i] + right * offsets[i] + Vector3.down * (i == 1 ? .5f : 1.5f);
            target.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            var body = target.AddComponent<Rigidbody>();
            body.isKinematic = true; body.useGravity = false;
            var logic = target.AddComponent<TutorialTarget>();
            logic.moving = i == 2;
            logic.hitSound = guide.sonidoPaso;
            for (int ring = 0; ring < 5; ring++)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Anillo " + ring;
                disc.transform.SetParent(target.transform, false);
                float diameter = 3.4f * (1 - ring * .19f);
                disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
                disc.transform.localScale = new Vector3(diameter, .07f, diameter);
                disc.transform.localPosition = new Vector3(0, 0, ring * .025f);
                disc.GetComponent<Renderer>().sharedMaterial = materials[ring];
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                if (ring == 0)
                {
                    var mesh = disc.AddComponent<MeshCollider>();
                    mesh.sharedMesh = disc.GetComponent<MeshFilter>().sharedMesh;
                    mesh.convex = true;
                }
                if (ring == 4) logic.center = disc.GetComponent<Renderer>();
            }
            var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = "Soporte de madera";
            stand.transform.SetParent(target.transform, false);
            stand.transform.localPosition = new Vector3(0, -2, -.15f);
            stand.transform.localScale = new Vector3(.22f, 2, .22f);
            stand.GetComponent<Renderer>().sharedMaterial = wood;
            Object.DestroyImmediate(stand.GetComponent<Collider>());
            var label = new GameObject("Estado", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(target.transform, false);
            label.transform.localPosition = new Vector3(0, 2.2f, 0);
            label.transform.localRotation = Quaternion.Euler(0, 180, 0);
            label.rectTransform.sizeDelta = new Vector2(5, 1);
            label.fontSize = 6;
            label.alignment = TextAlignmentOptions.Center;
            label.text = i == 2 ? "03 · EN MOVIMIENTO" : "0" + (i + 1) + " · FIJA";
            logic.label = label;
        }
        guide.dianasTutorial = group;
        Adjust(guide, cannon);
        if (guide.cajasTutorial != null) guide.cajasTutorial.SetActive(false);
        guide.pasoInclinar = "Inclina el Barrel: W/S o joystick vertical (A1).";
        guide.pasoGirar = "Gira el cañón: A/D o joystick horizontal (A0).";
        guide.pasoDisparar = "Dispara: ESPACIO o botón físico de disparo.";
        guide.pasoObjetivo = "Acierta las 3 dianas. La tercera se mueve de izquierda a derecha. ¡Anticipa su movimiento!";
        EditorUtility.SetDirty(guide);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Tutorial] Tres dianas instaladas: dos fijas y una horizontal.");
    }

    static void Adjust(TutorialGuiado guide, CannonController cannon)
    {
        var spawn = new SerializedObject(cannon).FindProperty("barrelSpawnLocation").objectReferenceValue as Transform;
        var forward = Vector3.ProjectOnPlane(cannon.transform.forward, Vector3.up).normalized;
        var right = Vector3.Cross(Vector3.up, forward);
        float[] distances = { 14, 18, 24 };
        float[] offsets = { -4, 4, 0 };
        int i = 0;
        foreach (var target in guide.dianasTutorial.GetComponentsInChildren<TutorialTarget>(true))
        {
            target.transform.position = spawn.position + forward * distances[i] + right * offsets[i] + Vector3.down * 1.5f;
            target.label.transform.localPosition = new Vector3(0, -1.95f, .2f);
            target.label.fontSize = 4;
            i++;
        }
        EditorSceneManager.MarkSceneDirty(guide.gameObject.scene);
    }
}

