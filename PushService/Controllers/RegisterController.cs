﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web.Http;
using ServiceStack.Redis;

namespace PushService.Controllers
{
    public class RegisterController : ApiController
    {
        private readonly string redis_host;
        private readonly int redis_port;
        private readonly string redis_password;
        public RegisterController()
        {
            var redisUrl = ConfigurationManager.AppSettings["redistogo"];
            var connectionUri = new Uri(redisUrl);
            var password = connectionUri.UserInfo.Split(':').LastOrDefault();

            this.redis_host = connectionUri.Host;
            this.redis_port = connectionUri.Port;
            this.redis_password = password;
        }

        public IEnumerable<string> Get()
        {
            using (var redis = new RedisClient(this.redis_host, this.redis_port, this.redis_password))
            {
                var users = redis.LRange("users", 0, -1);
                var userList = new List<string>(users.Length);
                for (int i = 0; i < users.Length; i++)
                {
                    var curUser = users[i];
                    userList.Add(Encoding.UTF8.GetString(curUser));
                }
                return userList;
            }
        }

        public string Get(string id)
        {
            using (var redis = new RedisClient(this.redis_host, this.redis_port, this.redis_password))
            {
                if (id.Length == 64)
                {
                    var prevUserData = redis.Get(id);
                    if (prevUserData == null)
                    {
                        return "Empty";
                    }
                    return Encoding.UTF8.GetString(prevUserData);
                }
                else
                {
                    var deviceTokens = redis.LRange(id, 0, -1);
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
        }

        public void Post(FormDataCollection form)
        {
            using (var redis = new RedisClient(this.redis_host, this.redis_port, this.redis_password))
            {
                var userId = form["key"];
                var deviceToken = form["value"];
                var device = form["device"];

                var prevUserData = redis.Get(deviceToken);
                if (prevUserData == null)
                {
                    redis.RPush(userId, deviceToken.ToUtf8Bytes()); //list
                    redis.SetEntry(deviceToken, userId + "," + device); //key-value
                }
                else
                {
                    var prevUserDataString = Encoding.UTF8.GetString(prevUserData);
                    var prevUser = prevUserDataString.Split(',').FirstOrDefault();
                    var deviceTokens = redis.LRange(prevUser, 0, -1);
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
                            redis.LRem(prevUser, 0, deviceToken.ToUtf8Bytes());
                        }
                        redis.RPush(userId, deviceToken.ToUtf8Bytes()); //list
                    }

                    redis.SetEntry(deviceToken, userId +","+device); //update
                }

                var users = redis.LRange("users", 0, -1);
                var userList = new List<string>(users.Length);
                for (int i = 0; i < users.Length; i++)
                {
                    var curUser = users[i];
                    userList.Add(Encoding.UTF8.GetString(curUser));
                }
                if (!userList.Contains(userId)) 
                {
                    redis.RPush("users", userId.ToUtf8Bytes()); //list
                }
            }
        }
    }
}
