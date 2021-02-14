using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;


namespace KB30
{
    public class History
    {
        DropOutStack<string> UndoStack = new DropOutStack<string>(50);
        MainWindow mainWindow;
        int pause_count = 0;
        bool undo_in_progress = false;

        public History(MainWindow main_window)
        {
            mainWindow = main_window;
        }
        public void Reset()
        {
            UndoStack = new DropOutStack<string>(50);
        }

        public void Record()
        {
            if (pause_count > 0 || undo_in_progress)
            {
                return;
            }
            UndoStack.Push(mainWindow.album.ToJson());
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
                string current = mainWindow.album.ToJson();
                string previous = UndoStack.Pop();
            }
            else
            {
                Console.Beep(600, 600);
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
