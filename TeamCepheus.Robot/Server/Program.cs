using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using TeamCepheus.Robot.Motion;
using TeamCepheus.Robot.Sensors;

var builder = WebApplication.CreateBuilder(args);

// Use the mock by default so the web server can run without hardware.
builder.Services.AddSingleton<IMotionController, MotionController>();
builder.Services.AddSingleton<ISensorsController, SensorsController>();
builder.Services.AddSingleton<IServoController, ServoController>();

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Configure host options for graceful shutdown
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(10);
});

// Add hosted service for sensor background task
builder.Services.AddHostedService<SensorBackgroundService>();

// Create a shared sockets dictionary for broadcasting
var sockets = new ConcurrentDictionary<string, WebSocket>();
builder.Services.AddSingleton(sockets);

// Add hosted service for status broadcasting
builder.Services.AddHostedService<StatusBroadcasterService>();

var app = builder.Build();

#region Webcam streamer (MediaMTX)

// Start mediamtx process
Process? mediamtxProcess = null;
try
{
    var cameraPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "camera");
    var mediamtxPath = Path.Combine(cameraPath, "mediamtx");

    mediamtxProcess = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = mediamtxPath,
            WorkingDirectory = cameraPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }
    };

    mediamtxProcess.Start();
    app.Logger.LogInformation("mediamtx process started with PID {pid}", mediamtxProcess.Id);

    // Log mediamtx output
    mediamtxProcess.OutputDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            app.Logger.LogInformation("[mediamtx] {output}", e.Data);
    };
    mediamtxProcess.ErrorDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            app.Logger.LogError("[mediamtx] {error}", e.Data);
    };

    mediamtxProcess.BeginOutputReadLine();
    mediamtxProcess.BeginErrorReadLine();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to start mediamtx process");
}

// Register shutdown handler to ensure mediamtx is stopped
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    if (mediamtxProcess != null && !mediamtxProcess.HasExited)
    {
        try
        {
            app.Logger.LogInformation("Stopping mediamtx process");
            mediamtxProcess.Kill();
            mediamtxProcess.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error stopping mediamtx process");
        }
    }
});

#endregion

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Endpoint to accept websocket connections
app.Map("/ws", async (HttpContext context, ConcurrentDictionary<string, WebSocket> sockets) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid().ToString();
    sockets[id] = ws;
    var logger = app.Logger;
    logger.LogInformation("WebSocket connected: {id}", id);

    var motion = app.Services.GetRequiredService<IMotionController>();

    try
    {
        var buffer = new byte[4096];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            try
            {
                using var doc = JsonDocument.Parse(message);
                if (doc.RootElement.TryGetProperty("cmd", out var cmdEl))
                {
                    var cmd = cmdEl.GetString();
                    switch (cmd)
                    {
                        case "drive":
                            var forward = doc.RootElement.TryGetProperty("forward", out var r) && r.GetBoolean();
                            motion.Drive(forward);
                            await SendAck(ws, $"drive: {forward}");
                            break;

                        case "rotateLeft":
                            motion.RotateLeft();
                            await SendAck(ws, "rotateLeft");
                            break;

                        case "rotateRight":
                            motion.RotateRight();
                            await SendAck(ws, "rotateRight");
                            break;

                        case "speed":
                            var motorA = doc.RootElement.GetProperty("motorA").GetUInt32();
                            var motorB = doc.RootElement.GetProperty("motorB").GetUInt32();
                            motion.Speed(motorA, motorB);
                            await SendAck(ws, $"speed: {motorA}, {motorB}");
                            break;

                        case "stop":
                            motion.Stop();
                            await SendAck(ws, "stop");
                            break;

                        case "brake":
                            motion.Brake();
                            await SendAck(ws, "brake");
                            break;

                        case "servoAngle":
                            if (doc.RootElement.TryGetProperty("angle", out var angleEl) && angleEl.TryGetInt32(out int angle))
                            {
                                var servo = app.Services.GetRequiredService<IServoController>();
                                if (!servo.IsRunning())
                                {
                                    servo.Start();
                                }
                                servo.SetAngle(angle);
                                await SendAck(ws, $"servoAngle: {angle}°");
                            }
                            else
                            {
                                await SendError(ws, "servoAngle requires 'angle' property (0-180)");
                            }
                            break;

                        case "requestStatus":
                            var sensors = app.Services.GetRequiredService<ISensorsController>();
                            var sensorData = sensors.GetAverages();
                            await SendStatus(ws, new
                            {
                                battery = 12.34,
                                temperature = sensorData.Temperature,
                                humidity = sensorData.Humidity,
                                pressure = sensorData.Pressure
                            });
                            break;

                        default:
                            await SendError(ws, "unknown command");
                            break;
                    }
                }
                else
                {
                    await SendError(ws, "missing cmd");
                }
            }
            catch (JsonException)
            {
                await SendError(ws, "invalid json");
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "WebSocket handling error");
    }
    finally
    {
        sockets.TryRemove(id, out _);
        if (ws.State == WebSocketState.Open)
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
        ws.Dispose();
        app.Logger.LogInformation("WebSocket disconnected: {id}", id);
    }
});

