using Mediator;
using OpenTalkie.Application.Abstractions.Repositories;

namespace OpenTalkie.Application.Settings.Queries;

public readonly record struct GetAudioManagerSettingsQuery : IQuery<AudioManagerSettingsState>;

public sealed class GetAudioManagerSettingsQueryHandler(IAudioManagerSettingsRepository repository)
    : IQueryHandler<GetAudioManagerSettingsQuery, AudioManagerSettingsState>
{
    public ValueTask<AudioManagerSettingsState> Handle(GetAudioManagerSettingsQuery query, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(repository.GetSettings());
    }
}
