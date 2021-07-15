
using Unity.Mathematics;

public class BMath
{
    public static  readonly  int3 nagtiveUnit = new int3(-1,-1,-1);
    public static int3 PositionToGridCoord(float3 position)
    {
        return new int3((int) math.floor(position.x), (int) math.floor(position.y), (int) math.floor(position.z));
    }
}
