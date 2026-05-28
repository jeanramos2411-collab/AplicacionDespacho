using AplicacionDespacho.utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AplicacionDespacho.Services
{
    public class PalletScanQueue
    {
        private readonly Queue<PalletScanRequest> _queue = new Queue<PalletScanRequest>();
        private bool _isProcessing = false;
        private readonly object _lock = new object();

        public event Action<PalletScanRequest> ProcessRequest;

        public void EnqueueScan(string palletNumber, string deviceId)
        {
            lock (_lock)
            {
                _queue.Enqueue(new PalletScanRequest
                {
                    PalletNumber = palletNumber,
                    DeviceId = deviceId,
                    Timestamp = FechaOperacionalHelper.ObtenerFechaOperacionalActual()
                });

                if (!_isProcessing)
                {
                    _isProcessing = true;
                    Task.Run(ProcessQueue);
                }
            }
        }

        private async Task ProcessQueue()
        {
            while (true)
            {
                PalletScanRequest request = null;

                lock (_lock)
                {
                    if (_queue.Count == 0)
                    {
                        _isProcessing = false;
                        return;
                    }
                    request = _queue.Dequeue();
                }

                if (request != null)
                {
                    ProcessRequest?.Invoke(request);
                    // Pequeña pausa entre procesamiento para evitar saturación  
                    await Task.Delay(500);
                }
            }
        }
    }

    public class PalletScanRequest
    {
        public string PalletNumber { get; set; }
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}