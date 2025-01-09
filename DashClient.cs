using System.IO;
using System.Net.Sockets;

namespace ReefDash;

public class DashClient
{
    public enum EventType
    {
        Error,
        Notice
    }

    private string                  _serverIp;
    private int                     _serverPort;
    private TcpClient               _tcpClient;
    private NetworkStream           _stream;
    private StreamReader            _reader;
    private StreamWriter            _writer;
    private CancellationTokenSource _connectionCancellation;
    private CancellationTokenSource _pingCancellation;
    private CancellationTokenSource _dataCancellation;
    private CancellationTokenSource _eventCancellation;
    private bool                    _isConnected;
    private bool                    _isConnecting;
    private bool                    _sendDataEnabled;
    private bool                    _sendEventEnabled;

    public event Action<string>            DataReceived;
    public event Action<EventType, string> EventReceived;
    public event Action<string>            LogMessage;
    public event Action<bool>              ConnectionStatusChanged;

    public bool IsConnected => _isConnected;

    public DashClient(string ip, int port)
    {
        _serverIp = ip;
        _serverPort = port;
        _tcpClient = new TcpClient();
        _sendDataEnabled = true;
        _isConnected = false;
        _isConnecting = false;
    }

    public void Start()
    {
        if (!_isConnecting && !_isConnected)
        {
            StartConnectionLoop();
        }
    }

    public void Stop()
    {
        StopConnectionLoop();
        Disconnect();
    }

    public void SetServerAddress(string ip, int port)
    {
        if (_serverIp != ip || _serverPort != port)
        {
            _serverIp = ip;
            _serverPort = port;

            LogMessage?.Invoke($"Updated server address/port: {_serverIp}:{_serverPort}");

            if (_isConnected)
            {
                Disconnect();
                StartConnectionLoop();
            }
        }
    }

    public void EnableDataTransmission(bool enable)
    {
        _sendDataEnabled = enable;

        if (_sendDataEnabled)
        {
            LogMessage?.Invoke($"Starting data message transmission request");
            StartDataRequestLoop();
        }
        else
        {
            LogMessage?.Invoke($"Stopping data message transmission request");
            StopDataRequestLoop();
        }
    }

    public void EnableEventTransmission(bool enable)
    {
        _sendEventEnabled = enable;

        if (_sendEventEnabled)
        {
            LogMessage?.Invoke($"Starting event message transmission request");
            StartEventRequestLoop();
        }
        else
        {
            LogMessage?.Invoke($"Stopping event message transmission request");
            StopEventRequestLoop();
        }
    }

