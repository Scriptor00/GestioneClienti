using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.ViewModel
{
    public class SicurezzaViewModel
    {
         public bool IsTwoFactorEnabled { get; set; }
        public string SharedKey { get; set; }
        public string Username { get; set; }
        public List<string> RecoveryCodes { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }
}