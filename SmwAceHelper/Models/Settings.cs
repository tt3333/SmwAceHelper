using Microsoft.Toolkit.Mvvm.ComponentModel;
using SmwAceHelper.Utilities;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SmwAceHelper.Models
{
    [JsonConverter(typeof(CustomEnumConverter<Language>))]
    public enum Language
    {
        English,
        Japanese,
    }

    public class Settings : ObservableObject
    {
        private Language language = Language.English;
        private int thumbnailScale = 50;
        private int zoomScale = 4;
        private int windowX = 0;
        private int windowY = 0;

        public ObservableCollection<string> RecentFiles { get; set; } = new ObservableCollection<string>();

        public Language Language
        {
            get { return language; }
            set { SetProperty(ref language, value); }
        }

        public int ThumbnailScale
        {
            get { return thumbnailScale; }
            set { SetProperty(ref thumbnailScale, value); }
        }

        public int ZoomScale
        {
            get { return zoomScale; }
            set { SetProperty(ref zoomScale, value); }
        }

        public int WindowX
        {
            get { return windowX; }
            set { SetProperty(ref windowX, value); }
        }

        public int WindowY
        {
            get { return windowY; }
            set { SetProperty(ref windowY, value); }
        }
    }
}