// Simple HTTP POST to broadcast a JSON report to all connected websocket clients.
// This allows the robot (or tests) to push telemetry to web clients.
app.MapPost("/report", async (HttpContext ctx) =>
{
    using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
    var json = JsonSerializer.Serialize(doc.RootElement);
    var tasks = sockets.Values.Select(s => s.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None));
    await Task.WhenAll(tasks);
    return Results.Ok(new { sent = sockets.Count });
});

app.UseWebSockets();

app.Run();

static Task SendAck(WebSocket ws, string msg)
{
    var obj = new { type = "ack", msg };
    return ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj)), WebSocketMessageType.Text, true, CancellationToken.None);
}

static Task SendError(WebSocket ws, string error)
{
    var obj = new { type = "error", error };
    return ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj)), WebSocketMessageType.Text, true, CancellationToken.None);
}

static Task SendStatus(WebSocket ws, object status)
{
    var obj = new { type = "status", data = status };
    return ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj)), WebSocketMessageType.Text, true, CancellationToken.None);
}

/// <summary>
/// Background service that broadcasts sensor status to all connected WebSocket clients every second.
/// </summary>
class StatusBroadcasterService : BackgroundService
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets;
    private readonly ISensorsController _sensorsController;
    private readonly ILogger<StatusBroadcasterService> _logger;

    public StatusBroadcasterService(
        ConcurrentDictionary<string, WebSocket> sockets,
        ISensorsController sensorsController,
        ILogger<StatusBroadcasterService> logger)
    {
        _sockets = sockets;
        _sensorsController = sensorsController;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StatusBroadcasterService starting");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Get current sensor data
                var sensorData = _sensorsController.GetAverages();
                var statusMessage = new
                {
                    type = "status",
                    data = new
                    {
                        battery = 12.34,
                        temperature = sensorData.Temperature,
                        humidity = sensorData.Humidity,
                        pressure = sensorData.Pressure,
                        timestamp = DateTime.UtcNow
                    }
                };

                var json = JsonSerializer.Serialize(statusMessage);
                var bytes = Encoding.UTF8.GetBytes(json);

                // Broadcast to all connected clients
                var disconnectedClients = new List<string>();
                foreach (var kvp in _sockets)
                {
                    try
                    {
                        if (kvp.Value.State == WebSocketState.Open)
                        {
                            await kvp.Value.SendAsync(bytes, WebSocketMessageType.Text, true, stoppingToken);
                        }
                        else
                        {
                            disconnectedClients.Add(kvp.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error sending status to client {clientId}", kvp.Key);
                        disconnectedClients.Add(kvp.Key);
                    }
                }

                // Clean up disconnected clients
                foreach (var clientId in disconnectedClients)
                {
                    _sockets.TryRemove(clientId, out _);
                }

                // Wait 1 second before broadcasting again
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("StatusBroadcasterService cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StatusBroadcasterService");
        }
    }
}
