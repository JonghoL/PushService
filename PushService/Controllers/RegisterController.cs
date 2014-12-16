using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;

namespace PushService.Controllers
{
    public class RegisterController : ApiController
    {
        Redis Redis { get; set; }
        public RegisterController(Redis r)
        {
            this.Redis = r;
            var redisUrl = ConfigurationManager.AppSettings["redistogo"];
            var connectionUri = new Uri(redisUrl);
            var password = connectionUri.UserInfo.Split(':').LastOrDefault();

            this.Redis.Host = connectionUri.Host;
            this.Redis.Port = connectionUri.Port;
            this.Redis.Password = password;
        }

        public IEnumerable<string> Get()
        {
            return this.Redis.Keys;
        }

        public string Get(string id)
        {
            return this.Redis.GetString(id);
        }

        public void Post(FormDataCollection form)
        {
            var key = form["key"];
            var value = form["value"];
            this.Redis.Set(key, value);
        }
    }
}
