using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TorrentCs.Application.Pipelines;

public class PipelineBuilder
{
    private ImmutableList<Type> _stages = ImmutableList<Type>.Empty;

    public PipelineBuilder AddStage<T>() where T : IPipelineStage
    {
        _stages = _stages.Add(typeof(T));
        return this;
    }

    public IPipelineFactory Build(IServiceProvider services)
        => new PipelineStageFactory(services, _stages);

    private sealed class PipelineStageFactory : IPipelineFactory
    {
        private readonly IServiceProvider _services;
        private readonly ImmutableList<Type> _stageTypes;

        public PipelineStageFactory(IServiceProvider services, ImmutableList<Type> stageTypes)
        {
            _services = services;
            _stageTypes = stageTypes;
        }

        public IPipeline CreatePipeline(params object[] additionalDependencies)
        {
            var stages = _stageTypes
                .Select(t => (IPipelineStage)ActivatorUtilities.CreateInstance(
                    _services, t, additionalDependencies));
            return new Pipeline(
                _services.GetRequiredService<ILogger<Pipeline>>(),
                stages);
        }
    }
}
