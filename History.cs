using System;
using System.Collections.Generic;
using System.Text;

namespace KB30
{
    public partial class History
    {

        class DropOutStack<T>
        {
            private T[] items;
            private int top = 0;
            private int count = 0;
            private int capacity;
            public DropOutStack(int _capacity)
            {
                capacity = _capacity;
                items = new T[capacity];
            }

            public void Push(T item)
            {
                items[top] = item;
                top = (top + 1) % items.Length;
                if (count < capacity)
                {
                    count += 1;
                }
            }
            public T Pop()
            {
                top = (items.Length + top - 1) % items.Length;
                count -= 1;
                return items[top];
            }

            public bool Empty()
            {
                return count == 0;
            }
        }


        public MainWindow mainWindow;
        DropOutStack<UndoItem> undoStack = new DropOutStack<UndoItem>(50);
        bool UndoInProgress = false;

        public History(MainWindow _mainWindow)
        {
            mainWindow = _mainWindow;
        }

 

        public void Undo()
        {
            if (!undoStack.Empty())
            {
                UndoItem item = undoStack.Pop();
                UndoInProgress = true;
                item.Undo(mainWindow);
                UndoInProgress = false;
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
        
        public void Add(UndoItem item)
        {
            if (UndoInProgress == false)
            {
                undoStack.Push(item);
            }
        }
    }
}
