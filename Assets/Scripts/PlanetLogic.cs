using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class PlanetLogic : MonoBehaviour
{
    public PlanetGeneratorData planetGeneratorData;

    void Start()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        var planet = PlanetGenerator.generate(planetGeneratorData);
        var comp = GetComponent<MeshFilter>();
        comp.mesh = PlanetRenderer.createMesh(planet);

        sw.Stop();
        UnityEngine.Debug.Log("Elapsed " + sw.Elapsed);
    }
}
