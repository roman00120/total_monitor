using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public sealed class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService authentication;
    private string username = string.Empty;
    private string password = string.Empty;
    private string error = string.Empty;
    private string status = string.Empty;
    private bool isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;
    public string Username { get => username; set { username = value; Notify(); } }
    public string Password { private get => password; set { password = value; Notify(); } }
    public string ErrorMessage { get => error; private set { error = value; Notify(); } }
    public string StatusMessage { get => status; private set { status = value; Notify(); } }
    public bool IsBusy { get => isBusy; private set { isBusy = value; Notify(); Notify(nameof(CanLogin)); } }
    public bool CanLogin => !IsBusy;
    public bool Succeeded { get; private set; }
    public ICommand LoginCommand { get; }

    public LoginViewModel(IAuthenticationService authentication)
    {
        this.authentication = authentication;
        LoginCommand = new AsyncCommand(LoginAsync);
    }

    private async Task LoginAsync()
    {
        if (IsBusy) return;
        Debug.WriteLine("LOGIN START");
        IsBusy = true;
        StatusMessage = "Iniciando sesión...";
        ErrorMessage = string.Empty;
        try
        {
            Debug.WriteLine("LOGIN → preparando request");
            Debug.WriteLine("LOGIN → POST /api/v1/auth/login");
            var result = await authentication.LoginAsync(Username, Password);
            Debug.WriteLine($"LOGIN → respuesta recibida: Success={result.Success}");
            if (!result.Success)
            {
                Debug.WriteLine("LOGIN → token obtenido/no obtenido: no obtenido");
                Debug.WriteLine("LOGIN → usuario obtenido/no obtenido: no obtenido");
                ErrorMessage = result.Message;
            }
            else
            {
                Debug.WriteLine("LOGIN → token obtenido/no obtenido: obtenido");
                Debug.WriteLine($"LOGIN → usuario obtenido/no obtenido: {(result.User is null ? "no obtenido" : "obtenido")}");
                Succeeded = true;
                Notify(nameof(Succeeded));
                Debug.WriteLine("LOGIN → navegación al dashboard");
            }
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"LOGIN TIMEOUT: {ex.Message}");
            ErrorMessage = "La solicitud tardó demasiado. Verifique que el servidor esté disponible.";
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"LOGIN HTTP REQUEST ERROR: {ex}");
            ErrorMessage = "No se pudo conectar con el servidor.";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOGIN UNEXPECTED ERROR: {ex}");
            ErrorMessage = "No fue posible iniciar sesión.";
        }
        finally
        {
            StatusMessage = string.Empty;
            Password = string.Empty;
            IsBusy = false;
            Debug.WriteLine("LOGIN END");
        }
    }

    public void SetPassword(string value) => Password = value;
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
