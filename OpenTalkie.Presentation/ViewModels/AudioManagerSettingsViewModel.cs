using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mediator;
using OpenTalkie.Application.Abstractions.Services;
using OpenTalkie.Application.Settings.Commands;
using OpenTalkie.Application.Settings.Queries;
using OpenTalkie.Application.Streams;
using OpenTalkie.Presentation.Abstractions.Services;

namespace OpenTalkie.Presentation.ViewModels;

public partial class AudioManagerSettingsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserDialogService _dialogService;
    private readonly IMicrophoneBroadcastService _microphoneBroadcastService;
    private readonly IPlaybackBroadcastService _playbackBroadcastService;
    private readonly IReceiverService _receiverService;

    [ObservableProperty]
    public partial string SelectedAudioManagerMode { get; set; } = string.Empty;

    public AudioManagerSettingsViewModel(
        IMediator mediator,
        IUserDialogService dialogService,
        IMicrophoneBroadcastService microphoneBroadcastService,
        IPlaybackBroadcastService playbackBroadcastService,
        IReceiverService receiverService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _microphoneBroadcastService = microphoneBroadcastService;
        _playbackBroadcastService = playbackBroadcastService;
        _receiverService = receiverService;

        _ = ReloadStateAsync();
    }

    [RelayCommand]
    private async Task EditField(string fieldName)
    {
        var option = MapOption(fieldName);

        if (option == null)
            return;

        var options = await _mediator.Send(new GetAudioManagerSettingOptionsQuery(option.Value));
        var currentValue = GetCurrentValue(option.Value);
        var labels = options.Select(item => item.DisplayName).ToArray();

        await _dialogService.ShowOptionsAsync(
            $"Choose {fieldName}",
            labels,
            async result =>
            {
                var selectedOption = options.FirstOrDefault(item => item.DisplayName == result);

                if (string.IsNullOrWhiteSpace(selectedOption.Value) || selectedOption.DisplayName == currentValue)
                    return;

                var updateResult = await _mediator.Send(new SetAudioManagerSettingOptionCommand(option.Value, selectedOption.Value));
                if (!updateResult.IsSuccess)
                {
                    await ShowErrorAsync(updateResult.ErrorMessage);
                    await ReloadStateAsync();
                    return;
                }

                SetCurrentValue(option.Value, selectedOption.DisplayName);
            });
    }

    [RelayCommand]
    private async Task RestartServices()
    {
        bool wasMicRunning = _microphoneBroadcastService.Status.Phase == StreamSessionPhase.Running;
        bool wasPlaybackRunning = _playbackBroadcastService.Status.Phase == StreamSessionPhase.Running;
        bool wasReceiverRunning = _receiverService.Status.Phase == StreamSessionPhase.Running;

        if (!wasMicRunning && !wasPlaybackRunning && !wasReceiverRunning)
        {
            await _dialogService.ShowErrorAsync("No active services are currently running to restart.");
            return;
        }

        var failures = new List<string>();

        if (wasMicRunning)
        {
            var stopResult = await _microphoneBroadcastService.SwitchAsync();
            AddFailureIfNeeded(failures, "Microphone", "stop", stopResult);
        }

        if (wasPlaybackRunning)
        {
            var stopResult = _playbackBroadcastService.Switch();
            AddFailureIfNeeded(failures, "Cast", "stop", stopResult);
        }

        if (wasReceiverRunning)
        {
            _receiverService.Switch();

            if (_receiverService.Status.Phase != StreamSessionPhase.Stopped)
                failures.Add(CreateFailureMessage("Receiver", "stop", _receiverService.Status.ErrorMessage));
        }

        await Task.Delay(500);

        if (wasMicRunning)
        {
            var startResult = await _microphoneBroadcastService.SwitchAsync();
            AddFailureIfNeeded(failures, "Microphone", "restart", startResult);

            if (_microphoneBroadcastService.Status.Phase != StreamSessionPhase.Running)
                failures.Add(CreateFailureMessage("Microphone", "restart", _microphoneBroadcastService.Status.ErrorMessage));
        }

        if (wasPlaybackRunning)
        {
            if (await _playbackBroadcastService.RequestPermissionAsync())
            {
                var startResult = _playbackBroadcastService.Switch();
                AddFailureIfNeeded(failures, "Cast", "restart", startResult);

                if (_playbackBroadcastService.Status.Phase != StreamSessionPhase.Running)
                    failures.Add(CreateFailureMessage("Cast", "restart", _playbackBroadcastService.Status.ErrorMessage));
            }
            else
            {
                failures.Add("Cast could not be restarted because screen audio capture permission was not granted.");
            }
        }

        if (wasReceiverRunning)
        {
            _receiverService.Switch();

            if (_receiverService.Status.Phase != StreamSessionPhase.Running)
                failures.Add(CreateFailureMessage("Receiver", "restart", _receiverService.Status.ErrorMessage));
        }

        if (failures.Count > 0)
        {
            await _dialogService.ShowErrorAsync(
                "Some services could not be restarted:\n" + string.Join("\n", failures.Distinct()));
            return;
        }

        await _dialogService.ShowErrorAsync("Active services restarted successfully.");
    }

    private async Task ReloadStateAsync()
    {
        try
        {
            var state = await _mediator.Send(new GetAudioManagerSettingsQuery());
            SelectedAudioManagerMode = state.SelectedMode.DisplayName;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
    }

    private static AudioManagerSettingOption? MapOption(string fieldName)
    {
        return fieldName switch
        {
            "AudioManagerMode" => AudioManagerSettingOption.Mode,
            _ => null
        };
    }

    private string GetCurrentValue(AudioManagerSettingOption option)
    {
        return option switch
        {
            AudioManagerSettingOption.Mode => SelectedAudioManagerMode,
            _ => string.Empty
        };
    }

    private void SetCurrentValue(AudioManagerSettingOption option, string value)
    {
        if (option == AudioManagerSettingOption.Mode)
            SelectedAudioManagerMode = value;
    }

    private async Task ShowErrorAsync(string? errorMessage)
    {
        await _dialogService.ShowErrorAsync(errorMessage ?? "Unable to update audio manager settings.");
    }

    private static void AddFailureIfNeeded(List<string> failures, string serviceName, string action, OperationResult result)
    {
        if (!result.IsSuccess)
        {
            failures.Add(CreateFailureMessage(serviceName, action, result.ErrorMessage));
        }
    }

    private static string CreateFailureMessage(string serviceName, string action, string? errorMessage)
    {
        return string.IsNullOrWhiteSpace(errorMessage)
            ? $"{serviceName} could not {action}."
            : $"{serviceName} could not {action}: {errorMessage}";
    }
}
