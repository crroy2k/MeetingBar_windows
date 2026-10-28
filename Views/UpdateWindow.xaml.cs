using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MeetingBar.Windows.Services;

namespace MeetingBar.Windows.Views
{
    public partial class UpdateWindow : Window
    {
        private readonly UpdateService _service;
        private readonly CancellationTokenSource _closed = new CancellationTokenSource();
        private UpdateRelease _release;
        private bool _busy;
        private string _downloadedFile;
        private bool _installing;
        public UpdateWindow(UpdateService service)
        {
            InitializeComponent(); _service = service;
            VersionText.Text = "Versión instalada: " + UpdateService.InstalledVersion;
            Loaded += async (s, e) => await CheckAsync();
            Closed += (s, e) =>
            {
                _closed.Cancel();
                if (!_installing && _downloadedFile != null)
                    try { System.IO.File.Delete(_downloadedFile); System.IO.Directory.Delete(System.IO.Path.GetDirectoryName(_downloadedFile)); } catch { }
            };
        }

        private async Task CheckAsync()
        {
            _busy = true; ActionButton.IsEnabled = false; _release = null;
            StatusText.Text = "Buscando la versión marcada como Latest en GitHub…";
            try
            {
                var release = await _service.CheckAsync(_closed.Token);
                if (release.Version > UpdateService.InstalledVersion)
                {
                    _release = release; StatusText.Text = "Disponible: " + release.Tag;
                    NotesText.Text = release.Notes;
                    ActionButton.Content = "Descargar e instalar";
                }
                else
                {
                    StatusText.Text = "MeetingBar está actualizado. No hay una versión posterior disponible.";
                    NotesText.Text = "Última versión publicada: " + release.Tag;
                    ActionButton.Content = "Buscar de nuevo";
                }
            }
            catch (OperationCanceledException) { if (!_closed.IsCancellationRequested) StatusText.Text = "Se agotó el tiempo de comprobación. Inténtalo más tarde."; }
            catch (Exception error) { StatusText.Text = "No se pudo comprobar la actualización. " + error.Message; }
            finally { _busy = false; ActionButton.IsEnabled = true; }
        }

        private async void Action_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            if (_release == null) { await CheckAsync(); return; }
            _busy = true; ActionButton.IsEnabled = false; DownloadProgress.Visibility = Visibility.Visible;
            try
            {
                StatusText.Text = "Descargando y verificando el instalador… Puedes cerrar esta ventana para cancelar.";
                if (_downloadedFile == null)
                    _downloadedFile = await _service.DownloadAsync(_release, new Progress<int>(value => DownloadProgress.Value = value), _closed.Token);
                _closed.Token.ThrowIfCancellationRequested();
                StatusText.Text = "Descarga verificada. Lista para instalar.";
                if (MessageBox.Show(this, "Se instalará " + _release.Tag + " y se reiniciará MeetingBar. Se conservarán tus calendarios y ajustes. Guarda los cambios pendientes de Configuración antes de continuar.", "Instalar actualización", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                { ActionButton.Content = "Instalar actualización"; return; }
                _service.Install(_downloadedFile, _release);
                _installing = true;
                Application.Current.Shutdown();
            }
            catch (OperationCanceledException) { if (!_closed.IsCancellationRequested) StatusText.Text = "Se agotó el tiempo de descarga. Puedes intentarlo de nuevo."; }
            catch (Exception error) { StatusText.Text = "No se pudo instalar la actualización. " + error.Message; }
            finally { _busy = false; ActionButton.IsEnabled = true; DownloadProgress.Visibility = Visibility.Collapsed; }
        }
    }
}
