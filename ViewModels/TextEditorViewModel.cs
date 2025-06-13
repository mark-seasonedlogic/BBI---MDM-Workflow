using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    public class TextEditorViewModel : ObservableObject
    {
        private string _fileName = string.Empty;
        private string _fileContent = string.Empty;

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string FileContent
        {
            get => _fileContent;
            set => SetProperty(ref _fileContent, value);
        }

        public string FullFilePath { get; private set; } = string.Empty;

        public void LoadFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                FullFilePath = filePath;
                FileName = Path.GetFileName(filePath);
                FileContent = File.ReadAllText(filePath);
            }
            else
            {
                FileName = "[File not found]";
                FileContent = "";
            }
        }

        public void SaveFile()
        {
            if (!string.IsNullOrWhiteSpace(FullFilePath))
            {
                File.WriteAllText(FullFilePath, FileContent);
            }
        }
    }

}
