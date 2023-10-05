using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using CodeNameLoader.API.Core;
using CodeNameLoader.API.Requests;
using CodeNameLoader.API.Responses;

namespace CodeNameLoader;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _requestCTS = null;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        if(_requestCTS is null)
            _requestCTS = new CancellationTokenSource();
        else
        {
            _requestCTS?.Cancel();
            _requestCTS = new CancellationTokenSource();
        }

        _ = Debug_RedeemKeyRequest(_requestCTS.Token);
    }

#if DEBUG
    private async Task Debug_RedeemKeyRequest
        (CancellationToken cancellationToken = default)
    {
        try
        {
            var timeKeyRequest = new TimeKeyRequest()
            {
                Command = TimeKeyCommand.Redeem,
                UserId = new Guid("08dba174-9a64-406e-879c-104c5b6f9fde"),
                UserPassword = "DankBoii",
                Key = "NtpRKQpladvazOGN0l0yD0b"
            };

            var response = await Loader
                .SendRequest<TimeKeyResponse, TimeKeyRequest>
                (new Request<TimeKeyRequest>(timeKeyRequest)) ??
                throw new NullReferenceException("Failed to send request!");

            if (response.HasErrors)
            {
                foreach (var errorString in response.Errors ?? Array.Empty<string>())
                    Console.WriteLine(errorString);

                throw new Exception("Server response has errors.");
            }

            Console.WriteLine("Time Key Response:");
            Console.WriteLine("Time Key Response was successful!");
        }

        catch (NullReferenceException ex)
        {
            Console.WriteLine(ex.Message);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    private async Task Debug_SendUserAuthRequest
        (CancellationToken cancellationToken = default)
    {

        try
        {
            var userRequest = new UserRequest()
            {
                UserCommand = UserCommand.Authenticate,
                Name = "DiiTz",
                Email = "DebugUser123@mail.com",
                Password = "DankBoii",
                HardwareId = "DebugData"
            };

            var response = await Loader
                .SendRequest<UserResponse, UserRequest>
                (new Request<UserRequest>(userRequest)) ??
                throw new NullReferenceException("Failed to send request!");

            if (response.HasErrors)
            {
                foreach (var errorString in response.Errors ?? Array.Empty<string>())
                    Console.WriteLine(errorString);

                throw new Exception("Server response has errors.");
            }

            Console.WriteLine("User Auth Response:");
            Console.WriteLine($"Name: {response.ResponseObject?.Name}");
            Console.WriteLine($"Email: {response.ResponseObject?.Email}");
            Console.WriteLine($"Admin: {response.ResponseObject?.Admin}");
            Console.WriteLine($"Active: {response.ResponseObject?.Active}");
        }

        catch (NullReferenceException ex) 
        {
            Console.WriteLine(ex.Message);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }
#endif
}
