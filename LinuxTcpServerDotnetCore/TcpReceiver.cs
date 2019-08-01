using LinuxTcpServerDotnetCore.Statics;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LinuxTcpServerDotnetCore
{
    public class TcpReceiver
    {
        TcpClient Client;
        public TcpReceiver(TcpClient client)
        {
            this.Client = client;
            this.Client.GetStream().ReadTimeout = 100000;
        }

        public int ReadRecevieData(out string data)
        {
            if (!Client.Connected)
            {
                data = null;
                return 0;
            }

            //var stream = Client.GetStream();

            List<byte> buffer_ls = new List<byte>();
            int len = 0;
            int offset = 0;
            int target_len = 0;
            do
            {
                byte[] tmp_buffer = new byte[255];
                if (target_len == 0)
                {
                    try
                    {
                        len = Client.Client.Receive(tmp_buffer, 0, 10, SocketFlags.None);
                    }
                    catch(Exception ex)
                    {
                        Debuger.PrintStr(ex.Message, EPRINT_TYPE.WARNING);
                        data = null;
                        return -1;
                    }
                    var length_str = Encoding.UTF8.GetString(tmp_buffer);
                    int tmp_targetlen = target_len;
                    if (!int.TryParse(length_str, out target_len))
                    {
                        target_len = tmp_targetlen;
                    }
                }
                else
                {
                    len = Client.Client.Receive(tmp_buffer, 0, 255, SocketFlags.None);// stream.Read(tmp_buffer, 0, tmp_buffer.Length);
                    buffer_ls.AddRange(tmp_buffer);
                    offset += len;
                }
                if(len == 0)
                {
                    data = null;
                    return 0;
                }
                
                Debuger.PrintStr(len.ToString(), EPRINT_TYPE.ERROR);
            } while (target_len - offset > 0);
            target_len = 0;

            data = Encoding.UTF8.GetString(buffer_ls.ToArray());
            if (offset > 0)
                Debuger.PrintStr("Receive msg: " + data, EPRINT_TYPE.NORMAL);
            return offset;
        }
    }
}
