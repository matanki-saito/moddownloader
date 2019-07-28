using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace claes
{
    class MyWebClient : WebClient
    {
        public long ContentLength { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);

            request.AddRange(0);

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            // GoogleドライブがContentLengthを返却しない問題に対応する
            // https://stackoverflow.com/questions/52044489/how-to-get-content-length-for-google-drive-download

            var response = base.GetWebResponse(request, result);
            var contentRange = response.Headers.Get("Content-Range");

            if (contentRange != null)
            {
                var s = contentRange.Split('/');

                if (s.Length > 1 && response.ContentLength == -1)
                {
                    // GoogleDriveの場合
                    ContentLength = long.Parse(s.Last());
                }
                else
                {
                    //CloudFront
                    ContentLength = response.ContentLength;
                }
            } else
            {
                // range未対応のサーバ？
                ContentLength = response.ContentLength;
            }

            return response;
        }
    }
}
