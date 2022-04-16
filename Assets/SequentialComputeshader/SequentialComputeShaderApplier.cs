using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class SequentialComputeShaderApplier : MonoBehaviour
{
    public RenderTexture renderTex;
    public ComputeShader shader;

    private int power = 8; //8=256
    public int TexResolution;

    private float [,] array2d;
    private ComputeBuffer compute2DArr;

    public struct Particle
    {
        public Vector3 pos;
        public Vector3 vec;
    }

    private Particle[] particles; 
    private int particleCount;
    private int showPKernel;

    // Start is called before the first frame update
    void Start()
    {
        ShowParticles();
    }

    void Update()
    {
        UpdateParticles();
    }

    private void UpdateParticles()
    {
        var updatePKernel = shader.FindKernel("UpdateParticles");
        var wipeKernel = shader.FindKernel("RenderWipe");

        shader.Dispatch(updatePKernel, particleCount / 8, 1, 1);
        shader.Dispatch(wipeKernel, TexResolution / 8, TexResolution / 8, 1);
        shader.Dispatch(showPKernel, particleCount / 8, 1, 1);
    }

    private void ShowParticles()
    {
        TexResolution = (int) Math.Pow(2, power);
        particleCount = (int) Math.Pow(8, 6);

        particles = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            particles[i].pos = Random.insideUnitSphere*100+new Vector3(100,100,100);
            particles[i].vec = Random.insideUnitSphere*-1;
        }

        ComputeBuffer particleBuffer = new ComputeBuffer(particleCount, sizeof(float)*3*2);
        particleBuffer.SetData(particles);

        renderTex = new RenderTexture(TexResolution, TexResolution, 24);
        renderTex.enableRandomWrite = true;
        renderTex.Create();

        showPKernel = shader.FindKernel("ShowParticles");
        var wipeKernel = shader.FindKernel("RenderWipe");
        var updatePKernel = shader.FindKernel("UpdateParticles");

        shader.SetTexture(wipeKernel, "Result", renderTex);
        shader.SetTexture(updatePKernel, "Result", renderTex);
        shader.SetTexture(showPKernel, "Result", renderTex);
        shader.SetInt("TextureSide", TexResolution);
        
        shader.SetBuffer(showPKernel, "particleBuffer", particleBuffer);
        shader.SetBuffer(updatePKernel, "particleBuffer", particleBuffer);

        shader.Dispatch(showPKernel, particleCount / 8, 1, 1);
        //  shader.Dispatch(showKernel, TexResolution / 8, TexResolution / 8, 1);
        //  shader.Dispatch(showKernel, TexResolution / 8, TexResolution / 8, 1);

        GetComponent<Renderer>().material.SetTexture("_MainTex", renderTex);
    }
    
    /// <summary>
    /// puts out 2d array into texture;
    /// </summary>
    private void Show2dArray()
    {
        TexResolution = (int) Math.Pow(2, power);

        array2d = new float[TexResolution, TexResolution];
        for (int i = 0; i < TexResolution; i++)
        {
            for (int j = 0; j < TexResolution; j++)
            {
                array2d[i, j] = Random.Range(0, 2000);
            }
        }

        ComputeBuffer arrayBuffer = new ComputeBuffer(TexResolution * TexResolution, sizeof(float));
        arrayBuffer.SetData(array2d);

        renderTex = new RenderTexture(TexResolution, TexResolution, 24);
        renderTex.enableRandomWrite = true;
        renderTex.Create();

        var showKernel = shader.FindKernel("ShowTexture");

        shader.SetTexture(showKernel, "Result", renderTex);
        shader.SetBuffer(showKernel, "dataBuffer", arrayBuffer);
        shader.SetInt("TextureSide", TexResolution);

        shader.Dispatch(showKernel, TexResolution / 8, TexResolution / 8, 1);
        //  shader.Dispatch(showKernel, TexResolution / 8, TexResolution / 8, 1);
        //  shader.Dispatch(showKernel, TexResolution / 8, TexResolution / 8, 1);

        GetComponent<Renderer>().material.SetTexture("_MainTex", renderTex);
    }

    private void CreateYellow()
    {
        TexResolution = (int) Math.Pow(2, power);

        renderTex = new RenderTexture(TexResolution, TexResolution, 24);
        renderTex.enableRandomWrite = true;
        renderTex.Create();

        var redKernel = shader.FindKernel("AddRed");
        var greenKernel = shader.FindKernel("AddGreen");
        var sumKernel = shader.FindKernel("SumAlongX");

        shader.SetTexture(redKernel, "Result", renderTex);
        shader.SetTexture(greenKernel, "Result", renderTex);
        shader.SetTexture(sumKernel, "Result", renderTex);

        shader.Dispatch(redKernel, TexResolution / 8, TexResolution / 8, 1);
        shader.Dispatch(redKernel, TexResolution / 8, TexResolution / 8, 1);
        shader.Dispatch(greenKernel, TexResolution / 8, TexResolution / 8, 1);

        for (int i = 1; i <= power; i++)
        {
            shader.SetInt("EachN", (int) Math.Pow(2, i));
            shader.Dispatch(sumKernel, TexResolution / 8, TexResolution / 8, 1);
        }

        GetComponent<Renderer>().material.SetTexture("_MainTex", renderTex);
    }
}