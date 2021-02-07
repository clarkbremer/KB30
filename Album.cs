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

        public string Soundtrack;

        [JsonProperty]
        public string soundtrack
        {
            get
            {
                if (!string.IsNullOrEmpty(basePath) && !string.IsNullOrEmpty(Soundtrack))
                {
                    return (Path.GetRelativePath(basePath, Soundtrack));
                }
                return (Soundtrack);
            }
            set
            {
                if (!Path.IsPathFullyQualified(value) && !string.IsNullOrEmpty(basePath))
                {
                    Soundtrack = Path.GetFullPath(value, basePath);
                }
                else
                {
                    Soundtrack = value;
                }
            }
        }

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
        public Album(Slides _slides, string _soundtrack, string _filename)
        {
            Filename = _filename;
            slides = _slides;
            Soundtrack = _soundtrack;
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
            slides.SetBasePath(basePath);
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
            if (Convert.ToDouble(album.version) > CONFIG_VERSION)
            {
                throw new InvalidOperationException("Album File version is newer than this version of the program");
            }
            Slides slides = album.slides;
            slides.SetBasePath(album.basePath);

            for (int i = slides.Count - 1; i >= 0; i--)
            {
                if (!File.Exists(slides[i].fileName))
                {
                    MessageBox.Show("File Not Found: " + slides[i].fileName, "File Not Found");
                    slides.RemoveAt(i);
                }
            }
            return album;
        }
    }
}
