
namespace HexMapTutorials
{
    using System;
    using System.Collections.Generic;

    public class HexCellPriorityQueue
    {
        List<HexCell> list = new List<HexCell>();
        int count = 0;
        int minimum = 0;

        public int Count => count;

        public void Enqueue(HexCell cell)
        {
            count ++;
            var priority = cell.SearchPriority;
            if(priority < minimum)
                minimum = priority;

            while(priority >= list.Count)
                list.Add(null);
            cell.NextWithSamePriority = list[priority];
            list[priority] = cell;
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

            // if this is hit, there is a bug somewhere (or dequeue was called when count was <= 0)
            throw new IndexOutOfRangeException();
        }

        // re-adds cell to queue with a difference priority
        // first removes it at the position of the old priority
        // then re-adds it.
        public void Change(HexCell cell, int oldPriority)
        {
            var current = list[oldPriority];
            var next = current.NextWithSamePriority;

            // remove from list, preserving the stack
            if(current == cell)
                list[oldPriority] = next; // if the cell is top of the stack, set the stack to next
            else
            {
                // find where it is in the stack
                while(next != cell)
                {
                    current = next;
                    next = current.NextWithSamePriority;
                }
                // at this point current is the prior cell, next is the cell to remove, and next.next is the cell after
                // so we attack prior to next.next, snipping out the cell to remove
                current.NextWithSamePriority = cell.NextWithSamePriority;
            }
            count --; // cell has been manually removed, so decrement count

            Enqueue(cell); // re-add (which increments count)
        }

        public void Clear()
        {
            list.Clear();
            count = 0;
            minimum = int.MaxValue;
        }
    }
}