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
    int X, Y, Z;
    public void Init(int x, int y, int z, float dx, float dy, float dz, Fluid3DSolver solver)
    {
        X = x;
        Y = y;
        Z = z;
        Solver = solver;
        Instances = new CFDCollider[x, y, z];
        pos = new Vector3[x, y, z];
        DX = dx;
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
                    pos[i,j,k] = instance.transform.position;
                    Instances[i, j, k] = instance;
                }
            }
        }
    }
    public bool DrawObstacle;
    Vector3[,,] pos;
    private void OnDrawGizmos()
    {
        if ((DrawObstacle) == false)
        {
            return;
        }
        Vector3 p;
        Vector3 s = Vector3.one * DX;
        Gizmos.color = new Color(1, 1, 1, 0.2f);
        for (int i = 0; i < X; i++)
        {
            for (int j = 0; j < Y; j++)
            {
                for (int k = 0; k < Z; k++)
                {
                    if (Instances[i, j, k].IsOn == false)
                        continue;
                    p = pos[i, j, k];
                    Gizmos.DrawWireCube(p, s);
                }
            }
        }
    }
    float DX;
    public CFDCollider Prefab;
}
