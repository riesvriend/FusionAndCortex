using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.Services
{
    [ComputeService(typeof(ITodoService))]
    public class TodoService : ITodoService
    {
        private readonly IKeyValueStore _keyValueStore;
        private readonly IServerSideAuthService _auth;

        public TodoService(IKeyValueStore keyValueStore, IServerSideAuthService auth)
        {
            _keyValueStore = keyValueStore;
            _auth = auth;
        }

        // Commands

        public virtual async Task<Todo> AddOrUpdate(AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default)
        {
            if (Computed.IsInvalidating())
                return default!;
            var (session, todo) = command;
            var user = await _auth.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            if (string.IsNullOrEmpty(todo.Id))
                todo = todo with { Id = Ulid.NewUlid().ToString() };
            var key = GetTodoKey(user, todo.Id);
            await _keyValueStore.Set(key, todo, cancellationToken);
            return todo;
        }

        public virtual async Task Remove(RemoveTodoCommand command, CancellationToken cancellationToken = default)
        {
            if (Computed.IsInvalidating())
                return;
            var (session, id) = command;
            var user = await _auth.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            var key = GetTodoKey(user, id);
            await _keyValueStore.Remove(key, cancellationToken);
        }

        // Queries

        public virtual async Task<Todo?> TryGet(Session session, string id, CancellationToken cancellationToken = default)
        {
            var user = await _auth.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            var key = GetTodoKey(user, id);
            var todoOpt = await _keyValueStore.TryGet<Todo>(key, cancellationToken);
            return todoOpt.IsSome(out var todo) ? todo : null;
        }

        public virtual async Task<Todo[]> List(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default)
        {
            var user = await _auth.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            var keyPrefix = GetTodoKeyPrefix(user);
            var keys = await _keyValueStore.ListKeysByPrefix(keyPrefix, pageRef, cancellationToken);
            var tasks = keys.Select(key => _keyValueStore.TryGet<Todo>(key, cancellationToken));
            var todoOpts = await Task.WhenAll(tasks);
            return todoOpts.Where(todo => todo.HasValue).Select(todo => todo.Value).ToArray();
        }

        public virtual async Task<TodoPageGetResponse> GetTodoPage(Session session, TodoPageGetRequest request, CancellationToken cancellationToken = default)
        {
            var pageRef = request.PageRef ?? new PageRef<string>(Count: 5);
            var getOneMorePagePlusOne = pageRef with { Count = pageRef.Count + 1 };
            var items = await List(session, getOneMorePagePlusOne, cancellationToken);
            var response = new TodoPageGetResponse {
                HasMore = items.Length > pageRef.Count,
                LastUpdatedUtc = DateTime.UtcNow,
                TotalItems = 100, // todo: make computed method to get count
            };
            if (response.HasMore)
                items = items[0..pageRef.Count];
            response.Todos = items;
            return response;
        }
            
        // Private methods

        private string GetTodoKey(User user, string id)
        => $"{GetTodoKeyPrefix(user)}/{id}";

        private string GetTodoKeyPrefix(User user)
            => $"todo/{user.Id}/items";
    }
}
