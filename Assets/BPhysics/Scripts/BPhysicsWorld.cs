using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BPhysicsWorld : IDisposable
{
    public NativeHashMap<int3, BBody> m_posBBodyMap;
    
    
    
    public void Dispose()
    {
        if (m_posBBodyMap.IsCreated)
        {
            m_posBBodyMap.Dispose();
        }
    }

    public bool AddBBody(int3 pos,ref BBody bBody)
    {
        return m_posBBodyMap.TryAdd(pos, bBody);
    }

    public bool GetBBody(int3 pos, out BBody bBody)
    { 
        return m_posBBodyMap.TryGetValue(pos, out bBody);
    }

    public void AddBBodyCapacity(int count)
    {
 
        if(m_posBBodyMap.Count() + count > m_posBBodyMap.Capacity) m_posBBodyMap.Capacity = m_posBBodyMap.Count() + count;
    }
    
    public   bool CastRay(BRaycastInput input,   out BRaycastHit hit)
    {
        float3 src = input.Start;
        float3 target = input.End;
        hit = new BRaycastHit();
        float3 dir = target - src;
        
        float3 now = src;
        int3 nowgrid = (int3) math.floor(now);
        int3 targetGrid = (int3) math.floor(target);
        bool3 isOnEdge;
        int3 nextgrid;
        float3 nextgridt;
        float totalT = 0; 
        int i = 0;
        float3 nowfraction = now - nowgrid;
        nextgrid = nowgrid;
        if (math.abs(nowfraction.x) < math.EPSILON)
        {
            isOnEdge.x = true;
        }
        else
        {
            isOnEdge.x = false;
        }
        if (math.abs(nowfraction.y) < math.EPSILON)
        {
            isOnEdge.y = true;
        }
        else
        {
            isOnEdge.y = false;
        }
        if (math.abs(nowfraction.z) < math.EPSILON)
        {
            isOnEdge.z = true;
        }
        else
        {
            isOnEdge.z = false;
        }
        
        while (nowgrid.x != targetGrid.x || nowgrid.y != targetGrid.y || nowgrid.z != targetGrid.z)
        {
            nextgridt.x = nextgridt.y = nextgridt.z = float.MaxValue;
            if (dir.x > float.Epsilon)
            {
                nextgrid.x = nowgrid.x + 1;
                nextgridt.x = (nextgrid.x - now.x) / dir.x;
            }else if (dir.x < -float.Epsilon)
            {
                nextgrid.x = nowgrid.x ;
                if (isOnEdge.x)
                {
                    nextgrid.x  = nowgrid.x - 1;
                }
                nextgridt.x  = (nextgrid.x  - now.x) / dir.x;
            } 
   
            if (dir.y > 0)
            {
                nextgrid.y = nowgrid.y + 1;
                nextgridt.y = (nextgrid.y - now.y) / dir.y;
            }else if (dir.y < 0)
            {
                nextgrid.y = nowgrid.y ;
                if (isOnEdge.y)
                {
                    nextgrid.y = nowgrid.y - 1;
                }

                nextgridt.y = (nextgrid.y - now.y) / dir.y;
            }

            if (dir.z >  0)
            {
                nextgrid.z = nowgrid.z + 1;
                nextgridt.z = (nextgrid.z - now.z) / dir.z;
            }else if (dir.z < 0)
            {     nextgrid.z = nowgrid.z ;
                if (isOnEdge.z)
                {
                    nextgrid.z = nowgrid.z - 1;
                }
                nextgridt.z = (nextgrid.z - now.z) / dir.z;
            }

            float t = math.min(nextgridt.x , math.min(nextgridt.y, nextgridt.z));
            totalT += t;
            if (totalT > 1)
            {
                return false;
            }

            now = now + dir * t;
            if (math.abs(t - nextgridt.x) < math.EPSILON)
            {
                now.x = nextgrid.x;
            }
            if (math.abs(t - nextgridt.y) < math.EPSILON)
            {
                now.y = nextgrid.y;
            }
            if (math.abs(t - nextgridt.z) < math.EPSILON)
            {
                now.z = nextgrid.z;
            }

            nowgrid = (int3) math.floor(now);
            
            nowfraction = now - nowgrid;
            nextgrid = nowgrid;
            if (math.abs(nowfraction.x) < math.EPSILON)
            {
                isOnEdge.x = true;
            }
            else
            {
                isOnEdge.x = false;
            }
            if (math.abs(nowfraction.y) < math.EPSILON)
            {
                isOnEdge.y = true;
            }
            else
            {
                isOnEdge.y = false;
            }
            if (math.abs(nowfraction.z) < math.EPSILON)
            {
                isOnEdge.z = true;
            }
            else
            {
                isOnEdge.z = false;
            }
            BBody body; 
            if (m_posBBodyMap.TryGetValue(nowgrid, out body))
            {
                hit.Position = now;
                hit.EntityGrid = nowgrid;
                hit.Entity = body.Entity;
                if (isOnEdge.x)
                {
                    hit.GenGrid = new int3(nowgrid.x - 1, nowgrid.y, nowgrid.z) ;
                } else if (isOnEdge.y)
                {
                    hit.GenGrid = new int3(nowgrid.x , nowgrid.y - 1, nowgrid.z) ;
                } else if (isOnEdge.z)
                {
                    hit.GenGrid = new int3(nowgrid.x, nowgrid.y, nowgrid.z  - 1) ;
                }
                
                
                return true;
            }

            int3 checkg = nowgrid;
            
            if (isOnEdge.x && dir.x < -float.Epsilon)
            {
                checkg.x -= 1;
                if (m_posBBodyMap.TryGetValue(checkg, out body))
                {
                    hit.Position = now;
                    hit.EntityGrid = checkg;
                    hit.Entity = body.Entity;
                    hit.GenGrid = nowgrid;
                    return true;
                }
            }

            checkg = nowgrid;
            if (isOnEdge.y && dir.y < -float.Epsilon)
            {
                checkg.y -= 1;
                if (m_posBBodyMap.TryGetValue(checkg, out body))
                {
                    hit.Position = now;
                    hit.EntityGrid = checkg;
                    hit.Entity = body.Entity;
                    hit.GenGrid = new int3(checkg.x , checkg.y + 1, checkg.z) ;
                    return true;
                }
            }
            
            checkg = nowgrid;
            if (isOnEdge.z && dir.z < -float.Epsilon)
            {
                checkg.z -= 1;
                if (m_posBBodyMap.TryGetValue(checkg, out body))
                {
                    hit.Position = now;
                    hit.EntityGrid = checkg;
                    hit.Entity = body.Entity;
                    hit.GenGrid = nowgrid;
                    return true;
                }
            }
            
            if (now.y <= 0 && nowgrid.x >=0 && nowgrid.z >= 0)
            {
                    
                hit.Position = now;
                hit.EntityGrid = checkg;
                hit.Entity = Entity.Null;
                hit.GenGrid = checkg ; 
                return true;
            }

            if (i > 20)
            {
                
                break;
            }

            i++;

        }

        return false;
    }
}
