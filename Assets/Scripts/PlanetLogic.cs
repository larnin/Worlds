using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class PlanetLogic : MonoBehaviour
{
    public PlanetGeneratorData planetGeneratorData;
    PlanetData m_planet;
    int seed = 4;

    void Start()
    {
        generate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            generate();

        int nb = 0;
    }

    void generate()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        planetGeneratorData.seed = seed++;
        //planetGeneratorData.sphereDivisionLevel = seed++;
        var planet = PlanetGenerator.generate(planetGeneratorData);
        var compGround = GetComponent<MeshFilter>();
        compGround.mesh = PlanetRenderer.createSurfaceMesh(planet);
        var compWater = transform.Find("Water").GetComponent<MeshFilter>();
        compWater.mesh = PlanetRenderer.CreateWaterMesh(planet);
        var compRivers = transform.Find("Rivers").GetComponent<MeshFilter>();
        compRivers.mesh = PlanetRenderer.CreateRiversMesh(planet);
        m_planet = planet;

        sw.Stop();
        UnityEngine.Debug.Log("Elapsed total " + sw.Elapsed);
    }
}
