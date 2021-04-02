using Stl.Fusion.Extensions;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Net;
using Cortex.Net.Api;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;
using Templates.Blazor2.Abstractions;
using Newtonsoft.Json;
using Stl.CommandR;

namespace Templates.Blazor2.UI.Stores
{
    public class ExceptionStore
    {
        public ExceptionStore(Exception ex)
        {
            Message = ex.Message;
        }

        public string Message { get; set; }
    }

    [Observable]
    public class TodoPageStore
    {
        protected ITodoService TodoService = default!;
        protected LiveStateStore<GetTodoPageResponse> F;

        public string? GetTodoPageResponseAsJson { get; set; }
        public ExceptionStore? FusionQueryException { get; set; }
        public ExceptionStore? FusionCommandException { get; set; }
        public GetTodoPageRequest GetTodoPageRequest { get; set; }
        public FusionStateStatusEnum FusionStateStatus { get; set; }

        public TodoPageStore(ISharedState sharedState,
            IStateFactory stateFactory,
            Session session,
            ITodoService todoService,
            ICommander commander)
        {
            if (todoService == null)
                throw new ArgumentNullException(nameof(todoService));
            TodoService = todoService;
            SetGetTodoPageRequest(new GetTodoPageRequest { PageRef = new PageRef<string>(), PageSize = 5 });

            F = new LiveStateStore<GetTodoPageResponse>(sharedState, stateFactory, session, commander, ComputeState);
            F.OnLiveStateChanged += F_OnLiveStateChanged;
        }

        private void F_OnLiveStateChanged(object? sender, FusionStateStatusEnum status)
        {
            Debug.WriteLine($"OnLiveStateChanged {status}. Todos: {F.LiveState?.UnsafeValue?.Todos.Length} ");
            if (status == FusionStateStatusEnum.InSync)
                SetGetTodoPageResponse(F.LiveState?.UnsafeValue);
            UpdateQueryException();
            SetFusionStatus(status);
        }

        [Computed]
        public GetTodoPageResponse? GetTodoPageResponse {
            get {
                if (GetTodoPageResponseAsJson == null)
                    return null;
                else
                    return JsonConvert.DeserializeObject<GetTodoPageResponse>(GetTodoPageResponseAsJson);
            }
        }

        [Computed]
        public DateTime? LastStateUpdateTimeUtc => GetTodoPageResponse?.LastUpdatedUtc;

        [Computed]
        public bool HasMore => GetTodoPageResponse?.HasMore == true;

        [Action]
        public void SetGetTodoPageResponse(GetTodoPageResponse? getTodoPageResponse)
        {
            if (getTodoPageResponse == null)
                GetTodoPageResponseAsJson = null;
            else
                // Consider to pass in the raw JSON from the RestClient when working in WebAssembly
                // saving the serialization
                // We do the serialization so that all the derived values are cached and matched by value
                // automatically saving rerenders if the object tree shape stays similar
                GetTodoPageResponseAsJson = JsonConvert.SerializeObject(getTodoPageResponse);
        }

        protected async Task<GetTodoPageResponse> ComputeState(CancellationToken cancellationToken)
        {
            var response = await TodoService.GetTodoPage(F.Session, GetTodoPageRequest, cancellationToken);
            return response;
        }

        [Action]
        public void LoadMore()
        {
            GetTodoPageRequest.PageSize *= 2;
            F.Requery();  // See if we can make an autorunner for this
        }

        [Action]
        public async Task CreateTodo(string newTodoTitle)
        {
            var todo = new Todo(Id: "", newTodoTitle, IsDone: false);
            await Call(new AddOrUpdateTodoCommand(F.Session, todo));
        }

        [Action]
        public async Task ToggleDone(Todo todo)
        {
            todo = todo with { IsDone = !todo.IsDone };
            await Call(new AddOrUpdateTodoCommand(F.Session, todo));
        }

        [Action]
        public async Task UpdateTitle(Todo todo, string title)
        {
            title = title.Trim();
            if (todo.Title == title)
                return;
            todo = todo with { Title = title };
            await Call(new AddOrUpdateTodoCommand(F.Session, todo));
        }

        [Action]
        public async Task Remove(Todo todo)
        {
            await Call(new RemoveTodoCommand(F.Session, todo.Id));
        }

        protected async Task Call(ICommand command)
        {
            UpdateCommandException(null);
            try {
                await F.Commander.Call(command, cancellationToken: default);
                //TryInvalidate();
            }
            catch (Exception e) {
                UpdateCommandException(e);
            }
        }

        protected void UpdateCommandException(Exception? e)
        {
            if (FusionCommandException == null && e == null)
                return;

            if (e != null)
                FusionCommandException = new ExceptionStore(e);
            else
                FusionCommandException = null;
        }

        [Action]
        public void UpdateQueryException()
        {
            if (FusionQueryException == null && F.LiveState?.Error == null)
                return;

            if (F.LiveState?.Error != null)
                FusionQueryException = new ExceptionStore(F.LiveState.Error);
            else
                FusionQueryException = null;
        }

        [Action]
        public void SetFusionStatus(FusionStateStatusEnum status)
        {
            FusionStateStatus = status;
        }

        [Action]
        public void SetGetTodoPageRequest(GetTodoPageRequest request)
        {
            GetTodoPageRequest = request;
        }
    }
}
