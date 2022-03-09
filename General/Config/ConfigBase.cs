using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Config
{
    public class ConfigBase
    {
        public int Port { get; set; }

        public override string ToString()
        {
            var content = "";

            var type = GetType();

            foreach (var propertie in type.GetProperties())
            {
                content += $"{propertie.Name}:{propertie.GetValue(this)}\n";
            }

            return content;
        }
    }
}
