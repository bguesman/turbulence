namespace Geometry
{

struct Ray
{
  float3 origin;
  float3 direction;
  Ray transform(float4x4 mat)
  {
    Ray r = {
      mul(mat, float4(origin, 1)).xyz,
      mul(mat, float4(direction, 0)).xyz
    };
    return r;
  }
};

struct Bounds
{
  float3 low;
  float3 high;
  bool Test(float3 p)
  {
    return all(p > low && p < high);
  }
  bool TestInclusive(float3 p)
  {
    return all(p >= low && p <= high);
  }
  static Bounds MakeBounds(float3 low, float3 high)
  {
    Bounds b = {low, high};
    return b;
  }
  static Bounds Unit()
  {
    return MakeBounds(-0.5, 0.5);
  }
};

// Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
float2 RayBoxDst(Ray r, Bounds b) {
    float3 invRaydir = 1.0 / r.direction;
    // Adapted from: http://jcgt.org/published/0007/03/04/
    float3 t0 = (b.low - r.origin) * invRaydir;
    float3 t1 = (b.high - r.origin) * invRaydir;
    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);
    
    float dstA = max(max(tmin.x, tmin.y), tmin.z);
    float dstB = min(tmax.x, min(tmax.y, tmax.z));

    // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
    // dstA is dst to nearest intersection, dstB dst to far intersection

    // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
    // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

    // CASE 3: ray misses box (dstA > dstB)

    float dstToBox = max(0, dstA);
    float dstInsideBox = max(0, dstB - dstToBox);
    return float2(dstToBox, dstInsideBox);
}

float3 UnitUVAABB(float3 pWS, float4x4 worldToObject) 
{
  float3 pOS = mul(worldToObject, float4(pWS, 1)).xyz;
  return frac(pOS + 0.5);
}

// Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
bool pointInBox(float3 boundsMin, float3 boundsMax, float3 p) {
    return all(p < boundsMax && p > boundsMin);
}

float3 uvAABB(float3 pWS, float4x4 wToO) 
{
  float3 pOS = mul(wToO, float4(pWS, 1)).xyz;
  return frac(pOS + 0.5);
}

}