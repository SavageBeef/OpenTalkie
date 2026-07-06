namespace OpenTalkie.Application.Abstractions.Repositories;

public interface IAudioManagerSettingsRepository
{
    AudioManagerSettingsState GetSettings();
    IReadOnlyList<SettingOptionItem> GetOptions(AudioManagerSettingOption option);
    void SetOption(AudioManagerSettingOption option, string value);
}
