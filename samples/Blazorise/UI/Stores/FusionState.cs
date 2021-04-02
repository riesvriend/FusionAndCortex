using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Net;
using Cortex.Net.Api;
using Stl.Async;
using Stl.CommandR;
using Stl.Fusion;
using Stl.Fusion.Blazor;
using Stl.Internal;
using Stl.Fusion.Authentication;

namespace Templates.Blazor2.UI.Stores
{
    public enum FusionStateStatusEnum { Loading, Updating, UpdatePending, InSync };

    /// <summary>
    /// Wrapper for use in a Cortrex store to keep subscribed to one or more
    /// FusionLiveStates
    /// </summary>
    public class FusionState<T> : IDisposable
    {
        protected IStateFactory StateFactory;
        public Session Session;
        public ICommander Commander;
        private bool _disposedValue;
        protected Func<CancellationToken, Task<T>> ComputeState;
        protected Action<IState, StateEventKind> HandleStateChangedInternal { get; set; }
        public ILiveState<T>? LiveState { get; set; }
        public FusionStateStatusEnum FusionStateStatus { get; set; }

        public FusionState(
            IStateFactory stateFactory,
            Session session,
            ICommander commander,
            Func<CancellationToken, Task<T>> computeState,
            Action<FusionStateStatusEnum> onLiveStateChanged)
        {
            StateFactory = stateFactory;
            Session = session;
            Commander = commander;
            ComputeState = computeState;
            HandleStateChangedInternal = (state, eventKind) => {
                var status = CurrentFusionStateStatus();
                //if (eventKind == StateEventKind.Updated)
                onLiveStateChanged(status);
            };
        }

        // Cant do this from constructur as the ComputeState delegate can be called before the constructor is finished causing a race condition
        public void GoLive()
        {
            LiveState = StateFactory.NewLive<T>(ConfigureState, computer: (_, ct) => ComputeState(ct), argument: this);
            ((IState)LiveState).AddEventHandler(StateEventKind.All, HandleStateChangedInternal);
        }

        public FusionStateStatusEnum CurrentFusionStateStatus()
        {
            FusionStateStatusEnum status;
            if (LiveState == null! || LiveState.Snapshot.UpdateCount == 0)
                status = FusionStateStatusEnum.Loading;
            else if (LiveState.Snapshot.IsUpdating)
                status = FusionStateStatusEnum.Updating;
            else if (LiveState.Snapshot.Computed.IsInvalidated())
                status = FusionStateStatusEnum.UpdatePending;
            else
                status = FusionStateStatusEnum.InSync;
            return status;
        }

        
        protected virtual void ConfigureState(LiveState<T>.Options options) { }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue) {
                if (disposing) {
                    ((IState?)LiveState)?.RemoveEventHandler(StateEventKind.All, HandleStateChangedInternal);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Typically you need to call this method after UI actions to ensure
        /// the update from server is requested instantly.
        /// </summary>
        /// <param name="cancelUpdateDelay">Cancels update delay, i.e. requests instant update.</param>
        /// <param name="cancellationDelay">The delay between this call and update delay cancellation.
        /// The default (null) means it's governed by <see cref="IUpdateDelayer{T}"/>, which does this
        /// in 50ms by default.</param>
        public void TryInvalidate(bool cancelUpdateDelay = true, TimeSpan? cancellationDelay = null)
        {
            LiveState?.Invalidate();
            if (cancelUpdateDelay)
                LiveState?.UpdateDelayer.CancelDelays(cancellationDelay);
        }
    }
}
