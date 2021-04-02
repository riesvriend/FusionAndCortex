using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cortex.Net.Api;

namespace Templates.Blazor2.UI.Stores
{
    [Observable]
    public class AppStore
    {
        private Timer _timer = null!;

        public string? CurrentUtcTime { get; set; }

        public void OnTick(object source, ElapsedEventArgs e)
        {
            UpdateTime();
        }

        [Action]
        public void UpdateTime()
        {
            CurrentUtcTime = $"{DateTime.UtcNow:HH:mm:ss} UTC";
        }

        public void OnCreate()
        {
            UpdateTime();
            _timer = new Timer(interval: 1000);
            _timer.Elapsed += OnTick; // TODO: Check if there is a dispose
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }
    }
}
