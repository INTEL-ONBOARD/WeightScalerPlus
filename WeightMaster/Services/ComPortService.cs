using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WeightMaster.Services
{
    /// <summary>
    /// Service for detecting, testing, and monitoring COM ports for weight scale data.
    /// </summary>
    public class ComPortService : IDisposable
    {
        private SerialPort? _activePort;
        private CancellationTokenSource? _monitorCts;
        private bool _disposed;

        public event EventHandler<ComPortDataEventArgs>? DataReceived;
        public event EventHandler<ComPortStatusEventArgs>? StatusChanged;

        /// <summary>
        /// Gets all available COM port names on the system.
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            try
            {
                return SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Tests if a COM port is accessible and optionally checks for incoming data.
        /// </summary>
        /// <param name="portName">The COM port name (e.g., "COM3")</param>
        /// <param name="baudRate">Baud rate (default: 9600)</param>
        /// <param name="timeoutMs">Timeout in milliseconds to wait for data (default: 2000)</param>
        /// <returns>TestResult with status and any received data</returns>
        public async Task<ComPortTestResult> TestPortAsync(string portName, int baudRate = 9600, int timeoutMs = 2000)
        {
            var result = new ComPortTestResult { PortName = portName };

            try
            {
                using var port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = timeoutMs,
                    WriteTimeout = timeoutMs
                };

                port.Open();
                result.CanOpen = true;

                // Try to read data within the timeout period
                var dataReceived = new List<string>();
                var cts = new CancellationTokenSource(timeoutMs);

                try
                {
                    await Task.Run(() =>
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                if (port.BytesToRead > 0)
                                {
                                    string data = port.ReadExisting();
                                    if (!string.IsNullOrEmpty(data))
                                    {
                                        dataReceived.Add(data);
                                        result.HasData = true;
                                    }
                                }
                                Thread.Sleep(50);
                            }
                            catch (TimeoutException)
                            {
                                break;
                            }
                        }
                    }, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when timeout occurs
                }

                result.ReceivedData = string.Join("", dataReceived);
                port.Close();
            }
            catch (UnauthorizedAccessException)
            {
                result.Error = "Port is in use by another application";
            }
            catch (ArgumentException ex)
            {
                result.Error = $"Invalid port name: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Scans all available ports and tests each one for data.
        /// Returns the port that has active data flowing.
        /// </summary>
        public async Task<ComPortTestResult?> FindActivePortAsync(int baudRate = 9600, int timeoutMs = 1500)
        {
            var ports = GetAvailablePorts();

            foreach (var port in ports)
            {
                var result = await TestPortAsync(port, baudRate, timeoutMs);
                if (result.HasData)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Starts monitoring a COM port for incoming data.
        /// </summary>
        public void StartMonitoring(string portName, int baudRate = 9600)
        {
            StopMonitoring();

            try
            {
                _activePort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _activePort.DataReceived += OnSerialDataReceived;
                _activePort.ErrorReceived += OnSerialErrorReceived;
                _activePort.Open();

                OnStatusChanged(portName, true, "Connected and monitoring");
            }
            catch (Exception ex)
            {
                OnStatusChanged(portName, false, $"Failed to open: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops monitoring the current COM port.
        /// </summary>
        public void StopMonitoring()
        {
            _monitorCts?.Cancel();

            if (_activePort != null)
            {
                try
                {
                    _activePort.DataReceived -= OnSerialDataReceived;
                    _activePort.ErrorReceived -= OnSerialErrorReceived;

                    if (_activePort.IsOpen)
                    {
                        _activePort.Close();
                    }

                    _activePort.Dispose();
                }
                catch { }

                _activePort = null;
            }
        }

        /// <summary>
        /// Checks if currently monitoring a port.
        /// </summary>
        public bool IsMonitoring => _activePort?.IsOpen ?? false;

        /// <summary>
        /// Gets the currently monitored port name.
        /// </summary>
        public string? CurrentPortName => _activePort?.PortName;

        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_activePort == null || !_activePort.IsOpen) return;

            try
            {
                string data = _activePort.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    DataReceived?.Invoke(this, new ComPortDataEventArgs
                    {
                        PortName = _activePort.PortName,
                        Data = data,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(_activePort.PortName, false, $"Read error: {ex.Message}");
            }
        }

        private void OnSerialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            OnStatusChanged(_activePort?.PortName ?? "Unknown", false, $"Serial error: {e.EventType}");
        }

        private void OnStatusChanged(string portName, bool isConnected, string message)
        {
            StatusChanged?.Invoke(this, new ComPortStatusEventArgs
            {
                PortName = portName,
                IsConnected = isConnected,
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        public void Dispose()
        {
            if (_disposed) return;

            StopMonitoring();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }

    public class ComPortTestResult
    {
        public string PortName { get; set; } = string.Empty;
        public bool CanOpen { get; set; }
        public bool HasData { get; set; }
        public string ReceivedData { get; set; } = string.Empty;
        public string? Error { get; set; }

        public bool IsSuccess => CanOpen && string.IsNullOrEmpty(Error);

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Error))
                return $"{PortName}: Error - {Error}";
            if (HasData)
                return $"{PortName}: Active (receiving data)";
            if (CanOpen)
                return $"{PortName}: Available (no data)";
            return $"{PortName}: Unknown status";
        }
    }

    public class ComPortDataEventArgs : EventArgs
    {
        public string PortName { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ComPortStatusEventArgs : EventArgs
    {
        public string PortName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
