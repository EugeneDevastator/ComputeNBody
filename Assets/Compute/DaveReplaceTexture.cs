using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DaveReplaceTexture : MonoBehaviour
{
    public ComputeShader cShader;
    public Renderer renderer;
    public RenderTexture outTexture;

    public int TexResolution = 512;
    
    // Start is called before the first frame update
    void Start()
    {
        outTexture=new RenderTexture(TexResolution,TexResolution,24);
        outTexture.enableRandomWrite = true;
        outTexture.Create();

        renderer = GetComponent<Renderer>();
        renderer.enabled = true;

    }

    private void ApplyComputeShader()
    {
        int kernelHandle = cShader.FindKernel("CSMain");
        cShader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad*1000));
        cShader.SetTexture(kernelHandle,"Result",outTexture);
        cShader.Dispatch(kernelHandle,TexResolution/8, TexResolution/8,1);
        renderer.material.SetTexture("_MainTex",outTexture);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ApplyComputeShader();
        }
    }
}
