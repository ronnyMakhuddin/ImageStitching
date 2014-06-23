using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ImageStitching
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PanoramaCreator panoramaCreator;

        public MainWindow()
        {
            InitializeComponent();

            panoramaCreator = new PanoramaCreator();
        }

        private void butReadImg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Multiselect = true;
            op.Title = "Wybierz obraz";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                "Portable Network Graphic (*.png)|*.png|" +
                "Wszystkie pliki (*.*)|*.*";

            if (op.ShowDialog() == true)
            {
                string[] filePath = op.FileNames;
                List<string> addedFilePath = new List<string>();

                foreach (string path in filePath)
                {
                    if (panoramaCreator.AddImage(path))
                    {
                        addedFilePath.Add(path);
                    }
                }

                if (addedFilePath.Count > 0)
                {
                    int lastSelectedIndex = comboBoxChooseImg.SelectedIndex;

                    for (int i = 0; i < addedFilePath.Count; i++)
                    {
                        ComboBoxItem cbi = new ComboBoxItem();
                        cbi.Content = addedFilePath[i];
                        comboBoxChooseImg.Items.Add(cbi);
                    }

                    int newSelectedIndex = panoramaCreator.GetImagesCount() - addedFilePath.Count;

                    imagePhoto.Source = panoramaCreator.GetImage(newSelectedIndex).GetSource();

                    if (lastSelectedIndex >= 0)
                    {
                        comboBoxChooseImg.SelectedIndex = newSelectedIndex;
                    }
                    else
                    {
                        comboBoxChooseImg.SelectedIndex = 0;
                    }
                }
            }
        }

        private void comboBoxChooseImg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxChooseImg.SelectedIndex > -1)
            {
                imagePhoto.Source = panoramaCreator.GetImage(comboBoxChooseImg.SelectedIndex).GetSource();
            }
        }

        private void butImageStitch_Click(object sender, RoutedEventArgs e)
        {
            MyImage panoramaImage;
            MyImage pointsMatch;
            string result = panoramaCreator.StitchImages(drawPoints.IsChecked == true, out panoramaImage, out pointsMatch);

            panorama.Source = panoramaImage != null ? panoramaImage.GetSource() : null;
            punkty.Source = pointsMatch != null ? pointsMatch.GetSource() : null;

            if (result.Length > 0)
            {
                MessageBox.Show(result);
            }
        }

        private void butImageNext_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxChooseImg.SelectedIndex < (panoramaCreator.GetImagesCount() - 1))
            {
                comboBoxChooseImg.SelectedIndex = (comboBoxChooseImg.SelectedIndex + 1);
                imagePhoto.Source = panoramaCreator.GetImage(comboBoxChooseImg.SelectedIndex).GetSource();
            }
        }

        private void butImagePrev_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxChooseImg.SelectedIndex > 0)
            {
                comboBoxChooseImg.SelectedIndex = (comboBoxChooseImg.SelectedIndex - 1);
                imagePhoto.Source = panoramaCreator.GetImage(comboBoxChooseImg.SelectedIndex).GetSource();
            }
        }

        private void removeImage_Click(object sender, RoutedEventArgs e)
        {
            int lastSelectedIndex = comboBoxChooseImg.SelectedIndex;

            if (lastSelectedIndex >= 0)
            {
                panoramaCreator.RemoveImage(comboBoxChooseImg.SelectedIndex);
                comboBoxChooseImg.Items.RemoveAt(comboBoxChooseImg.SelectedIndex);

                int newSelectedIndex = Math.Min(lastSelectedIndex, panoramaCreator.GetImagesCount() - 1);

                if (panoramaCreator.GetImagesCount() > 0)
                {
                    imagePhoto.Source = panoramaCreator.GetImage(newSelectedIndex).GetSource();
                    comboBoxChooseImg.SelectedIndex = newSelectedIndex;
                }
                else
                {
                    comboBoxChooseImg.SelectedIndex = -1;
                    imagePhoto.Source = null;
                }
            }
        }

        private void removeAll_Click(object sender, RoutedEventArgs e)
        {
            if (panoramaCreator.GetImagesCount() > 0)
            {
                panoramaCreator.ClearImages();

                comboBoxChooseImg.SelectedIndex = -1;
                comboBoxChooseImg.Items.Clear();
                imagePhoto.Source = null;
            }
        }

        private void savePanorama_Click(object sender, RoutedEventArgs e)
        {
            if (panoramaCreator.GetPanorama() != null)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Title = "Wybierz plik";
                saveDialog.Filter = "Portable Network Graphic (*.png)|*.png";

                if (saveDialog.ShowDialog() == true)
                {
                    panoramaCreator.GetPanorama().SaveImage(saveDialog.FileName);
                }
            }
            else
            {
                MessageBox.Show("Panorama nie jest utworzona");
            }
        }
    }
}
