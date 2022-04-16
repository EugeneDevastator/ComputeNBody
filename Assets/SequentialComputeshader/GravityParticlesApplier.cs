using System;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class GravityParticlesApplier : MonoBehaviour
{
    /*
     //good preset
     private const float SpaceExpansion = 0.005f;
    private const float DeltaTime = 0.00000002f;
    
        private const float SpaceExpansion = 0.0002f;
    private const float DeltaTime = 0.00000004f;
    private const float MassMul = 1.1f;
        private const float DistanceClip = 0.1f;
    
*/

    //PHYSICS

    //SPAWN

    //RENDER

    //Controls

    public RenderTexture renderTex;
    public ComputeShader shader;

                                 //8=256 12=4096
    public int TexResolution;

    private float[,] array2d;
    private ComputeBuffer compute2DArr;

    [Serializable]
    public struct Particle
    {
        public Vector3 pos;
        public Vector3 vel;
        public float mass;
        public Vector3 accel;
        public Vector3 accelPrev;
    }

    private GravityParticlesApplier.Particle[] particles;

    [SerializeField]
    public GravityParticlesApplier.Particle[] particlesOut;

    private Vector3[] forces;


    private int particleCount;
    private int batchCountPowFactor;
    private int particleBatchCount;
    private int particlesInBatch;
    private int particleBatches;

    private int showPKernel;
    private int wipeKernel;
    private int applyFKernel;
    private int findForcesKernel;
    private int sumForcesKernel;
    private int sumInitKernel;

    private int[] kernels;
    private int renderDebugKernel;
    private ComputeBuffer particleBuffer;
    private ComputeBuffer forcesBuffer;

    private int renderDebugKernelS;


    private float yRot = 0f;

    private bool simulate = false;

    // Start is called before the first frame update
    void Start()
    {
        InitShader();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            simulate = !simulate;
        }

        if (simulate)
        {
            shader.SetFloat("RenderRetain", global::Constants.SimulationRetain);
            for (int i = 0; i < global::Constants.ItersPerFrame; i++)
            {
                UpdateParticles();
            }
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            ShowDebug();
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            ShowDebugS();
        }

        if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            UpdateParticles();
            particleBuffer.GetData(particlesOut);
        }

        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            shader.SetFloat("RenderRetain", 0.0f);
            Constants.ViewPortSide *= 0.8f;
            shader.SetFloat("ViewExtentsToTexSizeK", TexResolution / Constants.ViewPortSide);
            RenderCurrentParticles();
        }

        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            shader.SetFloat("RenderRetain", 0.0f);
            Constants.ViewPortSide *= 1.2f;
            shader.SetFloat("ViewExtentsToTexSizeK", TexResolution / Constants.ViewPortSide);
            RenderCurrentParticles();
        }


        if (Input.GetKey(KeyCode.LeftArrow))
        {
            shader.SetFloat("RenderRetain", 0.0f);
            yRot -= Mathf.Deg2Rad * global::Constants.YRotPerFrame;
            shader.SetFloat("yRot", yRot);
            RenderCurrentParticles();
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            shader.SetFloat("RenderRetain", 0.0f);
            yRot += Mathf.Deg2Rad * global::Constants.YRotPerFrame;
            shader.SetFloat("yRot", yRot);
            RenderCurrentParticles();
        }
    }

    private void InitShader()
    {
        // init kernels

        wipeKernel = shader.FindKernel("RenderWipe");
        showPKernel = shader.FindKernel("RenderParticles");

        findForcesKernel = shader.FindKernel("GenerateForcesSelf");
        sumInitKernel = shader.FindKernel("SumForcesSelf_Init");
        sumForcesKernel = shader.FindKernel("SumForcesSelf");
        applyFKernel = shader.FindKernel("ApplyForcesSelf");
        renderDebugKernel = shader.FindKernel("RenderDebug");
        renderDebugKernelS = shader.FindKernel("RenderDebugS");

        kernels = new[]
        {
            wipeKernel,
            showPKernel,
            findForcesKernel,
            sumForcesKernel,
            applyFKernel,
            sumInitKernel,
            renderDebugKernel,
            renderDebugKernelS
        };

        // init physics
        TexResolution = global::Constants.TextureSize;
        batchCountPowFactor = Constants.countPower;
        particleCount = (int) Math.Pow(2, Constants.countPower);
        particlesInBatch = particleCount;
        particleBatches = particleCount / particlesInBatch;

        int batchMatrixCount = particlesInBatch * particlesInBatch; //size of forces and summation arrays

        forces = new Vector3[batchMatrixCount];

        particles = new GravityParticlesApplier.Particle[particleCount];


        for (int i = 0; i < particleCount; i++)
        {
            particles[i].pos = Random.insideUnitSphere * global::Constants.SpawnRadius;
            if (Constants.SquishZ)
            {
                particles[i].pos.z = 0;
            }

            particles[i].vel = Vector3.zero;
            ; //Random.insideUnitSphere * 0.01f;
            particles[i].mass = Random.Range(global::Constants.MassMin, global::Constants.MassMax);
            particles[i].accel = Vector3.zero;
            particles[i].accelPrev = Vector3.zero;
        }

        particlesOut = new GravityParticlesApplier.Particle[particleCount];
        particleBuffer = new ComputeBuffer(particleCount, 4 * (sizeof(float) * 3) + sizeof(float));
        forcesBuffer = new ComputeBuffer(batchMatrixCount, sizeof(float) * 3);

        particleBuffer.SetData(particles);
        forcesBuffer.SetData(forces);
Debug.Log("FBsize MB:"+forcesBuffer.stride*forcesBuffer.count/1000000.0f);
        
        shader.SetInt("ParticleCount", particleCount);
        shader.SetInt("ParticlesInBatch", particlesInBatch);
        shader.SetInt("BatchOffset1", 0);
        shader.SetInt("BatchOffset2", 0);
        shader.SetFloat("dt", global::Constants.DeltaTime);
        Debug.Log("VK:" + (TexResolution) / Constants.ViewPortSide);
        shader.SetFloat("ViewExtentsToTexSizeK", TexResolution / Constants.ViewPortSide);
        shader.SetFloat("yRot", 0f);

        shader.SetFloat("RenderRetain", 0.5f);
        shader.SetFloat("DistanceClip", global::Constants.DistanceClip);
        shader.SetBool("MergeMasses", global::Constants.MergeMasses);

        foreach (var kernel in kernels)
        {
            shader.SetBuffer(kernel, "ParticleBuffer", particleBuffer);
            shader.SetBuffer(kernel, "ForcesBuffer", forcesBuffer);
        }

        //RENDER


        renderTex = new RenderTexture(TexResolution, TexResolution, 24);
        renderTex.enableRandomWrite = true;
        renderTex.Create();

        shader.SetInt("TextureSide", TexResolution);

        foreach (var kernel in kernels)
        {
            shader.SetTexture(kernel, "RenderTex", renderTex);
        }
    }

    private void OnDestroy()
    {
        particleBuffer.Dispose();
        forcesBuffer.Dispose();
    }

    private void UpdateParticles()
    {
        Profiler.BeginSample("CSG_Forces");
        shader.Dispatch(findForcesKernel, particlesInBatch / 8, particlesInBatch / 8, 1);
        Profiler.EndSample();
        SumAndApplyForces();

        RenderCurrentParticles();
    }

    private void RenderCurrentParticles()
    {
        Profiler.BeginSample("CSG_Render particles");
        shader.Dispatch(wipeKernel, TexResolution / 8, TexResolution / 8, 1);
        //shader.Dispatch(renderDebugKernel, TexResolution / 8, TexResolution / 8, 1);
        shader.Dispatch(showPKernel, particleCount / (8*1), 1, 1);

        GetComponent<Renderer>().material.SetTexture("_MainTex", renderTex);
        Profiler.EndSample();
    }

    private void ShowDebug()
    {
        shader.Dispatch(wipeKernel, TexResolution / 8, TexResolution / 8, 1);
        shader.Dispatch(renderDebugKernel, TexResolution / 8, TexResolution / 8, 1);

        GetComponent<Renderer>().material.SetTexture("_MainTex", renderTex);
    }

    private void ShowDebugS()
    {
        shader.Dispatch(wipeKernel, TexResolution / 8, TexResolution / 8, 1);
        shader.Dispatch(renderDebugKernelS, TexResolution / 8, TexResolution / 8, 1);

        GetComponent<Renderer>().material.SetTexture("_MainTex", renderTex);
    }

    private void SumAndApplyForces()
    {
        Profiler.BeginSample("CSG_Summation");
        shader.Dispatch(sumInitKernel, (particlesInBatch / 2) / 16, (particlesInBatch) / 16, 1);

        int targetOffset = particlesInBatch / 2;
        int sourceOffset = 0;

        int threadGroupsX = 0;
        int threadGroupsY = 0;
        //first step was done, start from second.
        //sequential summation
        for (int i = 2; i <= batchCountPowFactor; i++)
        {
            shader.SetInt("SumSourceOffset", sourceOffset);
            shader.SetInt("SumTargetOffset", targetOffset);

//            Debug.Log("offsets src+dst:" + sourceOffset + " t:" + targetOffset + " fc:" + Time.frameCount);
            int targetW = particlesInBatch / (int) Math.Pow(2, i);
            threadGroupsY = Math.Max(particlesInBatch / 256, 1);
            threadGroupsX = Math.Max(targetW, 1);

            //          Debug.Log("counts x:" + threadGroupsX + " y:" + threadGroupsY + " fc:" + Time.frameCount);
            shader.Dispatch(sumForcesKernel, threadGroupsX, threadGroupsY, 1);

            sourceOffset += particlesInBatch / (int) Math.Pow(2, i - 1);
            targetOffset += particlesInBatch / (int) Math.Pow(2, i);
        }

        //    Debug.Log("offsets apply src:" + sourceOffset + " fc:" + Time.frameCount);
        shader.SetInt("SumTargetOffset", sourceOffset);
        Profiler.EndSample();
        
        Profiler.BeginSample("CSG_Apply");
        //    Debug.Log("counts for apply x:" + threadGroupsX + " y:" + threadGroupsY + " fc:" + Time.frameCount);
        //auto-reuse target offset from final summation and summation count;
        shader.Dispatch(applyFKernel, threadGroupsX, threadGroupsY, 1);
        Profiler.EndSample();
    }
}