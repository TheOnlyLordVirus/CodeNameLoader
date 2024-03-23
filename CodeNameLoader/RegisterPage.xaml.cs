using CodeNameLoader.API.Core;
using System.Windows.Controls;

namespace CodeNameLoader;

/// <summary>
/// Interaction logic for RegisterPage.xaml
/// </summary>
public partial class RegisterPage : Page
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        //_ = Loader.Debug_RegisterNewUser("NewEmail@Email.com", "Test User", "DankBoii");
        //_ = Loader.Debug_RedeemKeyRequest();
        //_ = Loader.Debug_DownloadCheatRequest();
        //Loader.Debug_LoadDllByFile();
        Loader.Debug_LoadDllByMem();
    }
}
