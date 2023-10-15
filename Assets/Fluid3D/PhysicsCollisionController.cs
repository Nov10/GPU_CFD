using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;


public class PhysicsCollisionController : MonoBehaviour
{
    public void OnEnter(int x, int y, int z)
    {
        Solver.SetObstacle(x, y, z, 0);
    }
    public void OnStay(int x, int y, int z)
    {
        //Solver.SetObstacle(x, y, z, 0);
        //Solver.FluidCubeSetDensity(Solver.Cube, x, y, z, 0);
    }
    public void OnExit(int x, int y, int z)
    {
        Solver.SetObstacle(x, y, z, 1);
    }
    private void Start()
    {

    }
    Fluid3DSolver Solver;
    public CFDCollider[,,] Instances;
    public void Init(int x, int y, int z, float dx, float dy, float dz, Fluid3DSolver solver)
    {
        Solver = solver;
        Instances = new CFDCollider[x, y, z];
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                for (int k = 0; k < z; k++)
                {
                    CFDCollider instance = Instantiate(Prefab, new Vector3((float)(i * dx), (float)(j * dy), (float)(k * dz)), Quaternion.identity);
                    instance.Initialize(this, i, j, k);
                    instance.transform.localScale = Vector3.one * dx;
                    instance.transform.position -= (Vector3.one * dx * x) / 2f;
                    instance.transform.SetParent(transform);
                    Instances[i, j, k] = instance;
                }
            }
        }
    }
    public CFDCollider Prefab;
}
