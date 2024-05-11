using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.IO;
using MathNet.Numerics.Integration;
using MathNet.Numerics;
using System.Linq;


namespace AlloyAct_Pro
{

    class Binary_model
    {
        private const double P_TT = constant.P_TT;
        private const double P_TN = constant.P_TN;
        private const double P_NN = constant.P_NN;
        private const double QtoP = constant.QtoP;
        private Element Ea { get; set; }
        private Element Eb { get; set; }
        private string state { get => _state; }

        private double lammda { get => _lammda;  }
        
        private string _state;
        private enum  _orderDegree  {SS,AMP,IM};
        private double _lammda =0;
        private double T { get; set; }
        private bool isEntropy { get; set; }

        public Binary_model() 
        {
           
        }
        public Binary_model(Element A, Element B)
        {
            this.Ea = A;
            this.Eb = B;

        }
        public void setTemperature(double Tem)
        {
            this.T = Tem;
        }
        public void setPairElement(string element_a, string element_b)
        {

            this.Ea = new Element(element_a);
            this.Eb = new Element(element_b);
        }
        
        public void setState(string state)
        {
            this._state = state;

        }
        public void setLammda(double n)
        {
            this._lammda = n;

        }
        public void setEntropy(bool is_sE)
        {
            this.isEntropy = is_sE;
        }

        protected double Abs(double x)
        { return Math.Abs(x); }
        protected double pow(double x, double y)
        {
            return Math.Pow(x, y);
        }
        public double fab(Element Ea, Element Eb, string state)
        {
            double diff;
            

            if (Ea.isExist && Eb.isExist)
            {
                double P_AB, RP;
                P_AB = (Ea.isTrans_group && Eb.isTrans_group) ? P_TT : ((Ea.isTrans_group || Eb.isTrans_group) ? P_TN : P_NN);
                RP = rp(Ea, Eb, state);
                
                diff = 2 * P_AB * (-pow(Ea.Phi - Eb.Phi, 2.0) + QtoP * pow(Ea.N_WS - Eb.N_WS, 2.0) - RP) / (1.0 / Ea.N_WS + 1.0 / Eb.N_WS);
            }
            else { diff = Double.NaN; }
            return diff;


        }
        
     
     

        /// <summary>
        /// extract the information of Compound AxBy,return (A,x,B,y)
        /// </summary>
        /// <param name="compound">AxBy</param>
        /// <returns></returns>
        private (string A, double x, string B, double y) extract_Compound(string compound)
        {
            Regex_Extend re = new Regex_Extend(@"^([A-Z]{1}[a-z]?)(\d+\.?\d*)?([A-Z]{1}[a-z]?)(\d+\.?\d*)?");

            string A, B;
            double x = 1.0, y = 1.0;


            GroupCollection groups = re.group(compound);
            bool t1 = double.TryParse(groups[2].Value, out x);
            bool t2 = double.TryParse(groups[4].Value, out y);
            if (t1) { } else { x = 1.0; }
            if (t2) { } else { y = 1.0; }
            A = groups[1].Value;
            B = groups[3].Value;
            return (A, x, B, y);
        }
        private double rp(Element _Ea, Element _Eb, string _state)
        {
            double alpha = 0.0;
            if (_state == "solid")
            {
                alpha = 1.0;
            }
            else
            {
                alpha = 0.73;
            }
            if (_Ea.hybird_factor == "other" || _Eb.hybird_factor == "other")
            {
                return 0.0;
            }
            else
            {
                return (_Ea.hybird_factor == _Eb.hybird_factor) ? 0.0 : alpha * _Ea.hybird_Value * _Eb.hybird_Value;
            }
        }

