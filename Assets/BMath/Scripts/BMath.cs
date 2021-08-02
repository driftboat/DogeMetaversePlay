
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

    public static float3 GetWorldStartPos(int worldId)
    {
        int worldIndex = worldId - 1;
        int worldx = worldIndex / 3;
        int worldy = worldIndex % 3;
        return new float3(worldx*256*50,0,worldy*256*50);
    }

    public static float3 GetLandOffsetPos(int landx, int landy)
    {
        return new float3(landx*50,0,landy*50);
    }

    public static int3 WorldPosToLand(float3 pos)
    {
        int worldx = (int)pos.x / (256*50);
        int worldy = (int)pos.z / (256*50); 
        worldx = math.min(math.max(worldx, 0), 2);
        worldy = math.min(math.max(worldy, 0), 2);
        int worldId = worldy * 3 + worldx + 1;
        int landx = (int)pos.x / 50 % 256;
        int landy = (int)pos.z / 50 % 256;
        return new int3(worldId,landx,landy);
    }

    public static float3 LandToWorldPos(int3 land)
    {
      return   GetWorldStartPos(land.x) + GetLandOffsetPos(land.y, land.z);
    }

    public static int3 WorldPosToLandPos(float3 pos)
    {
        return new int3((int)pos.x%50,(int)pos.y,(int)pos.z%50);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worldId"></param>
    /// <param name="landx"></param>
    /// <param name="landy"></param>
    /// <param name="dir">
    ///  1 right 2 top right 3 top 4 top left 5 left 6 bottom left  7 bottom 8 bottom right
    /// </param>
    /// <returns></returns>
    public static int3 GetLandNearBy(int worldId, int landx, int landy, int dir)
    {
        int3 nearby = int3.zero;
        int worldIndex = worldId - 1;
        int worldX = worldIndex % 3;
        int worldY = worldIndex / 3;
        if (dir == 1)
        {
            nearby.y = landx + 1;
        }
        else if (dir == 2)
        {
            nearby.y = landx + 1;
            nearby.z = landy + 1;
        }
        else if (dir == 3)
        {
            nearby.z = landy + 1;
        }
        else if (dir == 4)
        {
            nearby.y = landx - 1;
            nearby.z = landy + 1;
        }
        else if (dir == 5)
        {
            nearby.y = landx - 1; 
        }
        else if (dir == 6)
        {
            nearby.y = landx - 1; 
            nearby.z = landy - 1;
        } 
        else if (dir == 7)
        {
            nearby.z = landy - 1;
        } 
        else if (dir == 8)
        {
            nearby.y = landx + 1; 
            nearby.z = landy - 1;
        }

        if (nearby.y > 255)
        {
            worldX += 1;
            nearby.y -= 256;
        }

        if (nearby.z > 255)
        {
            worldY += 1;
            nearby.z -= 256;
        }

        if (nearby.y < 0)
        {
            worldX -= 1;
            nearby.y += 256;
        }

        if (nearby.z < 0)
        {
            worldY -= 1;
            nearby.z += 256;
        }

        if (worldX < 0 || worldX > 2 || worldY < 0 || worldY > 2)
        {
            return int3.zero;
        }

        nearby.x = worldY * 3 + worldX + 1;

        return nearby;
    }

}
