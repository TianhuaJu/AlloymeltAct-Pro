using MathNet.Numerics;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlloyAct_Pro
{
    class Activity_Coefficient
    {
        private const double R = constant.R;
        private const double P_TT = constant.P_TT;
        private const double P_TN = constant.P_TN;
        private const double P_NN = constant.P_NN;
        /// <summary>
        /// 熔体的组成，{"A",x}形式的集合字典，非标准摩尔形式
        /// </summary>
        private Dictionary<string, double> _comp_dict = new Dictionary<string, double>();
        /// <summary>
        /// 熔体的原始组成，{"A",x}形式的集合字典，非标准摩尔形式
        /// </summary>
        private Dictionary<string, double> _comp_dict_Original 
        { 
            get 
            { return this._comp_dict; }
        }
        /// <summary>
        /// 熔体的组成{"A",x}形式的集合字典，标准摩尔形式
        /// </summary>
        private Dictionary<string, double> melts_dict {
            get
            {
                double sum = 0;
                Dictionary<string, double> compDict = new Dictionary<string, double>();
                if (_comp_dict.Count > 1)
                {
                    foreach (var item in _comp_dict)
                    {
                        sum = sum + item.Value;
                    }
                    foreach (var item in _comp_dict)
                    {
                        if (compDict.ContainsKey(item.Key))
                        {
                            compDict[item.Key] += item.Value / sum;
                        }
                        else
                        {
                            compDict.Add(item.Key, item.Value / sum);
                        }
                    }
                }
                return compDict;
            }
        }


        public Activity_Coefficient()
        {

        }
        private double pow(double x, double y)
        {
            return Math.Pow(x, y);
        }
        private double ln(double x)
        {
            if (x > 0)
            {
                return Math.Log(x);
            }
            else
                return double.NaN;
        }
        private double Exp(double x)
        {
            return Math.Exp(x);
        }
        /// <summary>
        /// 设定系统的初始组成{"A",x}形式
        /// </summary>
        /// <param name="text">AxByCz形式</param>
        public void set_CompositionDict(string text)
        {
            
            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");
            MatchCollection matchs = re.Matches(text);

            foreach (System.Text.RegularExpressions.Match match in matchs)
            {
                double x = 1.0;
                string A = "";
                GroupCollection groups = match.Groups;
                A = groups[1].Value;
                if (double.TryParse(groups[2].Value, out x))
                {
                    double.TryParse(groups[2].Value, out x);
                }
                else { x = 1.0; }

                if (this._comp_dict.ContainsKey(A))
                {
                    this._comp_dict[A] += x;
                }
                else
                {
                    this._comp_dict.Add(A, x);
                }

            }

        }
        /// <summary>
        /// 返回新条件下的系统摩尔组成{"A",x}
        /// </summary>
        /// <param name="varable">变量组分</param>
        /// <param name="x">输入变量的摩尔组成</param>
        /// <returns></returns>
        public Dictionary<string, double> get_NewCompositionDict(string varable, double x) 
        {

            Dictionary<string,double> componet_molarPairs = new Dictionary<string,double>();
            Dictionary<string,double> componet_molarfractionPairs = new Dictionary<string,double>();
            double nA = 0, sum_noA = 0;
            foreach (var item in this._comp_dict_Original)
            {
                if (!item.Key.Equals(varable))
                {
                    componet_molarPairs.Add(item.Key, item.Value);
                    sum_noA = sum_noA + item.Value;
                }
                
            }
            nA = x * sum_noA / (1 - x);

            double n_total;
            foreach (var item in this._comp_dict_Original)
            {
                if (item.Key.Equals(varable))
                {
                    componet_molarfractionPairs.Add(varable,x); 
                }
                else
                {
                    componet_molarfractionPairs.Add(item.Key, item.Value*(1-x)/sum_noA);
                }
                
            }
            return componet_molarfractionPairs;           


        }

        /// <summary>
        /// 计算活度系数,多组分体系。利用Wagner稀溶液模型
        /// </summary>
        /// <param name="solvent">基体或溶剂</param>
        /// <param name="solute_i">待计算活度系数的组元</param>
        /// <param name="compositions">不包含集体元素的熔体组成的字典</param>
        /// <param name="Tem">温度</param>
        /// <param name="lnYi">无限稀活度系数的对数</param>
        public double activity_Coefficient_Wagner(string solvent, string solute_i,  Geo_Model geo_Model,string GeoModel,(string state, bool excessetropy,bool cp, double T) info)
        {

            double lnY0 = 0.0,lnYi;
            
            Element solv = new Element(solvent);
            Element solu_i = new Element(solute_i);
            double acf = 0.0;
            if (this.melts_dict.ContainsKey(solute_i))
            {
                /**判断需求活度系数的溶质是否包含在组成内，如果包含，执行下列计算 */


                Ternary_melts inacoef = new Ternary_melts(info.T,info.state,info.excessetropy);
              
                lnY0 = inacoef.lnY0(solv, solu_i);
                foreach (string elementSymbol in this.melts_dict.Keys)
                {

                    Element solu_j = new Element(elementSymbol);
                    Binary_model miedemal = new Binary_model();
                    if (elementSymbol != solvent)
                    {
                        double x = this.melts_dict[elementSymbol];
                        
                        acf += this.melts_dict[elementSymbol] * inacoef.Activity_Interact_Coefficient_Model(solv, solu_i, solu_j,geo_Model, GeoModel);
                        
                    }

                   
                }

                lnYi = lnY0 + acf;

                


            }
            else
            {
                MessageBox.Show("组成中不存在" + solute_i);
                lnYi = 0.0;
            }
           
            return lnYi;

        }

        /// <summary>
        /// 在Wagner稀溶液模型基础上添加修正项
        /// </summary>
        /// <param name="composition"></param>
        /// <param name="i">溶质</param>
        /// <param name="k">基体元素</param>
        /// <param name="T">熔体温度</param>
        /// <param name="geo_Model">计算相互作用系数时使用的几何模型</param>
        /// <param name="GeoModel">几何模型的名称</param>
        /// <returns></returns>
        public double activity_coefficient_Pelton(string composition, string i, string k, double T,Geo_Model geo_Model,string GeoModel,bool isEntropy = true,string phase_state = "liquid")
        {
            set_CompositionDict(composition);
            Element solv = new Element(k);
            Element solui = new Element(i);
            double lnYi_0 = 0, lnYi = 0;
            Ternary_melts melts = new Ternary_melts(T,phase_state,isEntropy);
            lnYi_0 = melts.lnY0(solv, solui);
            if (GeoModel == "T-K")
            {
                GeoModel = "Ding";
            }

            if (this.melts_dict.ContainsKey(solv.Name) && this.melts_dict.ContainsKey(solui.Name))
            {
                double sum_xsij = 0,sum_xskj = 0;
                foreach (var item in this.melts_dict)
                {
                    if (item.Key != solv.Name)
                    {
                        //计算∑xjɛ^j_i
                        double sji = melts.Activity_Interact_Coefficient_Model(solv, solui, new Element(item.Key), geo_Model, GeoModel);
                        sum_xsij += sji * item.Value;
                    }
                }
              
                for (int p = 0; p < this.melts_dict.Count; p++)
                {
                    //计算∑xj*xi*ɛ^j_i
                    for (int q = p; q < this.melts_dict.Count; q++)
                    {
                        
                        string m, n;
                        m = this.melts_dict.ElementAt(p).Key;
                        n = this.melts_dict.ElementAt(q).Key;
                        if (m != solv.Name && n != solv.Name)
                        {
                            double xm, xn;
                            xm = this.melts_dict.ElementAt(p).Value;
                            xn = this.melts_dict.ElementAt(q).Value;
                            double sji = melts.Activity_Interact_Coefficient_Model(solv, new Element(m), new Element(n), geo_Model, GeoModel);


                            sum_xskj += xm * xn * sji;
                        }
                        
                        
                    }
                }
                
                lnYi = lnYi_0 + sum_xsij - 1.0 / 2 * sum_xskj; 
                return lnYi;
            }
            else
            { return 0.0; }


           
        }
        
       

        public Tuple<string,double> getTuple(string A, double x) 
        {
            return new Tuple<string, double>(A,x);
        }


    }
}
