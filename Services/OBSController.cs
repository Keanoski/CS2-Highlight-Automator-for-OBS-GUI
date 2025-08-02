using HighlightReel.Models;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HighlightReel.Services
{
    public class OBSController
    {
        private readonly OBSWebsocket _obs = new();

        private const int TickRate = 64;
        private const int PreRollTicks = 4 * TickRate;
        private const int PostRollTicks = 3 * TickRate;

        public async Task<(bool IsConnected, string? ErrorMessage)> ConnectAsync(string ip, string port, string password)
        {
            var tcs = new TaskCompletionSource<(bool, string?)>();

            void OnConnected(object? sender, EventArgs e)
            {
                _obs.Connected -= OnConnected;
                _obs.Disconnected -= OnDisconnected; 
                tcs.TrySetResult((true, null));
            }

            void OnDisconnected(object? sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
            {
                _obs.Connected -= OnConnected;
                _obs.Disconnected -= OnDisconnected; 
                tcs.TrySetResult((false, e.DisconnectReason));
            }

            _obs.Connected += OnConnected;
            _obs.Disconnected += OnDisconnected; 

            try
            {
                _obs.ConnectAsync($"ws://{ip}:{port}", password);

                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    // Unsubscribe on timeout
                    _obs.Connected -= OnConnected;
                    _obs.Disconnected -= OnDisconnected; 
                    return (false, "Connection to OBS timed out.");
                }
            }
            catch (Exception ex)
            {
                _obs.Connected -= OnConnected;
                _obs.Disconnected -= OnDisconnected; 
                return (false, ex.Message);
            }
        }

        public async Task RecordHighlightsAsync(Process cs2, List<Highlight> highlights)
        {
            var console = cs2.StandardInput;
            await console.WriteLineAsync("exec setup_script.cfg");

            foreach (var h in highlights)
            {
                Debug.WriteLine($"Recording {h.PlayerName}, {h.KillCount} kills...");

                int start = h.StartTick - PreRollTicks;
                int end = h.EndTick + PostRollTicks;
                int duration = (int)((end - start) / (float)TickRate * 1000);

                await console.WriteLineAsync($"demo_gototick {start}");
                await console.WriteLineAsync($"spec_player_by_name \"{h.PlayerName}\"");
                await console.WriteLineAsync("demo_pause");
                await Task.Delay(500);

                _obs.StartRecord();
                await console.WriteLineAsync("demo_resume");
                await Task.Delay(duration);
                _obs.StopRecord();

                await console.WriteLineAsync("demo_pause");
                await Task.Delay(500);
            }
        }

        public void Disconnect()
        {
            if (_obs.IsConnected)
            {
                _obs.Disconnect();
            }
        }
    }
}