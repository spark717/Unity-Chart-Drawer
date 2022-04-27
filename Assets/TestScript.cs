using Cinemachine;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public Transform a;
    public Transform b;
    public Transform obj;
    public float smoothTime;
    public float maxSpeed;
    [Range(0, 1f)]
    public float lerpFactor1;
    [Range(0, 1f)]
    public float lerpFactor2;
    public float aprox;
    public float updateMinTime;

    public AnimationCurve curve;
    public float speedMultiplyer;

    private bool bTarget;
    private Vector3 currentVelocity;
    private float lastUpdateTime;

    private Vector3 intermediate;
    
    private void Awake()
    {
        obj.position = a.position;
        intermediate = a.position;
        bTarget = true;
    }

    private void FixedUpdate()
    {
        Vector3 currentPos = obj.position;

        Vector3 targetPos = bTarget ? b.position : a.position;
        
        //Vector3 newPos = Vector3.SmoothDamp(currentPos, targetPos, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        
        //intermediate = SmoothLerp(intermediate, targetPos, lerpFactor1);
        //Vector3 newPos = SmoothLerp(currentPos, intermediate, lerpFactor2);

        float dist = (targetPos - currentPos).magnitude;

        float delta = dist - Damp(dist, smoothTime, Time.deltaTime);

        Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, delta);
        
        obj.position = newPos;
        
        if ((targetPos - newPos).magnitude <= aprox)
        {
            bTarget = !bTarget;
        }

        float velocity = (newPos - currentPos).magnitude;
        float pos = currentPos.x;

        float timeFromLastUpdate = Time.time - lastUpdateTime;
        
        if (timeFromLastUpdate >= updateMinTime)
        {
            DebugData.Time = Time.timeSinceLevelLoad;
            DebugData.Velocity = velocity;
            DebugData.VelocityChanged?.Invoke();
            
            lastUpdateTime = Time.time;
        }
    }

    private void CustomSmooth(ref Vector3 current, Vector3 target)
    {
        float x = current.x;
        float y = current.y;
        float z = current.z;
        
        CustomSmooth(ref x, target.x);
        CustomSmooth(ref y, target.y);
        CustomSmooth(ref z, target.z);

        current = new Vector3(x, y, z);
    }
    
    private void CustomSmooth(ref float current, float target)
    {
        current = ((current * (lerpFactor1 - 1)) + target) / lerpFactor1;
    }

    private Vector3 SmoothLerp(Vector3 a, Vector3 b, float t)
    {
        Vector3 result = Vector3.zero;

        result.x = Mathf.SmoothStep(a.x, b.x, t);
        result.y = Mathf.SmoothStep(a.y, b.y, t);
        result.z = Mathf.SmoothStep(a.z, b.z, t);

        return result;
    }
    
    const float kLogNegligibleResidual = -4.605170186f; // == math.Log(kNegligibleResidual=0.01f);

    public static float Damp(float initial, float dampTime, float deltaTime)
    {
        //float k = -kLogNegligibleResidual / dampTime;
        
        return initial * (1 - Mathf.Exp(kLogNegligibleResidual / dampTime * deltaTime));
    }
}
