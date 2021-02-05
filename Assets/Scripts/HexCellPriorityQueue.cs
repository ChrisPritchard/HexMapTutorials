
namespace DarkDomains
{
    using System.Collections.Generic;

    public class HexCellPriorityQueue
    {
        List<HexCell> list;
        int count = 0;
        int minimum = 0;

        public HexCellPriorityQueue(int capacity)
        {
            list = new List<HexCell>(capacity);
        }

        public int Count => count;

        public void Enqueue(HexCell cell)
        {
            count ++;
            var priority = cell.SearchPriority;

            while(priority >= list.Count)
                list.Add(null);

            cell.NextWithSamePriority = list[priority];
            list[priority] = cell;

            if(priority < minimum)
                minimum = priority;
        }

        public HexCell Dequeue()
        {
            count --;
            for(; minimum < list.Count; minimum++)
            {
                var cell = list[minimum];
                if(cell != null)
                {
                    list[minimum] = cell.NextWithSamePriority;
                    return cell;
                }
            }
            return null;
        }

        public void Change(HexCell cell, int oldPriority)
        {
            var current = list[oldPriority];
            var next = current.NextWithSamePriority;
            if(current == cell)
                list[oldPriority] = next;
            else
            {
                while(next != cell)
                {
                    current = next;
                    next = current.NextWithSamePriority;
                }
                current.NextWithSamePriority = cell.NextWithSamePriority;
                Enqueue(cell);
                count --;
            }
        }

        public void Clear()
        {
            list.Clear();
            count = minimum = 0;
        }
    }
}