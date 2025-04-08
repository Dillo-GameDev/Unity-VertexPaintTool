using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ark
{
    public static class Obj
    {
        public static T GetComponent<T>(GameObject obj)
        {
            if (Core.LogIfError(() => obj == null, "Trying to get component from a gameObject, but the object was null!")) return default(T);
            var component = obj.GetComponent <T>();
            Core.LogIfError(() => component == null, "Tried to get component of type " + typeof(T).Name + " from gameobject " + obj + ", but we did not find that component on the object!");
            return obj.GetComponent<T>();
        }

        public static T GetComponent<T>(UnityEngine.Transform trans)
        {
            if (Core.LogIfError(() => trans == null, "Trying to get component from a transform, but the transform was null!")) return default(T);
            return GetComponent<T>(trans.gameObject);
        }

        public static T[] GetAllComponentsOfType<T>() where T: UnityEngine.Object
        {
            return GameObject.FindObjectsOfType<T>();
        }

        public static T[] GetComponentsInChildren<T>(GameObject obj) where T : UnityEngine.Object
        {
            if (Core.LogIfError(() => obj == null, "Trying to get component from a gameObject, but the object was null!")) return default(T[]);
            return obj.GetComponentsInChildren<T>(includeInactive: true);
        }
        public static T[] GetComponentsInChildren<T>(UnityEngine.Transform trans) where T: UnityEngine.Object
        {
            if (Core.LogIfError(() => trans == null, "Trying to get component from a transform, but the transform was null!")) return default(T[]);
            return trans.GetComponentsInChildren<T>(includeInactive: true);
        }
    }
}