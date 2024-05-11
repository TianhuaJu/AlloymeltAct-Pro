using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro
{
    class Regex_Extend :Regex
    {
        
        string text { get; set; }
        public  Regex_Extend( string pattern ):base(pattern) {
            this.pattern = pattern;
         }
        public GroupCollection group(string text) {
            this.text = text;
            return new Regex_Extend(pattern).Match(text).Groups;
        }
    }
}