        /// <summary>
        /// H(X,Y)二元相互作用项，考虑过剩熵时返回过剩吉布斯自由能，否则返回混合焓,kJ/mol
        /// </summary>
        /// <param name="A">元素A</param>
        /// <param name="B">元素B</param>
        /// <param name="Xa">A的摩尔组成</param>
        /// <param name="Xb">B的摩尔组成</param>
        /// <returns>二元系性质kJ/mol</returns>
        public double binary_Model(string A, string B, double Xa, double Xb)
        {
            setPairElement(A, B);

            double f_AB = fab(this.Ea, this.Eb, this.state);
            double entropy_term = 0;
            if (this.isEntropy)
            {
                double avg_Tm = 1.0/this.Ea.Tm + 1.0/this.Eb.Tm;
                if (this.state == "solid")
                {
                    entropy_term = 1.0 / 15.1 * avg_Tm * this.T;
                }
                else
                {
                    entropy_term = 1.0 / 14 * avg_Tm * this.T;
                }

            }
            f_AB = f_AB * (1 - entropy_term);

            double Vaa, Vba;
            (Vaa, Vba) = V_inalloy(this.Ea, this.Eb, Xa, Xb);

            double fB;

            double cA, cB, cAS, cBS;
            cA = Xa / (Xa + Xb);
            cB = Xb / (Xa + Xb);

            cAS = cA * Vaa / (cA * Vaa + cB * Vba);
            cBS = cB * Vba / (cA * Vaa + cB * Vba);
            fB = cBS * (1 + lammda * Math.Pow(cAS * cBS, 2.0));

            double dH_trans = 0.0;
            
            dH_trans = this.Ea.dH_Trans * Xa / (Xa + Xb) + this.Eb.dH_Trans * Xb / (Xa + Xb);
            
            return fB * f_AB*cA*Vaa + dH_trans;


           
        }
       
        /// <summary>
        /// xa=1,xb=0时的O_AB
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="Xa"></param>
        /// <param name="Xb"></param>
        /// <returns></returns>
        public double infinity_enthalpy(string A, string B, double Xa = 1, double Xb = 0)
        {
            setPairElement(A, B);
            double Vb = this.Eb.V * (1 + this.Eb.u * (this.Eb.Phi - this.Ea.Phi));
            double fab = this.fab(this.Ea, this.Eb, this.state);
            double e_term = 0;
            
            
          
            return fab * Vb + this.Eb.dH_Trans;

        }
        /// <summary>
        /// 返回A在B中的偏摩尔溶解焓ΔH(B) = H + (1-xA)(dH/dxA)
        /// </summary>
        /// <param name="A">组元A</param>
        /// <param name="B">组元B</param>
        /// <param name="phaseState">合金相态，液态or固态</param>
        /// <param name="orderDegree">合金结构化参数，无序取0，有序取8.0，非晶5.0</param>
        /// <returns></returns>
        public double partialmolarEnthalpy(string A, string B, string phaseState, double orderDegree)
        {
            setPairElement(A, B);
            setState(phaseState);
            setLammda(orderDegree);
            double dH_trans = 0;
            double fAB = this.fab(this.Ea,this.Eb, this.state);
            double VAa = Ea.V * (1 + this.Ea.u * (Ea.Phi - Eb.Phi));
            

            return fAB * VAa + Ea.dH_Trans;

        }
       
        private (double V1, double V2) V_inalloy(Element Ea,Element Eb, double xa, double xb)
        {
            double VAa, VBa;
          
                double PAx, PBx;

                double new_VAa, new_VBa;
            double ya, yb;
            ya = xa/(xa+xb);
            yb = xb/(xb+xb);
                
            DateTime start = DateTime.Now;
            if (Ea.Name == "H" || Eb.Name == "H")
            {
                //H与其它元素形成有序化合物时，合金中的体积
                VAa = Ea.V;
                VBa = Eb.V;
                do
                {
                    new_VAa = VAa;
                    new_VBa = VBa;
                    PAx = ya * VAa / (ya * VAa + yb * VBa);
                    PBx = yb * VBa / (ya * VAa + yb * VBa);
                    VAa = Ea.V * (1 + Ea.u * PBx * (1 + lammda * Math.Pow(PAx * PBx, 2.0)) * (Ea.Phi - Eb.Phi));
                    VBa = Eb.V * (1 + Eb.u * PAx * (1 + lammda * Math.Pow(PAx * PBx, 2.0)) * (Eb.Phi - Ea.Phi));
                    DateTime stop = DateTime.Now; //获取代码段执行结束时的时间
                    TimeSpan tspan = stop - start;
                    if (tspan.TotalMilliseconds > 15000)
                    {
                        break;
                    }

                } while (VAa != new_VAa && VBa != new_VBa);

            }
            else
            {
                VAa = Ea.V*(1+Ea.u*ya*(Ea.Phi-Eb.Phi));
                VBa = Eb.V*(1+Eb.u*yb*(Eb.Phi-Ea.Phi));
            }

            
            return (VAa, VBa);
            

        }
     
       
       
       
       

