using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace RegexTextEditor.Services
{
    public class FileService
    {
        public async Task<string> ReadTextAsync(StorageFile file)
        {
            return await FileIO.ReadTextAsync(file);
        }

        public async Task WriteTextAsync(StorageFile file, string text)
        {
            await FileIO.WriteTextAsync(file, text);
        }

        public string GetSafeFileName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Без имени";

            return Path.GetFileName(path);
        }
    }
}