﻿namespace AlloyAct_Pro
{
    class constant
    {

        public const double R = 8.314;
        public const double QtoP = 9.4;
        public const double P_TT = 14.2;
        public const double P_NN = 10.7;
        public const double P_TN = 12.35;
        public const double Q_TT = 132;
        public const double Q_TN = 116;
        public const double Q_NN = 100;

        public static Dictionary<string, int> periodicTable = new Dictionary<string, int>()
        {
            {"H",1 },{"Li",3 },{"Be",4 },{"B",5 },{"C",6 },{"N",7 },{"O",8 },{"F",9 },
            {"Na",11 },{"Mg",12 },{"Al",13 },{"Si",14 },{ "P",15},{"S",16 },{"Cl",17 },
            {"K",19 },{"Ca",20 },{"Sc",21 },{"Ti",22 },{"V",23 },{"Cr",24 },{"Mn",25 },{"Fe",26 },
            {"Co",27 },{"Ni",28 },{"Cu",29 },{"Zn",30 },{"Ga",31 },{"Ge",32 },{"As", 33},{"Se",34 },{"Br",35 },
             {"Rb",37},{"Sr",38},{"Y",39},{"Zr",40},{"Nb",41},{"Mo",42},{"Tc",43},{"Ru",44},{"Rh",45},
            { "Pd",46},{"Ag",47},{"Cd",48},{"In",49},{"Sn",50},{"Sb",51},{"Te",52},{"I",53},{"Cs",55},
            { "Ba",56},{"Hf",72},{"Ta",73},{"W",74},{"Re",75},{"Os",76},{"Ir",77},{"Pt",78},{"Au",79},
            { "Hg",80},{"Tl",81},{"Pb",82},{"Bi",83},{"Po",84},{"At",85},{"Fr",87},{"Ra",88},{"Rf",104},
            { "Db",105},{"Sg",106},{"Bh",107},{"Hs",108},{"Mt",109},{"Ds",110},{"Rg",111},{"Cn",112},
            { "Nh",113},{"Fl",114},{"Mc",115},{"Lv",116},{"Ts",117},{"La",57},{"Ce",58},{"Pr",59},
            { "Nd",60},{"Pm",61},{"Sm",62},{"Eu",63},{"Gd",64},{"Tb",65},{"Dy",66},{"Ho",67},{"Er",68},
            { "Tm",69},{"Yb",70},{"Lu",71},{"Ac",89},{"Th",90},{"Pa",91},{"U",92},{"Np",93},{"Pu",94},
            { "Am",95},{"Cm",96},{"Bk",97},{"Cf",98},{"Es",99},{"Fm",100},{"Md",101},{"No",102},
            { "Lr",103}


        };
        public static List<string> non_metallst = new List<string>()
        {
            "H","B","C","N","Si","P","Ge"
        };

    }
}