        public double Elastic_AinB(string A, string B)
        {
            //for solid solution phase
            double dHe;
            setPairElement(A, B);
            double Va, Vb, alpha;
            alpha = -6.0 * Ea.V * (1 + Ea.u * (Ea.Phi - Eb.Phi)) / (1.0/Ea.N_WS + 1.0 / Eb.N_WS);
            Va = pow(Ea.V, 3.0 / 2) + alpha * (Eb.Phi - Ea.Phi) / pow(Ea.N_WS, 3.0);
            Vb = pow(Eb.V, 3.0 / 2) + alpha * (Eb.Phi - Ea.Phi) / pow(Eb.N_WS, 3.0);
            dHe = 2 * Ea.Bkm * Eb.Shm * pow(Vb - Va, 2.0) / (3 * Ea.Bkm * Vb + 4 * Eb.Shm * Va);
            return pow(10.0,-9.0)*dHe;
        }
        
        
        /// <summary>
        /// 1 mole B（溶质 solute）在体积无限大的A（溶剂solv）中的溶解热
        /// </summary>
        /// <param name="solv">溶剂</param>
        /// <param name="solute">溶质</param>
        /// <param name="state">状态</param>
        /// <returns></returns>
        public double Solution_Heat(Element solute, Element solv,  string state)
        {
          
            
           
            double diff = fab(solv, solute, state);
            double Vb = solute.V * (1.0 + solute.u * (solute.Phi - solv.Phi));
            double dHtrans;
            bool b = solv.Name == "Si" || solv.Name == "Ge";
           
                if (state == "liquid")
                {
                   
                    if (b)
                    {
                        dHtrans = 0;
                    }
                    else
                    {
                        dHtrans = solv.dH_Trans;
                    }

                }

                else
                {
                    dHtrans = solv.dH_Trans;
                }


            
            return (Vb * diff+dHtrans)*1000;
        }

       

