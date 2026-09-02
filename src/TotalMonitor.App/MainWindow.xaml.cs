using System.Windows;
namespace TotalMonitor.App;
public partial class MainWindow : Window
{ public MainWindow(MainViewModel viewModel, ServerRealtimeClient realtime) { InitializeComponent(); DataContext = viewModel; Loaded += async (_, _) => { await viewModel.LoadAsync(); try { await realtime.StartAsync(); } catch { } }; } }
