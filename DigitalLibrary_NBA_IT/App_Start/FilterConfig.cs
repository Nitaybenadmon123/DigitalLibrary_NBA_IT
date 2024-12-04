using System.Web;
using System.Web.Mvc;

namespace DigitalLibrary_NBA_IT
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
