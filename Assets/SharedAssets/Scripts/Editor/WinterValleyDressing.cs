using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WinterValleyDressing
{
    const string Folder = "Assets/Settings/WinterValley";
    static Transform root, cannon;
    static Terrain terrain;
    static float ground;
    static System.Random random;

    [MenuItem("Tools/Simulation/Dress winter valley %#F6")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        var controller = Object.FindAnyObjectByType<CannonController>();
        if (controller == null || controller.gameObject.scene.name != "S3_JuegoPrincipal") return;
        if (GameObject.Find("Winter Valley Scenery") != null)
        {
            PineMesh("PineSnowCaps", true);
            AssetDatabase.SaveAssets();
            Debug.Log("[Valley] Nieve de las copas actualizada; se conservan las posiciones editadas.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Settings", "WinterValley");
        cannon = controller.transform;
        terrain = GameObject.Find("TerrenoNieve").GetComponent<Terrain>();
        ground = Object.FindAnyObjectByType<GeneradorEnemigos>().transform.position.y;
        root = new GameObject("Winter Valley Scenery").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Ambientar valle invernal");
        random = new System.Random(481);
        var needles = Material("PineNeedles", new Color(0.08f, 0.17f, 0.19f));
        var snow = Material("PineSnow", new Color(0.79f, 0.88f, 0.95f));
        var bark = Material("PineBark", new Color(0.21f, 0.19f, 0.17f));
        var foliageMesh = PineMesh("PineBranches", false);
        var snowMesh = PineMesh("PineSnowCaps", true);
        var clusters = new[] { new Vector2(-27, 48), new Vector2(29, 57), new Vector2(-38, 76),
            new Vector2(39, 89), new Vector2(-20, 107), new Vector2(14, 119), new Vector2(-5, 140) };
        foreach (var cluster in clusters)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = cluster.x + Range(-7, 7), z = cluster.y + Range(-6, 6);
                var tree = new GameObject("Abeto nevado").transform;
                tree.SetParent(root, false);
                tree.position = Position(x, z);
                tree.rotation = Quaternion.Euler(0, Range(0, 360), Range(-2, 2));
                float height = Range(6.5f, 11.5f);
                tree.localScale = new Vector3(height, height, height);
                MeshPart(tree, "Ramas", foliageMesh, needles);
                MeshPart(tree, "Nieve", snowMesh, snow);
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Tronco";
                trunk.transform.SetParent(tree, false);
                trunk.transform.localPosition = new Vector3(0, 0.3f, 0);
                trunk.transform.localScale = new Vector3(0.035f, 0.3f, 0.035f);
                Object.DestroyImmediate(trunk.GetComponent<Collider>());
                trunk.GetComponent<Renderer>().sharedMaterial = bark;
            }
        }
        // Roca baja en los laterales; la franja central de 40 m queda despejada.
        var rockGroups = new[] { new Vector2(-23, 27), new Vector2(25, 36), new Vector2(-29, 62),
            new Vector2(31, 73), new Vector2(-12, 95), new Vector2(19, 109) };
        foreach (var group in rockGroups)
            for (int i = 0; i < 4; i++)
                Place("Assets/Scenes/Oasis/Art/Environment/Prefabs/Rock_0" + (1 + i % 3) + "_Prefab.prefab",
                    "Rocas del valle", group.x + Range(-4, 4), group.y + Range(-4, 4), Range(1.1f, 3.3f), Range(0, 360));
        const string walls = "Assets/Loafbrr Asset/CastleWall_Kit/prefab/walls/walls/";
        Place(walls + "Wall_Door_Arch.prefab", "Puerta de la antigua fortificación", -30, 83, 7.5f, 165);
        Place(walls + "Wall_Wall.prefab", "Muralla izquierda", -37, 85, 4.2f, 165);
        Place(walls + "Wall_Window_Arch.prefab", "Muralla derruida", -23, 81, 3.4f, 165);
        Place(walls + "Wall_Door_Arch.prefab", "Ruina del paso oriental", 28, 108, 6.5f, 195);
        Place(walls + "Wall_Wall.prefab", "Fragmento de muralla", 34, 110, 3.5f, 195);
        Place("Assets/Loafbrr Asset/CastleWall_Kit/prefab/Pillar/Pillar.prefab", "Pilar antiguo", 23, 107, 4.7f, 195);
        Place("Assets/Prefabs/WoodenCrate_01.prefab", "Suministros abandonados", -23, 43, 1.2f, 25);
        Place("Assets/Prefabs/WoodenCrate_01.prefab", "Suministros abandonados", -25, 44, 0.9f, 5);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
        Debug.Log("[Valley] 35 abetos nevados, 24 rocas y ruinas laterales guardados; corredor central libre.");
    }

    static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    static Vector3 Position(float x, float z)
    {
        var p = cannon.position + cannon.right * x + cannon.forward * z;
        p.y = terrain != null ? terrain.SampleHeight(p) + terrain.transform.position.y : ground;
        return p;
    }
    static void Place(string path, string name, float x, float z, float height, float rotation)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new System.InvalidOperationException(path);
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        obj.name = name;
        obj.transform.position = Position(x, z);
        obj.transform.rotation = Quaternion.Euler(0, rotation, 0);
        var renderers = obj.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        obj.transform.localScale *= height / Mathf.Max(0.01f, bounds.size.y);
        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        obj.transform.position += Vector3.up * (Position(x, z).y - bounds.min.y - height * 0.08f);
        foreach (var collider in obj.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var t in obj.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
    }
    static Material Material(string name, Color color)
    {
        var path = Folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.18f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
    static void MeshPart(Transform parent, string name, Mesh mesh, Material material)
    {
        var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(parent, false);
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        GameObjectUtility.SetStaticEditorFlags(part, StaticEditorFlags.BatchingStatic);
    }
    static Mesh PineMesh(string name, bool snow)
    {
        string path = Folder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        const int segments = 14;
        for (int layer = 0; layer < 5; layer++)
        {
            float bottom = 0.15f + layer * 0.145f;
            float radius = 0.255f - layer * 0.043f;
            float top = Mathf.Min(1, bottom + 0.36f);
            if (snow) { bottom += 0.045f; radius *= 0.96f; top += 0.008f; }
            int start = vertices.Count;
            vertices.Add(new Vector3(0, top, 0));
            for (int j = 0; j < segments; j++)
            {
                float angle = j * Mathf.PI * 2 / segments + layer * 0.47f;
                float r = radius * (j % 2 == 0 ? 1 : 0.83f);
                vertices.Add(new Vector3(Mathf.Cos(angle) * r, bottom + (j % 2) * 0.018f, Mathf.Sin(angle) * r));
            }
            for (int j = 0; j < segments; j++)
            {
                triangles.Add(start); triangles.Add(start + 1 + (j + 1) % segments); triangles.Add(start + 1 + j);
            }
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }
}
