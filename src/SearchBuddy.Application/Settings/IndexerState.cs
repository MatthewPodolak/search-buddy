using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Settings
{
    public sealed class IndexerState
    {
        private readonly string _path;
        private readonly Lock _lock = new();

        private bool _enabled;
        private TaskCompletionSource _enabledTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenSource _disabledCts = new();

        public IndexerState(string path)
        {
            _path = path;
            _enabled = Load();

            if (_enabled)
            {
                _enabledTcs.TrySetResult();
            }
        }

        public bool IsEnabled
        {
            get { lock (_lock) { return _enabled; } }
        }

        public CancellationToken DisabledToken
        {
            get { lock (_lock) { return _disabledCts.Token; } }
        }

        public Task WaitUntilEnabledAsync(CancellationToken ct)
        {
            lock (_lock) { return _enabledTcs.Task.WaitAsync(ct); }
        }

        public void SetEnabled(bool value)
        {
            lock (_lock)
            {
                if (_enabled == value) { return; }

                _enabled = value;
                Save(value);

                if (value)
                {
                    _disabledCts.Dispose();
                    _disabledCts = new CancellationTokenSource();
                    _enabledTcs.TrySetResult();
                }
                else
                {
                    _enabledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _disabledCts.Cancel();
                }
            }
        }

        private bool Load()
        {
            try
            {
                return File.ReadAllText(_path).Trim() != "off";
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return true;
            }
        }

        private void Save(bool value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, value ? "on" : "off");
            File.Move(tmp, _path, overwrite: true);
        }
    }
}
