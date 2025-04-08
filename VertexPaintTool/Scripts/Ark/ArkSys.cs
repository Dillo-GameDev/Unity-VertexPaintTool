using System.Collections;
using System.Collections.Generic;
using UnityEngine;

   namespace Ark
{
    public static class Sys
    {
        public class Stack<T>
        {
            private List<T> list;
            private int capacity = 0;

            public Stack(int Capacity)
            {
                list = new List<T>();
                capacity = Capacity;
            }

            public void Push(T obj)
            {
                if (list == null) list = new List<T>();
                list.Insert(0, obj);
                for (int i = list.Count - 1; i >= capacity; i--)
                {
                    list.RemoveAt(i);
                }
            }

            public T Pop()
            {
                if (list == null) list = new List<T>();
                if (list.Count == 0) return default(T);
                T obj = list[0];
                list.RemoveAt(0);
                return obj;
            }

            public T Peek()
            {
                if (list == null) list = new List<T>();
                return list.Count > 0 ? list[0] : default(T);
            }

            public T GetAt(int index)
            {
                if (list == null) list = new List<T>();
                if (Core.LogIfError(() => index >= list.Count, "Trying to get element in stack at index " + index + " but this is greater than the stack size of " + list.Count + "!")) return default(T);
                return list[index];
            }

            public List<T> GetAll() { return new List<T>(list); }

            public void Clear() { list = new List<T>(); }
            public bool Contains(T obj)
            {
                if (list == null) return false;
                foreach (T other in list)
                {
                    if (other == null && obj == null) return true;
                    if (other == null) continue;
                    if (other.Equals(obj)) return true;
                }
                return false;
            }
            public int Length { get { return list != null ? list.Count : 0; } }
        }
    }
}