        static Dictionary<string, double> yeta_DICT = new Dictionary<string, double>();
        /// <summary>
        /// 用于求GSM's 相似系数 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public double yeta(string k, string A, string B)
        {
            Binary_model m1 = new Binary_model();
            Binary_model m2 = new Binary_model();
            m1.setState("liquid");
            m2.setState("liquid");
            m1.setTemperature(this.T);
            m2.setTemperature(this.T);
                        
            Func<double, double> func = x => m1.binary_Model(A, B, x, 1 - x) -
                m2.binary_Model(A, k, x, 1 - x);

            Func<double, double> func2 = x => func(x) * func(x);
            double f;
            if (yeta_DICT.ContainsKey(k + A + B + this.T))
            {
                f = yeta_DICT[k + A + B + this.T];
            }
            else
            {
                f = Integrate.OnClosedInterval(func2, 0, 1);
                yeta_DICT.Add(k + A + B + this.T, f);
            }
            
            m1 = m2 = null;
            System.GC.Collect();
            return f;


        }
        static Dictionary<string, double> newdf_UEM2 = new Dictionary<string, double>();
        
     
        static Dictionary<string, double> df_UEM2 = new Dictionary<string, double>();
        public double deviation_Func(string k, string i, string j, double T)
        {

            Binary_model mij = new Binary_model();
            Binary_model mkj = new Binary_model();
            mij.setEntropy(true);
            mkj.setEntropy(true);
            mij.setPairElement(i, j);
            mij.setState("liquid");
            mij.setTemperature(T);
            mkj.setPairElement(k, j);
            mkj.setState("liquid");
            mkj.setTemperature(T);
            Func<double, double> func_ij = x =>  mij.binary_Model(i, j, x, 1 - x)*1000 /(8.314*T)
                                                ;
            Func<double, double> func_kj = x =>  mkj.binary_Model(k, j, x, 1 - x) * 1000 / (8.314 * T)
                                                   ;
            double f_ij, f_kj;
            string cond1 = i + j + this.lammda + this.state + this.T ;
            string cond2 = k + j + this.lammda + this.state + this.T;
            if (df_UEM2.Keys.Contains(cond1))
            {
                f_ij = df_UEM2[cond1];
            }
            else
            {
                f_ij = Integrate.OnClosedInterval(func_ij, 0, 1);
                df_UEM2.Add(cond1, f_ij);
            }
            if (df_UEM2.Keys.Contains(cond2))
            {
                f_kj = df_UEM2[cond2];
            }
            else
            {
                f_kj = Integrate.OnClosedInterval(func_kj, 0, 1);
                df_UEM2.Add(cond2, f_kj);
            }


           
             


            mkj = mij = null;
            System.GC.Collect();
            double tem = 0;
            tem = (f_ij - f_kj) / (f_ij + f_kj);
            return Abs(tem*1);
        }
        static Dictionary<string, double> df_UEM2advx = new Dictionary<string, double>();
        static Dictionary<string, double> df_UEM2advy = new Dictionary<string, double>();
        static Dictionary<string, double> df_UEM2advA = new Dictionary<string, double>();
        /// <summary>
        /// 计算函数围成的图像的中心坐标（x,y)
        /// </summary>
        /// <param name="k">xk</param>
        /// <param name="i">1-xk</param>
        /// <param name="phaseState"></param>
        /// <returns></returns>
        public (double x, double y) get_GraphicCenter(string k, string i, string phaseState = "liquid") 
        {
            Element Ei = null;
            Element Ek = null;
            Binary_model mki = new Binary_model();
          
            mki.setEntropy(true);
            mki.setPairElement(i, k);
            mki.setState("liquid");
            mki.setTemperature(T);
            Func<double, double> func_x = x => mki.binary_Model(k, i, x, 1 - x) * 1000 ;
            Func<double, double> xfunc_x = x => x * func_x(x) ;
            Func<double, double> func_x2 = x => func_x(x)*func_x(x);

            double x_bar ;
            double A ;
            double y;

           
            string cond1 = i + k + this.lammda + this.state + this.T;

            if (df_UEM2advx.Keys.Contains(cond1))
            {
                x_bar = df_UEM2advx[cond1];
                A = df_UEM2advA[cond1];
                y = df_UEM2advy[cond1];
            }
            else
            {
                x_bar = Integrate.OnClosedInterval(xfunc_x, 0, 1);
                A = Integrate.OnClosedInterval(func_x, 0, 1);
                y = Integrate.OnClosedInterval(func_x2, 0, 1);
                df_UEM2advx.Add(cond1, x_bar);
                df_UEM2advA.Add(cond1, A);
                df_UEM2advy.Add(cond1 , y);
            }
            

            double x_ = x_bar / A;
            double y_ = y / (2.0 * A);

            return (x_-0.5,y_); 

        }

