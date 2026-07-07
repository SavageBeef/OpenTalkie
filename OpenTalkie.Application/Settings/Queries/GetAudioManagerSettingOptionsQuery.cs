using Mediator;
using OpenTalkie.Application.Abstractions.Repositories;

namespace OpenTalkie.Application.Settings.Queries;

public readonly record struct GetAudioManagerSettingOptionsQuery(AudioManagerSettingOption Option)
    : IQuery<IReadOnlyList<SettingOptionItem>>;

public sealed class GetAudioManagerSettingOptionsQueryHandler(IAudioManagerSettingsRepository repository)
    : IQueryHandler<GetAudioManagerSettingOptionsQuery, IReadOnlyList<SettingOptionItem>>
{
    public ValueTask<IReadOnlyList<SettingOptionItem>> Handle(GetAudioManagerSettingOptionsQuery query, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(repository.GetOptions(query.Option));
    }
}
