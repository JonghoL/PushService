using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
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
            if (id.Length == 64)
            {
                var prevUserData = this.Redis.Get(id);
                if (prevUserData == null)
                {
                    return "Empty";
                }
                return Encoding.UTF8.GetString(prevUserData);
            }
            else
            {
                var deviceTokens = this.Redis.ListRange(id, 0, -1);
                var deviceTokenList = new List<string>(deviceTokens.Length);
                for (int i = 0; i < deviceTokens.Length; i++)
                {
                    var curDeviceToken = deviceTokens[i];
                    deviceTokenList.Add(Encoding.UTF8.GetString(curDeviceToken));
                }
                var sb = new StringBuilder();
                foreach (var dt in deviceTokenList)
                {
                    sb.AppendLine(dt);
                }

                return sb.ToString();
            }
        }

        public void Post(FormDataCollection form)
        {
            var userId = form["key"]; 
            var deviceToken = form["value"];

            var prevUserData = this.Redis.Get(deviceToken);
            if (prevUserData == null)
            {
                this.Redis.RightPush(userId, deviceToken); //list
                this.Redis.Set(deviceToken, userId); //key-value
            }
            else
            {
                var prevUser = Encoding.UTF8.GetString(prevUserData);
                var deviceTokens = this.Redis.ListRange(prevUser, 0, -1);
                var deviceTokenList = new List<string>(deviceTokens.Length);
                for (int i = 0; i < deviceTokens.Length; i++)
                {
                    var curDeviceToken = deviceTokens[i];
                    deviceTokenList.Add(Encoding.UTF8.GetString(curDeviceToken));
                }
                if (prevUser != userId)
                {
                    if (deviceTokenList.Exists(t => t == deviceToken))
                    {
                        this.Redis.ListRemove(prevUser, deviceToken);
                    }
                    this.Redis.RightPush(userId, deviceToken); //list
                }

                this.Redis.Set(deviceToken, userId); //update
            }
        }
    }
}
