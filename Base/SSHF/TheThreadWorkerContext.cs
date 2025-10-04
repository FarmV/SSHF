using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FVH.SSHF
{
    /// <summary>
    /// Управляет выделенным потоком с собственным циклом обработки задач,
    /// предоставляя доступ к нему через SynchronizationContext.
    /// </summary>
    public sealed class TheThreadWorkerContext : IAsyncDisposable
    {
        private readonly Thread                                            _thread;
        private readonly CancellationTokenSource                           _cts;
        private readonly Task                                              _threadCompletionTask;
        private readonly TaskCompletionSource                              _threadCompletionTaskSource; 
        private readonly ConcurrentQueue<WorkItem>                         _workItems;
        private readonly SemaphoreSlim                                     _workSignal;

        private ShutdownMode _shutdownMode;
        private State _state = State.Created;
        private enum State { Created = 1, Running = 2, ShutdownRequested = 3, Disposed = 4  }
        /// <summary>
        /// Контекст синхронизации, который отправляет задачи в этот выделенный поток.
        /// </summary>
        public SynchronizationContext Context { get; }
        /// <summary>
        /// Управляемый идентификатор выделенного потока.
        /// </summary>
        public TheThreadWorkerContext(ThreadPriority priority = ThreadPriority.Normal)
        {
            _cts                        = new CancellationTokenSource();
            _workItems                  = new ConcurrentQueue<WorkItem>();
            _workSignal                 = new SemaphoreSlim(0);
            _threadCompletionTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _threadCompletionTask       = _threadCompletionTaskSource.Task;

            TaskCompletionSource<SynchronizationContext> contextSource = new TaskCompletionSource<SynchronizationContext>(TaskCreationOptions.RunContinuationsAsynchronously);

            _thread = new Thread(ThreadProc)
            {
                Name         = $"FVH: Worker thread: {this.GetHashCode()}",
                IsBackground = false,
                Priority     = priority
            };

            _thread.UnsafeStart(parameter: contextSource);
            Context = contextSource.Task.GetAwaiter().GetResult();
        }

        private void ThreadProc(object? state)
        {
            Debug.Assert(state is not null);

            try
            {
                QueuedSynchronizationContext syncContext = new QueuedSynchronizationContext(this);
                SynchronizationContext.SetSynchronizationContext(syncContext);
                ((TaskCompletionSource<SynchronizationContext>)state).SetResult(syncContext);

                _ = Interlocked.Exchange<State>(ref _state, State.Running);

                CancellationToken token = _cts.Token;
             
                while(token.IsCancellationRequested is false)
                {
                    try { _workSignal.Wait(token); }
                    catch(OperationCanceledException) { break; }

                    while(_workItems.TryDequeue(out WorkItem workItem)) workItem.Callback(workItem.State);                    
                }

                if(_shutdownMode is ShutdownMode.DrainQueue)
                {
                    while(_workItems.TryDequeue(out WorkItem workItem)) workItem.Callback(workItem.State);                 
                }
            }
            finally { _threadCompletionTaskSource.SetResult(); }
        }

        /// <summary>
        /// Инициирует завершение работы потока и асинхронно ожидает его остановки.
        /// </summary>
        /// <param name="mode">Режим завершения: штатный или немедленный.</param>
        /// <param name="timeout">Максимальное время ожидания завершения потока.</param>
        /// <returns>Задача, которая завершается, когда поток остановлен.</returns>
        /// <exception cref="InvalidOperationException">Поток не был запущен или уже завершается.</exception>
        /// <exception cref="TimeoutException">Поток не смог завершиться за указанное время.</exception>
        public async Task ShutdownAsync(ShutdownMode mode, TimeSpan timeout)
        {
            State previousState = Interlocked.CompareExchange<State>(ref _state, State.ShutdownRequested, State.Running);

            if(previousState is not State.Running) return;
            
            _shutdownMode = mode;

            _cts.Cancel(); 

            _ = _workSignal.Release();

            try {  await _threadCompletionTask.WaitAsync(timeout); }
            finally { _ = Interlocked.Exchange<State>(ref _state, State.Disposed); _cts.Dispose(); }
        }
        /// <summary>
        /// Асинхронно освобождает ресурсы, инициируя штатное завершение потока
        /// с таймаутом по умолчанию в 650 миллисекунд.
        /// </summary>
        public async ValueTask DisposeAsync() => await ShutdownAsync(ShutdownMode.DrainQueue, TimeSpan.FromMilliseconds(650)).ConfigureAwait(false);        
        private sealed class QueuedSynchronizationContext(TheThreadWorkerContext owner) : SynchronizationContext
        {
            private readonly TheThreadWorkerContext _owner = owner;
            public override void Post(SendOrPostCallback d, object? state)
            {
                if(_owner._state >= State.ShutdownRequested) return;

                _owner._workItems.Enqueue(new WorkItem(callback: d, state: state));
                _ = _owner._workSignal.Release();
            }
            public override void Send(SendOrPostCallback _, object? __) { ThrowNotSupported();[DoesNotReturn] static void ThrowNotSupported() => throw new NotSupportedException($"{nameof(Send)} is not supported by this context."); }
            public override SynchronizationContext CreateCopy() => this;
        }
        private readonly struct WorkItem(SendOrPostCallback callback, object? state)
        {
            public readonly SendOrPostCallback Callback = callback;
            public readonly object?            State    = state;
        }
    }
    public enum ShutdownMode
    {
        /// <summary>
        /// Выполнить все задачи, находящиеся в очереди на момент запроса о завершении,
        /// и только после этого остановить поток. Новые задачи не принимаются.
        /// </summary>
        DrainQueue,

        /// <summary>
        /// Немедленно остановить поток, отбросив все задачи, ожидающие в очереди.
        /// </summary>
        Immediate
    }
}
