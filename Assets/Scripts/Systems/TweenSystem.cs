using PaintECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateBefore(typeof(TransformSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class TweenSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        return JobHandle.CombineDependencies(
            new MoveToJob
            {
                deltaTime = Time.DeltaTime
            }.Schedule(this, inputDependencies),
            new RotateToJob
            {
                deltaTime = Time.DeltaTime
            }.Schedule(this, inputDependencies)
        );
    }

    [BurstCompile]
    struct MoveToJob : IJobForEach<Position, MoveTo>
    {
        public float deltaTime;

        public void Execute(ref Position position, ref MoveTo moveTo)
        {
            
            if (HandleTweenTiming(ref moveTo, ref deltaTime))
            {
                float3 length = moveTo.Destination - moveTo.Origin;

                position.Value = moveTo.Origin + length * GetTweenProgress(ref moveTo);
            }
            
        }
    }

    [BurstCompile]
    struct RotateToJob : IJobForEach<Rotation, RotateTo>
    {
        public float deltaTime;

        public void Execute(ref Rotation rotation, ref RotateTo rotateTo)
        {
            if (HandleTweenTiming(ref rotateTo, ref deltaTime))
            {
                float angleToRotate = rotateTo.Angle * GetTweenProgress(ref rotateTo);

                rotation.Value = math.mul(
                    quaternion.identity,
                    quaternion.AxisAngle(rotateTo.Axis, angleToRotate * rotateTo.Direction)
                );
            }
        }
    }


    /**
     * Handle tween delay &
     */
    public static bool HandleTweenTiming<T>(ref T tween, ref float deltaTime) where T : struct, ITween
    {
        if (!tween.Enabled || tween.Elapsed == tween.Duration)
        {
            return false;
        }

        if (tween.Delay != 0)
        {
            // TODO: handle delay precision
            tween.Delay -= deltaTime;
            if (tween.Delay > 0)
            {
                // tween is still on cooldown...
                return false;
            }

            if (tween.Delay != 0)
            {
                tween.Elapsed = 0;
                tween.Delay = 0;
            }
        }
        else
        {
            tween.Elapsed += deltaTime;
        }

        if (tween.Elapsed > tween.Duration)
        {
            tween.Elapsed = tween.Duration;
        }

        return true;
    }


    public static float GetTweenProgress<T>(ref T tween) where T : struct, ITween
    {
        // based on Robert Penner's easing functions
        // http://robertpenner.com/easing/

        float p = tween.Elapsed / tween.Duration;

        switch (tween.Easing)
        {
            case Easing.StrongIn:
                return math.pow(p, 5);

            case Easing.StrongOut:
                return 1 - math.pow(1 - p, 5);

            case Easing.ElasticIn:
                return math.sin(13f * math.PI / 2f * p) * math.pow(2f, 20 * (p - 1));

            case Easing.ElasticOut:
                return math.sin(-13 * math.PI / 2f * (p + 1)) * math.pow(2, -10 * p) + 1;

            case Easing.BounceOut:
                if (p < (1f / 2.75f))
                {
                    return 7.5625f * p * p;
                }
                else if (p < (2f / 2.75f))
                {
                    return 7.5625f * (p -= (1.5f / 2.75f)) * p + 0.75f;
                }
                else if (p < (2.5f / 2.75f))
                {
                    return 7.5625f * (p -= (2.25f / 2.75f)) * p + 0.9375f;
                }
                else
                {
                    return 7.5625f * (p -= (2.625f / 2.75f)) * p + 0.984375f;
                }

            case Easing.BackOut :
                 float s = 1.70158f;
                 float t = tween.Elapsed;
                 float d = tween.Duration;
                 
		
                return (t = t / d - 1) * t * ((s + 1) * t + s) + 1;
            
            
            
            default:
                return p;
        }
    }
}