using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;

namespace Templates.Blazor2.Abstractions
{
    public class GetTodoPageRequest
    {
        public PageRef<string>? PageRef { get; set; } = null;
        public int PageSize { get; set; } = 5;
    }

    public class GetTodoPageResponse
    {
        public Todo[] Todos { get; set; } = default!;
        public int TotalItems { get; set; }
        public bool HasMore { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
    }

    public record Todo(string Id, string Title, bool IsDone = false)
    {
        public Todo() : this("", "") { }
    }

    public record AddOrUpdateTodoCommand(Session Session, Todo Item) : ISessionCommand<Todo>
    {
        public AddOrUpdateTodoCommand() : this(Session.Null, default(Todo)!) { }
    }

    public record RemoveTodoCommand(Session Session, string Id) : ISessionCommand<Unit>
    {
        public RemoveTodoCommand() : this(Session.Null, "") { }
    }

    public interface ITodoService
    {
        [CommandHandler]
        Task<Todo> AddOrUpdate(AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Remove(RemoveTodoCommand command, CancellationToken cancellationToken = default);

        [ComputeMethod]
        Task<Todo?> TryGet(Session session, string id, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<Todo[]> List(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<GetTodoPageResponse> GetTodoPage(Session session, GetTodoPageRequest request, CancellationToken cancellationToken = default);
    }
}
