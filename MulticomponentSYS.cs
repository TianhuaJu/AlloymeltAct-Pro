using System.Collections;
using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;

namespace AlloyAct_Pro
{


    class binary_tuple
    {
        private string _a;
        private string _b;
        private string _name;
        private Dictionary<string, Dictionary<string, double>> _compositionInfo_Dict = new Dictionary<string, Dictionary<string, double>>();
        public Dictionary<string, Dictionary<string, double>> compositionInfo_Dict { get => _compositionInfo_Dict; }
        public string Name { get => _name; }
        public string A { get => _a; }
        public string B { get => _b; }
        public binary_tuple(string T1, string T2)
        {
            this._a = T1;
            this._b = T2;
            this._name = this.A + this.B;
        }
        public bool Equals(binary_tuple binary_Tuple)
        {
            if (this.A == binary_Tuple.A && this.B == binary_Tuple.B)
            {
                return true;
            }
            else if (this.A == binary_Tuple.B && this.B == binary_Tuple.A)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void reverse()
        {
            string C;

            C = this.A;
            this._a = this._b;
            this._b = C;

        }
        public void compositionInfo_Add(string Member, string contributor_Member, double x)
        {
            Dictionary<string, double> constructInfo = new Dictionary<string, double>();
            constructInfo.Add(contributor_Member, x);

            if (this._compositionInfo_Dict.ContainsKey(Member))
            {
                if (this._compositionInfo_Dict[Member].ContainsKey(contributor_Member))
                {
                    this._compositionInfo_Dict[Member][contributor_Member] = x;

                }
                else
                {
                    this._compositionInfo_Dict[Member].Add(contributor_Member, x);
                }

            }
            else
            {
                this._compositionInfo_Dict.Add(Member, constructInfo);
            }


        }
        public bool isContain(string A)
        {
            if (this.A == A || this.B == A)
            {
                return true;
            }
            else
            { return false; }
        }
        public static bool operator !=(binary_tuple b1, binary_tuple b2)
        {
            if (b1.Equals(b2))
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        public static bool operator ==(binary_tuple b1, binary_tuple b2)
        {
            if ((b1.A == b2.A && b1.B == b2.B) || (b1.A == b2.B && b1.B == b2.A))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

    }
    /// <summary>
    /// 由3个组元组成的3元系
    /// </summary>
    class ternary_tuple
    {


        private string _a;
        private string _b;
        private string _c;
        private string _name;
        public string Name { get => _name; }
        public string A { get => _a; }
        public string B { get => _b; }
        public string C { get => _c; }
        public ternary_tuple(string T1, string T2, string T3)
        {
            this._a = T1;
            this._b = T2;
            this._c = T3;
            this._name = this.A + this.B + this.C;
        }
        public string odd_component(binary_tuple b1)
        {
            string a, b, c;
            (a, b, c) = (this.A, this.B, this.C);
            bool t1 = b1.isContain(a);
            bool t2 = b1.isContain(b);
            bool t3 = b1.isContain(c);

            if (!t1)
            {
                return a;
            }
            if (!t2)
            {
                return b;
            }
            if (!t3)
            {
                return c;
            }
            else
            {
                return null;
            }

        }
        public bool isContains(string A)
        {
            if (this.A == A || this.B == A || this.C == A)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


    }
    /// <summary>
    /// 可拆分为3个二元系的三元系
    /// </summary>
    class Ternary_tuple
    {


        private binary_tuple _binary1;
        private binary_tuple _binary2;
        private binary_tuple _binary3;
        public binary_tuple binary1 { get => _binary1; }
        public binary_tuple binary2 { get => _binary2; }
        public binary_tuple binary3 { get => _binary3; }
        public Ternary_tuple(binary_tuple b1, binary_tuple b2, binary_tuple b3)
        {
            this._binary1 = b1;
            this._binary2 = b2;
            this._binary3 = b3;

        }
        private List<string> subDifferent(string A, List<string> B)
        {
            List<string> subdiff = new List<string>();
            foreach (var item in B)
            {
                if (A != item)
                {
                    subdiff.Add(item);
                }
            }
            return subdiff;
        }
        public bool isContains(binary_tuple binary)
        {
            if (binary.Equals(this.binary1) || binary.Equals(this.binary2) || binary.Equals(this.binary3))
            {
                return true;
            }
            return false;
        }
        public bool Equals(Ternary_tuple ternary)
        {
            if (ternary.isContains(binary1) && ternary.isContains(binary2) && ternary.isContains(binary3))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isCoaxial(Ternary_tuple ternary)
        {
            if (this.isContains(ternary.binary1))
            {
                return true;

            }
            else if (this.isContains(ternary.binary2))
            {
                return true;
            }
            else if (this.isContains(ternary.binary3))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public binary_tuple get_Coaxial(binary_tuple binary)
        {
            if (this.isContains(binary))
            {
                if (this.binary1.Equals(binary))
                {
                    return this.binary1;

                }
                else if (this.binary1.Equals(binary))
                {
                    return this.binary2;
                }
                else
                {
                    return this.binary3;
                }

            }

            else
            {
                return null;
            }


        }
        public ternary_tuple degrade()
        {
            string A, B, C, D, E, F;
            binary_tuple b1, b2, b3;
            (b1, b2, b3) = (this.binary1, this.binary2, this.binary3);
            (A, B, C, D, E, F) = (b1.A, b1.B, b2.A, b2.B, b3.A, b3.B);
            List<string> lst = new List<string>() { A, B, C, D, E, F };
            List<string> sublst1 = subDifferent(A, lst);
            List<string> sublst2 = subDifferent(sublst1[0], sublst1);
            string a, b, c;
            if (sublst2.Count >= 1)
            {
                (a, b, c) = (A, sublst1[0], sublst2[0]);
                return new ternary_tuple(a, b, c);

            }




            else
            {
                return null;
            }

        }


    }



    class MultiComponnetSYS
    {

        private Dictionary<string, double> _multiComponnets_dict;
        private string _state = "liquid";
        private double _lammda;
        private double _Temp;
        private Dictionary<binary_tuple, (double Xi, double Xj)> _binary;
        private Entropy entropy
        {
            get
            {
                return new Entropy();
            }
        }

        public string state { get => _state; }
        public double lammda { get => _lammda; }
        public double Temp { get => _Temp; }
        /// <summary>
        /// 计算多元混合体的理想混合熵，S^id = -R(xi∑lnxi),kJ/mol
        /// </summary>
        public double Entropy_id
        {
            get
            {
                double entropy_id = 0;

                foreach (var item in Initial_Componnets_Dict.Values)
                {
                    if (item != 0)
                    {
                        entropy_id += this.entropy.id_Entropy(item);

                    }

                }
                return entropy_id / 1000.0;
            }
        }
        private const double R = constant.R;
        /// <summary>
        /// {"A",x}形式的字典集合，用于存储1mol熔体中每个组元的含量
        /// </summary>
        public Dictionary<string, double> Initial_Componnets_Dict
        {
            get
            {
                double sum = 0;
                Dictionary<string, double> molarFraction_dict = new Dictionary<string, double>();
                foreach (var item in this._multiComponnets_dict.Values)
                {
                    sum += item;
                }
                foreach (string item in this._multiComponnets_dict.Keys)
                {
                    if (sum != 0)
                    {
                        if (this._multiComponnets_dict[item] != 0.0)
                        {
                            molarFraction_dict.Add(item, this._multiComponnets_dict[item] * 1.0 / sum);

                        }

                    }

                }
                return molarFraction_dict;
            }
        }
        private Dictionary<binary_tuple, (double Xi, double Xj)> Binary { get => _binary; }



        public MultiComponnetSYS()
        {
        }
        public MultiComponnetSYS(string composition)
        {
            this.setComposition(composition);
        }

        public MultiComponnetSYS(Dictionary<string, double> component_dict)
        {
            this._multiComponnets_dict = component_dict;
        }

        /// <summary>
        /// 熔体状态
        /// </summary>
        /// <param name="state"></param>
        public void setstate(string state)
        {
            this._state = state;
        }

        /// <summary>
        /// 设置熔体温度
        /// </summary>
        /// <param name="Temp"></param>
        public void setTemp(double Temp)
        {
            this._Temp = Temp;
        }
        /// <summary>
        /// 设定合金组成，AxByCz存储为{"A",x},{"B",y},{"C",z}形式
        /// </summary>
        /// <param name="Composition"></param>
        public void setComposition(string Composition)
        {
            //设定合金组成，AxByCz存储为{"A",x},{"B",y},{"C",z}形式
            Dictionary<string, double> compo_dict = new Dictionary<string, double>();
            Dictionary<string, double> dict = new Dictionary<string, double>();
            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");
            MatchCollection matchs = re.Matches(Composition);

            foreach (Match match in matchs)
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
                if (compo_dict.ContainsKey(A))
                {
                    compo_dict[A] = x;
                }
                else
                {
                    compo_dict.Add(A, x);
                }

            }

            double sum = 0.0;
            foreach (var item in compo_dict.Values)
            {
                sum += item;
            }
            if (sum != 0.0)
            {
                foreach (var item in compo_dict.Keys)
                {
                    dict.Add(item, compo_dict[item] / sum);

                }

            }

            this._multiComponnets_dict = dict;
        }
        public void add_component(string component, double x)
        {
            if (this.Initial_Componnets_Dict.Count > 0)
            {
                if (this.Initial_Componnets_Dict.ContainsKey(component))
                {
                    this._multiComponnets_dict[component] = x;
                }
                else
                {
                    this._multiComponnets_dict.Add(component, x);
                }

            }
            else
            {
                this._multiComponnets_dict.Add(component, x);

            }
        }



        protected List<binary_tuple> binary_Lst(Dictionary<string, double> composition_dict)
        {
            List<string> component_list = new List<string>(composition_dict.Keys);
            List<binary_tuple> pair_list = new List<binary_tuple>();
            for (int i = 0; i < component_list.Count; i++)

            {

                for (int j = i + 1; j < component_list.Count; j++)
                {

                    binary_tuple _tuple = new binary_tuple(component_list[i], component_list[j]);
                    pair_list.Add(_tuple);

                }



            }
            return pair_list;

        }




        static Dictionary<string, double> Contri_eff1 = new Dictionary<string, double>();
        static Dictionary<string, double> Contri_eff2 = new Dictionary<string, double>();

        /// <summary>
        /// 指定几何模型给出的子二元系摩尔组成
        /// </summary>
        /// <param name="original_composition">系统的原始组成</param>
        /// <param name="geo_Model">几何模型</param>
        /// <param name="T">系统的温度</param>
        /// <returns></returns>
        protected Dictionary<binary_tuple, Dictionary<string, double>> sub_Binary_Composition(Dictionary<string, double> original_composition, Geo_Model geo_Model, string GeoModel, double T)
        {

            List<binary_tuple> binaryLst = binary_Lst(original_composition);
            Dictionary<binary_tuple, Dictionary<string, double>> binary_composition = new Dictionary<binary_tuple, Dictionary<string, double>>();

            foreach (binary_tuple bItem in binaryLst)
            {
                Dictionary<string, double> bcomp = new Dictionary<string, double>();
                double delta_kAB = 0, delta_kBA = 0, Xa, Xb;
                foreach (var item in original_composition)
                {
                    if (!bItem.isContain(item.Key))
                    {
                        string K, A, B;
                        K = item.Key;
                        A = bItem.A;
                        B = bItem.B;

                        delta_kAB = delta_kAB + original_composition[item.Key] * geo_Model(K, A, B, GeoModel);
                        delta_kBA = delta_kBA + original_composition[item.Key] * geo_Model(K, B, A, GeoModel);

                    }
                }

                Xa = original_composition[bItem.A] + delta_kAB;
                Xb = original_composition[bItem.B] + delta_kBA;
                Xa = Xa / (Xa + Xb);
                Xb = 1 - Xa;
                if (bcomp.ContainsKey(bItem.A))
                {
                    bcomp[bItem.A] = Xa;
                }
                else
                {
                    bcomp.Add(bItem.A, Xa);
                }
                if (bcomp.ContainsKey(bItem.B))
                {
                    bcomp[bItem.B] = Xb;
                }
                else
                {
                    bcomp.Add(bItem.B, Xb);
                }
                if (binary_composition.ContainsKey(bItem))
                {
                    binary_composition[bItem] = bcomp;
                }
                else
                {
                    binary_composition.Add(bItem, bcomp);
                }
            }

            return binary_composition;



        }
        /// <summary>
        /// 导出贡献系数值
        /// </summary>
        /// <param name="path">保存路径</param>
        /// <param name="dictionary">构成元素的字典</param>
        /// <param name="geo_Model">几何模型</param>
        /// <param name="GeoModel">几何模型标签</param>
        /// <param name="T">系统温度</param>
        public void output_contri_eff(string path, Dictionary<string, double> dictionary, Geo_Model geo_Model, string GeoModel, double T)
        {
            double alpha_ki_ij, alpha_kj_ij, alpha_ij_kj, alpha_ik_kj, alpha_ji_ik, alpha_jk_ik;
            ArrayList arrayList = new ArrayList();
            Dictionary<string, double> k_i_ij_dict = new Dictionary<string, double>();

            string ki_ij;
            List<string> list = new List<string>();

            foreach (var item in dictionary.Keys)
            {
                arrayList.Add(item);
            }
            string solv, solutei, solutej;
            for (int i = 0; i < arrayList.Count; i++)
            {
                for (int j = i + 1; j < arrayList.Count; j++)
                {
                    for (int k = j + 1; k < arrayList.Count; k++)
                    {

                        solv = arrayList[k].ToString();
                        solutei = arrayList[i].ToString();
                        solutej = arrayList[j].ToString();

                        alpha_ij_kj = geo_Model(solutei, solutej, solv, GeoModel);
                        alpha_ik_kj = geo_Model(solutei, solv, solutej, GeoModel);
                        alpha_ji_ik = geo_Model(solutej, solutei, solv, GeoModel);
                        alpha_jk_ik = geo_Model(solutej, solv, solutei, GeoModel);
                        alpha_ki_ij = geo_Model(solv, solutei, solv, GeoModel);
                        alpha_kj_ij = geo_Model(solv, solutej, solutei, GeoModel);

                        ki_ij = string.Format("{0}-{1}: \t {3}, \t {0}-{2}: \t {4} \t in ( {1}-{2})\n" +
                                             "{1}-{0}: \t {8}, \t {1}-{2}: \t {7} \t in ( {2}-{0})\n" +
                                             "{2}-{1}: \t {5}, \t {2}-{0}: \t {6} \t in ( {1}-{0})\n",
                                             solv, solutei, solutej, alpha_ki_ij, alpha_kj_ij, alpha_ji_ik, alpha_jk_ik, alpha_ij_kj, alpha_ik_kj);
                        list.Add(ki_ij);

                    }
                }
            }




            string content = "";
            foreach (var item in list)
            {

                content += item + '\r' + '\n';
            }

            myFunctions.WriteLog(path + "\\" + "贡献系数" + "(" + GeoModel + ").txt", content);


        }



        /// <summary>
        /// 外推模型表示的多元系溶体性质
        /// </summary>
        /// <param name="initial_Composition">溶体的组成</param>
        /// <param name="b_func">二元相互作用函数</param>
        /// <param name="geo_Model">外推模型</param>
        /// <param name="GeoModel">外推模型代码</param>
        /// <param name="T">溶体的温度</param>
        /// <returns></returns>
        public double multi_ComponentProperties_byUEM(Interaction_Func_b b_func, Geo_Model geo_Model, string GeoModel, double T)
        {
            double sum = 0;

            Dictionary<binary_tuple, Dictionary<string, double>> sub_binaryComp = sub_Binary_Composition(this.Initial_Componnets_Dict, geo_Model, GeoModel, T);

            foreach (var item in sub_binaryComp)
            {
                string A, B;
                double xA, xB, yA, yB;
                A = item.Key.A; B = item.Key.B;
                xA = this.Initial_Componnets_Dict[A]; xB = this.Initial_Componnets_Dict[B];
                yA = item.Value[A]; yB = item.Value[B];

                sum = sum + b_func(A, B, yA, yB) * xA * xB / (yA * yB);
            }
            return sum;


        }



    }
}
