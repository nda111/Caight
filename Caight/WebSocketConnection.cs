using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Caight
{
    public class WebSocketConnection
    {
        public WebSocketConnection(WebSocket webSocket)
        {
            WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            BufferSize = 4096;
        }

        public WebSocket WebSocket { get; } = null;

        public WebSocketState? State => WebSocket?.State;

        private int bufferSize = 4096;
        public int BufferSize
        {
            get => bufferSize;
            set
            {
                if (bufferSize != value)
                {
                    if (value > 0)
                    {
                        bufferSize = value;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("BufferSize == 0");
                    }
                }
            }
        }

        public string TextMessage { get; private set; } = null;

        public byte[] BinaryMessage { get; private set; } = null;

        public async Task SendTextAsync(string message, CancellationToken cancellationToken = default)
        {
            await WebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task SendBinaryAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            await WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, cancellationToken);
        }

        public async Task CloseAsync(string statusDescription = null, CancellationToken cancellationToken = default)
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, statusDescription, cancellationToken);
        }

        public async Task<WebSocketReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[bufferSize];

            var result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    TextMessage = Encoding.UTF8.GetString(buffer.Take(result.Count).ToArray());
                    break;

                case WebSocketMessageType.Binary:
                    BinaryMessage = buffer.Take(result.Count).ToArray();
                    break;

                case WebSocketMessageType.Close:
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                    break;

                default:
                    break;
            }

            return result;
        }
    }
}
