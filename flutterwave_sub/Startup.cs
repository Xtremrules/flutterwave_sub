using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(flutterwave_sub.Startup))]
namespace flutterwave_sub
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
