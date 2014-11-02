using System;
using System.Configuration;
using System.IO;
using System.Net.Http.Formatting;
using System.Web;
using System.Web.Http;
using PushSharp;
using PushSharp.Apple;

namespace PushService.Controllers
{
    public class PushController : ApiController
    {
        private readonly PushSharp.PushBroker pushBroker;
        public PushController(PushSharp.PushBroker pushBroker)
        {
            this.pushBroker = pushBroker;
            Register();
        }

        void Register()
        {
            var path = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            var appleCert = File.ReadAllBytes(Path.Combine(path, "cert/Certificates.p12"));
            var certPwd = ConfigurationManager.AppSettings["certificateFilePwd"];
            pushBroker.RegisterAppleService(new ApplePushChannelSettings(appleCert, certPwd));
        }

        public void Post(FormDataCollection form)
        {
            var deviceToken = form["deviceToken"];
            var message = form["message"];
            pushBroker.QueueNotification(new AppleNotification()
                .ForDeviceToken(deviceToken)
                .WithAlert(message));
        }
    }
}
