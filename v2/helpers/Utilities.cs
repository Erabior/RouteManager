using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteManager.v2.helpers
{
    public class Utilities
    {

        //Simplify Enum Parsing
        public static T ParseEnum<T>(string value)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch { return default(T); }
        }
    }
}
