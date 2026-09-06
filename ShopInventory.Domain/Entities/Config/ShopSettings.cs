using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Config
{
    public class ShopSettings
    {
        public string ShopName { get; set; } = string.Empty;

        public string LogoPath { get; set; } = string.Empty;

        public string ThemeColor { get; set; } = "#cc0000";

        public bool IsConfigured { get; set; }
    }
}
