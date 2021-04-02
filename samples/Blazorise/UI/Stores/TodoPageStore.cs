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
using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace Templates.Blazor2.UI.Stores
{
    /// <summary>
    /// [Action] methods synchronize to the Blazor component's SynchronizationContext via the
    ///   ISharedState instance that is injected in the constructor. Therefore all updates
    ///   triggered by Fusion are done in Action methods 
    /// </summary>
    [Observable]
    public class TodoPageStore
    {
        protected ITodoService TodoService = default!;
        protected FusionState<TodoPageGetResponse?> F;

        public string? PageResponseAsJson { get; set; }
        public ExceptionStore? FusionQueryException { get; set; }
        public ExceptionStore? FusionCommandException { get; set; }
        public TodoPageGetRequest? PageRequest { get; set; }
        public FusionStateStatusEnum FusionStateStatus { get; set; }

        public TodoPageStore(
            IStateFactory stateFactory,
            Session session,
            ITodoService todoService,
            ICommander commander)
        {
            if (todoService == null)
                throw new ArgumentNullException(nameof(todoService));
            TodoService = todoService;
            SetPageRequest(new TodoPageGetRequest { PageRef = new PageRef<string>(Count: 5) });
            F = new FusionState<TodoPageGetResponse?>(stateFactory, session, commander, ComputeState, OnLiveStateChanged);
            F.GoLive();
        }

        private void OnLiveStateChanged(FusionStateStatusEnum status)
        {
            Debug.WriteLine($"OnLiveStateChanged {status}. Todos: {F.LiveState?.UnsafeValue?.Todos?.Length} ");
            if (status == FusionStateStatusEnum.InSync && F.LiveState?.HasValue == true)
                SetPageResponse(F.LiveState?.Value);
            UpdateQueryException();
            SetFusionStatus(status);
        }

        protected async Task<TodoPageGetResponse?> ComputeState(CancellationToken cancellationToken)
        {
            if (TodoService == null || PageRequest == null)
                return null;

            var session = F.Session;
            return await TodoService.GetTodoPage(session, PageRequest, cancellationToken);
        }

        [Computed]
        public TodoPageGetResponse? PageResponse {
            get {
                TodoPageGetResponse? response;

                if (PageResponseAsJson == null)
                    response = null;
                else {
                    response = JsonConvert.DeserializeObject<TodoPageGetResponse>(PageResponseAsJson);
                    if (response == null)
                        Debugger.Break();
                }

                return response;
            }
        }

        [Computed]
        public DateTime? LastStateUpdateTimeUtc => PageResponse?.LastUpdatedUtc;

        [Computed]
        public bool HasMore => PageResponse?.HasMore == true;

        [Action]
        public void SetPageResponse(TodoPageGetResponse? todoPageResponse)
        {
            if (todoPageResponse == null)
                PageResponseAsJson = null;
            else
                // Consider to pass in the raw JSON from the RestClient when working in WebAssembly
                // saving the serialization
                // We do the serialization so that all the derived values are cached and matched by value
                // automatically saving rerenders if the object tree shape stays similar
                PageResponseAsJson = JsonConvert.SerializeObject(todoPageResponse);

            Debug.WriteLine($"PageResponseAsJson: {PageResponseAsJson}");
        }

        [Action]
        public void LoadMore()
        {
            if (PageRequest == null)
                // workaround: PageRequest can be non-nullable because the constructor cant assign a value, only [Actions] can.
                // But is initialized anyway in the constructor but intellisense does not see that
                return; 

            PageRequest.PageRef = PageRequest.PageRef with { Count = PageRequest.PageRef.Count * 2 };
            // BUG: Why is this not trigger a recompute/call and call to this.ComputeState?
            F.TryInvalidate();  // See if we can make an autorunner for this
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
                F.TryInvalidate();
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
            if (FusionQueryException == null && F.LiveState?.HasError != true)
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
            Debug.WriteLine($"Fusion state status: {status}");
        }

        [Action]
        public void SetPageRequest(TodoPageGetRequest request)
        {
            PageRequest = request;
        }
    }
}
