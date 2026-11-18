using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CastilloWinterLook
{
    const string ProfilePath = "Assets/Settings/VolumeProfiles/CastilloWinter.asset";
    [MenuItem("Tools/Simulation/Apply winter atmosphere")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.name != "Level_Castillo") return;
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        var fog = Component<OasisFogVolumeComponent>(profile);
        fog.Tint.Override(new Color(0.38f, 0.61f, 0.82f));
        fog.Density.Override(0.004f);
        fog.StartDistance.Override(25f);
        fog.HeightRange.Override(new Vector2(-5, 35));
        fog.SunScatteringIntensity.Override(0.35f);
        var balance = Component<WhiteBalance>(profile);
        balance.temperature.Override(-14f);
        balance.tint.Override(-3f);
        var colors = Component<ColorAdjustments>(profile);
        colors.postExposure.Override(-0.1f);
        colors.contrast.Override(8f);
        colors.saturation.Override(-8f);
        colors.colorFilter.Override(new Color(0.94f, 0.98f, 1f));
        var bloom = Component<Bloom>(profile);
        bloom.intensity.Override(0.12f);
        bloom.threshold.Override(1.15f);
        Component<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        foreach (var c in profile.components) EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(profile);

        var root = GameObject.Find("Castillo Winter Atmosphere");
        if (root == null) root = new GameObject("Castillo Winter Atmosphere", typeof(Volume));
        var volume = root.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100;
        volume.weight = 1;
        volume.sharedProfile = profile;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.48f, 0.65f, 0.79f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 35;
        RenderSettings.fogEndDistance = 320;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.38f, 0.52f, 0.70f);
        RenderSettings.ambientEquatorColor = new Color(0.49f, 0.60f, 0.72f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.21f, 0.29f);
        foreach (var light in Object.FindObjectsByType<Light>())
        {
            if (light.gameObject.scene != scene || light.type != LightType.Directional) continue;
            Undo.RecordObject(light, "Luz invernal");
            light.color = new Color(0.80f, 0.88f, 1f);
            light.useColorTemperature = false;
            light.intensity = 1.1f;
            EditorUtility.SetDirty(light);
        }
        foreach (var camera in Object.FindObjectsByType<Camera>())
        {
            if (camera.gameObject.scene != scene) continue;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.volumeLayerMask |= 1;
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Winter] Bruma azul, luz fría y perfil propio guardados en Level_Castillo.");
    }

    static T Component<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var component)) return component;
        component = profile.Add<T>(false);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }
}
