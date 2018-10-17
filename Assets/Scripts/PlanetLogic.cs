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

        if (m_planet != null)
        {
            for(int i = 0; i < m_planet.points.Length; i++)
            {
                if (!m_planet.points[i].riverInfo.isRiver())
                    continue;
                int p = m_planet.points[i].riverInfo.nextIndex;
                if (p < 0)
                    continue;
                UnityEngine.Debug.DrawLine(m_planet.points[i].point * (m_planet.points[i].height + 1.001f) * m_planet.scale
                                             , m_planet.points[p].point * (m_planet.points[p].height + 1.001f) * m_planet.scale, Color.red);
            }
        }
    }

    void generate()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        //planetGeneratorData.seed = seed++;
        //planetGeneratorData.sphereDivisionLevel = seed++;
        var planet = PlanetGenerator.generate(planetGeneratorData);
        var comp = GetComponent<MeshFilter>();
        comp.mesh = PlanetRenderer.createMesh(planet);
        m_planet = planet;

        sw.Stop();
        UnityEngine.Debug.Log("Elapsed total " + sw.Elapsed);
    }
}