    private void StartConnectionLoop()
    {
        _isConnecting = true;
        _connectionCancellation?.Cancel();
        _connectionCancellation = new CancellationTokenSource();
        var token = _connectionCancellation.Token;

        LogMessage?.Invoke($"Starting connection loop to {_serverIp}:{_serverPort}");

        Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!_isConnected)
                    {
                        await TryConnectAsync(token);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
            finally
            {
                _isConnecting = false;
            }
        });
    }

    private async Task TryConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            LogMessage?.Invoke($"Trying to connect to {_serverIp}:{_serverPort}");

            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_serverIp, _serverPort);

            if (cancellationToken.IsCancellationRequested)
                return;

            LogMessage?.Invoke("Connected to server");

            _isConnected = true;
            ConnectionStatusChanged?.Invoke(_isConnected);

            _stream = _tcpClient.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };
            
            StartPingLoop();

            if (_sendDataEnabled)
            {
                StartDataRequestLoop();
            }

            if (_sendEventEnabled)
            {
                StartEventRequestLoop();
            }
        }
        catch (Exception)
        {
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(_isConnected);
        }
    }

    private void Disconnect()
    {
        LogMessage?.Invoke("Disconnecting from server");

        _isConnected = false;
        ConnectionStatusChanged?.Invoke(_isConnected);
        _pingCancellation?.Cancel();
        _dataCancellation?.Cancel();
        _eventCancellation?.Cancel();
        _tcpClient.Close();
    }

    private void StartPingLoop()
    {
        _pingCancellation?.Cancel();
        _pingCancellation = new CancellationTokenSource();
        var token = _pingCancellation.Token;

        LogMessage?.Invoke($"Starting ping loop");

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (_isConnected)
                {
                    await SendPingAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        });
    }

    private async Task SendPingAsync()
    {
        try
        {
            await _writer.WriteLineAsync("PING");
            LogMessage?.Invoke("Client: PING");

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1));
            var responseTask = _reader.ReadLineAsync();

            if (await Task.WhenAny(responseTask, timeoutTask) == timeoutTask)
            {
                LogMessage?.Invoke("PING timeout");
                // Timeout
                Disconnect();
            }
            else
            {
                string? response = await responseTask;
                LogMessage?.Invoke($"Server: {response}");

                if (response != "PONG")
                {
                    // Unexpected response, disconnect
                    Disconnect();
                }
            }
        }
        catch (Exception)
        {
            LogMessage?.Invoke("Ping exception");
            Disconnect();
        }
    }

    private void StartDataRequestLoop()
    {
        _dataCancellation?.Cancel();
        _dataCancellation = new CancellationTokenSource();
        var token = _dataCancellation.Token;

        LogMessage?.Invoke($"Starting data request loop");

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (_isConnected)
                {
                    await SendDataRequestAsync();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        });
    }

    private async Task SendDataRequestAsync()
    {
        try
        {
            await _writer.WriteLineAsync("DATA");
            LogMessage?.Invoke("Client: DATA");

            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(250));
            var responseTask = _reader.ReadLineAsync();

            if (await Task.WhenAny(responseTask, timeoutTask) == responseTask)
            {
                string? response = await responseTask;
                LogMessage?.Invoke($"Server: {response}");
                DataReceived?.Invoke(response ?? string.Empty);
            }
        }
        catch (Exception e)
        {
            LogMessage?.Invoke("Error receiving data request response: " + e.Message);
        }
    }

    private void StartEventRequestLoop()
    {
        _eventCancellation?.Cancel();
        _eventCancellation = new CancellationTokenSource();
        var token = _eventCancellation.Token;

        LogMessage?.Invoke($"Starting event request loop");

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (_isConnected)
                {
                    await SendEventRequestAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
        });
    }

    private async Task SendEventRequestAsync()
    {
        try
        {
            await _writer.WriteLineAsync("EVENT");
            LogMessage?.Invoke("Client: EVENT");

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(0.5));
            var responseTask = _reader.ReadLineAsync();

            if (await Task.WhenAny(responseTask, timeoutTask) == responseTask)
            {
                string? response = await responseTask;
                LogMessage?.Invoke($"Server: {response}");

                if (response?.StartsWith("EVENT:") ?? false)
                {
                    var events = response.Substring("EVENT:".Length).Split('|');

                    foreach (var e in events)
                    {
                        var parts = e.Split(",", 2);

                        var eventType = parts[0] switch
                        {
                            "notice" => EventType.Notice,
                            "error"  => EventType.Error,
                            _        => throw new Exception($"Unknown event type {parts[0]}")
                        };

                        EventReceived?.Invoke(eventType, parts[1]);
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogMessage?.Invoke($"Error receiving event request response: " + e.Message);
        }
    }

    private void StopConnectionLoop()
    {
        LogMessage?.Invoke("Stopping connection loop");
        _connectionCancellation?.Cancel();
    }

    private void StopDataRequestLoop()
    {
        LogMessage?.Invoke("Stopping data request loop");
        _dataCancellation?.Cancel();
    }

    private void StopEventRequestLoop()
    {
        LogMessage?.Invoke("Stopping event request loop");
        _eventCancellation?.Cancel();
    }
}
