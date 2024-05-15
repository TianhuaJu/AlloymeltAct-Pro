namespace AlloyAct_Pro
{
    [Serializable]
    public class Element
    {
        private double _hs_D;
        private double _hs_V;
        public double Phi { get; set; }
        public double N_WS { get; set; }
        public double V { get; set; }
        public double u { get; set; }
        public double M { get; set; }
        public double dH_Trans { get; set; }
        public double hybird_Value { get; set; }
        public double Tm { get; set; }
        public double Tb { get; set; }
        public bool isExist { get; set; }
        public String hybird_factor
        {
            get;
            set;
        }
        public string Name { get; set; }
        public Boolean isTrans_group { get; set; }
        public double fuse_heat { get; set; }
        public string V0 { get; set; }
        public double Dcov { get; set; }
        public double Datom { get; set; }
        /// <summary>
        /// 体积模量
        /// </summary>
        public double Bkm { get; set; }
        /// <summary>
        /// 剪切模量
        /// </summary>
        public double Shm { get; set; }
        public double density { get; set; }
        public double hs_D
        {
            get
            {
                if (double.TryParse(str_hs_D, out _hs_D))
                {
                    return _hs_D;
                }
                else
                {
                    return double.NaN;
                }
            }

        }
        public double hs_V
        {
            get
            {
                if (double.TryParse(str_hs_V, out _hs_V))
                {
                    return _hs_V;
                }
                else
                {
                    return double.NaN;
                }

            }


        }
        public bool Ueno_Validation
        {
            get
            {
                if (double.IsNaN(hs_D) || double.IsNaN(hs_V))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        public string str_hs_D { get; set; }
        public string str_hs_V { get; set; }


        public Element(string name)
        {

            if (constant.periodicTable.ContainsKey(name))
            {
                this.isExist = true;
                this.Name = name;

                DataCenter.get_MiedemaData(this);


            }
            else
            {
                this.isExist = false;
            }



        }
        public Element(string Symbol, bool isconn_mysql)
        {
            this.Name = Symbol;
            if (isconn_mysql)
            {
                this.isExist = true;

            }
            else
            {
                this.isExist = false;
                MessageBox.Show(this.Name + "  is not exist", "warning!!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }





    }
}
