
using Unity.Mathematics;

public class BMath
{
    public static  readonly  int3 nagtiveUnit = new int3(-1,-1,-1);
    public static int3 PositionToGridCoord(float3 position)
    {
        return new int3((int) math.floor(position.x), (int) math.floor(position.y), (int) math.floor(position.z));
    }
    
    public static float3 ToEuler(quaternion q)
    {
        const float epsilon = 1e-6f;

        //prepare the data
        var qv = q.value;
        var d1 = qv * qv.wwww * new float4(2.0f); //xw, yw, zw, ww
        var d2 = qv * qv.yzxw * new float4(2.0f); //xy, yz, zx, ww
        var d3 = qv * qv;
        var euler = new float3(0.0f);

        const float CUTOFF = (1.0f - 2.0f * epsilon) * (1.0f - 2.0f * epsilon);

            
        var y1 = d2.y - d1.x;
        if (y1 * y1 < CUTOFF)
        {
            var x1 = d2.x + d1.z;
            var x2 = d3.y + d3.w - d3.x - d3.z;
            var z1 = d2.z + d1.y;
            var z2 = d3.z + d3.w - d3.x - d3.y;
            euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
        }
        else //zxz
        {
            y1 = math.clamp(y1, -1.0f, 1.0f);
            var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
            euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
        }

        return   euler.yzx;;

    }

}
