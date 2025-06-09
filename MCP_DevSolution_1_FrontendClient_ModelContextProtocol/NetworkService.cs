using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class NetworkService : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private CancellationTokenSource _receiveCts;
        private Task _receiveTask;

        public bool IsConnected => _tcpClient?.Connected ?? false;

        public event Action<string> DataReceived;
        public event Action<string> StatusChanged; // For messages like "Connecting...", "Connected", "Disconnected", "Error: ..."
        public event Action ConnectionEstablished;
        public event Action ConnectionLost;


        public NetworkService()
        {
            // Constructor can be light or initialize some defaults
        }

        public async Task<bool> ConnectAsync(string host, int port)
        {
            if (IsConnected)
            {
                StatusChanged?.Invoke("Already connected. Please disconnect first.");
                return false;
            }

            try
            {
                StatusChanged?.Invoke($"Connecting to {host}:{port}...");
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(host, port);

                if (!_tcpClient.Connected) // Double check after await
                {
                    StatusChanged?.Invoke("Failed to connect: Connection was not established.");
                    ConnectionLost?.Invoke();
                    CleanUpNetworkResources();
                    return false;
                }

                _networkStream = _tcpClient.GetStream();
                _receiveCts = new CancellationTokenSource();
                // Ensure the task is properly awaited or managed if it can throw unhandled exceptions.
                // For now, Task.Run starts it on a thread pool thread.
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);

                StatusChanged?.Invoke($"Connected to {host}:{port}.");
                ConnectionEstablished?.Invoke();
                return true;
            }
            catch (SocketException ex)
            {
                StatusChanged?.Invoke($"Connection error: {ex.Message} (SocketErrorCode: {ex.SocketErrorCode})");
                ConnectionLost?.Invoke();
                CleanUpNetworkResources();
                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Unexpected connection error: {ex.Message}");
                ConnectionLost?.Invoke();
                CleanUpNetworkResources();
                return false;
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested && _networkStream != null && _networkStream.CanRead && _tcpClient.Connected)
                {
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0)
                    {
                        StatusChanged?.Invoke("Disconnected by server (0 bytes read).");
                        ConnectionLost?.Invoke();
                        CleanUpNetworkResources();
                        break;
                    }
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    DataReceived?.Invoke(receivedData);
                }
            }
            catch (OperationCanceledException)
            {
                StatusChanged?.Invoke("Receive operation canceled (disconnecting).");
            }
            catch (IOException ex)
            {
                // Only report and cleanup if not initiated by cancellation
                if (!token.IsCancellationRequested)
                {
                    StatusChanged?.Invoke($"Network error during receive: {ex.Message}");
                    ConnectionLost?.Invoke();
                    CleanUpNetworkResources();
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    StatusChanged?.Invoke($"Unexpected error during receive: {ex.Message}");
                    ConnectionLost?.Invoke();
                    CleanUpNetworkResources();
                }
            }
            finally
            {
                // This finally block might be redundant if all exit paths call CleanUpNetworkResources or are due to cancellation.
                // However, it's a safeguard.
                if (!token.IsCancellationRequested && !IsConnected) // If loop exited and we are not connected anymore
                {
                     // StatusChanged?.Invoke("Receive loop terminated and connection lost."); // Already handled by specific cases
                     // ConnectionLost?.Invoke(); // Already handled
                     // CleanUpNetworkResources(); // Already handled
                }
            }
        }

        public async Task<bool> SendDataAsync(string data)
        {
            if (!IsConnected || _networkStream == null || !_networkStream.CanWrite)
            {
                StatusChanged?.Invoke("Cannot send data: Not connected or stream not writable.");
                return false;
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data + "\n");
                await _networkStream.WriteAsync(buffer, 0, buffer.Length);
                await _networkStream.FlushAsync();
                return true;
            }
            catch (IOException ex)
            {
                StatusChanged?.Invoke($"Error sending data: {ex.Message}");
                ConnectionLost?.Invoke();
                CleanUpNetworkResources();
                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Unexpected error sending data: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected && _receiveCts == null && _tcpClient == null) // Already disconnected and cleaned up
            {
                StatusChanged?.Invoke("Already disconnected.");
                return;
            }
            StatusChanged?.Invoke("Disconnecting...");
            CleanUpNetworkResources();
            StatusChanged?.Invoke("Disconnected."); // This might be premature if CleanUp is async or takes time
                                                 // ConnectionLost is invoked by CleanUp or its consequences.
        }

        private void CleanUpNetworkResources()
        {
            _receiveCts?.Cancel(); // Signal cancellation to ReceiveLoopAsync

            // _networkStream?.Close(); // Closing the TcpClient will close the stream.
            // _networkStream = null;

            _tcpClient?.Close(); // This closes the connection and the stream.
            _tcpClient = null;

            // It's good practice to wait for the receive task to complete after cancellation,
            // but do it with a timeout to avoid deadlocks.
            // However, Task.Run tasks might not be joinable in the same way as created Tasks.
            // For now, rely on cancellation token and prompt loop exit.
            // if (_receiveTask != null && _receiveTask.Status == TaskStatus.Running) { /* log or wait briefly */ }


            _receiveCts?.Dispose();
            _receiveCts = null;
            _networkStream = null; // Ensure stream is null after client is closed.

            // ConnectionLost event should be reliably raised when connection is actually confirmed to be lost.
            // Often, this is best done after attempting cleanup or when an error indicating loss occurs.
            // If Disconnect() is called, it's an intentional loss.
        }

        public void Dispose()
        {
            CleanUpNetworkResources();
            GC.SuppressFinalize(this); // If you add a finalizer ~NetworkService()
        }
    }
}
