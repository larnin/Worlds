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

        if(m_planet != null)
        {
            for (int i = 0; i < m_planet.rivers.Count; i++)
                for (int j = 0; j < m_planet.rivers[i].Length - 1; j++)
                {
                    int p1 = m_planet.rivers[i][j].index;
                    int p2 = m_planet.rivers[i][j + 1].index;
                    UnityEngine.Debug.DrawLine(m_planet.points[p1].point * (m_planet.points[p1].height + 1.01f) * m_planet.scale
                                             , m_planet.points[p2].point * (m_planet.points[p2].height + 1.01f) * m_planet.scale, Color.red);
                }
        }
    }

    void generate()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        //planetGeneratorData.seed = seed++;
        planetGeneratorData.sphereDivisionLevel = seed++;
        var planet = PlanetGenerator.generate(planetGeneratorData);
        var comp = GetComponent<MeshFilter>();
        comp.mesh = PlanetRenderer.createMesh(planet);
        m_planet = planet;

        sw.Stop();
        UnityEngine.Debug.Log("Elapsed total " + sw.Elapsed);
    }
}
