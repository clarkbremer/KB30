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
                Album current = mainWindow.album;
                Album previous = new Album(current.Filename, UndoStack.Pop());
                mainWindow.album = previous;
                mainWindow.slides = previous.slides;
                mainWindow.initializeSlidesUI();
            }
        }

        public void OldUndo()
        {
            if (UndoStack.Count > 0)
            {
                Album current = mainWindow.album;
                Album previous = new Album(current.Filename, UndoStack.Pop());

                undo_in_progress = true;

                if (previous.Soundtrack != current.Soundtrack)
                {
                    current.Soundtrack = previous.Soundtrack;
                }

                else if (previous.slides.Count != current.slides.Count)  // slides were added or deleted
                {
                    if (previous.slides.Count < current.slides.Count) // slide(s) were added
                    {
                        for (int s = 0; s < previous.slides.Count; s++)
                        {
                            Slide ps = previous.slides[s];
                            Slide cs = current.slides[s];
                            while (!ps.SameAs(cs))
                            {
                                mainWindow.removeSlide(cs);
                                cs = current.slides[s];
                            }
                        }
                        while (previous.slides.Count != current.slides.Count) // in case they were added at the end
                        {
                            mainWindow.removeSlide(current.slides[current.slides.Count - 1]);
                        }
                    }
                    else  // slide(s) were deleted
                    {
                    }

                    // loop through the slides looking for what changed
                    for (int s = 0; s < previous.slides.Count; s++)
                    {
                        Slide ps = previous.slides[s];
                        Slide cs = current.slides[s];
                        if (ps.fileName != cs.fileName) // slides were moved
                        {
                            // TODO
                        }
                        else if (ps.keys.Count != cs.keys.Count) // key was added or deleted
                        {
                            if (ps.keys.Count < cs.keys.Count) // key was added 
                            {
                                bool found = false;
                                for (int k = 0; k < ps.keys.Count; k++)
                                {
                                    Keyframe pk = ps.keys[k];
                                    Keyframe ck = cs.keys[k];

                                    if (!pk.SameVals(ck))
                                    {
                                        mainWindow.selectSlide(cs);
                                        mainWindow.deleteKeyframe(ck);
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) // must have been the last one
                                {
                                    mainWindow.selectSlide(cs);
                                    mainWindow.deleteKeyframe(cs.keys[cs.keys.Count - 1]);
                                }
                            }
                            else // key was deleted
                            {
                                bool found = false;
                                for (int k = 0; k < cs.keys.Count; k++)
                                {
                                    Keyframe pk = ps.keys[k];
                                    Keyframe ck = cs.keys[k];

                                    if (!pk.SameVals(ck))
                                    {
                                        mainWindow.selectSlide(cs);
                                        Keyframe newKey = new Keyframe(pk.zoomFactor, pk.x, pk.y, pk.duration);
                                        KeyframeControl kfControl = mainWindow.addKeyframeControl(newKey);
                                        mainWindow.placeKeyframe(newKey, k);
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)  // must have been the last one
                                {
                                    mainWindow.selectSlide(cs);
                                    Keyframe pk = ps.keys[ps.keys.Count - 1];
                                    Keyframe newKey = new Keyframe(pk.zoomFactor, pk.x, pk.y, pk.duration);
                                    mainWindow.appendKeyframe(newKey);
                                }
                            }
                        }

                        else  // one of the keys was modified
                        {
                            for (int k = 0; k < ps.keys.Count; k++)
                            {
                                Keyframe pk = ps.keys[k];
                                Keyframe ck = cs.keys[k];

                                if (!pk.SameVals(ck))
                                {
                                    ck.duration = pk.duration;
                                    ck.zoomFactor = pk.zoomFactor;
                                    ck.x = pk.x;
                                    ck.y = pk.y;
                                    mainWindow.selectSlide(cs);
                                    mainWindow.selectKeyframe(ck);
                                    break;
                                }
                            }
                        }
                    }
                }
                undo_in_progress = false;
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
