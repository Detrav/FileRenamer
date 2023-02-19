using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FileRenamer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly List<StorageFile> storageFiles = new List<StorageFile>();
        private StorageFolder targetFolder;

        public MainPage()
        {
            this.InitializeComponent();
            tbMask.Text = "{0:000}_ИмяФайла";
            nbStartFrom.Value = 0;
        }

        private async void btnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            var files = await picker.PickMultipleFilesAsync();

            storageFiles.AddRange(files);


            lbFiles.ItemsSource = null;
            lbFiles.ItemsSource = storageFiles;

        }

        private void btnSortByName_Click(object sender, RoutedEventArgs e)
        {
            var newList = storageFiles.OrderBy(m => m.Name).ToList();
            storageFiles.Clear();
            storageFiles.AddRange(newList);
            lbFiles.ItemsSource = null;
            lbFiles.ItemsSource = storageFiles;
        }

        private void btnSortByCreated_Click(object sender, RoutedEventArgs e)
        {
            var newList = storageFiles.OrderBy(m => m.DateCreated).ToList();
            storageFiles.Clear();
            storageFiles.AddRange(newList);
            lbFiles.ItemsSource = null;
            lbFiles.ItemsSource = storageFiles;
        }

        private void btnPutUp_Click(object sender, RoutedEventArgs e)
        {
            int i = lbFiles.SelectedIndex;
            if (i > 0)
            {
                var temp = storageFiles[i];
                storageFiles[i] = storageFiles[i - 1];
                storageFiles[i - 1] = temp;
                lbFiles.ItemsSource = null;
                lbFiles.ItemsSource = storageFiles;
                lbFiles.SelectedIndex = i - 1;
            }
        }

        private void btnPutDown_Click(object sender, RoutedEventArgs e)
        {
            int i = lbFiles.SelectedIndex;
            if (i < storageFiles.Count - 1)
            {
                var temp = storageFiles[i];
                storageFiles[i] = storageFiles[i + 1];
                storageFiles[i + 1] = temp;
                lbFiles.ItemsSource = null;
                lbFiles.ItemsSource = storageFiles;
                lbFiles.SelectedIndex = i + 1;
            }
        }

        private void btnRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            int i = lbFiles.SelectedIndex;
            if (i >= 0)
            {
                storageFiles.RemoveAt(i);
                lbFiles.ItemsSource = null;
                lbFiles.ItemsSource = storageFiles;
                if (i < storageFiles.Count)
                {
                    lbFiles.SelectedIndex = i;
                }
                else
                {
                    lbFiles.SelectedIndex = i - 1;
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            storageFiles.Clear();
            lbFiles.ItemsSource = null;
            lbFiles.ItemsSource = storageFiles;
        }

        private async void btnSelectTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            targetFolder = await folderPicker.PickSingleFolderAsync();
            if (targetFolder != null)
            {
                tbTargetFolder.Text = targetFolder.Path;
            }
            else
            {
                tbTargetFolder.Text = "";
            }
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (targetFolder == null)
            {
                await ShowDialogAsync("Ошибка! Сначала надо выбрать директорию куда сохранить файлы!");
                return;
            }
            if (storageFiles.Count == 0)
            {
                await ShowDialogAsync("Ошибка! Не выбраны файлы для переименования!");
                return;
            }

            var targetList = new List<(StorageFile file, string newName)>();

            string mask = tbMask.Text;
            int startIndex = (int)(Math.Floor(nbStartFrom.Value));

            foreach (var item in storageFiles)
            {
                targetList.Add((
                    item,
                    string.Format(mask, startIndex) + System.IO.Path.GetExtension(item.Name)


                    ));

                startIndex++;
            }

            var query = targetList.GroupBy(x => x.newName)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();

            if (query.Count > 0)
            {
                await ShowDialogAsync("Ошибка! Имена некоторых файлов пересекаются!\n" + String.Join("\n", query));
                return;
            }

            if (cbReplaceFiles.IsChecked != true)
            {
                List<string> errors = new List<string>();
                foreach (var item in targetList)
                {
                    var newPath = System.IO.Path.Combine(targetFolder.Path, item.newName);
                    if (File.Exists(newPath))
                    {
                        errors.Add(item.newName);
                    }
                }
                if (errors.Count > 0)
                {
                    await ShowDialogAsync("Ошибка! Следующие файлы уже существуют!\n" + String.Join("\n", errors));
                    return;
                }
            }

            foreach (var item in targetList)
            {
                var newPath = System.IO.Path.Combine(targetFolder.Path, item.newName);
                try
                {
                    File.Copy(item.file.Path, newPath, true);
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Ошибка! Файлы частично скопированы, но возникла ошибка!\n" + ex.Message);
                    return;
                }
            }

            await ShowDialogAsync("Операция переименования успешно выполнена!");

            nbStartFrom.Value = startIndex;
        }

        private async Task ShowDialogAsync(string v)
        {
            var dialog = new MessageDialog(v);
            await dialog.ShowAsync();
        }
    }
}
