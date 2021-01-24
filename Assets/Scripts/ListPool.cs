
namespace DarkDomains
{
    using System.Collections.Generic;

    // the purpose of this class is to cut down on list initialisation
    // when a list is used, its inner array resized and memory reserved, it is kept for reuse later
    // cuts down on total used memory, and GC operations presumably. a small optimation but still.
    public static class ListPool<T>
    {
        private static Stack<List<T>> stack = new Stack<List<T>>();

        public static List<T> Get() => stack.Count > 0 ? stack.Pop() : new List<T>();

        public static void Put(List<T> noLongerUsed) 
        {
            noLongerUsed.Clear();
            stack.Push(noLongerUsed);
        }
    }
}