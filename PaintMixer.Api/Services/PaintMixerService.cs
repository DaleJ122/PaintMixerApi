namespace PaintMixer.Api
{
    public sealed class PaintMixerService : IPaintMixerService, IHostedService, IAsyncDisposable
    {
        private readonly PaintMixerDeviceEmulator _device;
        private bool _disposed;

        public PaintMixerService(IConfiguration configuration)
        {
            var seconds = configuration.GetValue("PaintMixer:ProcessingTimeSeconds", 15.0);
            _device = new PaintMixerDeviceEmulator
            {
                ProcessingTime = TimeSpan.FromSeconds(seconds)
            };
        }

        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

        public async Task StopAsync(CancellationToken ct) => await DisposeAsync();

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _device.DisposeAsync();
        }

        public int SubmitJob(int red, int black, int white, int yellow, int blue, int green)
            => _device.SubmitJob(red, black, white, yellow, blue, green);

        public int CancelJob(int jobCode) => _device.CancelJob(jobCode);

        public int QueryJobState(int jobCode) => _device.QueryJobState(jobCode);
    }
}
