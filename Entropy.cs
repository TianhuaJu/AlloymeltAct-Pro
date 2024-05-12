using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlloyAct_Pro
{
    class Entropy
    {
        string A { get; set; }
        string B { get; set; }
        double X { get; set; }
        double Y { get; set; }
        string state { get; set; }
        double lamda { get; set; }
        double Temp { get; set; }
        public Entropy(string A, string B, double x, double y)
        {
            (this.A, this.B, this.X, this.Y) = (A, B, x, y);
        }
        public Entropy(string State, double Lamba)
        {
            this.state = State;
            this.lamda = Lamba;
        }
        public Entropy()
        { }
        public void setTemp(double T)
        {
            this.Temp = T;
        }
        public void setState(string state)
        {
            this.state = state;
        }
        double Ln(double n)
        {
            if (n > 0)
            {
                return Math.Log(n);
            }
            else
            {
                return double.NaN;
            }
        }
        public void SetState(string State)
        {
            this.state = State;
        }
        public void SetLambda(double Lamda)
        {
            this.lamda = Lamda;
        }
        public double id_Entropy(double x)
        {
           
            double sum = 0;
            if (x > 0)
            {
                sum = -constant.R * x * Ln(x);
            }
            return sum/1000.0;
        }
        /// <summary>
        /// Tanaka 过剩熵,由形成热带来的过剩熵
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        public double excess_Entropy_Tanaka(string A, string B, double X, double Y)
        {
            double Hmix,entropy;
            Binary_model miedemal_ = new Binary_model();
            miedemal_.setLammda(this.lamda);
            miedemal_.setState(this.state);
            
            Element Ea = new Element(A);
            Element Eb = new Element(B);
            if (this.state == "solid")
            {
                if (Y == 0)
                {
                    Hmix = miedemal_.infinity_enthalpy(A, B);
                }
                else
                {
                    Hmix = miedemal_.binary_Model(A, B, X, Y);
                }
                
                entropy = 1.0 / 15.1 * (1.0 / Ea.Tm + 1.0 / Eb.Tm) * Hmix;
            }
            else
            {


                if (Y == 0)
                {
                    Hmix = miedemal_.infinity_enthalpy(A, B);
                }
                else
                {
                    Hmix = miedemal_.binary_Model(A, B, X, Y);
                }
                entropy = 1.0 / 14 * (1.0 / Ea.Tm + 1.0 / Eb.Tm) * Hmix;
            }
            
            return entropy;
        }
        /// <summary>
        /// 尺寸差带来的过剩熵，该函数只针对合金熔体
        /// </summary>
        /// <param name="compositions"></param>
        /// <returns></returns>
        public double excess_Entropy_dSize(Dictionary<string, double> compositions)
        {
            double entropy0 = 0,sum_V = 0;
            
            foreach (var item1 in compositions.Keys)
            {
                Element Ei = new Element(item1);
                sum_V += compositions[item1] * Ei.V;
            }
            foreach (var item in compositions.Keys)
            {
               Element Ei = new Element(item);
                double V_ic = compositions[item] * Ei.V / sum_V;
                entropy0 += compositions[item] * Ln(V_ic / compositions[item]);
                
            }
            return -constant.R * entropy0;
        }
    }
}
