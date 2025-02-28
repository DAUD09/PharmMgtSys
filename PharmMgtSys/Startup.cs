using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PharmMgtSys.Startup))]
namespace PharmMgtSys
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
