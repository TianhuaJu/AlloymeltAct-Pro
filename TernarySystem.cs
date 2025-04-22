namespace AlloyAct_Pro
{


    class Ternary_melts
    {
        private (Element M, Element N, double ration) Bbased;
        private double Tem { get; set; }
        private bool entropy { get => _entropy; }
        private bool _cp;
        private bool cp { get => _cp; }

        private string state { get => _state; }
        private static double R = constant.R;
        private bool _entropy = false;
        private (bool entropy, bool cp) _condition = (false, false);
        private string _state;

        private (bool entropy, bool cp) condition { get => _condition; }
        delegate double delgateFfab(Element Ei, Element Ej, bool extra_term = false);

        public Ternary_melts(double T, string phaseState = "liquid", bool isSE = false)
        {


            this._condition = (isSE, cp);
            this._state = phaseState;
            this.Tem = T;



            this._entropy = isSE;

        }
        public Ternary_melts()
        {



        }

        public void setTemperature(double Tem)
        {
            this.Tem = Tem;
        }



        public void setEntropy(bool entropy)
        {
            this._entropy = entropy;
        }
        public void setState(string state)
        {
            this._state = state;
        }

        /// <summary>
        /// apply for adding excess entropy by Witusiewicz
        /// </summary>
        /// <param name="Ea"></param>
        /// <param name="Eb"></param>
        /// <returns></returns>
        private double alpha_ab(Element Ea, Element Eb)
        {
            double omga;
            omga = 1.0 / (2 * Math.PI) * Pow((Ea.Tm + Eb.Tm) / (Ea.Tb + Eb.Tb) + 1, Math.E);

            return Pow(omga - 1, 2) / (this.Tem - 298 * omga) * this.Tem;
        }
        /// <summary>
        /// apply for adding excess entropy by Witusiewicz
        /// </summary>
        /// <param name="Ea"></param>
        /// <param name="Eb"></param>
        /// <returns></returns>
        private double belta_ab(Element Ea, Element Eb)
        {
            double omga, Pt, beta;
            omga = 1.0 / (2 * Math.PI) * Pow((Ea.Tm + Eb.Tm) / (Ea.Tb + Eb.Tb) + 1, Math.E);
            Pt = 1.0 / 2 + 4 * (Ea.Tm + Eb.Tm) / (6 * this.Tem) + 2 * Math.Log(this.Tem * 2 / (Ea.Tb + Eb.Tb - Ea.Tm - Eb.Tm));
            beta = (1 - omga) * omga * constant.R * (2 - 4 * (Ea.Tm + Eb.Tm) / (6 * this.Tem)) - omga * this.Tem * constant.R * Pt;
            return beta;
        }

        /// <summary>
        /// 纯粹的Fab,不含熵项
        /// </summary>
        /// <param name="Ei"></param>
        /// <param name="Ej"></param>
        /// <returns></returns>
        private double fab_pure(Element Ei, Element Ej, bool S = false)
        {


            double fij, rp, P, alpha;


            if (this.state == "liquid")
            {
                alpha = 0.73;
            }
            else
            {
                alpha = 1.0;
            }

            if (Ei.hybird_factor != "other" || Ej.hybird_factor != "other")
            {
                rp = (Ei.hybird_factor == Ej.hybird_factor) ? 0.0 : Ei.hybird_Value * Ej.hybird_Value;
            }
            else
            {
                rp = 0.0;
            }


            P = (Ei.isTrans_group && Ej.isTrans_group) ? constant.P_TT : ((Ei.isTrans_group || Ej.isTrans_group) ? constant.P_TN : constant.P_NN);


            fij = 2 * P * (constant.QtoP * Math.Pow(Ei.N_WS - Ej.N_WS, 2.0) - Math.Pow(Ei.Phi - Ej.Phi, 2.0) - alpha * rp) / (1 / Ei.N_WS + 1 / Ej.N_WS);

            return fij;
        }
        /// <summary>
        /// 添加过剩热容，fab是Ding定义的，包含了a，b的体积项
        /// </summary>
        /// <param name="Ei"></param>
        /// <param name="Ej"></param>
        /// <param name="state">熔体温度及相态</param>
        /// <param name="condition">是否考虑熵和热容</param>
        /// <returns></returns>
        private double Dfab_Func(Element Ei, Element Ej, bool s = true)
        {
            (bool entropy, bool Cp) condition = this.condition;
            (string phase, double Tem) state = (this.state, this.Tem);
            double fij = 0, sij, alpha, rp, P;
            double avg_Tm = 1.0 / Ei.Tm + 1.0 / Ej.Tm;


            if (condition.entropy)
            {
                if (state.phase == "liquid")
                {
                    sij = 1.0 / 14 * state.Tem * avg_Tm;

                }
                else
                {
                    sij = 1.0 / 15.1 * state.Tem * avg_Tm;
                }


            }
            else
            {
                sij = 0;
            }

            if (state.phase == "liquid")
            {
                alpha = 0.73;
            }
            else
            {
                alpha = 1.0;
            }
            if (Ei.hybird_factor != "other" || Ej.hybird_factor != "other")
            {
                rp = (Ei.hybird_factor == Ej.hybird_factor) ? 0.0 : Ei.hybird_Value * Ej.hybird_Value;
            }
            else
            {
                rp = 0.0;
            }
            P = (Ei.isTrans_group && Ej.isTrans_group) ? constant.P_TT : ((Ei.isTrans_group || Ej.isTrans_group) ? constant.P_TN : constant.P_NN);


            fij = 2 * P * Ei.V * Ej.V * (constant.QtoP * Pow(Ei.N_WS - Ej.N_WS, 2.0) - Pow(Ei.Phi - Ej.Phi, 2.0) - alpha * rp) / (1 / Ei.N_WS + 1 / Ej.N_WS);



            return fij * (1.0 - sij);
        }
        /// <summary>
        /// 纯Fab，包含熵项
        /// </summary>
        /// <param name="Ei"></param>
        /// <param name="Ej"></param>
        /// <returns></returns>
        private double fab_func_ContainS(Element Ei, Element Ej, bool S = false)
        {
            double alpha, Rp, Pij, entropy_term, fij;
            double avg_Tm = 1.0 / Ei.Tm + 1.0 / Ej.Tm;
            if (this.state == "liquid")
            {
                alpha = 0.73;
            }
            else
            { alpha = 1.0; }
            if (this.entropy)
            {
                if (this.state == "liquid")
                {
                    entropy_term = 1.0 / 14 * this.Tem * avg_Tm;
                }
                else
                {
                    entropy_term = 1.0 / 15.1 * this.Tem * avg_Tm;
                }

            }
            else
            { entropy_term = 0.0; }

            if (Ei.hybird_factor != "other" || Ej.hybird_factor != "other")
            {
                Rp = (Ei.hybird_factor == Ej.hybird_factor) ? 0.0 : Ei.hybird_Value * Ej.hybird_Value;
            }
            else
            {
                Rp = 0.0;
            }
            Pij = (Ei.isTrans_group && Ej.isTrans_group) ? constant.P_TT : ((Ei.isTrans_group || Ej.isTrans_group) ? constant.P_TN : constant.P_NN);
            fij = 2.0 * Pij * (constant.QtoP * Pow(Ei.N_WS - Ej.N_WS, 2.0) - Pow(Ei.Phi - Ej.Phi, 2.0) - alpha * Rp) / ((1.0 / Ei.N_WS + 1.0 / Ej.N_WS));


            return fij * (1 - entropy_term);


        }





        /// <summary>
        /// 用于计算UEM1条件下，两组分间的性质差。ξ^k_i，D_ki = Abs(ξ^k_i-ξ^i_k)
        /// 主要是对Si、Ge在液态下的转变焓及非金属元素在无限稀条件下的溶解自由能做了改动
        /// </summary>
        /// <param name="solvent"></param>
        /// <param name="solutei"></param>
        /// <returns></returns>
        public double kexi(Element solvent, Element solutei)
        {

            double lny0;

            double fik, dHtrans_i = 0, dHtrans_slv = 0, dHtrans = 0;


            fik = fab_pure(solvent, solutei);
            List<string> elemets_lst = new List<string>() { "Si", "Ge" };

            if (state == "liquid")
            {
                if (elemets_lst.Contains(solutei.Name))
                {
                    dHtrans_i = 0;
                }
                else
                {
                    dHtrans_i = solutei.dH_Trans;
                }

                if (elemets_lst.Contains(solvent.Name))
                {
                    dHtrans_slv = 0;
                }
                else
                {
                    dHtrans_slv = solvent.dH_Trans;
                }
            }

            dHtrans = dHtrans_slv;
            lny0 = 1000 * fik * solutei.V * (1 + solutei.u * (solutei.Phi - solvent.Phi)) / (R * Tem) + 1000 * dHtrans / (R * Tem);

            return lny0;

        }


        /// <summary>
        /// Q(x)的一阶偏导,
        /// </summary>
        /// <param name="solv"></param>
        /// <param name="solutei"></param>
        /// <returns></returns>
        public double first_Derative_Qx(Element i_element, Element j_element, double xi = 0)
        {//Q(x)的一阶偏导,
            double fij = fab_func_ContainS(i_element, j_element);
            double vi, vj, ui, uj, phi_i, phi_j;
            vi = i_element.V;
            vj = j_element.V;
            ui = i_element.u;
            uj = j_element.u;
            phi_i = i_element.Phi;
            phi_j = j_element.Phi;
            double delta_phi = phi_i - phi_j;

            double Ax = vi * (1 + ui * delta_phi * (1 - xi));
            double Bx = vj * (1 - uj * delta_phi * xi);
            double Dx = xi * Ax + (1 - xi) * Bx;
            double Nx = Ax * Bx;

            double dAx = -ui * delta_phi * vi;
            double dBx = -uj * delta_phi * vj;

            double dDx = Ax + xi * dAx - Bx + (1 - xi) * dBx;
            double dNx = dAx * Bx + Ax * dBx;

            double dfx = (dNx * Dx - dDx * Nx) / (Dx * Dx);


            return dfx;
        }

        /// <summary>
        /// Q(x)的二阶偏导在0处的值
        /// </summary>
        /// <param name="solv"></param>
        /// <param name="solutei"></param>
        /// <returns></returns>
        public double second_Derative_Q0(Element i_element, Element j_element, double xi = 0)
        {//Q(x)的二阶偏导在0处的值
            double fij = fab_func_ContainS(i_element, j_element);
            double Vj, Vi, ui, phi_i, phi_j, uj;
            Vi = i_element.V;
            Vj = j_element.V;
            ui = i_element.u;
            uj = j_element.u;
            phi_i = i_element.Phi;
            phi_j = j_element.Phi;
            double delta_phi = phi_i - phi_j;
            double dd_f = 2 * fij * Pow(Vi, 3) * (1 + 3 * ui * delta_phi + ui * ui * Pow(delta_phi, 2) + 2 * uj * delta_phi + ui * uj * delta_phi * delta_phi) / (Vj * Vj);


            return dd_f;
        }

        /// <summary>
        /// 无限稀活度系数lnYi0 = GE_i/(RT)
        /// </summary>
        /// <param name="solvent"></param>
        /// <param name="solutei"></param>
        /// <returns></returns>
        public double lnY0(Element solvent, Element solutei)
        {

            double lny0;

            double fik, dHtrans = 0;
            fik = fab_func_ContainS(solvent, solutei);


            dHtrans = solutei.dH_Trans;
            lny0 = 1000 * fik * solutei.V * (1 + solutei.u * (solutei.Phi - solvent.Phi)) + 1000 * dHtrans;

            return lny0 / (constant.R * Tem);

        }

        private double Pow(double x, double y)
        {
            return Math.Pow(x, y);
        }

        /// <summary>
        /// 固溶体中弹性项对一阶相互作用系数的贡献,使用新几何模型展开。
        /// </summary>
        /// <param name="solv"></param>
        /// <param name="solui"></param>
        /// <param name="soluj"></param>
        /// <param name="Fab"></param>
        /// <param name="contri_Func"></param>
        /// <returns></returns>
        private double PresentModel1_AIP_Elac(Element solv, Element solui, Element soluj, Geo_Model contri_Func, string GeoModel, string mode = "Normal")
        {
            double Hij, Hik, Hjk, dHik, dHjk;
            double Hj_in_i, Hi_in_j, Hi_in_k, Hk_in_i, Hj_in_k, Hk_in_j;
            double alphai_jk, alphai_kj, alphaj_ki, alphaj_ik, alphak_ij, alphak_ji;

            alphai_jk = contri_Func(solui.Name, soluj.Name, solv.Name, mode);
            alphaj_ik = contri_Func(soluj.Name, solui.Name, solv.Name, mode);
            alphai_kj = contri_Func(solui.Name, solv.Name, soluj.Name, mode);
            alphaj_ki = contri_Func(soluj.Name, solv.Name, solui.Name, mode);
            alphak_ij = contri_Func(solv.Name, solui.Name, soluj.Name, mode);
            alphak_ji = contri_Func(solv.Name, soluj.Name, solui.Name, mode);
            Hj_in_i = new Binary_model().Elastic_AinB(soluj.Name, solui.Name);
            Hi_in_j = new Binary_model().Elastic_AinB(solui.Name, soluj.Name);
            Hi_in_k = new Binary_model().Elastic_AinB(solui.Name, solv.Name);
            Hk_in_i = new Binary_model().Elastic_AinB(solv.Name, solui.Name);
            Hj_in_k = new Binary_model().Elastic_AinB(soluj.Name, solv.Name);
            Hk_in_j = new Binary_model().Elastic_AinB(solv.Name, soluj.Name);

            Hik = Hi_in_k;
            Hjk = Hj_in_k;
            dHik = alphaj_ik * (Hk_in_i - Hi_in_k);
            dHjk = alphai_jk * (Hk_in_j - Hj_in_k);

            Hij = alphak_ij / (alphak_ij + alphak_ji) * Hj_in_i + alphak_ji / (alphak_ij + alphak_ji) * Hi_in_j;



            return 1000.0 * (Hij - Hik - Hjk + dHik + dHjk) / (constant.R * Tem);

        }




        public double Activity_Interact_Coefficient_1st(Element solv, Element solui, Element soluj, Geo_Model geo_Model, string GeoModel = "UEM1")
        {


            double fij = fab_func_ContainS(solui, soluj);
            double fik = fab_func_ContainS(solv, solui);
            double fjk = fab_func_ContainS(solv, soluj);
            string filePath = Environment.CurrentDirectory + "\\" + "Contribution Coefficient\\";
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            double aji_ik = 0, aij_jk = 0, aki_ij = 0, akj_ij = 0, aik_jk = 0, ajk_ik = 0;
            double omaga_ij = 0, omaga_ik = 0, omaga_jk = 0, d_omaga_ik_j = 0, d_omaga_jk_i = 0, Via, Vja;


            aji_ik = geo_Model(soluj.Name, solui.Name, solv.Name, GeoModel);
            ajk_ik = geo_Model(soluj.Name, solv.Name, solui.Name, GeoModel);
            aij_jk = geo_Model(solui.Name, soluj.Name, solv.Name, GeoModel);
            aki_ij = geo_Model(solv.Name, solui.Name, soluj.Name, GeoModel);
            akj_ij = geo_Model(solv.Name, soluj.Name, solui.Name, GeoModel);
            aik_jk = geo_Model(solui.Name, solv.Name, soluj.Name, GeoModel);

            string fileName = filePath + GeoModel + ".txt";
            string content = string.Format("{0}-{1}: \t {3}, \t {0}-{2}: \t {4} \t in ( {1}-{2})\n" +
                                           "{1}-{0}: \t {8}, \t {1}-{2}: \t {7} \t in ( {2}-{0})\n" +
                                           "{2}-{1}: \t {5}, \t {2}-{0}: \t {6} \t in ( {1}-{0})\n",
                                           solv.Name, solui.Name, soluj.Name, aki_ij, akj_ij, aji_ik, ajk_ik, aij_jk, aik_jk);

            myFunctions.WriteLog(fileName, content);
            if (aki_ij == 0 && akj_ij == 0)
            {
                aki_ij = akj_ij = 0.5;
            }

            Via = (1 + solui.u * (solui.Phi - soluj.Phi) * akj_ij * soluj.V / (aki_ij * solui.V + akj_ij * soluj.V)) * solui.V;
            Vja = (1 + soluj.u * (soluj.Phi - solui.Phi) * aki_ij * solui.V / (aki_ij * solui.V + akj_ij * soluj.V)) * soluj.V;
            omaga_ij = fij * Via * Vja * (aki_ij + akj_ij) / (aki_ij * Via + akj_ij * Vja);
            omaga_ik = fik * solui.V * (1 + solui.u * (solui.Phi - solv.Phi));
            omaga_jk = fjk * soluj.V * (1 + soluj.u * (soluj.Phi - solv.Phi));
            d_omaga_ik_j = aji_ik * omaga_ik * (1 - solui.V / solv.V * (1 + 2 * solui.u * (solui.Phi - solv.Phi)));
            d_omaga_jk_i = aij_jk * omaga_jk * (1 - soluj.V / solv.V * (1 + 2 * soluj.u * (soluj.Phi - solv.Phi)));



            double chemical_term = omaga_ij - omaga_jk - omaga_ik + d_omaga_ik_j + d_omaga_jk_i;

            System.GC.Collect();

            return 1000 * chemical_term / (R * Tem);


            // Notes：考虑非金属元素的金属态转变会得到失败的结果

        }

        /// <summary>
        /// 二阶自身活度相互作用系数ρi^ii
        /// </summary>
        /// <param name="solv"></param>
        /// <param name="solui"></param>
        /// <param name="geo_Model"></param>
        /// <param name="GeoModel"></param>
        /// <returns></returns>
        public double Roui_ii(Element solv, Element solui, Geo_Model geo_Model, string GeoModel = "UEM1")
        {
            //二阶自身活度相互作用系数2*ρi^ii=(-sii + d^3G^E_m/dx^3 (x=0)*1/RT)

            double sii = Activity_Interact_Coefficient_1st(solv, solui, solui, geo_Model);
            double df10 = first_Derative_Qx(solui, solv, 0);
            double df20 = second_Derative_Q0(solui, solv, 0);

            double rii = -sii + 1000 * (-6 * df10 + 3 * df20) / (R * Tem);

            return rii * 1.0 / 2;
        }

        /// <summary>
        /// 二阶活度相互作用系数ρi^jj
        /// </summary>
        /// <param name="solv"></param>
        /// <param name="solui"></param>
        /// <param name="soluj"></param>
        /// <param name="geo_Model"></param>
        /// <param name="GeoModel"></param>
        /// <returns></returns>
        public double Roui_jj(Element solv, Element solui, Element soluj, Geo_Model geo_Model, string GeoModel = "UEM1")
        {
            //二阶活度相互作用系数2*ρi^jj=-sjj +  d^3G^E_m/dxidxjdxj (x=0)*1/RT

            double aji_ik = 0, aij_jk = 0, aki_ij = 0, akj_ij = 0, aik_jk = 0, ajk_ik = 0, Qij = 0, Qik = 0, Qjk = 0;
            double sjj = Activity_Interact_Coefficient_1st(solv, soluj, soluj, geo_Model);
            aji_ik = geo_Model(soluj.Name, solui.Name, solv.Name, GeoModel);
            ajk_ik = geo_Model(soluj.Name, solv.Name, solui.Name, GeoModel);
            aij_jk = geo_Model(solui.Name, soluj.Name, solv.Name, GeoModel);
            aki_ij = geo_Model(solv.Name, solui.Name, soluj.Name, GeoModel);
            akj_ij = geo_Model(solv.Name, soluj.Name, solui.Name, GeoModel);
            aik_jk = geo_Model(solui.Name, solv.Name, soluj.Name, GeoModel);

            Qij = -2 * aki_ij / Pow(aki_ij + akj_ij, 2) * first_Derative_Qx(solui, soluj, aki_ij / (aki_ij + akj_ij));
            Qik = aji_ik * aji_ik * second_Derative_Q0(solui, solv, 0) - 2 * aji_ik * (aji_ik + ajk_ik) * first_Derative_Qx(solui, solv, 0);
            Qjk = 2 * aij_jk * second_Derative_Q0(soluj, solv, 0) - 2 * (2 * aij_jk + aik_jk) * first_Derative_Qx(soluj, solv, 0);

            double ri_jj = (-sjj + 1000 * (Qij + Qik + Qjk) / (R * Tem));


            return ri_jj / 2.0;
        }
        /// <summary>
        /// 二阶交互活度相互作用系数ρi^ij
        /// </summary>
        /// <param name="solv"></param>
        /// <param name="solui"></param>
        /// <param name="soluj"></param>
        /// <param name="geo_Model"></param>
        /// <param name="GeoModel"></param>
        /// <returns></returns>
        public double Roui_ij(Element solv, Element solui, Element soluj, Geo_Model geo_Model, string GeoModel = "UEM1")
        {//二阶交互活度相互作用系数ρi^ij-sji +  d^3G^E_m/dxidxidxj (x=0)*1/RT

            double aji_ik = 0, aij_jk = 0, aki_ij = 0, akj_ij = 0, aik_jk = 0, ajk_ik = 0, Qij = 0, Qik = 0, Qjk = 0;
            double sji = Activity_Interact_Coefficient_1st(solv, solui, soluj, geo_Model);
            aji_ik = geo_Model(soluj.Name, solui.Name, solv.Name, GeoModel);
            ajk_ik = geo_Model(soluj.Name, solv.Name, solui.Name, GeoModel);
            aij_jk = geo_Model(solui.Name, soluj.Name, solv.Name, GeoModel);
            aki_ij = geo_Model(solv.Name, solui.Name, soluj.Name, GeoModel);
            akj_ij = geo_Model(solv.Name, soluj.Name, solui.Name, GeoModel);
            aik_jk = geo_Model(solui.Name, solv.Name, soluj.Name, GeoModel);

            Qij = 2 * akj_ij / Pow(akj_ij + aki_ij, 2) * first_Derative_Qx(solui, soluj, aki_ij / (aki_ij + aki_ij));
            Qik = 2 * aji_ik * second_Derative_Q0(solui, solv, 0) - 2 * (2 * aji_ik + ajk_ik) * first_Derative_Qx(solui, solv, 0);
            Qjk = aij_jk * aij_jk * second_Derative_Q0(soluj, solv, 0) - 2 * aij_jk * (aij_jk + aik_jk) * first_Derative_Qx(soluj, solv, 0);



            return (-sji + (Qij + Qik + Qjk) / (R * Tem));
        }

        /// <summary>
        /// 交互作用参数，组分j、k对i的影响
        /// </summary>
        /// <param name="m">基体</param>
        /// <param name="i">溶质i</param>
        /// <param name="j">溶质j</param>
        /// <param name="k">溶质k</param>
        /// <param name="geo_Model">外推模型</param>
        /// <param name="GeoModel">外推模型符号</param>
        /// <returns></returns>
        public double Roui_jk(Element m, Element i, Element j, Element k, Geo_Model geo_Model, string GeoModel = "UEM1")
        {
            //交互作用参数，组分j、k对i的影响
            double skj = Activity_Interact_Coefficient_1st(m, j, k, geo_Model);
            double amj_ij = 0, ami_ij = 0, aki_ij = 0, akj_ij = 0, dfij = 0;
            double amk_ik = 0, ami_ik = 0, aji_ik = 0, ajk_ik = 0, dfik = 0;
            double aji_im = 0, ajm_im = 0, aki_im = 0, akm_im = 0, dfim = 0, ddfim = 0;
            double amk_jk = 0, amj_jk = 0, aij_jk = 0, aik_jk = 0, dfjk = 0;
            double aij_jm = 0, aim_jm = 0, akj_jm = 0, akm_jm = 0, dfjm = 0, ddfjm = 0;
            double aik_km = 0, aim_km = 0, ajk_km = 0, ajm_km = 0, dfkm = 0, ddfkm = 0;

            amj_ij = geo_Model(m.Name, j.Name, i.Name, GeoModel);
            ami_ij = geo_Model(m.Name, i.Name, j.Name, GeoModel);
            aki_ij = geo_Model(k.Name, i.Name, j.Name, GeoModel);
            akj_ij = geo_Model(k.Name, j.Name, i.Name, GeoModel);

            amk_ik = geo_Model(m.Name, k.Name, j.Name, GeoModel);
            ami_ik = geo_Model(m.Name, i.Name, k.Name, GeoModel);
            aji_ik = geo_Model(j.Name, i.Name, i.Name, GeoModel);
            ajk_ik = geo_Model(j.Name, k.Name, i.Name, GeoModel);

            aji_im = geo_Model(j.Name, i.Name, m.Name, GeoModel);
            ajm_im = geo_Model(j.Name, m.Name, i.Name, GeoModel);
            aki_im = geo_Model(k.Name, i.Name, m.Name, GeoModel);
            akm_im = geo_Model(k.Name, m.Name, i.Name, GeoModel);

            amk_jk = geo_Model(m.Name, k.Name, j.Name, GeoModel);
            amj_jk = geo_Model(m.Name, j.Name, k.Name, GeoModel);
            aik_jk = geo_Model(i.Name, k.Name, j.Name, GeoModel);
            aij_jk = geo_Model(i.Name, j.Name, k.Name, GeoModel);

            aij_jm = geo_Model(i.Name, j.Name, m.Name, GeoModel);
            aim_jm = geo_Model(i.Name, m.Name, j.Name, GeoModel);
            akj_jm = geo_Model(k.Name, j.Name, m.Name, GeoModel);
            akm_jm = geo_Model(k.Name, m.Name, j.Name, GeoModel);

            aik_km = geo_Model(i.Name, k.Name, m.Name, GeoModel);
            aim_km = geo_Model(i.Name, m.Name, k.Name, GeoModel);
            ajk_km = geo_Model(j.Name, k.Name, m.Name, GeoModel);
            ajm_km = geo_Model(j.Name, m.Name, k.Name, GeoModel);

            dfij = first_Derative_Qx(i, j, ami_ij / (ami_ij + amj_ij));
            dfik = first_Derative_Qx(i, k, ami_ik / (ami_ik + amk_ik));

            double Qij = (amj_ij * aki_ij - ami_ij * akj_ij) / Pow(ami_ij + amj_ij, 2) * dfij;
            double Qik = (amk_ik * aji_ik - ami_ik * ajk_ik) / Pow(ami_ik + amk_ik, 2) * dfik;

            dfim = first_Derative_Qx(i, m, 0);
            ddfim = second_Derative_Q0(i, m, 0);

            double Qim = aji_im * aki_im * ddfim - (aji_im * akm_im + aki_im * ajm_im + 2 * aji_im * aki_im) * dfim;

            dfjk = first_Derative_Qx(j, k, amj_jk / (amj_jk + amk_jk));

            double Qjk = (amk_jk * aij_jk - amj_jk * aik_jk) / Pow(amj_jk + amk_jk, 2) * dfjk;

            dfjm = first_Derative_Qx(j, m, 0);
            ddfjm = second_Derative_Q0(j, m, 0);
            double Qjm = aij_jm * akj_jm * ddfjm - (aij_jm * akm_jm + akj_jm * aim_jm + 2 * aij_jm * akj_jm) * dfjm;

            dfkm = first_Derative_Qx(k, m, 0);
            ddfkm = second_Derative_Q0(k, m, 0);
            double Qkm = aik_km * ajk_km * ddfkm - (aik_km * ajm_km + ajk_km * aim_km + 2 * aik_km * ajk_km) * dfkm;

            return 1000 * (Qij + Qik + Qim + Qjk + Qjm + Qkm) / (R * Tem) - skj;


        }



    }
}
