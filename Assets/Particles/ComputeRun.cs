using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class ComputeRun : MonoBehaviour
{
    public ComputeShader shader;
    public RenderTexture tex;
    void RunShader()
    {
        int kernelHandle = shader.FindKernel("CSMain");

        shader.SetTexture(kernelHandle, "Result", tex);
        shader.Dispatch(kernelHandle, 256 / 8, 256 / 8, 1);
    }

    private void InitTex()
    {
        tex = new RenderTexture(256, 256, 24);
        tex.enableRandomWrite = true;
        tex.Create();
    }
    private void Start()
    {
        InitTex();
    }
    private void Update()
    {
        RunShader();
    }
}
