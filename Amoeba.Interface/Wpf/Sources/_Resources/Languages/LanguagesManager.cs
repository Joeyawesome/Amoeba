using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Amoeba.Interface
{
    sealed partial class LanguagesManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public static LanguagesManager Instance { get; } = new LanguagesManager();

        private Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private string _currentLanguage;

        private LanguagesManager()
        {
            if ((bool)(System.ComponentModel.DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(System.Windows.DependencyObject)).DefaultValue))
            {
                string path = @"C:\Local\Projects\Alliance-Network\Amoeba\Amoeba.Interface\Wpf\Resources\Languages";
                if (!Directory.Exists(path)) path = AmoebaEnvironment.Paths.LanguagesDirectoryPath;

                this.Load(path);
            }
            else
            {
                this.Load(AmoebaEnvironment.Paths.LanguagesDirectoryPath);
            }
        }

        private void Load(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            _dic.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var dic = new Dictionary<string, string>();

                    using (var xml = new XmlTextReader(path))
                    {
                        while (xml.Read())
                        {
                            if (xml.NodeType == XmlNodeType.Element)
                            {
                                if (xml.LocalName == "Property")
                                {
                                    dic.Add(xml.GetAttribute("Name"), xml.GetAttribute("Value"));
                                }
                            }
                        }
                    }

                    _dic[Path.GetFileNameWithoutExtension(path)] = dic;
                }
                catch (XmlException)
                {

                }
            }

            // this.SetCurrentLanguage("Japanese");
            // this.SetCurrentLanguage("Chinese");
            this.SetCurrentLanguage("English");
        }

        public IEnumerable<string> Languages
        {
            get
            {
                var dic = new Dictionary<string, string>();

                foreach (string path in _dic.Keys.ToList())
                {
                    dic[System.IO.Path.GetFileNameWithoutExtension(path)] = path;
                }

                var pairs = dic.ToList();

                pairs.Sort((x, y) =>
                {
                    return x.Key.CompareTo(y.Key);
                });

                return pairs.Select(n => n.Value).ToArray();
            }
        }

        public string CurrentLanguage
        {
            get
            {
                return _currentLanguage;
            }
        }

        public void SetCurrentLanguage(string value)
        {
            if (!_dic.ContainsKey(value)) throw new ArgumentException();

            _currentLanguage = value;
            this.OnPropertyChanged(null);
        }

        public string Translate(string key)
        {
            if (_currentLanguage == null) return null;

            if (_dic[_currentLanguage].TryGetValue(key, out string result))
            {
                return Regex.Unescape(result);
            }

            return null;
        }
    }
}
