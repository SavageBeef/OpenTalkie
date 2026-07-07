using Android.Media;
using OpenTalkie.Application.Abstractions.Repositories;

namespace OpenTalkie.Infrastructure.Android.Platforms.Android.Infrastructure.Repositories;

public sealed class AudioManagerSettingsRepository : IAudioManagerSettingsRepository
{
    private static readonly Mode[] SupportedModes =
    [
        Mode.Normal,
        Mode.InCommunication
    ];

    public AudioManagerSettingsState GetSettings()
    {
        var mode = (Mode)Preferences.Get("AudioManagerMode", (int)Mode.Normal);
        if (!SupportedModes.Contains(mode))
        {
            mode = Mode.Normal;
        }

        return new AudioManagerSettingsState(CreateOption(mode));
    }

    public IReadOnlyList<SettingOptionItem> GetOptions(AudioManagerSettingOption option)
    {
        return option switch
        {
            AudioManagerSettingOption.Mode => SupportedModes.Select(CreateOption).ToArray(),
            _ => []
        };
    }

    public void SetOption(AudioManagerSettingOption option, string value)
    {
        switch (option)
        {
            case AudioManagerSettingOption.Mode:
                var parsedMode = Enum.Parse<Mode>(value);
                if (!SupportedModes.Contains(parsedMode))
                {
                    throw new NotSupportedException($"Unsupported audio manager mode: {parsedMode}");
                }

                Preferences.Set("AudioManagerMode", (int)parsedMode);
                break;
            default:
                throw new NotSupportedException($"Unsupported audio manager setting option: {option}");
        }
    }

    private static SettingOptionItem CreateOption(Mode mode)
    {
        string value = mode.ToString();
        return new(value, value);
    }
}
