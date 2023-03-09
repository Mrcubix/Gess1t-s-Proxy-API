using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Proxy_API.HTTP.Websocket
{
    public class WebSocketEncoding
    {
        public static string DecodeEncodedString(byte[] data, SocketConnection connection)
        {
            if (data[0] == 129)
            {
                if (data[1] > 127)
                {
                    int length = data[1] - 128;
                    int offset = 2;

                    if (length == 126)
                    {
                        length = BitConverter.ToUInt16(data[2..4]);
                        offset = 4;
                    }

                    if (length == 127)
                    {
                        length = BitConverter.ToUInt16(data[2..10]);
                        offset = 10;
                    }

                    // YEP first copy pasta for the decoding part https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server#decoding_algorithm adapted to use slices like previously
                    byte[] decodedData = new byte[length];
                    byte[] mask = data[offset..(offset + 4)];
                    offset += 4;

                    for (int i = 0; i < length; ++i)
                    {
                        decodedData[i] = (byte)(data[offset + i] ^ mask[i % 4]);
                    }

                    return Encoding.UTF8.GetString(decodedData);
                }
                else
                {
                    connection.CloseConnection();
                    return null!;
                }
            }
            else
            {
                return null!;
            }
        }

        public static byte[] EncodeDecodedString(string response)
        {
            List<byte> data = new List<byte>();
            data.Add(129);

            if (response.Length < 126)
            {
                data.Add((byte)(response.Length));
            }

            if (response.Length > 125 & response.Length < 65536)
            {
                data.Add(126);
                ushort midlength = Convert.ToUInt16(response.Length);
                data.AddRange(BitConverter.GetBytes(midlength).Reverse());
            }

            if (response.Length > 65535)
            {
                data.Add(127);
                ulong longlength = Convert.ToUInt64(response.Length);
                data.AddRange(BitConverter.GetBytes(longlength).Reverse());
            }

            data.AddRange(Encoding.UTF8.GetBytes(response));

            return data.ToArray();
        }
    }
}