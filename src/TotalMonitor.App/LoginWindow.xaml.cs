using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TotalMonitor.App;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.Succeeded) && viewModel.Succeeded)
            {
                DialogResult = true;
            }
        };
    }

    private void PasswordBox_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox box)
            vm.SetPassword(box.Password);
    }

    private void LoginField_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm && vm.CanLogin)
        {
            e.Handled = true;
            vm.LoginCommand.Execute(null);
        }
    }
}
