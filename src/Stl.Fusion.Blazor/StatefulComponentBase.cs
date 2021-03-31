using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Stl.Internal;

namespace Stl.Fusion.Blazor
{
    public abstract class StatefulComponentBase : ComponentBase, IDisposable, IHandleEvent
    {
        [Inject]
        protected IServiceProvider Services { get; set; } = null!;
        protected IStateFactory StateFactory => Services.StateFactory();
        protected bool OwnsState { get; set; } = true;
        protected internal abstract IState UntypedState { get; }
        protected Action<IState, StateEventKind> StateChanged { get; set; }
        protected StateEventKind StateHasChangedTriggers { get; set; } = StateEventKind.Updated;
        // It's typically much more natural for stateful components to recompute State
        // and trigger StateHasChanged only as a result of this or parameter changes.
        protected bool MustTriggerStateHasChangedOnEvent { get; set; } = false;

        public bool IsLoading => UntypedState == null! || UntypedState.Snapshot.UpdateCount == 0;
        public bool IsUpdating => UntypedState == null! || UntypedState.Snapshot.IsUpdating;
        public bool IsUpdatePending => UntypedState == null! || UntypedState.Snapshot.Computed.IsInvalidated();

        protected StatefulComponentBase()
        {
            StateChanged = (_, eventKind) => {
                if ((eventKind & StateHasChangedTriggers) == 0)
                    return;
                this.StateHasChangedAsync();
            };
        }

        public virtual void Dispose()
        {
            UntypedState.RemoveEventHandler(StateEventKind.All, StateChanged);
            if (OwnsState && UntypedState is IDisposable d)
                d.Dispose();
        }

        Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
        {
            // This code is copied from ComponentBase
            var task = callback.InvokeAsync(arg);
            var shouldAwaitTask =
                task.Status != TaskStatus.RanToCompletion &&
                task.Status != TaskStatus.Canceled;

            if (MustTriggerStateHasChangedOnEvent) // But this line is added
                StateHasChanged();

            return shouldAwaitTask ? CallStateHasChangedOnAsyncCompletion(task) : Task.CompletedTask;
        }

        private async Task CallStateHasChangedOnAsyncCompletion(Task task)
        {
            try {
                await task;
            }
            catch {
                // Avoiding exception filters for AOT runtime support.
                // Ignore exceptions from task cancelletions, but don't bother issuing a state change.
                if (task.IsCanceled)
                    return;
                throw;
            }
            StateHasChanged();
        }
    }

    public abstract class StatefulComponentBase<TState> : StatefulComponentBase, IDisposable
        where TState : class, IState
    {
        private TState? _state;

        protected internal override IState UntypedState => State;

        protected internal TState State {
            get => _state!;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (_state == value)
                    return;
                if (_state != null)
                    throw Errors.AlreadyInitialized(nameof(State));
                _state = value;
            }
        }

        protected override void OnInitialized()
        {
            // ReSharper disable once ConstantNullCoalescingCondition
            State ??= CreateState();
            UntypedState.AddEventHandler(StateEventKind.All, StateChanged);
        }

        protected virtual TState CreateState()
            => Services.GetRequiredService<TState>();
    }
}
