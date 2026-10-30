using System.IO;
using MeetingBar.Windows.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MeetingBar.SettingsApp;

public sealed partial class MainWindow
{
    private readonly UpdateService _updateService = new();
    private readonly CancellationTokenSource _updateClosed = new();
    private UpdateRelease? _updateRelease;
    private string? _updateFile;
    private bool _updateBusy, _updateInstalling;
    private string _updateMessage = "La búsqueda se realiza únicamente al pulsar el botón.";
    private Button? _updateAction;
    private TextBlock? _updateStatus, _updateNotes;
    private ProgressBar? _updateProgress;

    private void BuildUpdatesCard()
    {
        var panel = new StackPanel { Spacing = 10 };
        _updateStatus = new TextBlock { Text = _updateMessage, TextWrapping = TextWrapping.Wrap };
        _updateNotes = new TextBlock { Text = _updateRelease?.Notes ?? "", TextWrapping = TextWrapping.Wrap };
        _updateProgress = new ProgressBar { Minimum = 0, Maximum = 100, Visibility = _updateBusy ? Visibility.Visible : Visibility.Collapsed, IsIndeterminate = _updateBusy };
        _updateAction = new Button { Content = _updateRelease == null ? "Buscar actualizaciones" : _updateFile == null ? "Descargar e instalar" : "Instalar actualización", IsEnabled = !_updateBusy };
        _updateAction.Click += async (_, _) => await UpdateActionAsync();
        panel.Children.Add(_updateStatus); panel.Children.Add(new ScrollViewer { Content = _updateNotes, MaxHeight = 180, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); panel.Children.Add(_updateProgress); panel.Children.Add(_updateAction);
        AddWideCard("Actualizaciones de MeetingBar", "Versión instalada: " + UpdateService.InstalledVersion + ". Fuente: última versión estable del repositorio de GitHub.", panel);
    }

    private void SetUpdateMessage(string message)
    {
        _updateMessage = message;
        if (_updateStatus != null) _updateStatus.Text = message;
    }

    private async Task UpdateActionAsync()
    {
        if (_updateBusy) return;
        _updateBusy = true;
        if (_updateAction != null) _updateAction.IsEnabled = false;
        if (_updateProgress != null) { _updateProgress.Visibility = Visibility.Visible; _updateProgress.IsIndeterminate = true; }
        try
        {
            if (_updateRelease == null)
            {
                SetUpdateMessage("Buscando actualizaciones…");
                var release = await _updateService.CheckAsync(_updateClosed.Token);
                if (release.Version > UpdateService.InstalledVersion)
                {
                    _updateRelease = release;
                    SetUpdateMessage("Nueva versión disponible: " + release.Tag);
                    if (_updateNotes != null) _updateNotes.Text = release.Notes;
                }
                else SetUpdateMessage("MeetingBar está actualizado. Última versión publicada: " + release.Tag);
                return;
            }
            SetUpdateMessage("Descargando y verificando el instalador… Cierra Ajustes para cancelar.");
            _updateFile ??= await _updateService.DownloadAsync(_updateRelease, new Progress<int>(value =>
                DispatcherQueue.TryEnqueue(() => { if (_updateProgress != null) { _updateProgress.IsIndeterminate = false; _updateProgress.Value = value; } })), _updateClosed.Token);
            _updateClosed.Token.ThrowIfCancellationRequested();
            SetUpdateMessage("Descarga verificada. Lista para instalar.");
            var confirm = new ContentDialog {
                XamlRoot = RootGrid.XamlRoot, Title = "Instalar " + _updateRelease.Tag,
                Content = "Se guardarán tus ajustes y se reiniciará MeetingBar. Se conservarán tus calendarios y servicios.",
                PrimaryButtonText = "Instalar y reiniciar", CloseButtonText = "Ahora no", DefaultButton = ContentDialogButton.Close
            };
            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
            CaptureCurrentPage(); SettingsStore.Save(_settings);
            var root = Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!.FullName;
            if (!File.Exists(Path.Combine(root, "MeetingBar.exe"))) throw new InvalidOperationException("No se encuentra la instalación de MeetingBar.");
            SettingsLog.Info("Update installation requested; target=" + _updateRelease.Tag);
            try { await SettingsStore.SendCommandAsync("prepare-update"); }
            catch { SettingsLog.Write("Update intent could not be delivered to the running app"); }
            _updateService.Install(_updateFile, _updateRelease, root);
            _updateInstalling = true;
            Application.Current.Exit();
        }
        catch (OperationCanceledException) { if (!_updateClosed.IsCancellationRequested) SetUpdateMessage("Se agotó el tiempo de espera. Vuelve a intentarlo."); }
        catch (Exception error) { SetUpdateMessage("No se pudo completar la actualización. " + error.Message); }
        finally
        {
            _updateBusy = false;
            if (_updateAction != null) { _updateAction.IsEnabled = true; _updateAction.Content = _updateRelease == null ? "Buscar actualizaciones" : _updateFile == null ? "Descargar e instalar" : "Instalar actualización"; }
            if (_updateProgress != null) _updateProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void DisposeUpdates()
    {
        _updateClosed.Cancel();
        if (!_updateInstalling && _updateFile != null)
            try { File.Delete(_updateFile); Directory.Delete(Path.GetDirectoryName(_updateFile)!); } catch { }
        _updateService.Dispose();
    }
}
