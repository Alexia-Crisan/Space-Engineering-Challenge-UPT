# RobotWebControl Web UI

This is a minimal ASP.NET Core web app that serves a static page and accepts robot commands over WebSocket.

How to run

1. From repo root run:

```bash
cd Web
dotnet run
```

2. Open http://localhost:5000 (or the port dotnet reports).

Usage

- The page connects to `/ws` and sends JSON commands like:
  - `{ "cmd": "drive", "angle": 0 }`
  - `{ "cmd": "speed", "speed": 70 }`
  - `{ "cmd": "stop" }`
  - `{ "cmd": "break" }`
  - `{ "cmd": "requestStatus" }`

- The server broadcasts reports posted to `/report` to all connected websocket clients.
