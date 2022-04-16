using System;

public static class Constants
{

    public const int ItersPerFrame = 1;
    
    public const float SpaceExpansion = 0.0002f;
    public const float DeltaTime = 0.000000001f;
    public const float MassMul = 1.1f; // 1.1
    public const float DistanceClip = 0.01f;
    public const bool MergeMasses = false;

    public const int countPower = 13;
   
    public const float MassMax = 10000 * MassMul;
    public const float MassMin = 10 * MassMul;
    public const float SpawnRadius = 200f * SpaceExpansion;
    public static bool SquishZ = false;
    
    public const int TextureSize = 1080;
    public const float SimulationRetain = 0.98f;
    public static float ViewPortSide = 1000 * SpaceExpansion;
    public const float YRotPerFrame = 0.3f;
}