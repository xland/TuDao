using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TuDao.App
{
    public class ShangPin
    {
        public Dictionary<string,string> TiTu = new Dictionary<string, string>();
        public Dictionary<string,string> SeTu = new Dictionary<string, string>();
        public Dictionary<string, string> NeiRongTu = new Dictionary<string, string>();
        public string DetailJsonUrl
        {
            get;
            set;
        }
        public string HuoHao
        {
            get;
            set;
        }
        public string Id
        {
            get;
            set;
        }
    }
}