        /// <summary>
        /// 计算组分k与i的性质差，k-j，i-j相互作用的性质差
        ///  //D_ki=h(x1,x2)t(y1,y2),h(x1,x2)=|x1-x2|/|x1+x2|,t(y1,y2) = exp(|y1-y2|/|y1+y2|)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public double get_D_ki(string k, string i, string j) 
        {
            double xkj, xij, ykj, yij;
            (xij, yij) = get_GraphicCenter(i, j);
            (xkj, ykj) = get_GraphicCenter(k, j);

            double hx1_x2 = Abs(xij - xkj) / Abs(xij + xkj);
            double ty1_y2 = Math.Exp(Abs(yij - ykj) / Abs(yij + ykj));

            double theta10, theta20, theta11, theta21;
            theta10 = Math.Atan2(xij, yij);
            theta11 = Math.Atan2(yij, xij);
            theta20 = Math.Atan2(xkj, ykj);
            theta21 = Math.Atan2(ykj, xkj);
            double a, b;
            a = Math.Sqrt(xij * xij + yij * yij);
            b = Math.Sqrt(xkj*xkj + ykj*ykj);
            double dki = Abs((Math.PI/2.0  * (theta10*theta10 - theta20*theta20) + delta_x(theta10, theta20)) /(theta10*theta10 + theta20*theta20))*Abs(a-b) /Math.Sqrt(a*a+b*b);
            return dki;

        }
        public double delta_x(double x, double y)
        {
            if ((Abs(x) >= 0 && Abs(x) <= Math.PI / 2.0) && (Abs(y) >= 0 && Abs(y) <= Math.PI / 2.0))
            {
                return 0;
            }
            else if ((Abs(x) >= Math.PI / 2.0 && Abs(x) <= Math.PI) && (Abs(y) >= Math.PI / 2.0 && Abs(y) <= Math.PI))
            {
                return 0;
            }
            else
            {
                return Math.PI / 2.0;
            }

            
        }
              
       
        public double UEM1(string k, string i, string j, string mode)
        {
            double alpha_KA, wka, wkb, identy_ka, identy_kb;
            double R = constant.R;
            Element Ek = new Element(k);
            Element Ei = new Element(i);
            Element Ej = new Element(j);

            Ternary_melts ternary_ = new Ternary_melts();
            ternary_.setState(state);
            ternary_.setTemperature(T);

            double inter_ik, inter_ki, inter_jk, inter_kj;
            inter_ik = ternary_.kexi(Ek, Ei);   
            inter_ki = ternary_.kexi(Ei, Ek);
            inter_jk = ternary_.kexi(Ek, Ej);
            inter_kj = ternary_.kexi(Ej, Ek);

            double df_ki, df_kj;
            df_ki = Abs(inter_ik - inter_ki);
            df_kj = Abs(inter_jk - inter_kj);

            if (df_ki == 0 && df_kj == 0)
            {
                df_ki = df_kj = -0.0000000000001;
            }

            double alpha = Math.Exp(-df_ki);
            double beta3 = df_kj / (df_ki + df_kj);
            alpha_KA = alpha * beta3;

            ternary_ = null;
            System.GC.Collect();

            return alpha_KA;
        }
        public double UEM2(string k, string i, string j, string mode)
        {
            double alpha_KA, alpha_KA2;
            double R = constant.R;
            double identified_term;
            Element Ek = new Element(k);
            Element Ei = new Element(i);
            Element Ej = new Element(j);


            Ternary_melts ternary_ = new Ternary_melts();

            ternary_.setState(state);
            ternary_.setTemperature(T);
            ternary_.setEntropy(false);
            double weight1 = 0;
            double inter_IJ, inter_KJ, inter_KI, inter_JI;
            double df_KI, df_KJ;
            if (mode == "UEM2-N")
            {
                df_KI = deviation_Func(k, i, j, T);
                df_KJ = deviation_Func(k, j, i, T);


                df_KI = 1 * pow(df_KI, 1);
                df_KJ = 1 * pow(df_KJ, 1);

                weight1 = df_KJ / (df_KI + df_KJ);

                alpha_KA = Math.Exp(-df_KI) * weight1;


            }
           

            else
            {
                //UEM2_adv 图形中心之差
                double df_ki, df_kj, df_ij;
                df_ki = get_D_ki(k,i,j);
                df_kj = get_D_ki(k,j,i);
                

               
                if ((df_ki == 0||df_ki == double.NaN) &&  (df_kj == 0 || df_kj == double.NaN)) 
                {
                    weight1 = 1;
                }
                else
                {
                    weight1 = df_kj / (df_ki + df_kj);
                }

                alpha_KA = Math.Exp(-df_ki) * weight1;

            }



            ternary_ = null;
            Ek = Ei = Ej = null;
            System.GC.Collect();


            return alpha_KA;
        }
        public double GSM(string k, string i, string j, string mode)
        {
            double nki, nkj;
            nki = yeta(k, i, j);
            nkj = yeta(k, j, i);

            return nki / (nki + nkj);
        }

    }
}
