using System;
using UnityEditor;

namespace Ark
{
    public static class Core
    {
        public static bool LogIfError(Func<bool> condition, string message)
        {
            bool hasError = condition();
            if (hasError) UnityEngine.Debug.Log(message);
            return hasError;
        }

        public static UnityEngine.Object GetObjectWithID<T>(GlobalObjectId objectID, T expectedType)
        {
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(objectID);
            LogIfError(() => !(obj is T), "Found an object with global ID " + objectID + ", but it's not the expected type of " + expectedType + "!");
            return obj;
        }
    }
}
