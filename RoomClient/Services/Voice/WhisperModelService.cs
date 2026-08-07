using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Services.Voice
{
    public class WhisperModelService
    {
        private readonly string _modelDirectory;
        private readonly string _modelPath;

        public WhisperModelService()
        {
            _modelDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "RoomClient",
                "Models");

            _modelPath = Path.Combine(
                _modelDirectory,
                "ggml-small.bin");
        }

        public string ModelPath => _modelPath;

        public bool IsModelInstalled()
        {
            return File.Exists(_modelPath);
        }

        public string GetModelDirectory()
        {
            return _modelDirectory;
        }
    }
}
