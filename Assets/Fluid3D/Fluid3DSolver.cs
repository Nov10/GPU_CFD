using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Fluid3DSolver : MonoBehaviour
{
    public ComputeShader FluidShader;
    public Material ResultMaterial;
    public int Size = 256;
    public Transform Sphere;
    public int Iterations = 10;

    [Header("Force Settings")]
    public float forceIntensity = 200f;
    public float forceRange = 0.01f;
    public float Diffusion;
    public float Viscosity;
    public float DeltaSpace;
    private Vector3 preSprerePosition = Vector3.zero;
    public UnityEngine.Color DyeColor = UnityEngine.Color.white;

    ComputeBuffer Obstacles;
    int[] obsints;
    //private int[] Obstacles;
    private RenderTexture VelocityTex;
    private RenderTexture DensityTex;
    private RenderTexture PressureTex;
    private RenderTexture DivergenceTex;

    private int dispatchSize = 0;
    private int kernel_Init = 0;
    private int kernel_Diffusion = 0;
    private int kernel_UserInput = 0;
    private int kernel_Jacobi = 0;
    private int kernel_Advection = 0;
    private int kernel_Divergence = 0;
    private int kernel_SubtractGradient = 0;

    private RenderTexture CreateTexture(GraphicsFormat format)
    {
        RenderTexture dataTex = new RenderTexture(Size, Size, format, 0);
        dataTex.volumeDepth = Size;
        dataTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        dataTex.filterMode = FilterMode.Bilinear;
        dataTex.wrapMode = TextureWrapMode.Clamp;
        dataTex.enableRandomWrite = true;
        dataTex.Create();

        return dataTex;
    }

    private void DispatchCompute(int kernel)
    {
        FluidShader.Dispatch(kernel, dispatchSize, dispatchSize, dispatchSize);
    }

    public void SetObstacle(int x, int y, int z, int value)
    {
        obsints[x + y * Size + z * Size * Size] = value;
    }

    [SerializeField] PhysicsCollisionController PhysicsController;
    [SerializeField] float Distance;
    [SerializeField] float ElapsedTime;
    [SerializeField] float ET2;
    void Start()
    {
        //PhysicsController.Init(Size, Size, Size, Distance, Distance, Distance, this);

        //Create textures
        VelocityTex = CreateTexture(GraphicsFormat.R16G16B16A16_SFloat); //float3 velocity , float unused
        DensityTex = CreateTexture(GraphicsFormat.R16G16B16A16_SFloat); //float3 color , float density
        PressureTex = CreateTexture(GraphicsFormat.R16_SFloat); //float pressure
        DivergenceTex = CreateTexture(GraphicsFormat.R16_SFloat); //float divergence

        obsints = new int[Size * Size * Size];
        for (int i = 0; i < obsints.Length; i++)
        {
            obsints[i] = 1;
        }
        Obstacles = new ComputeBuffer(Size * Size * Size, sizeof(int));
        Obstacles.SetData(obsints);

        //Output
        ResultMaterial.SetTexture("_MainTex", DensityTex);

        FluidShader.SetFloat("Diffusion", Diffusion);
        FluidShader.SetFloat("DeltaSpace", DeltaSpace);
        //Set shared variables for compute shader
        FluidShader.SetInt("size", Size);
        FluidShader.SetFloat("forceIntensity", forceIntensity);
        FluidShader.SetFloat("forceRange", forceRange);

        //Set texture for compute shader
        /* 
		This example is not optimized, some textures are readonly, 
		but I keep it like this for the sake of convenience
		*/
        kernel_Init = FluidShader.FindKernel("Initialize");
        FluidShader.SetTexture(kernel_Init, "VelocityTex", VelocityTex);
        FluidShader.SetTexture(kernel_Init, "DensityTex", DensityTex);
        FluidShader.SetTexture(kernel_Init, "PressureTex", PressureTex);
        FluidShader.SetTexture(kernel_Init, "DivergenceTex", DivergenceTex);
        //FluidShader.SetBuffer(kernel_Init, "Obstacles", Obstacles);

        kernel_Diffusion = FluidShader.FindKernel("Diffuse");
        FluidShader.SetTexture(kernel_Diffusion, "DensityTex", DensityTex);
        FluidShader.SetTexture(kernel_Diffusion, "VelocityTex", VelocityTex);
        //FluidShader.SetBuffer(kernel_Diffusion, "Obstacles", Obstacles);

        kernel_Advection = FluidShader.FindKernel("Advect");
        FluidShader.SetTexture(kernel_Advection, "VelocityTex", VelocityTex);
        FluidShader.SetTexture(kernel_Advection, "DensityTex", DensityTex);
        //FluidShader.SetBuffer(kernel_Advection, "Obstacles", Obstacles);

        kernel_UserInput = FluidShader.FindKernel("UserInput");
        FluidShader.SetTexture(kernel_UserInput, "VelocityTex", VelocityTex);
        FluidShader.SetTexture(kernel_UserInput, "DensityTex", DensityTex);
        //FluidShader.SetBuffer(kernel_UserInput, "Obstacles", Obstacles);

        kernel_Divergence = FluidShader.FindKernel("Divergence");
        FluidShader.SetTexture(kernel_Divergence, "VelocityTex", VelocityTex);
        FluidShader.SetTexture(kernel_Divergence, "DivergenceTex", DivergenceTex);
        //FluidShader.SetBuffer(kernel_Divergence, "Obstacles", Obstacles);

        kernel_Jacobi = FluidShader.FindKernel("Jacobi");
        FluidShader.SetTexture(kernel_Jacobi, "DivergenceTex", DivergenceTex);
        FluidShader.SetTexture(kernel_Jacobi, "PressureTex", PressureTex);
        //FluidShader.SetBuffer(kernel_Jacobi, "Obstacles", Obstacles);

        kernel_SubtractGradient = FluidShader.FindKernel("SubtractGradient");
        FluidShader.SetTexture(kernel_SubtractGradient, "PressureTex", PressureTex);
        FluidShader.SetTexture(kernel_SubtractGradient, "VelocityTex", VelocityTex);
        //FluidShader.SetBuffer(kernel_SubtractGradient, "Obstacles", Obstacles);

        //Init data texture value
        dispatchSize = Mathf.CeilToInt(Size / 8);
        DispatchCompute(kernel_Init);
    }
    private void Update()
    {
        ElapsedTime += Time.deltaTime;
        if (Input.GetKey(KeyCode.C))
        {
            UnityEditor.EditorApplication.isPaused = true;
        }
    }
    public float timeSpeed = 10;
    public float T;
    public Vector3 Vel;
    void FixedUpdate()
    {
        //Send sphere (mouse) position
        //Sphere.position = new Vector3(Sphere.position.x, Mathf.Sin(Time.time * 2) - 2, Sphere.position.z);
        //forceIntensity = Mathf.Max(0, Mathf.Sin(Time.time * 2));
        Vector3 npos = new Vector3(Sphere.position.x / transform.lossyScale.x, Sphere.position.y / transform.lossyScale.y, Sphere.position.z / transform.lossyScale.z);
        FluidShader.SetVector("spherePos", npos);

        DyeColor = UnityEngine.Color.HSVToRGB(0.5f * (Mathf.Sin(Time.time * Time.fixedDeltaTime * timeSpeed) + 1f), 1f, 1f);
        //Send sphere (mouse) velocity
        Vector3 velocity = npos - preSprerePosition;
        FluidShader.SetVector("sphereVelocity", velocity);
        FluidShader.SetFloat("_deltaTime", Time.fixedDeltaTime * 1);
        FluidShader.SetVector("dyeColor", DyeColor);
        FluidShader.SetFloat("forceRange", forceRange);
        FluidShader.SetFloat("forceIntensity", forceIntensity);
        FluidShader.SetFloat("Diffusion", Diffusion);
        FluidShader.SetFloat("Viscosity", Viscosity);
        // FluidShader.SetInts("Obstacles", obsints);

        //Obstacles.SetData(obsints);
        //FluidShader.SetBuffer(kernel_Init, "Obstacles", Obstacles);
        //FluidShader.SetBuffer(kernel_Diffusion, "Obstacles", Obstacles);
        //FluidShader.SetBuffer(kernel_Advection, "Obstacles", Obstacles);
        //FluidShader.SetBuffer(kernel_UserInput, "Obstacles", Obstacles);
        //FluidShader.SetBuffer(kernel_Divergence, "Obstacles", Obstacles);
        //FluidShader.SetBuffer(kernel_Jacobi, "Obstacles", Obstacles);
        //FluidShader.SetBuffer(kernel_SubtractGradient, "Obstacles", Obstacles);


        //Run compute shader
        DispatchCompute(kernel_Diffusion);
        DispatchCompute(kernel_Advection);
        DispatchCompute(kernel_UserInput);
        DispatchCompute(kernel_Divergence);
        for (int i = 0; i < Iterations; i++)
        {
            DispatchCompute(kernel_Jacobi);
        }
        DispatchCompute(kernel_SubtractGradient);

        //Save the previous position for velocity
        preSprerePosition = npos;
        ET2 += Time.fixedDeltaTime;
    }
}
