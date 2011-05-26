using System;
using System.Collections.Generic;
using System.Text;

namespace Obj2Sql
{
    public class SampleObj
    {
        private int _id;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private decimal _dec;

        public decimal Dec
        {
            get { return _dec; }
            set { _dec = value; }
        }
        
    }
}
