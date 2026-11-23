using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class CelebracionSetup
{
    const string ScenePath = "Assets/Scenes/S4_Felicidades.unity";

    [MenuItem("Tools/Simulation/Create celebration scene %#F2")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (System.IO.File.Exists(ScenePath))
        {
            RegistrarEnBuildSettings();
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.22f, 0.34f);
        RenderSettings.fog = false;

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.transform.position = new Vector3(3.2f, 2.2f, 4.4f);
        cam.transform.LookAt(new Vector3(0f, 0.95f, 0.15f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.045f, 0.11f);
        cam.fieldOfView = 42f;

        var keyGo = new GameObject("Luz Principal", typeof(Light));
        var key = keyGo.GetComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.1f;
        key.color = new Color(0.85f, 0.92f, 1f);
        key.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        var cannonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cannon.prefab");
        var cannonInst = (GameObject)PrefabUtility.InstantiatePrefab(cannonPrefab);
        cannonInst.name = "Cannon";
        cannonInst.transform.position = new Vector3(0f, 0.25f, 0f);
        cannonInst.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

        var controller = new GameObject("Celebracion de victoria").AddComponent<CelebracionVictoria>();
        controller.segundos = 10f;
        controller.escenaMenu = "S1_Inicio";
        controller.canonObjeto = cannonInst.transform;
        controller.cannonPrefab = cannonPrefab;
        controller.fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/MedievalTimes-AL7l6 SDF.asset");
        controller.materialBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/TutorialTargets/Ring0.mat");
        controller.sonidoVictoria = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI/Sonido_Final_2.mp3");

        EditorSceneManager.SaveScene(scene, ScenePath);
        RegistrarEnBuildSettings();
        Debug.Log("[Celebracion] Felicidades guardada y registrada en Build Settings.");
    }

    static void RegistrarEnBuildSettings()
    {
        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
    }
}
