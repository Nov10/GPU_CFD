using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UAVTextureCS : MonoBehaviour
{
    public ComputeShader Shader;
    int Size = 128;
    int Kernel;
    public Material Material;
    // Start is called before the first frame update
    void Start()
    {
        Kernel = Shader.FindKernel("CSMain");

        RenderTexture texture = new RenderTexture(Size, Size, 0);
        texture.enableRandomWrite = true;
        texture.Create();

        Material.SetTexture("_MainTex", texture);
        Shader.SetTexture(Kernel, "Result", texture);
    }

    // Update is called once per frame
    void Update()
    {
        Shader.Dispatch(Kernel, Mathf.CeilToInt(Size / 8f), Mathf.CeilToInt(Size / 8f), 1);
    }
}
