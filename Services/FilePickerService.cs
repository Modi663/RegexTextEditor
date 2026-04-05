using Microsoft.UI.Xaml;
using RegexTextEditor.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RegexTextEditor.Services
{
    public class FilePickerService
    {
        public async Task<StorageFile?> PickOpenFileAsync(Window window)
        {
            FileOpenPicker picker = new();

            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add("*");

            InitializeWithWindow.Initialize(picker, WindowHandleHelper.GetHandle(window));

            return await picker.PickSingleFileAsync();
        }

        public async Task<StorageFile?> PickSaveFileAsync(Window window, string currentDocumentName)
        {
            FileSavePicker picker = new();

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Текстовый файл", new List<string> { ".txt" });

            picker.SuggestedFileName = currentDocumentName == "Без имени"
                ? "Новый документ"
                : Path.GetFileNameWithoutExtension(currentDocumentName);

            InitializeWithWindow.Initialize(picker, WindowHandleHelper.GetHandle(window));

            return await picker.PickSaveFileAsync();
        }
    }
}