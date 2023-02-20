using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.IO;

namespace FileRenamer
{
    public partial class MainWindow : Window
    {
        private readonly List<string> storageFiles = new List<string>();
        private string? targetFolder;



        public MainWindow()
        {
            InitializeComponent();
            tbMask.Text = "{0:000}_ИмяФайла";
            nbStartFrom.Value = 0;
        }

        private async void btnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog picker = new OpenFileDialog();
            picker.AllowMultiple = true;
            var files = await picker.ShowAsync(this);
            if (files != null)
            {
                storageFiles.AddRange(files);
                lbFiles.Items = storageFiles.Select(m => Path.GetFileName(m));
            }

        }

        private void btnSortByName_Click(object sender, RoutedEventArgs e)
        {
            var newList = storageFiles.OrderBy(m => Path.GetFileName(m)).ToList();
            storageFiles.Clear();
            storageFiles.AddRange(newList);
            lbFiles.Items = storageFiles.Select(m => Path.GetFileName(m));
        }

        private void btnSortByCreated_Click(object sender, RoutedEventArgs e)
        {
            var newList = storageFiles.OrderBy(m => File.GetCreationTime(m)).ToList();
            storageFiles.Clear();
            storageFiles.AddRange(newList);
            lbFiles.Items = storageFiles.Select(m => Path.GetFileName(m));
        }

        private void btnPutUp_Click(object sender, RoutedEventArgs e)
        {
            int i = lbFiles.SelectedIndex;
            if (i > 0)
            {
                var temp = storageFiles[i];
                storageFiles[i] = storageFiles[i - 1];
                storageFiles[i - 1] = temp;
                lbFiles.Items = storageFiles.Select(m => Path.GetFileName(m));
                lbFiles.SelectedIndex = i - 1;
            }
        }

        private void btnPutDown_Click(object sender, RoutedEventArgs e)
        {
            int i = lbFiles.SelectedIndex;
            if (i >= 0 && i < storageFiles.Count - 1)
            {
                var temp = storageFiles[i];
                storageFiles[i] = storageFiles[i + 1];
                storageFiles[i + 1] = temp;
                lbFiles.Items = storageFiles.Select(m => Path.GetFileName(m));
                lbFiles.SelectedIndex = i + 1;
            }
        }

        private void btnRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            int i = lbFiles.SelectedIndex;
            if (i >= 0)
            {
                storageFiles.RemoveAt(i);
                lbFiles.Items = storageFiles.Select(m => Path.GetFileName(m));
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
            lbFiles.Items = storageFiles.Select(m => Path.GetFileName(m));
        }

        private async void btnSelectTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new OpenFolderDialog();

            targetFolder = await folderPicker.ShowAsync(this);
            if (!String.IsNullOrEmpty(targetFolder))
            {
                tbTargetFolder.Text = targetFolder;
            }
            else
            {
                tbTargetFolder.Text = "";
                targetFolder = null;
            }
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (targetFolder == null)
            {
                await ShowDialogAsync("Ошибка!", "Сначала надо выбрать директорию куда сохранить файлы!");
                return;
            }
            if (storageFiles.Count == 0)
            {
                await ShowDialogAsync("Ошибка!", "Не выбраны файлы для переименования!");
                return;
            }

            var targetList = new List<(string oldPath, string newName)>();

            string mask = tbMask.Text;
            int startIndex = (int)(Math.Floor(nbStartFrom.Value));

            foreach (var item in storageFiles)
            {
                targetList.Add((
                    item,
                    string.Format(mask, startIndex) + System.IO.Path.GetExtension(item)


                    ));

                startIndex++;
            }

            var query = targetList.GroupBy(x => x.newName)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();

            if (query.Count > 0)
            {
                await ShowDialogAsync("Ошибка!", "Имена некоторых файлов пересекаются!\n" + String.Join("\n", query));
                return;
            }

            if (cbReplaceFiles.IsChecked != true)
            {
                List<string> errors = new List<string>();
                foreach (var item in targetList.Select(m => m.newName))
                {
                    var newPath = System.IO.Path.Combine(targetFolder, item);
                    if (File.Exists(newPath))
                    {
                        errors.Add(item);
                    }
                }
                if (errors.Count > 0)
                {
                    await ShowDialogAsync("Ошибка!", "Следующие файлы уже существуют!\n" + String.Join("\n", errors));
                    return;
                }
            }

            foreach (var item in targetList)
            {
                var newPath = System.IO.Path.Combine(targetFolder, item.newName);
                try
                {
                    File.Copy(item.oldPath, newPath, true);
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Ошибка!", "Файлы частично скопированы, но возникла ошибка!\n" + ex.Message);
                    return;
                }
            }

            await ShowDialogAsync("Готово", "Операция переименования успешно выполнена!");

            nbStartFrom.Value = startIndex;
        }

        private async Task ShowDialogAsync(string title, string value)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxStandardWindow(title, value);
            await messageBoxStandardWindow.Show();
        }
    }
}
