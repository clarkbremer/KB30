using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
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

        public string basePath;

        private string _filename;
        public string Filename { 
            get { return _filename; } 
            set {
                _filename = value;
                basePath = Path.GetDirectoryName(_filename); 
            } 
        }

        public Album() { }
        public Album(Slides _slides, string _filename)
        {
            Filename = _filename;
            slides = _slides;
            if (!string.IsNullOrEmpty(Filename) && Filename != "untitled")
            {
                basePath = Path.GetDirectoryName(Filename);
            }

            version = CONFIG_VERSION;
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

        public string SaveToFile()
        {
            if(Filename == "untitled" || Filename == "")
            {
                return "";
            }
            string json = this.ToJson();
            File.WriteAllText(Filename, json);
            return json;
        }
        public static Album LoadFromFile(string filename)
        {
            Album album = new Album();
            album.Filename = filename;
            
            string jsonString;
            jsonString = File.ReadAllText(filename);
            JsonConvert.PopulateObject(jsonString, album);
            album.MigrateRelativePaths();  // vestigal
            if (Convert.ToDouble(album.version) > CONFIG_VERSION)
            {
                throw new InvalidOperationException("Album File version is newer than this version of the program");
            }
            Slides slides = album.slides;
            for (int i = slides.Count - 1; i >= 0; i--)
            {
                var fname = slides[i].fileName;
                if (!File.Exists(fname) &&fname != "black" && fname != "white")
                {
                    NotFoundDialog not_found_dialog = new NotFoundDialog();
                    not_found_dialog.filename_message.Text = "File Not Found: " + fname;
                    if (not_found_dialog.ShowDialog() == true)
                    {
                        slides.RemoveAt(i);
                    }
                    else
                    {
                        throw new InvalidOperationException("Quiet");
                    }
                }
            }
            return album;
        }

        private void MigrateRelativePaths()  // vestigal
        {
            foreach (Slide slide in slides)
            {
                if (String.IsNullOrEmpty(slide.fileName))
                {
                    if (slide.relativePath == "black")
                    {
                        slide.fileName = "black";
                    }
                    else if (slide.relativePath == "white")
                    {
                        slide.fileName = "white";
                    }
                    else
                    {
                        slide.fileName = Path.GetFullPath(slide.relativePath, basePath);
                    }
                }
            }
        }
    }
}
