using OpenTalkie.Presentation.ViewModels;

namespace OpenTalkie.Presentation.Views;

public partial class AudioManagerSettingsPage : ContentPage
{
    public AudioManagerSettingsPage(AudioManagerSettingsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
