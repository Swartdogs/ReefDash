using System.IO;
using System.Net.Sockets;

namespace ReefDash;

public enum ElementType
{
    RobotValue,
    DashboardValue,
    Button
}

public class DashClient
{
    private string                  _serverIp;
    private int                     _serverPort;
    private TcpClient               _tcpClient;
    private NetworkStream           _stream;
    private StreamReader            _reader;
    private StreamWriter            _writer;
    private CancellationTokenSource _connectionCancellation;
    private bool                    _isConnected;
    private bool                    _isConnecting;

    public event Action<string> ServerResponseReceived;
    public event Action<string> ClientCommandTransmitted;
    public event Action<bool>   ConnectionStatusChanged;

    public bool IsConnected => _isConnected;

    public DashClient(string ip, int port)
    {
        _serverIp = ip;
        _serverPort = port;
        _tcpClient = new TcpClient();
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

            Console.WriteLine($"Updated server address/port: {_serverIp}:{_serverPort}");

            if (_isConnected)
            {
                Disconnect();
                StartConnectionLoop();
            }
        }
    }

    private void StartConnectionLoop()
    {
        _isConnecting = true;
        _connectionCancellation?.Cancel();
        _connectionCancellation = new CancellationTokenSource();
        var token = _connectionCancellation.Token;

        Console.WriteLine($"Starting connection loop to {_serverIp}:{_serverPort}");

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

            Console.WriteLine($"Trying to connect to {_serverIp}:{_serverPort}");

            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_serverIp, _serverPort);

            if (cancellationToken.IsCancellationRequested)
                return;

            Console.WriteLine("Connected to server");

            _isConnected = true;
            ConnectionStatusChanged?.Invoke(_isConnected);

            _stream = _tcpClient.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };
        }
        catch (Exception)
        {
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(_isConnected);
        }
    }

    private void Disconnect()
    {
        Console.WriteLine("Disconnecting from server");

        _isConnected = false;
        ConnectionStatusChanged?.Invoke(_isConnected);
        _tcpClient.Close();
    }

    public void SendQuery(ElementType type, int index)
    {
        if (_isConnected)
        {
            string t = type switch
            {
                ElementType.RobotValue     => "R",
                ElementType.DashboardValue => "D",
                ElementType.Button         => "B",
                _                          => string.Empty
            };

            if (t == string.Empty)
            {
                return;
            }

            _ = SendClientMessage($"QUERY:{t}{index}");
        }

    }

    public void SendGet(params int[] indices)
    {
        SendGet(indices as IEnumerable<int>);
    }

    public void SendGet(IEnumerable<int> indices)
    {
        if (_isConnected)
        {
            _ = SendClientMessage($"GET:{string.Join(",", indices)}");
        }
    }

    public void SendSet()
    {

    }

    public void SendEvent()
    {
        if (_isConnected)
        {
            _ = SendClientMessage($"EVENT:");
        }
    }

    public void SendButton()
    {

    }

    public void SendPing()
    {
        _ = SendClientMessage("PING");
    }

    private async Task SendClientMessage(string message)
    {
        try
        {
            ClientCommandTransmitted?.Invoke(message);
            await _writer.WriteLineAsync(message);

            await ReceiveServerMessage();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task ReceiveServerMessage()
    {
        try
        {
            string response = await _reader.ReadLineAsync() ?? string.Empty;
            ServerResponseReceived?.Invoke(response);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }

    private void StopConnectionLoop()
    {
        Console.WriteLine("Stopping connection loop");
        _connectionCancellation?.Cancel();
    }
}
