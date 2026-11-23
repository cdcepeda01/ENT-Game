using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CastilloSnowSetup
{
    [MenuItem("Tools/Simulation/Add gentle snow and blue cannon %#F7")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        var cannon = Object.FindAnyObjectByType<CannonController>();
        if (cannon == null || cannon.gameObject.scene.name != "S3_JuegoPrincipal") return;
        const string steelPath = "Assets/UI/CannonWinter.mat";
        var steel = AssetDatabase.LoadAssetAtPath<Material>(steelPath);
        if (steel == null)
        {
            steel = new Material(AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Samples/Cannon Mini Pack/Materials/Small_Cannon.mat"));
            AssetDatabase.CreateAsset(steel, steelPath);
        }
        steel.shader = Shader.Find("ENT/Winter Cannon Lit");
        steel.SetFloat("_Smoothness", 0.65f);
        steel.SetFloat("_EnvironmentReflections", 0);
        steel.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        steel.SetShaderPassEnabled("GBuffer", false);
        EditorUtility.SetDirty(steel);
        foreach (var renderer in cannon.GetComponentsInChildren<MeshRenderer>(true))
        {
            Undo.RecordObject(renderer, "Acero azul del cañón");
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = steel;
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        const string snowPath = "Assets/UI/GentleSnow.mat";
        var snowMaterial = AssetDatabase.LoadAssetAtPath<Material>(snowPath);
        if (snowMaterial == null)
        {
            snowMaterial = new Material(Shader.Find("ENT/Soft Snow"));
            AssetDatabase.CreateAsset(snowMaterial, snowPath);
        }
        var root = GameObject.Find("Gentle Winter Snow");
        if (root == null) root = new GameObject("Gentle Winter Snow", typeof(ParticleSystem));
        root.transform.position = cannon.transform.position + cannon.transform.forward * 32 + Vector3.up * 17;
        root.transform.rotation = cannon.transform.rotation;
        var snow = root.GetComponent<ParticleSystem>();
        snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = snow.main;
        main.loop = true;
        main.duration = 12;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9, 12);
        main.startSpeed = 0;
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        main.startColor = new Color(0.86f, 0.94f, 1, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1600;
        main.playOnAwake = true;
        var shape = snow.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(75, 1, 52);
        var emission = snow.emission;
        emission.rateOverTime = 100;
        var velocity = snow.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.8f, -1.3f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        var fade = snow.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
        fade.color = gradient;
        var rendererSnow = root.GetComponent<ParticleSystemRenderer>();
        rendererSnow.sharedMaterial = snowMaterial;
        rendererSnow.renderMode = ParticleSystemRenderMode.Billboard;
        rendererSnow.shadowCastingMode = ShadowCastingMode.Off;
        rendererSnow.receiveShadows = false;
        rendererSnow.maxParticleSize = 0.008f;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(cannon.gameObject.scene);
        EditorSceneManager.SaveScene(cannon.gameObject.scene);
        Debug.Log("[Winter] Nevada suave y material azul del cañón guardados.");
    }
}
