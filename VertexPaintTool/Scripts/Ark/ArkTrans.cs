using UnityEngine;
using Ark;

namespace Ark
{
    public static class Trans
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------------
        //Set methods for transform values.
        //These are the only allowed ways of manipulating transform. Direct calls to the Unity API outside of this class are forbidden.
        //---------------------------------------------------------------------------------------------------------------------------------------------------

        //Position setting methods.
        public static void SetLocalPosition(UnityEngine.Transform trans, Vector3 pos)
        {
            if (Core.LogIfError(() => trans == null, "Trying to set position on a null transform!")) return;
            trans.localPosition = pos;
        }

        public static void SetLocalPosition(GameObject obj, Vector3 pos)
        {
            if (Core.LogIfError(() => obj == null, "Trying to set position on a null gameobject!")) return;
            obj.transform.localPosition = pos;
        }

        public static void SetWorldPosition(UnityEngine.Transform trans, Vector3 pos)
        {
            if (Core.LogIfError(() => trans == null, "Trying to set position on a null transform!")) return;
            trans.position = pos;
        }

        public static void SetWorldPosition(GameObject obj, Vector3 pos)
        {
            if (Core.LogIfError(() => obj == null, "Trying to set position on a null gameobject!")) return;
            obj.transform.position = pos;
        }

        //Rotation setting methods, euler angles.
        public static void SetLocalEulerAngles(UnityEngine.Transform trans, Vector3 rot)
        {
            if (Core.LogIfError(() => trans == null, "Trying to set euler angles on a null transform!")) return;
            trans.localEulerAngles = rot;
        }

        public static void SetLocalEulerAngles(GameObject obj, Vector3 rot)
        {
            if (Core.LogIfError(() => obj == null, "Trying to set euler angles on a null object!")) return;
            obj.transform.localEulerAngles = rot;
        }

        public static void SetEulerAngles(UnityEngine.Transform trans, Vector3 rot)
        {
            if (Core.LogIfError(() => trans == null, "Trying to set euler angles on a null transform!")) return;
            trans.eulerAngles = rot;
        }

        public static void SetEulerAngles(GameObject obj, Vector3 rot)
        {
            if (Core.LogIfError(() => obj == null, "Trying to set euler angles on a null object!")) return;
            obj.transform.eulerAngles = rot;
        }

        //Rotation setting methods, quaternion.
        public static void SetLocalQuaternionRotation(UnityEngine.Transform trans, Quaternion quaternion)
        {
            if (Core.LogIfError(() => trans == null, "Trying to set rotation on a null transform!")) return;
            trans.localRotation = quaternion;
        }

        public static void SetLocalQuaternionRotation(GameObject obj, Quaternion quaternion)
        {
            if (Core.LogIfError(() => obj == null, "Trying to set rotation on a null gameobject!")) return;
            obj.transform.localRotation = quaternion;
        }

        public static void SetQuaternionRotation(UnityEngine.Transform trans, Quaternion quaternion)
        {
            if (Core.LogIfError(() => trans == null, "Trying to set rotation on a null object!")) return;
            trans.rotation = quaternion;
        }

        public static void SetQuaternionRotation(GameObject obj, Quaternion quaternion)
        {
            if (Core.LogIfError(() => obj == null, "Trying to set rotation on a null object!")) return;
            obj.transform.rotation = quaternion;
        }

        //Scale setting methods.
        public static void SetLocalScale(UnityEngine.Transform trans, Vector3 scale)
        {
            if (Core.LogIfError(() => trans == null, "Trying to set scale on a null transform!")) return;
            trans.localScale = scale;
        }

        public static void SetLocalScale(GameObject obj, Vector3 scale)
        {
            if (Core.LogIfError(() => obj == null, "Trying to set scale on a null object!")) return;
            obj.transform.localScale = scale;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        //Vector math wrappers.
        //---------------------------------------------------------------------------------------------------------------------------------------------------
        public static float DistanceBetween(Vector3 a, Vector3 b) { return Vector3.Distance(a, b); }
    }
}
