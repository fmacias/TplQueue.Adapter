using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.ObjectModel;

namespace Fmacias.TplQueue.Observers.ViewModel
{
    /// <summary>
    /// UI-friendly observer for <see cref="IJobEvent"/> streams.
    /// <para>
    /// Collects human-readable log lines in <see cref="ProgressEvents"/> and ensures updates
    /// are marshalled onto a UI (or other synchronization) context via an
    /// injected <see cref="IObserverDispatcher"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is frontend-agnostic. It does not reference any specific UI
    /// framework (WPF/WinUI/MAUI). Instead, it requires an <see cref="IObserverDispatcher"/>
    /// implementation supplied by your UI layer (e.g., a wrapper over WPF <c>Dispatcher</c>
    /// or WinUI <c>DispatcherQueue</c>). That keeps the observer testable and portable.
    /// </para>
    /// <para>
    /// Observer contract:
    /// <list type="bullet">
    ///   <item><description><see cref="OnNext(IJobEvent)"/> is called zero or more times
    ///   as the queue emits lifecycle events (Enqueued, Running, Completed, etc.).</description></item>
    ///   <item><description><see cref="OnError(Exception)"/> is called at most once to signal
    ///   a terminal failure of the observable stream.</description></item>
    ///   <item><description><see cref="OnCompleted()"/> is called at most once to signal
    ///   normal completion of the stream (e.g., the queue is disposed and will not produce more events).</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Thread-safety: all modifications to <see cref="ProgressEvents"/> occur inside
    /// <see cref="IObserverDispatcher.Invoke(Action)"/> so UI-bound collections are updated correctly
    /// from the appropriate context.
    /// </para>
    /// </remarks>
    public sealed class ViewModelObserver : IViewModelObserver
    {
        /// <summary>
        /// Append-only log of formatted event lines, suitable for simple UI binding.
        /// Each entry is added from the dispatcher context to avoid cross-thread violations.
        /// </summary>
        public ObservableCollection<string> ProgressEvents { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Captures raw exceptions raised by the observable stream. Errors are also
        /// rendered into <see cref="ProgressEvents"/> for UI-friendly display.
        /// </summary>
        public ObservableCollection<Exception> ErrorEvents { get; } = new ObservableCollection<Exception>();

        /// <summary>
        /// Completion notifications recorded as human-readable messages.
        /// </summary>
        public ObservableCollection<string> CompleteEvents { get; } = new ObservableCollection<string>();
        /// <summary>
        /// Dispatcher used to marshal callbacks to the UI or synchronization context.
        /// </summary>
        /// <remarks>
        /// The concrete instance should be provided by your UI layer:
        /// for example, a WPF adapter that calls <c>System.Windows.Threading.Dispatcher.Invoke</c>,
        /// a WinUI adapter that calls <c>DispatcherQueue.TryEnqueue</c>, or a test adapter that
        /// executes the action inline.
        /// </remarks>
        private readonly IObserverDispatcher _dispatcher;
        private bool _isCompleted;
        /// <summary>
        /// Initializes a new instance of <see cref="ViewModelObserver"/>.
        /// </summary>
        /// <param name="dispatcher">
        /// UI/synchronization dispatcher abstraction. This belongs to a concrete implementation
        /// of your UI engine (e.g., WPF/WinUI/MAUI) and is responsible for invoking actions
        /// on the correct thread/context.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dispatcher"/> is <c>null</c>.</exception>
        private ViewModelObserver(IObserverDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public static ViewModelObserver Create(IObserverDispatcher observerDispatcher)
        {
            return new ViewModelObserver(observerDispatcher);
        }


        /// <summary>
        /// Receives a task-runner event and appends a formatted line to <see cref="ProgressEvents"/>
        /// on the dispatcher context.
        /// </summary>
        /// <param name="value">The emitted <see cref="IJobEvent"/>.</param>
        public void OnNext(IJobEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_isCompleted) return;
            Dispatch(() => ProgressEvents.Add(FormatEvent(value)));
        }

        /// <summary>
        /// Receives a terminal error and appends an error line to <see cref="ProgressEvents"/>
        /// on the dispatcher context.
        /// </summary>
        /// <param name="error">The terminal exception for the stream.</param>
        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            if (_isCompleted) return;

            Dispatch(() =>
            {
                ErrorEvents.Add(error);
                ProgressEvents.Add(FormatError(error));
            });
        }

        /// <summary>
        /// Receives a terminal completion signal and appends a completion line to
        /// <see cref="ProgressEvents"/> on the dispatcher context.
        /// </summary>
        public void OnCompleted()
        {
            if (_isCompleted) return;
            _isCompleted = true;

            Dispatch(() =>
            {
                var completionMessage = FormatCompletion();
                ProgressEvents.Add(completionMessage);
                CompleteEvents.Add(completionMessage);
            });
        }
        private void Dispatch(Action action)
        {
            _dispatcher.Invoke(action ?? throw new ArgumentNullException(nameof(action)));
        }

        private static string FormatEvent(IJobEvent value)
        {
            var runnerName = value.JobInfo?.Name ?? "<unknown>";
            var status = value.Status.ToString();
            var timestamp = value.Timestamp.ToString("O");
            var retries = value.RetryCount;
            var exception = value.Exception != null
                ? $" | Error={value.Exception.GetType().Name}: {value.Exception.Message}"
                : string.Empty;

            return $"[{timestamp}] Status={status} Runner={runnerName} Retries={retries}{exception}";
        }

        private static string FormatError(Exception error)
            => $"[ERROR] {error.GetType().Name}: {error.Message}";

        private static string FormatCompletion()
            => $"Observer [{nameof(ViewModelObserver)}] COMPLETED";
    }
}
