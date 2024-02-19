using System;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;

namespace Insperex.EventHorizon.EventStreaming.Admins
{
    public class Admin<TM>
        where TM : ITopicMessage
    {
        private readonly ITopicAdmin<TM> _topicAdmin;
        private readonly ITopicResolver _topicResolver;

        public Admin(ITopicAdmin<TM> topicAdmin, ITopicResolver topicResolver)
        {
            _topicAdmin = topicAdmin;
            _topicResolver = topicResolver;
        }

        public async Task RequireTopicAsync(Type type, string name = default, CancellationToken ct = default)
        {
            await _topicAdmin.RequireTopicAsync(_topicResolver.GetTopic<TM>(type, name), ct);
        }

        public async Task DeleteTopicAsync(Type type, string name = default, CancellationToken ct = default)
        {
            await _topicAdmin.DeleteTopicAsync(_topicResolver.GetTopic<TM>(type, name), ct);
        }
    }
}
