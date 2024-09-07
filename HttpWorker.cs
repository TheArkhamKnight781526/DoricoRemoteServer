namespace Dorico_Remote;

using DoricoNet;
using DoricoNet.Commands;
using DoricoNet.Comms;
using DoricoNet.Responses;
using Lea;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using DoricoNet.Exceptions;
using System.Reflection;

public class HttpWorker : BackgroundService
{
    private readonly HttpListener _httpListener;
    private readonly IServiceProvider _serviceProvider;
    private IDoricoRemote? _remoteInstance;
    private static readonly string DoricoRemoteDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dorico_remote/");
    private static readonly string DoricoTokenFile = Path.Combine(DoricoRemoteDirectory, ".token");
    private static readonly string DoricoLogFile = Path.Combine(DoricoRemoteDirectory, "log.txt");
    private static readonly string DoricoPortFile = Path.Combine(DoricoRemoteDirectory, ".port");
    private int port = 5000;

    public HttpWorker()
    {
        // Change the port number if specified in the config file

        if (File.Exists(DoricoPortFile)) {
            port = int.Parse(File.ReadAllText(DoricoPortFile));
        }        

        // Create an HttpListener and specify the URL it will listen on
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{port}/");

        var serilog = new LoggerConfiguration().WriteTo.Console().WriteTo.File(DoricoLogFile, rollingInterval: RollingInterval.Day).CreateLogger();
        Log.Logger = serilog;

        // Setup Dependency Injection for DoricoRemote
        var services = new ServiceCollection()
            .AddSingleton(sp => new LoggerFactory().AddSerilog(serilog).CreateLogger("DoricoRemote"))
            .AddSingleton<IEventAggregator, EventAggregator>()
            .AddTransient<IClientWebSocketWrapper, ClientWebSocketWrapper>()
            .AddSingleton<IDoricoCommsContext, DoricoCommsContext>()
            .AddTransient<IDoricoRemote, DoricoRemote>();

        _serviceProvider = services.BuildServiceProvider();
    }

    // Start the background service
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _httpListener.Start();
        Log.Information($"HTTP Server started on http://localhost:{port}/");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait for an incoming request
            var context = await _httpListener.GetContextAsync();

            // Process the request and send command to Dorico if necessary
            var responseMessage = await HandleRequest(context.Request);

