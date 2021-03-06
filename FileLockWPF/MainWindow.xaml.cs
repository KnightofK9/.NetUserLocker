﻿using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FileLockWPF.design;
using Microsoft.Win32;

namespace FileLockWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ImageInfo> imageInfos;
        private FaceLockService faceLockService;
        private Guid currentGuid;

        public MainWindow()
        {
            InitializeComponent();
            this.imageInfos = new ObservableCollection<ImageInfo>();
            this.ImageBox.ItemsSource = imageInfos;
            this.faceLockService = new FaceLockService();
            Console.WriteLine("Initialized!");
        }

        // for this code image needs to be a project resource
        private void LoadImage(string filePath)
        {
            var bitmapImage = new BitmapImage(new Uri(filePath));
            ImageInfo imageInfo = new ImageInfo();
            imageInfo.ImageData = bitmapImage;
            imageInfo.ImagePath = filePath;
            imageInfo.Title = filePath;
            imageInfos.Add(imageInfo);
        }

        private void btnAddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter =
                "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (String filePath in openFileDialog.FileNames)
                {
                    LoadImage(filePath);
                }
            }
        }

        private async void btnAddPerson_Click(object sender, RoutedEventArgs e)
        {
            String personeName = PersonName.Text;
            if (personeName.Trim() == "")
            {
                showMessageBox("Failed!", "Person name must not empty ");
                return;
            }
            List<String> imagePaths = GetImagePaths();
            var result = await faceLockService.intPersonalItemOnServiceAsync(personeName, imagePaths);
            if (result != null)
            {
                showMessageBox("Create person success!", "Person id " + result.PersonId);
                SetGuid(result.PersonId);
            }
            else
            {
                showMessageBox("Create person Failed!", "Unknown Error");
            }
        }

        private List<String> GetImagePaths()
        {
            return imageInfos.Select(k => k.ImagePath).ToList();
        }

        private void SetGuid(Guid guid)
        {
            this.currentGuid = guid;
            PersonGuid.Text = guid.ToString();
        }

        private void showMessageBox(String title, String caption)
        {
            MessageBoxResult result = MessageBox.Show(caption,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                return;
            }
        }

        private async void btnDecryptFile_Click(object sender, RoutedEventArgs e)
        {
            var imagePaths = GetImagePaths();
            if (imagePaths.Count == 0)
            {
                showMessageBox("Error", "You must add 1 image for verification!");
                return;
            }
            var image = imagePaths[0];
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == true)
            {
                String filePath = openFileDialog.FileName;
                if (await faceLockService.DecryptFile(image, filePath))
                {
                    showMessageBox("Sucess", "File Decrypted!");
                }
                else
                {
                    showMessageBox("Failed", "Face not match!");
                }
            }

            return;
        }

        private async void btnEncryptFile_Click(object sender, RoutedEventArgs e)
        {
            if (PersonGuid.Text.Trim() == "")
            {
                showMessageBox("Failed!", "Person Guid must define ");
                return;
            }


            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == true)
            {
                String filePath = openFileDialog.FileName;
                await faceLockService.EncryptFile(filePath, Guid.Parse(PersonGuid.Text));
            }
            else
            {
                return;
            }
            showMessageBox("Sucess", "File Encrypted");
            return;
        }

        private async void btnVerifyPerson_Click(object sender, RoutedEventArgs e)
        {
            List<String> imagePaths = GetImagePaths();
            if (imagePaths.Count == 0)
            {
                showMessageBox("Error", "You must add some image for verification!");
                return;
            }
            foreach (var imagePath in imagePaths)
            {
                Guid guid = await this.faceLockService.detectPerson(imagePath, Constant.GROUP_ID);
                if (guid != Guid.Empty)
                {
                    SetGuid(guid);
                    showMessageBox("Sucess", "Guid "+ guid.ToString());
                    return;
                }
            }
            showMessageBox("Failed", "No face match!");
        }

        private async void btnResetTraining_Click(object sender, RoutedEventArgs e)
        {
            await this.faceLockService.ResetTrainingAsync();
            showMessageBox("Sucess","Traning resetted!");

        }
    }
}