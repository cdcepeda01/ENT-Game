using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

public class CelebracionTests
{
    [Test]
    public void DebeCelebrarSoloAlSuperar100PuntosEnJuegoPrincipal()
    {
        Assert.That(ControlPartida.DebeCelebrar("S3_JuegoPrincipal", 0), Is.False);
        Assert.That(ControlPartida.DebeCelebrar("S3_JuegoPrincipal", 100), Is.False);
        Assert.That(ControlPartida.DebeCelebrar("S3_JuegoPrincipal", 101), Is.True);
        Assert.That(ControlPartida.DebeCelebrar("S3_JuegoPrincipal", 110), Is.True);
        Assert.That(ControlPartida.DebeCelebrar("S1_Inicio", 150), Is.False);
        Assert.That(ControlPartida.DebeCelebrar("S2_Tutorial", 200), Is.False);
    }

    [Test]
    public void EscenasPrincipalesEstanOrdenadasEnBuildSettings()
    {
        string[] esperadas =
        {
            "Assets/Scenes/S1_Inicio.unity",
            "Assets/Scenes/S2_Tutorial.unity",
            "Assets/Scenes/S3_JuegoPrincipal.unity",
            "Assets/Scenes/S4_Felicidades.unity",
        };

        foreach (string path in esperadas)
            Assert.That(File.Exists(path), Is.True, $"Debe existir {path}");

        string[] activas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Assert.That(activas, Is.EqualTo(esperadas), "Las 4 escenas deben estar en orden S1 -> S2 -> S3 -> S4 en Build Settings.");
    }
}