            // Send a response back to the client
            var buffer = Encoding.UTF8.GetBytes(responseMessage);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, stoppingToken);
            context.Response.OutputStream.Close();
        }

        _httpListener.Stop();
    }

    // This method processes incoming requests and sends commands to Dorico
    private async Task<string> HandleRequest(HttpListenerRequest request)
    {
        var body = await new StreamReader(request.InputStream).ReadToEndAsync();
        DoricoCommandName command = JsonSerializer.Deserialize<DoricoCommandName>(body)!;

        if (!string.IsNullOrEmpty(command.command))
        {
            // Based on the command, send it to Dorico
            Log.Information($"Received command: {command}");

            bool result = false;

            if (command.command == "ToggleProperty") {
                ToggleProperty toggleProperty = JsonSerializer.Deserialize<ToggleProperty>(body)!;
                result = await ToggleProperty(toggleProperty.property, toggleProperty.property_value, toggleProperty.on_command, toggleProperty.off_command);
            } else {
                DoricoCommand sendCommand = JsonSerializer.Deserialize<DoricoCommand>(body)!;
                result = await SendCommandToDorico(sendCommand.command, sendCommand.parameterNames ?? Array.Empty<string>(), sendCommand.parameterValues ?? Array.Empty<string>());
            }

            return result ? "Command sent successfully." : "Failed to send command.";
        }

        // If no command was received, return a generic message
        return "No command received.";
    }

    private async Task<bool> ToggleProperty(string property, string? property_value, string on_command, string off_command)
    {
        bool connection = await ConnectToDorico();
        if (!connection)
        {
            return false;
        }

        PropertiesListResponse properties = await _remoteInstance!.GetPropertiesAsync();
        Property matchedProperty = properties.Properties.FirstOrDefault(p => p.Name == property);

        if (matchedProperty == null) {
            Log.Error($"Property '{property}' not found.");
            return false;
        } else {
            bool isOn = matchedProperty.CurrentValue == property_value;

            if (isOn) {
                await SendCommandToDorico(off_command, Array.Empty<string>(), Array.Empty<string>());
            } else {
                await SendCommandToDorico(on_command, Array.Empty<string>(), Array.Empty<string>());
            }

            return true;
        }
    }

    // This method sends the command to Dorico. If a Dorico instance is not connected, it creates one.
    private async Task<bool> SendCommandToDorico(string commandName, string[] parameterNames, string[] parameterValues)
    {
        try
        {
            // Establish a connection if one doesn't exist
            bool connection = await ConnectToDorico();
            if (!connection)
            {
                return false;
            }

            // If connection exists, send the command
            CommandParameter[] commandParameters = new CommandParameter[parameterNames.Length];
            for (int i = 0; i < parameterNames.Length; i++) {
                commandParameters[i] = new CommandParameter(parameterNames[i], parameterValues[i]);
            }
            Command command = new Command(commandName, commandParameters);
            var response = await _remoteInstance!.SendRequestAsync(command);

            // Check the response (Dorico usually returns "kOK" for successful reception)
            if (response != null)
            {
                Log.Information($"Command '{commandName}' sent successfully.");
                return true;
            }

            Log.Error($"Failed to send command '{commandName}'.");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"Error sending command to Dorico: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ConnectToDorico(bool retry = true)
    {
        // Check if there's an existing Dorico connection
        if (_remoteInstance == null || !_remoteInstance.IsConnected)
        {
            Log.Information("No existing Dorico connection. Establishing a new connection...");
            string? SessionToken = GetSessionToken();
            bool ExistingToken = !string.IsNullOrEmpty(SessionToken);
            ConnectionArguments connectionArguments;

            if (ExistingToken) {
                connectionArguments = new ConnectionArguments(SessionToken);
            } else {
                connectionArguments = new ConnectionArguments();
            }

            // Create a new Dorico remote control instance
            _remoteInstance = _serviceProvider.GetService<IDoricoRemote>()!;
            _remoteInstance.Timeout = -1; // Set infinite timeout

            // Attempt to connect to Dorico
            try {
                await _remoteInstance.ConnectAsync("Stream Deck", connectionArguments);
            } catch (DoricoException ex) {
                Log.Error($"Failed to connect to Dorico: {ex.InnerException}.");
                if (retry) {
                    Log.Information("Clearing session token and retrying...");
                    File.Delete(Path.Combine(DoricoRemoteDirectory, ".token"));
                    return await ConnectToDorico(false);
                }

                return false;
            }
            
            if (!_remoteInstance.IsConnected)
            {
                Log.Error("Failed to connect to Dorico.");
                return false;
            }

            if (!ExistingToken) {
                SaveSessionToken(_remoteInstance.SessionToken!);
            }
            Log.Information($"Connected to Dorico with code {_remoteInstance.SessionToken}");

            return true;
        } else {
            return true;
        }
    }

    public static void SaveSessionToken(string token) {
        Directory.CreateDirectory(DoricoRemoteDirectory);
        File.WriteAllText(DoricoTokenFile, token);
    }

    public static String? GetSessionToken() {
        if (File.Exists(DoricoTokenFile)) {
            return File.ReadAllText(DoricoTokenFile);
        }
        return null;
    }

    // Stop the background service
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Log.Information("Stopping HTTP Server...");
        _httpListener.Stop();
        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _httpListener.Close();
        base.Dispose();
    }
}

internal class DoricoCommandName {
    public required string command { get; set; }
}

internal class DoricoCommand : DoricoCommandName
{
    public string[]? parameterNames { get; set; }
    public string[]? parameterValues { get; set; }
}

internal class ToggleProperty : DoricoCommandName {
    public required string property { get; set; }
    public string? property_value { get; set; }
    public required string on_command { get; set; }
    public required string off_command { get; set; }
}



