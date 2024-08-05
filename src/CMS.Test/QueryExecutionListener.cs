using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Test
{
    public class QueryExecutionListener : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private IDisposable? listenerSubscription;
        
        private IDisposable? eventsSubscription;

        public static bool Enabled = false;

        public QueryExecutionListener()
        {
            if (Enabled)
            {
                Subscribe();
            }
        }

        public int ExecutedQueryCount { get; private set; }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DbLoggerCategory.Name)
            {
                eventsSubscription = listener.Subscribe(this);
            }
        }

        public void OnNext(KeyValuePair<string, object?> kvp)
        {
            if (kvp.Key == RelationalEventId.CommandExecuted.Name)
            {
                ExecutedQueryCount++;
                Debug.Print(ExecutedQueryCount.ToString());
            }
        }

        public void Subscribe()
        {
            listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void Unsubscribe()
        {
            eventsSubscription?.Dispose();
            listenerSubscription?.Dispose();
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }
}
