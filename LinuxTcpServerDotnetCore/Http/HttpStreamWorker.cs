using Json;
using LinuxTcpServerDotnetCore.Statics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LinuxTcpServerDotnetCore.Http
{
    class HttpStreamWorker
    {
        private static Dictionary<string, HttpStreamWorker> UsingHttpStreamWorker = new Dictionary<string, HttpStreamWorker>();

        private string OwnerIp;
        private int ContinueOffset = -1;
        private long sizeReaming = -1;
        private long readCount = -1;

        public static HttpStreamWorker CreateHttpStreamWorker(string ip)
        {
            if (UsingHttpStreamWorker.ContainsKey(ip))
            {
                return UsingHttpStreamWorker[ip];
            }
            HttpStreamWorker tmp = new HttpStreamWorker(ip);
            UsingHttpStreamWorker.Add(ip, tmp);
            return tmp;
        }
        private HttpStreamWorker(string ip)
        {
            OwnerIp = ip;
        }
        public string ReadInputStreamToString(Stream inputStream, int length)
        {

            {
                long logLength = Convert.ToInt64(length);

                byte[] buffer = new byte[logLength];
                //read text from client
                int jsonLength = inputStream.Read(buffer, 0, buffer.Length);

                if (jsonLength <= 0) return "";

                //cast to string
                string json = Encoding.UTF8.GetString(buffer, 0, jsonLength);
                return json;
            }
        }

        public string ReadInputStream(Stream inputStream, string fileName, string continueFlag)
        {
            byte[] data = null;
            try
            {

                var byteList = new List<byte>();
                var byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;
                //do
                //{
                //    readLen = inputStream.Read(byteArr, 0, byteArr.Length);
                //    if (readLen != 0)
                //    {
                //        len += readLen;
                //        byteList.AddRange(byteArr);
                //
                //
                //        if (readLen < 2048)
                //        {
                //            byteList.RemoveRange(readLen, 2048 - readLen);
                //        }
                //    }
                //} while (readLen != 0);
                while (true)
                {
                    readLen = inputStream.Read(byteArr, 0, byteArr.Length);
                    if (readLen > 0)
                    {
                        len += readLen;
                        byteList.AddRange(byteArr);
                        if (readLen < 2048)
                        {
                            MemoryStream ms = new MemoryStream();
                            ms.Write(byteArr, 0, readLen);
                            byteArr = ms.ToArray();
                            byteList.AddRange(byteArr);
                            ms.Flush();
                            ms.Close();
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (len == 0)
                {
                    data = null;
                    inputStream.Close();
                    return JsonWorker.MakeSampleReturnJson(null, new string[] { "error", "file size 0 byte", "200" }).jstr;
                }
                data = byteList.ToArray();


                inputStream.Close();
            }
            catch (Exception ex)
            {
                inputStream.Close();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"在接收数据时发生错误:{ex.ToString()}");
                return JsonWorker.MakeSampleReturnJson(null, new string[] { "error", ex.ToString(), "404" }).jstr;
            }
            if (!ByteToFile(data, fileName, continueFlag))
            {
                return JsonWorker.MakeSampleReturnJson(null, new string[] { "error", "save file faild", "200" }).jstr;

            }
            else
            {
                return JsonWorker.MakeSampleReturnJson(null, new string[] { "normal", continueFlag, "200" }).jstr;
            }
        }

        public string ReadLocalFile(string fileName, out byte[] bytes, out bool s)
        {

            FileInfo fi = new FileInfo(fileName);
            var fileSize = fi.Length;
            if (sizeReaming == -1)
            {
                sizeReaming = fileSize;
                readCount = 0;
            }
            if (!fi.Exists)
            {
                bytes = null;
                s = false;

                return "file does not exist";
            }
            byte[] data = null;
            FileStream fs = null;
            try
            {
                var byteList = new List<byte>();
                byte[] byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;
                if (fileSize < StaticObjects.StreamSizeLimit)
                {
                    fs = new FileStream(fileName, FileMode.Open);
                    do
                    {
                        readLen = fs.Read(byteArr, 0, byteArr.Length);
                        len += readLen;
                        byteList.AddRange(byteArr);
                    } while (readLen != 0);
                    data = byteList.ToArray();
                    fs.Flush();
                    fs.Close();
                    EndOnce();
                }
                else
                {
                    byte[] buffer = null;
                    var dataContinueFlag = "true";
                    if (sizeReaming < StaticObjects.SingleMemoryBarSize)
                    {
                        buffer = new byte[sizeReaming];
                        dataContinueFlag = "end";
                    }
                    else
                    {
                        buffer = new byte[StaticObjects.SingleMemoryBarSize];
                    }
                    fs = new FileStream(fileName, FileMode.Open);
                    fs.Seek(readCount, SeekOrigin.Begin);
                    fs.Read(buffer, 0, buffer.Length);
                    fs.Flush();
                    fs.Close();
                    readCount += StaticObjects.SingleMemoryBarSize;
                    sizeReaming -= StaticObjects.SingleMemoryBarSize;
                    if (sizeReaming <= 0)
                    {
                        EndOnce();
                    }
                }


            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"read error:{ex.Message}");
                fs.Close();
                bytes = null;
                s = false;
                EndOnce();
                return "read file error";
            }
            bytes = data;
            s = true;
            return "OK";
        }

        public bool ByteToFile(byte[] byteArray, string fileName, string continueFlag, bool isBinary = true)
        {
            bool result = false;
            string realFileName = StaticObjects.HttpContentDir + fileName;
            try
            {
                using (BinaryWriter fs = new BinaryWriter(new FileStream(realFileName, FileMode.OpenOrCreate, FileAccess.Write)))
                {
                    if (continueFlag == "false")
                    {
                        fs.Write(byteArray, 0, byteArray.Length);
                        result = true;
                    }
                    else if (continueFlag == "true")
                    {
                        if (ContinueOffset != -1)//middle
                        {
                            int offset = ContinueOffset;
                            fs.Seek(0, SeekOrigin.End);
                            fs.Write(byteArray, 0, byteArray.Length);
                            ContinueOffset = offset + byteArray.Length;
                            result = true;
                        }
                        else//first
                        {
                            fs.Write(byteArray, 0, byteArray.Length);
                            ContinueOffset = byteArray.Length;
                            result = true;
                        }

                    }
                    else//end
                    {
                        int offset = ContinueOffset;
                        fs.Seek(0, SeekOrigin.End);
                        fs.Write(byteArray, 0, byteArray.Length);
                        result = true;
                        EndOnce();
                    }

                    fs.Flush();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                FileInfo fi = new FileInfo(realFileName);
                fi.Delete();
                fi = null;
                UsingHttpStreamWorker.Remove(this.OwnerIp);
                result = false;

            }
            return result;
        }

        private void EndOnce()
        {
            ContinueOffset = -1;
            sizeReaming = -1;
            readCount = -1;
        }

    }
}
