using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PushService
{
    public static class StringExtensions
    {
        public static byte[] ToUtf8Bytes(this string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str);
        }
    }
}