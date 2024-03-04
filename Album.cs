using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;


namespace KB30
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Album
    {
        const double CONFIG_VERSION = 1.0;


        [JsonProperty]
        public double version { get; set; }

        [JsonProperty]
        public Slides slides { get; set; }

        private String lastSnapShot = "";

        private HashSet<string> search_paths = new HashSet<string>();

        public string Filename;
        public Album(string filename)
        {
            Filename = filename;

            string jsonString;
            jsonString = File.ReadAllText(filename);
            JsonConvert.PopulateObject(jsonString, this);
            lastSnapShot = ToJson();
            if (Convert.ToDouble(version) > CONFIG_VERSION)
            {
                throw new InvalidOperationException("Album File version is newer than this version of the program");
            }
            if (!ValidateFilenames())
            {
                throw new InvalidOperationException("Quiet");
            }
            if (!ValidateAudioFilenames("audio"))
            {
                throw new InvalidOperationException("Quiet");
            }
            if (!ValidateAudioFilenames("backgroundAudio"))
            {
                throw new InvalidOperationException("Quiet");
            }
        }

        private bool ValidateFilenames(){
            int i = 0;
            bool? repeat_for_all_missing_files = false;
            bool retry_this_file;
            int action = 0;
            while (i < slides.Count)  // We use this instead of ForEach because we may be deleting items
            {
                retry_this_file = false;
                do
                {
                    string fname = slides[i].fileName;
                    if (!File.Exists(fname) && fname != "black" && fname != "white")
                    {
                        if (repeat_for_all_missing_files != true)
                        {
                            NotFoundDialog not_found_dialog = new NotFoundDialog();
                            not_found_dialog.filename_message.Text = "File Not Found: " + fname;
                            if (not_found_dialog.ShowDialog() == true)
                            {
                                action = not_found_dialog.result;
                                repeat_for_all_missing_files = not_found_dialog.repeatCheckBox.IsChecked;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        switch (action)
                        {
                            case NotFoundDialog.ACTION_SKIP:
                                slides.RemoveAt(i);
                                break;
                            case NotFoundDialog.ACTION_BLACK:
                                slides[i].fileName = "black";
                                break;
                            case NotFoundDialog.ACTION_WHITE:
                                slides[i].fileName = "white";
                                break;
                            case NotFoundDialog.ACTION_FIND:
                                string found_file = FindMissingFile(fname, "Images (*.BMP;*.JPG;*.GIF,*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*");
                                if (found_file == "")
                                {
                                    retry_this_file = true;
                                    repeat_for_all_missing_files = false;
                                }
                                else
                                {
                                    slides[i].fileName = found_file;
                                }
                                break;
                        }
                    }
                } while (retry_this_file == true);
                i++;
            }
            return true;
        }

        private bool ValidateAudioFilenames(string audio_property)
        {
            bool? repeat_for_all_missing_files = false;
            bool retry_this_file;
            int action = 0;
            foreach (Slide slide in slides)
            {
                retry_this_file = false;
                do
                {
                    string fname = get_audio_filename(slide, audio_property);
                    if (fname != null) { 
                        if (!File.Exists(fname))
                        {
                            if (repeat_for_all_missing_files != true)
                            {
                                NotFoundDialog not_found_dialog = new NotFoundDialog();
                                not_found_dialog.bw_panel.Visibility = Visibility.Hidden;
                                not_found_dialog.filename_message.Text = "File Not Found: " + fname;
                                if (not_found_dialog.ShowDialog() == true)
                                {
                                    action = not_found_dialog.result;
                                    repeat_for_all_missing_files = not_found_dialog.repeatCheckBox.IsChecked;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            switch (action)
                            {
                                case NotFoundDialog.ACTION_SKIP:
                                    set_audio_filename(slide, audio_property, "");
                                    break;

                                case NotFoundDialog.ACTION_FIND:
                                    string found_file = FindMissingFile(fname, "Audio Files (*.MP3)|*.MP3|All files (*.*)|*.*");
                                    if (found_file == "")
                                    {
                                        retry_this_file = true;
                                        repeat_for_all_missing_files = false;
                                    }
                                    else
                                    {
                                        set_audio_filename(slide, audio_property, found_file);
                                    }
                                    break;
                            }
                        }
                    }
                } while (retry_this_file == true);
            }
            return true;
        }

        private string get_audio_filename(Slide slide, string filename_property)
        {
            string f = (string)typeof(Slide).GetProperty(filename_property, typeof(string)).GetValue(slide, null);
            return f;
        }
        private void set_audio_filename(Slide slide, string filename_property, string filename)
        {
            typeof(Slide).GetProperty(filename_property, typeof(string)).SetValue(slide, filename);
        }

        public Album(Slides _slides, string _filename)
        {
            Filename = _filename;
            slides = _slides;
            version = CONFIG_VERSION;
            lastSnapShot = ToJson();
        }

        public Boolean Valid()
        {
            if (slides.Count <= 0)
            {
                MessageBox.Show("Must have at least 1 slide.");
                return false;
            }
            for (int s = 0; s < slides.Count; s++)
            {
                Slide slide = slides[s];
                if (slide.keys.Count <= 0)
                {
                    MessageBox.Show("Slide " + s.ToString() + " must have at least 1 keyframe.");
                    return false;
                }
            }
            return true;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        private void TakeSnapshot(){
            lastSnapShot = ToJson();
        }

        public string SaveToFile()
        {
            if(Filename == "untitled" || Filename == "")
            {
                return "";
            }
            string json = ToJson();
            File.WriteAllText(Filename, json);
            lastSnapShot = ToJson();
            return json;
        }

        public Boolean SaveIfDirty()
        {
            String snapshot = ToJson();
            if (snapshot != lastSnapShot)
            {
                MessageBoxResult result = MessageBox.Show("Save changes to " + Filename + "?", "KB30", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        lastSnapShot = SaveToFile();
                        if (lastSnapShot == "")
                        {
                            return false;
                        }
                        return true;
                    case MessageBoxResult.No:
                        return true;
                    case MessageBoxResult.Cancel:
                        return false;
                }
            }
            return true;
        }

        private string FindMissingFile(string missing_file_with_path, string filter)
        {
            foreach (string search_path in search_paths)
            {
                string missing_file = Path.GetFileName(missing_file_with_path);

                // try in same directory as previous
                string candidate = Path.Combine(search_path, missing_file);
                if(File.Exists(candidate)){
                    return (candidate);
                }

                // try replacing just the last subdirectory
                string[] missing_parts = missing_file_with_path.Split("\\");
                if(missing_parts.Length < 2) { break; }
                string[] tail = missing_parts[(missing_parts.Length - 2)..(missing_parts.Length)];
                string[] candidate_parts = search_path.Split("\\");
                if(candidate_parts.Length < 2) { break; }
                string[] head = candidate_parts[0..(candidate_parts.Length - 1)];
                string[] all = new string[head.Length + tail.Length];
                Array.Copy(head, all, head.Length);
                Array.Copy(tail, 0, all, head.Length, tail.Length);
                candidate = String.Join("\\", all);
                if (File.Exists(candidate))
                {
                    return (candidate);
                }
            }

            // if all else fails, have user find it
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Missing File: " + missing_file_with_path;
            openFileDialog.Filter = filter;
            openFileDialog.FileName = Path.GetFileName(missing_file_with_path);
            if (openFileDialog.ShowDialog() == true)
            {
                string new_dir = Path.GetDirectoryName(openFileDialog.FileName);
                if (!search_paths.Contains(new_dir))
                {
                    search_paths.Add(new_dir);
                }
                return (openFileDialog.FileName);
            }
            return "";
        }
    }
}
