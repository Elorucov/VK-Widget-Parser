using Newtonsoft.Json;
using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VK_Widget_Parser {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Render();
        }

        private void Render() {
            try {
                WidgetContainer.Visibility = Visibility.Visible;
                ErrorInfo.Visibility = Visibility.Collapsed;

                Widget widget = JsonConvert.DeserializeObject<Widget>(PlainText.Text);
                WidgetContainer.Child = WidgetRenderer.Render(widget);
            } catch (Exception ex) {
                WidgetContainer.Visibility = Visibility.Collapsed;
                ErrorInfo.Visibility = Visibility.Visible;
                ErrorInfo.Text = $"Error 0x{ex.HResult.ToString("x8")}\n{ex.Message}\n\n{ex.StackTrace}";
            }
        }

        private async void LoadSamples(object sender, RoutedEventArgs e) {
            string samplesPath = $"{Package.Current.InstalledPath}\\Samples";
            StorageFolder samplesFolder = await StorageFolder.GetFolderFromPathAsync(samplesPath);
            var samples = await samplesFolder.GetFilesAsync();
            SamplesList.ItemsSource = samples;
            SamplesList.SelectedIndex = 0;
        }

        private void LoadSample(object sender, SelectionChangedEventArgs e) {
            StorageFile sample = SamplesList.SelectedItem as StorageFile;
            PlainText.Text = File.ReadAllText(sample.Path);
            Render();
        }
    }
}
