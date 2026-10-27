using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BotUSB.Device;

namespace BotUSB.Server;

/// <summary>
/// TCP server for scripts (UoPilot and others). Each client sends JSON objects such as
/// <c>{ 'Keys': [148] }</c>; several clients may be connected at once.
/// </summary>
internal sealed class CommandServer(BotEngine engine) : IDisposable
{
    private readonly ConcurrentDictionary<int, TcpClient> _clients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private int _nextClientId;

    public IPEndPoint? Endpoint { get; private set; }

    public string? Error { get; private set; }

    public int ClientCount => _clients.Count;

    /// <summary>Starts listening, replacing any previous listener.</summary>
    public void Start(int port, bool allowLan)
    {
        Stop();

        var endpoint = new IPEndPoint(allowLan ? IPAddress.Any : IPAddress.Loopback, port);
        var listener = new TcpListener(endpoint);
        try
        {
            listener.Start();
        }
        catch (SocketException ex)
        {
            Error = ex.SocketErrorCode == SocketError.AddressAlreadyInUse
                ? $"Port {port} is already in use by another program"
                : ex.Message;
            AppLog.Error("Server not started: " + Error);
            return;
        }

        Error = null;
        _listener = listener;
        Endpoint = endpoint;
        _cts = new CancellationTokenSource();
        _acceptLoop = AcceptLoopAsync(listener, _cts.Token);
        AppLog.Info(allowLan ? $"Listening on port {port} (all network adapters)" : $"Listening on 127.0.0.1:{port}");
    }

    public void Stop()
    {
        if (_listener is null)
        {
            return;
        }

        _cts!.Cancel();
        _listener.Stop();

        foreach (TcpClient client in _clients.Values)
        {
            client.Dispose();
        }

        try
        {
            _acceptLoop?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // Stopping: errors no longer matter.
        }

        _cts.Dispose();
        _listener = null;
        _cts = null;
        _acceptLoop = null;
        Endpoint = null;
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            int id = Interlocked.Increment(ref _nextClientId);
            _clients[id] = client;
            _ = HandleClientAsync(id, client, cancellationToken);
        }
    }

    private async Task HandleClientAsync(int id, TcpClient client, CancellationToken cancellationToken)
    {
        string name = $"Client {id}";
        AppLog.Info($"{name} connected from {client.Client.RemoteEndPoint}");
        client.NoDelay = true;

        var framer = new CommandFramer();
        Decoder decoder = Encoding.UTF8.GetDecoder();
        byte[] bytes = new byte[4096];
        char[] chars = new char[Encoding.UTF8.GetMaxCharCount(bytes.Length)];
        var objects = new List<string>();

        try
        {
            NetworkStream stream = client.GetStream();
            while (true)
            {
                int read = await stream.ReadAsync(bytes, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                int count = decoder.GetChars(bytes, 0, read, chars, 0);
                objects.Clear();
                if (framer.Push(chars.AsSpan(0, count), objects) > 0)
                {
                    AppLog.Warning($"{name} sent data that is not JSON; it was ignored");
                }

                foreach (string json in objects)
                {
                    Handle(name, json);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // Disconnected or stopping.
        }
        finally
        {
            _clients.TryRemove(id, out _);
            client.Dispose();
            if (!cancellationToken.IsCancellationRequested)
            {
                AppLog.Info($"{name} disconnected");
            }
        }
    }

    private void Handle(string client, string json)
    {
        if (!LenientJson.TryParseCommand(json, out ClientCommand? command, out string? parseError))
        {
            AppLog.Error($"{client} sent invalid data ({parseError}): {Shorten(json)}");
            return;
        }

        if (AppSettings.Current.ShowReceivedData)
        {
            AppLog.Data($"{client}: {command}");
        }

        BotCommand? botCommand = command!.ToBotCommand(out string? error);
        if (botCommand is null)
        {
            AppLog.Error($"{client}: {error}: {Shorten(json)}");
            return;
        }

        engine.Enqueue(botCommand);
    }

    private static string Shorten(string text) => text.Length <= 120 ? text : text[..117] + "...";

    public void Dispose() => Stop();
}
