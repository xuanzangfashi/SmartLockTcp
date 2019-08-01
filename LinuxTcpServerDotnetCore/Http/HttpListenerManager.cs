using Json;
using LinuxTcpServerDotnetCore.Statics;
using LinuxTcpServerDotnetCore.TemplateClass;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LinuxTcpServerDotnetCore.Http
{
    //namespace HttpHandlers
    //{
    public delegate string RequestHandleFunction(HttpListenerRequest request, HttpListenerResponse response);
        public class HttpRequsetHandlleCollection
        {
            public RequestHandleFunction postH;
            public RequestHandleFunction getH;
        }

        public class HttpRequestHandler
        {
            private string HttpSubDir;
            public HttpRequsetHandlleCollection RequestHandles { get; private set; }
            public HttpRequestHandler()
            {

            }
            protected virtual bool OnCreate()
            {
                return true;
            }


            protected virtual string GetHandle(HttpListenerRequest request, HttpListenerResponse response)
            {
                return "";
            }


            protected virtual string PostHandle(HttpListenerRequest request, HttpListenerResponse response)
            {
                return "";
            }

            public static T CreateHttpRequestHandler<T>(string httpSubDir) where T : HttpRequestHandler, new()
            {
                T httpHandlerObj = new T();
                httpHandlerObj.RequestHandles = new HttpRequsetHandlleCollection();
                httpHandlerObj.RequestHandles.getH = httpHandlerObj.GetHandle;
                httpHandlerObj.RequestHandles.postH = httpHandlerObj.PostHandle;
                HttpListenerManager.Instance.HttpRequestHandlerList.Add(httpHandlerObj);
                HttpListenerManager.Instance.SubDirHttpRequestHandlerDictionary.Add(httpSubDir, httpHandlerObj);
                httpHandlerObj.OnCreate();
                return httpHandlerObj;
            }
        }
    //}
    public class HttpListenerManager : Singleton<HttpListenerManager>
    {
        //private RequestHandleFunction PostHandle;
        //private RequestHandleFunction GetHandle;
        private HttpListener HttpObj;
        public List<HttpRequestHandler> HttpRequestHandlerList { get; private set; }
        public Dictionary<string, HttpRequestHandler> SubDirHttpRequestHandlerDictionary { get; private set; }
        public bool Init()
        {
            try
            {
                HttpRequestHandlerList = new List<HttpRequestHandler>();
                SubDirHttpRequestHandlerDictionary = new Dictionary<string, HttpRequestHandler>();
                HttpObj = new HttpListener();
                HttpObj.Prefixes.Add(StaticObjects.HttpServerIp);
                HttpObj.Start();
                HttpObj.BeginGetContext(new AsyncCallback(GetContextCallback), HttpObj);
                return true;
            }
            catch (Exception ex)
            {
                Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR);
                HttpObj = null;
                return false;
            }
        }




        private void GetContextCallback(IAsyncResult ar)
        {
            //continue Async listener
            HttpObj.BeginGetContext(new AsyncCallback(GetContextCallback), ar);

            //print base info
            var guid = Guid.NewGuid().ToString();
            Debuger.PrintStr($"Recive http request:{guid},time:{DateTime.Now.ToString()}", EPRINT_TYPE.NORMAL, false);

            //get context objects
            var context = HttpObj.EndGetContext(ar);
            var request = context.Request;
            var response = context.Response;

            //set response config
            context.Response.ContentType = "text/plain;charset=UTF-8";//pure text,utf-8 encode
            context.Response.AddHeader("Content-type", "text/plain");//set header
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            //get url params
            Dictionary<string, string> _params = null;
            GetUrlParams(request.Url.Query, out _params);


            //start handle request
            string returnObj = null;//define return text
            if (_params == null)
            {
                var errorStr = "Request does not have \"method\" param!";
                Debuger.PrintStr(errorStr, EPRINT_TYPE.WARNING);
                returnObj = JsonWorker.MakeSampleReturnJson(null, new string[] { "error", errorStr, "404" }).jstr;
                goto ReturnPart;
            }
            if (_params.ContainsKey("method"))
            {
                if (SubDirHttpRequestHandlerDictionary.Count == 0 || !SubDirHttpRequestHandlerDictionary.ContainsKey(_params["method"]))
                {
                    var errorStr = "HttpListenerManager does not have any handlers!";
                    Debuger.PrintStr(errorStr, EPRINT_TYPE.WARNING);
                    returnObj = JsonWorker.MakeSampleReturnJson(null, new string[] { "error", errorStr, "0" }).jstr;
                }
                else
                {
                    if (request.HttpMethod == "POST" && request.InputStream != null)//post request
                    {
                        returnObj = SubDirHttpRequestHandlerDictionary[_params["method"]].RequestHandles.postH(request, response);
                    }
                    else if (request.HttpMethod == "GET")//get request
                    {
                        returnObj = SubDirHttpRequestHandlerDictionary[_params["method"]].RequestHandles.getH(request, response);
                    }
                }
            }
            else
            {
                var errorStr = "Request does not have \"method\" param!";
                Debuger.PrintStr(errorStr, EPRINT_TYPE.WARNING);
                returnObj = JsonWorker.MakeSampleReturnJson(null, new string[] { "error", errorStr, "404" }).jstr;
            }
            ReturnPart:;
            ReturnMsgToClient(returnObj, response, guid);
        }

        private void ReturnMsgToClient(string returnObj, HttpListenerResponse response, string guid)
        {
            var returnByteArr = Encoding.UTF8.GetBytes(returnObj);//set back encoding
            try
            {
                using (var stream = response.OutputStream)
                {
                    //back message to client
                    stream.Write(returnByteArr, 0, returnByteArr.Length);
                }
            }
            catch (Exception ex)
            {
                Debuger.PrintStr($"network error:{ex.Message}", EPRINT_TYPE.ERROR, false);
            }
            Debuger.PrintStr($"Succeed:{guid},time:{DateTime.Now.ToString()},return message:{returnObj}", EPRINT_TYPE.NORMAL, false);
        }

        public void GetUrlParams(string query, out Dictionary<string, string> _params)
        {
            if (string.IsNullOrEmpty(query))
            {
                _params = null;
                return;
            }
            _params = new Dictionary<string, string>();
            string str = query.Remove(0, 1);
            var parts = str.Split('&');
            foreach (var i in parts)
            {
                string[] single = i.Split('=');
                string key = single[0];
                string value = single[1];
                _params.Add(key, value);
            }
        }



    }
}
