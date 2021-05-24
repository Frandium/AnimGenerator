using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AnimGenerator
{
    /// <summary>
    /// 负责插值的类，后续可以在此扩展以实现更平滑的自定义插值效果
    /// </summary>
    public class LerpUtility
    {
        public static Vector3 Linear(Vector3 v1, Vector3 v2, float p)
        {
            return Vector3.Lerp(v1, v2, p);
        }

        public static Quaternion Linear(Quaternion q1, Quaternion q2, float p)
        {
            return Quaternion.Lerp(q1, q2, p);
        }
    }
}