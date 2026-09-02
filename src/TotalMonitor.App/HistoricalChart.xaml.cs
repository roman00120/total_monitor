using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TotalMonitor.Core.Historical;
namespace TotalMonitor.App; public partial class HistoricalChart : UserControl { public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(nameof(Points), typeof(ObservableCollection<AggregatedPoint>), typeof(HistoricalChart)); public ObservableCollection<AggregatedPoint>? Points { get => (ObservableCollection<AggregatedPoint>?)GetValue(PointsProperty); set => SetValue(PointsProperty, value); } public HistoricalChart() => InitializeComponent(); }
