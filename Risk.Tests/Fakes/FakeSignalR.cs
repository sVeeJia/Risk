using System.Security.Claims;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Risk.Api.Realtime;

namespace Risk.Tests.Fakes;

public sealed class FakeGroupManager : IGroupManager
{
    public List<(string ConnectionId, string Group)> Added { get; } = new();
    public List<(string ConnectionId, string Group)> Removed { get; } = new();

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Added.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Removed.Add((connectionId, groupName));
        return Task.CompletedTask;
    }
}

public sealed class FakeHubCallerContext : HubCallerContext
{
    public FakeHubCallerContext(string connectionId) => ConnectionId = connectionId;

    public override string ConnectionId { get; }
    public override string? UserIdentifier => null;
    public override ClaimsPrincipal? User => null;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;
    public override void Abort() { }
}

public sealed class FakeHubContext : IHubContext<GameHub>
{
    public List<(string Group, string Method, object?[] Args)> Sent { get; } = new();

    public FakeHubContext()
    {
        Clients = new FakeHubClients(this);
        Groups = new FakeGroupManager();
    }

    public IHubClients Clients { get; }
    public IGroupManager Groups { get; }

    private sealed class FakeHubClients : IHubClients
    {
        private readonly FakeHubContext _owner;

        public FakeHubClients(FakeHubContext owner) => _owner = owner;

        public IClientProxy Group(string groupName) => new RecordingProxy(_owner, groupName);

        public IClientProxy All => throw new NotSupportedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy User(string userId) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class RecordingProxy : IClientProxy
    {
        private readonly FakeHubContext _owner;
        private readonly string _group;

        public RecordingProxy(FakeHubContext owner, string group)
        {
            _owner = owner;
            _group = group;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            _owner.Sent.Add((_group, method, args));
            return Task.CompletedTask;
        }
    }
}