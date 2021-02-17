using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;


namespace KB30
{
    public class History
    {
        DropOutStack<UndoItem> UndoStack = new DropOutStack<UndoItem>(50);
        MainWindow mainWindow;
        int pause_count = 0;
        bool undo_in_progress = false;

        public History(MainWindow main_window)
        {
            mainWindow = main_window;
        }
        public void Reset()
        {
            UndoStack = new DropOutStack<UndoItem>(50);
        }

        public void Add(UndoItem item)
        {
            if (pause_count > 0 || undo_in_progress)
            {
                return;
            }
            UndoStack.Push(item);
        }

        public void Pause()
        {
            pause_count++;
        }

        public void Resume()
        {
            pause_count--;
        }

        public void Undo()
        {
            if (UndoStack.Count > 0)
            {
                UndoItem item = UndoStack.Pop();
                undo_in_progress = true;
                item.Undo(mainWindow);
                undo_in_progress = false;
            }
            else
            {
                Console.Beep(500, 500);
            }
        }

        public abstract class UndoItem
        {
            public abstract void Undo(MainWindow mainWindow);
        }

        public class CompoundUndo : History.UndoItem
        {
            int count;
            public CompoundUndo(int _count)
            {
                count = _count;
            }
            public override void Undo(MainWindow mainWindow)
            {
                for (int i=0; i<count; i++)
                {
                    mainWindow.history.Undo();
                }
            }
        }
    }


    class DropOutStack<T>
    {
        private T[] items;
        private int top = 0;
        public int Count { get; set; }
        private int capacity;
        public DropOutStack(int _capacity)
        {
            capacity = _capacity;
            Count = 0;
            items = new T[capacity];
        }

        public void Push(T item)
        {
            items[top] = item;
            top = (top + 1) % items.Length;
            if (Count < capacity)
            {
                Count += 1;
            }
        }
        public T Pop()
        {
            top = (items.Length + top - 1) % items.Length;
            Count -= 1;
            return items[top];
        }
    }
}
