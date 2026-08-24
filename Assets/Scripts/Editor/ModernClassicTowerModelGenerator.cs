#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cohesive modern-classic procedural tower generator.
/// V2 replaces the previous scattered primitive look with a shared fortified base,
/// layered armor panels, connected weapon housings and logical phase progression.
/// </summary>
public static class ModernClassicTowerModelGenerator
{
    private const string Root = "Assets/TowerPrefabs/GeneratedModernClassic";
    private const string MatRoot = Root + "/Materials";

    private static Material dark, ivory, bronze, cyan, orange, crystal;

    [MenuItem("Tower Defense/Models/Generate Modern Classic 4-Phase Towers")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets/TowerPrefabs");
        EnsureFolder(Root);
        EnsureFolder(MatRoot);
        CreateMaterials();

        Generate("Archer Tower", BuildArcher);
        Generate("Xbow Tower", BuildXbow);
        Generate("Canon Tower", BuildCannon);
        Generate("Big Cannon", BuildBigCannon);
        Generate("Bomb Tower", BuildBomb);
        Generate("Burning Tower", BuildBurning);
        Generate("Ultimate Tower", BuildUltimate);
        Generate("Gold Mine", BuildGoldMine);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Modern Classic Towers V2",
            "Đã tạo lại toàn bộ tower theo silhouette sát concept hơn.\n" +
            "Prefab cũ trong GeneratedModernClassic được cập nhật tại chỗ.", "OK");
    }

    private static void Generate(string name, Action<Transform,int> builder)
    {
        GameObject root = new GameObject(name + "_Generated");
        Tower tower = root.AddComponent<Tower>();
        AudioSource audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = .75f;

        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 1.0f, 0);
        col.size = new Vector3(2.7f, 2.5f, 2.7f);

        tower.visualPhases = new GameObject[4];
        for (int p=1; p<=4; p++)
        {
            GameObject phase = new GameObject("Phase" + p);
            phase.transform.SetParent(root.transform, false);
            tower.visualPhases[p-1] = phase;
            builder(phase.transform, p);
            phase.SetActive(p == 1);
        }
        tower.ApplyVisualPhase();

        PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    // ---------------- SHARED ARCHITECTURE ----------------

    private static void Base(Transform t, int p, float mul=1f, bool useEnergy=true)
    {
        float s = mul * (1f + .03f*(p-1));

        // Strong square foundation like the concept instead of a round drum.
        Cube(t,"Foundation",new Vector3(0,.14f,0),new Vector3(2.50f*s,.28f,2.50f*s),dark);
        Cube(t,"MainArmor",new Vector3(0,.38f,0),new Vector3(2.14f*s,.42f,2.14f*s),ivory);
        Cyl(t,"Core",new Vector3(0,.64f,0),new Vector3(1.70f*s,.40f,1.70f*s),dark);
        Cyl(t,"BronzeBand",new Vector3(0,.80f,0),new Vector3(1.82f*s,.10f,1.82f*s),bronze);
        Cyl(t,"UpperArmor",new Vector3(0,.91f,0),new Vector3(1.55f*s,.22f,1.55f*s),ivory);
        Cyl(t,"TurretRing",new Vector3(0,1.05f,0),new Vector3(1.22f*s,.13f,1.22f*s),dark);

        Corner(t,-1,-1,s,p); Corner(t,1,-1,s,p); Corner(t,-1,1,s,p); Corner(t,1,1,s,p);
        Panel(t,new Vector3(0,.48f,-1.08f*s),new Vector3(.72f,.55f,.12f),dark,new Vector3(0,0,0));
        Panel(t,new Vector3(0,.48f, 1.08f*s),new Vector3(.72f,.55f,.12f),dark,new Vector3(0,180,0));
        Panel(t,new Vector3(-1.08f*s,.48f,0),new Vector3(.12f,.55f,.72f),ivory,new Vector3(0,90,0));
        Panel(t,new Vector3( 1.08f*s,.48f,0),new Vector3(.12f,.55f,.72f),ivory,new Vector3(0,-90,0));

        if (useEnergy && p >= 2)
        {
            Cube(t,"FrontGlow",new Vector3(0,.50f,-1.151f*s),new Vector3(.11f,.36f,.035f),cyan);
            Cube(t,"LeftGlow",new Vector3(-1.151f*s,.50f,0),new Vector3(.035f,.36f,.11f),cyan);
            Cube(t,"RightGlow",new Vector3(1.151f*s,.50f,0),new Vector3(.035f,.36f,.11f),cyan);
        }
        if (p >= 3)
        {
            Cap(t,-.82f*s,-.82f*s); Cap(t,.82f*s,-.82f*s); Cap(t,-.82f*s,.82f*s); Cap(t,.82f*s,.82f*s);
        }
        if (p >= 4 && useEnergy)
            Cyl(t,"Phase4Ring",new Vector3(0,1.10f,0),new Vector3(1.28f*s,.05f,1.28f*s),cyan);
    }

    private static void Corner(Transform t,int x,int z,float s,int p)
    {
        Vector3 pos = new Vector3(x*.95f*s,.34f,z*.95f*s);
        Cube(t,"Corner",pos,new Vector3(.48f*s,.56f,.48f*s),dark,new Vector3(0,x*z*8,0));
        Cube(t,"CornerArmor",pos+new Vector3(x*.025f,.06f,z*.025f),new Vector3(.36f*s,.38f,.36f*s),p>=3?bronze:ivory,new Vector3(0,x*z*8,0));
    }

    private static void Cap(Transform t,float x,float z)
    {
        Cube(t,"UpperCap",new Vector3(x,.95f,z),new Vector3(.34f,.22f,.34f),bronze);
    }

    private static void Panel(Transform t,Vector3 pos,Vector3 size,Material outer,Vector3 euler)
    {
        Cube(t,"ArmorPanel",pos,size,outer,euler);
        Vector3 inner = new Vector3(Mathf.Max(.04f,size.x*.64f),size.y*.60f,Mathf.Max(.04f,size.z*.64f));
        Vector3 nudge = new Vector3(Mathf.Sign(pos.x)*.012f,0,Mathf.Sign(pos.z)*.012f);
        Cube(t,"PanelTrim",pos+nudge,inner,bronze,euler);
    }

    private static Transform Turret(Transform phase,int p,float y=1.17f,float w=1.25f)
    {
        Transform h = New("TurretHead",phase,new Vector3(0,y,0));
        Cube(h,"TurretBlock",new Vector3(0,.10f,0),new Vector3(w,.30f,.92f),dark);
        Cube(h,"TurretArmor",new Vector3(0,.28f,.02f),new Vector3(w*.86f,.28f,.74f),ivory);
        Cube(h,"TurretTrim",new Vector3(0,.35f,.03f),new Vector3(w*.92f,.07f,.80f),bronze);
        if(p>=3) Cube(h,"TurretCore",new Vector3(0,.43f,.14f),new Vector3(w*.40f,.08f,.34f),cyan);
        return h;
    }

    // ---------------- ARCHER ----------------

    private static void BuildArcher(Transform phase,int p)
    {
        Base(phase,p);
        Transform h = Turret(phase,p,1.17f,1.24f);
        Cube(h,"Receiver",new Vector3(0,.50f,.48f),new Vector3(.48f,.30f,1.26f+.09f*p),dark);
        Cube(h,"ReceiverArmor",new Vector3(0,.62f,.30f),new Vector3(.38f,.18f,.72f),ivory);
        Cube(h,"BoltRail",new Vector3(0,.68f,.86f),new Vector3(.085f,.06f,1.12f+.10f*p),cyan);

        float span=1.42f+.12f*p;
        Bow(h,-1,span,p); Bow(h,1,span,p);
        Cyl(h,"WinchL",new Vector3(-.31f,.61f,.17f),new Vector3(.28f,.17f,.28f),bronze);
        Cyl(h,"WinchR",new Vector3(.31f,.61f,.17f),new Vector3(.28f,.17f,.28f),bronze);

        if(p>=3) Cube(h,"UpperBrace",new Vector3(0,.83f,.22f),new Vector3(.62f,.16f,.52f),dark);
        if(p>=4)
        {
            Cube(h,"Crest",new Vector3(0,1.02f,.16f),new Vector3(.18f,.42f,.34f),bronze,new Vector3(0,0,18));
            Crystal(h,new Vector3(0,.67f,1.70f),new Vector3(.24f,.24f,.58f),cyan,new Vector3(0,0,45));
        }
        FirePoint(h,new Vector3(0,.68f,1.76f+.10f*p));
    }

    private static void Bow(Transform h,int side,float span,int p)
    {
        Cube(h,"BowRoot",new Vector3(side*.58f,.60f,.42f),new Vector3(.28f,.24f,.58f),bronze,new Vector3(0,side*-14,side*7));
        Cube(h,"BowLimb",new Vector3(side*span*.48f,.69f,.42f),new Vector3(span*.55f,.12f,.18f),ivory,new Vector3(0,0,side*12));
        Cube(h,"BowTip",new Vector3(side*span*.78f,.78f,.42f),new Vector3(.18f,.22f,.22f),bronze);
        if(p>=2) Cube(h,"EnergyString",new Vector3(side*span*.31f,.71f,.43f),new Vector3(span*.44f,.03f,.03f),cyan,new Vector3(0,0,side*9));
    }

    // ---------------- XBOW ----------------

    private static void BuildXbow(Transform phase,int p)
    {
        Base(phase,p);
        Transform h=Turret(phase,p,1.17f,1.30f);
        Cube(h,"Receiver",new Vector3(0,.50f,.48f),new Vector3(.60f,.29f,1.55f+.10f*p),dark);
        Cube(h,"TopArmor",new Vector3(0,.65f,.30f),new Vector3(.49f,.16f,.94f),ivory);
        Cube(h,"Rail",new Vector3(0,.74f,.84f),new Vector3(.08f,.06f,1.28f+.10f*p),cyan);
        float w=1.55f+.11f*p;
        Cube(h,"CrossArm",new Vector3(0,.58f,.22f),new Vector3(w,.18f,.28f),bronze);
        Cube(h,"LeftWing",new Vector3(-w*.44f,.62f,.29f),new Vector3(w*.48f,.13f,.18f),ivory,new Vector3(0,-7,-8));
        Cube(h,"RightWing",new Vector3(w*.44f,.62f,.29f),new Vector3(w*.48f,.13f,.18f),ivory,new Vector3(0,7,8));
        if(p>=2){ Cyl(h,"SpoolL",new Vector3(-.35f,.79f,.04f),new Vector3(.28f,.18f,.28f),dark); Cyl(h,"SpoolR",new Vector3(.35f,.79f,.04f),new Vector3(.28f,.18f,.28f),dark); }
        if(p>=3) Cube(h,"ShoulderArmor",new Vector3(0,.43f,-.18f),new Vector3(1.18f,.34f,.48f),ivory);
        if(p>=4) Crystal(h,new Vector3(0,.72f,1.72f),new Vector3(.20f,.20f,.48f),cyan,new Vector3(0,0,45));
        FirePoint(h,new Vector3(0,.72f,1.86f+.08f*p));
    }

    // ---------------- CANNONS ----------------

    private static void BuildCannon(Transform phase,int p){ Base(phase,p); Transform h=Turret(phase,p); CannonAssembly(h,p,1f,false); }
    private static void BuildBigCannon(Transform phase,int p){ Base(phase,p,1.10f); Transform h=Turret(phase,p,1.21f,1.50f); CannonAssembly(h,p,1.22f,true); }

    private static void CannonAssembly(Transform h,int p,float mul,bool heavy)
    {
        float r=(heavy?.50f:.39f)*mul;
        float len=(heavy?1.82f:1.55f)+.12f*p;
        CylZ(h,"Barrel",new Vector3(0,.57f,.92f),new Vector3(r,r,len),dark);
        CylZ(h,"Breech",new Vector3(0,.57f,.10f),new Vector3(r*1.18f,r*1.18f,.68f),ivory);
        CylZ(h,"MuzzleBand",new Vector3(0,.57f,1.74f+.07f*p),new Vector3(r*1.24f,r*1.24f,.20f),bronze);
        for(int i=0;i<Mathf.Min(3,p);i++) CylZ(h,"Reinforce",new Vector3(0,.57f,.60f+i*.40f),new Vector3(r*1.10f,r*1.10f,.10f),bronze);
        Cube(h,"RecoilL",new Vector3(-.52f*mul,.39f,.12f),new Vector3(.27f*mul,.38f,.90f),dark);
        Cube(h,"RecoilR",new Vector3(.52f*mul,.39f,.12f),new Vector3(.27f*mul,.38f,.90f),dark);
        CylZ(h,"PistonL",new Vector3(-.52f*mul,.46f,.55f),new Vector3(.11f,.11f,.72f),bronze);
        CylZ(h,"PistonR",new Vector3(.52f*mul,.46f,.55f),new Vector3(.11f,.11f,.72f),bronze);
        if(p>=3) Cube(h,"UpperShell",new Vector3(0,.85f,.14f),new Vector3(heavy?1.18f:.94f,.24f,.82f),ivory);
        if(p>=4) CylZ(h,"EnergyMuzzle",new Vector3(0,.57f,1.91f+.07f*p),new Vector3(r*.60f,r*.60f,.16f),cyan);
        FirePoint(h,new Vector3(0,.57f,2.00f+.10f*p));
    }

    // ---------------- BOMB ----------------

    private static void BuildBomb(Transform phase,int p)
    {
        Base(phase,p);
        Transform h=Turret(phase,p);
        Transform mortar=New("MortarPivot",h,new Vector3(0,.43f,.08f),new Vector3(-34,0,0));
        CylZ(mortar,"MortarTube",new Vector3(0,0,.68f),new Vector3(.50f+.035f*p,.50f+.035f*p,1.30f+.10f*p),dark);
        CylZ(mortar,"Breech",new Vector3(0,0,.08f),new Vector3(.63f,.63f,.46f),ivory);
        CylZ(mortar,"Muzzle",new Vector3(0,0,1.32f+.08f*p),new Vector3(.66f,.66f,.19f),bronze);
        Cube(h,"SupportL",new Vector3(-.51f,.34f,.05f),new Vector3(.20f,.68f,.72f),bronze,new Vector3(0,0,-12));
        Cube(h,"SupportR",new Vector3(.51f,.34f,.05f),new Vector3(.20f,.68f,.72f),bronze,new Vector3(0,0,12));
        if(p>=3) Sphere(h,"Magazine",new Vector3(0,.92f,-.24f),new Vector3(.52f,.52f,.52f),dark);
        if(p>=4) Sphere(h,"MagazineCore",new Vector3(0,.92f,-.24f),new Vector3(.25f,.25f,.25f),orange);
        FirePoint(mortar,new Vector3(0,0,1.52f+.09f*p));
    }

    // ---------------- BURNING ----------------

    private static void BuildBurning(Transform phase,int p)
    {
        Base(phase,p);
        Transform h=Turret(phase,p);
        CylZ(h,"FlameBarrel",new Vector3(0,.55f,.94f),new Vector3(.28f+.02f*p,.28f+.02f*p,1.62f+.11f*p),dark);
        CylZ(h,"HeatJacket",new Vector3(0,.55f,.72f),new Vector3(.37f,.37f,.72f),bronze);
        CylZ(h,"HotMuzzle",new Vector3(0,.55f,1.70f+.08f*p),new Vector3(.40f,.40f,.18f),orange);
        float[] xs={-.46f,.46f,0}; int tanks=Mathf.Min(3,p);
        for(int i=0;i<tanks;i++){ Cyl(h,"FuelTank",new Vector3(xs[i],.80f,-.24f),new Vector3(.30f,.72f,.30f),bronze); Cyl(h,"FuelGlow",new Vector3(xs[i],.80f,-.24f),new Vector3(.17f,.53f,.17f),orange); }
        Cube(h,"FeedBlock",new Vector3(0,.39f,.06f),new Vector3(1.02f,.37f,.64f),ivory);
        if(p>=3) Cube(h,"HeatShield",new Vector3(0,.82f,.72f),new Vector3(.78f,.15f,.88f),dark);
        FirePoint(h,new Vector3(0,.55f,1.94f+.08f*p));
    }

    // ---------------- ULTIMATE ----------------

    private static void BuildUltimate(Transform phase,int p)
    {
        Base(phase,p,1.07f);
        Transform h=Turret(phase,p,1.19f,1.46f);
        Cube(h,"Housing",new Vector3(0,.53f,.42f),new Vector3(1.18f,.62f,1.24f),dark);
        Cube(h,"HousingArmor",new Vector3(0,.79f,.24f),new Vector3(1.02f,.28f,.82f),ivory);
        CylZ(h,"Emitter",new Vector3(0,.59f,1.08f),new Vector3(.46f+.035f*p,.46f+.035f*p,1.12f+.10f*p),dark);
        for(int i=0;i<p;i++) CylZ(h,"EnergyRing",new Vector3(0,.59f,.61f+i*.28f),new Vector3(.61f,.61f,.08f),cyan);
        Sphere(h,"Core",new Vector3(0,.59f,1.58f+.08f*p),new Vector3(.31f+.035f*p,.31f+.035f*p,.31f+.035f*p),cyan);
        if(p>=3){ Cube(h,"SideL",new Vector3(-.66f,.58f,.47f),new Vector3(.29f,.48f,.90f),ivory); Cube(h,"SideR",new Vector3(.66f,.58f,.47f),new Vector3(.29f,.48f,.90f),ivory); }
        if(p>=4){ Cube(h,"FinL",new Vector3(-.80f,.99f,.34f),new Vector3(.14f,.68f,.52f),bronze,new Vector3(0,0,-18)); Cube(h,"FinR",new Vector3(.80f,.99f,.34f),new Vector3(.14f,.68f,.52f),bronze,new Vector3(0,0,18)); }
        FirePoint(h,new Vector3(0,.59f,1.96f+.08f*p));
    }

    // ---------------- GOLD MINE ----------------

    private static void BuildGoldMine(Transform phase,int p)
    {
        Base(phase,p,1.04f);
        Transform c=New("MineCore",phase,new Vector3(0,1.08f,0));
        int count=3+p;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2f/count; float radius=i==0?0f:.42f;
            Vector3 pos=new Vector3(Mathf.Cos(a)*radius,.52f+(i%3)*.09f,Mathf.Sin(a)*radius);
            float h=i==0?1.16f+.10f*p:.70f+.07f*(i%3);
            Crystal(c,pos,new Vector3(.29f,h,.29f),cyan,new Vector3((i%2)*8,a*Mathf.Rad2Deg,(i%3-1)*7));
        }
        Transform arm=New("MiningArm",c,new Vector3(-.62f,.42f,-.16f));
        Cube(arm,"ArmBase",Vector3.zero,new Vector3(.34f,.60f,.34f),bronze);
        Cube(arm,"ArmSegment",new Vector3(.30f,.46f,0),new Vector3(.72f,.16f,.20f),dark,new Vector3(0,0,-24));
        Cube(arm,"ToolHead",new Vector3(.62f,.72f,0),new Vector3(.26f,.30f,.28f),bronze);
        if(p>=2){ Cyl(c,"Tank1",new Vector3(.68f,.45f,-.25f),new Vector3(.34f,.90f,.34f),dark); Cyl(c,"TankGlow1",new Vector3(.68f,.45f,-.25f),new Vector3(.20f,.64f,.20f),cyan); }
        if(p>=3){ Cyl(c,"Tank2",new Vector3(.62f,.44f,.35f),new Vector3(.30f,.84f,.30f),dark); Cube(c,"PipeBridge",new Vector3(.25f,.86f,.06f),new Vector3(.70f,.12f,.14f),bronze); }
        if(p>=4){ Cyl(c,"Containment",new Vector3(0,.64f,0),new Vector3(1.00f,1.36f,1.00f),crystal); Cyl(c,"ContainmentCap",new Vector3(0,1.31f,0),new Vector3(1.10f,.16f,1.10f),bronze); }
        FirePoint(c,new Vector3(0,1.70f,0));
    }

    // ---------------- HELPERS ----------------

    private static Transform New(string name,Transform parent,Vector3 pos,Vector3 euler=default)
    {
        GameObject go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localEulerAngles=euler; return go.transform;
    }

    private static GameObject Cube(Transform p,string name,Vector3 pos,Vector3 scale,Material mat,Vector3 euler=default)
    {
        GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(p,false); g.transform.localPosition=pos; g.transform.localScale=scale; g.transform.localEulerAngles=euler; Mat(g,mat); KillCol(g); return g;
    }

    private static GameObject Cyl(Transform p,string name,Vector3 pos,Vector3 scale,Material mat)
    {
        GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name=name; g.transform.SetParent(p,false); g.transform.localPosition=pos; g.transform.localScale=new Vector3(scale.x,scale.y*.5f,scale.z); Mat(g,mat); KillCol(g); return g;
    }

    private static GameObject CylZ(Transform p,string name,Vector3 pos,Vector3 scale,Material mat)
    {
        GameObject g=Cyl(p,name,pos,new Vector3(scale.x,scale.z,scale.y),mat); g.transform.localEulerAngles=new Vector3(90,0,0); return g;
    }

    private static GameObject Sphere(Transform p,string name,Vector3 pos,Vector3 scale,Material mat)
    {
        GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name=name; g.transform.SetParent(p,false); g.transform.localPosition=pos; g.transform.localScale=scale; Mat(g,mat); KillCol(g); return g;
    }

    private static void Crystal(Transform p,Vector3 pos,Vector3 scale,Material mat,Vector3 euler)
    {
        GameObject g=Cube(p,"Crystal",pos,scale,mat,euler); g.transform.localRotation*=Quaternion.Euler(0,45,0);
    }

    private static void FirePoint(Transform p,Vector3 pos){ GameObject fp=new GameObject("FirePoint"); fp.transform.SetParent(p,false); fp.transform.localPosition=pos; }
    private static void Mat(GameObject g,Material m){ Renderer r=g.GetComponent<Renderer>(); if(r!=null) r.sharedMaterial=m; }
    private static void KillCol(GameObject g){ Collider c=g.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.DestroyImmediate(c); }

    private static void CreateMaterials()
    {
        dark=Material("M_DarkMetal",new Color(.045f,.060f,.075f,1),.82f,.42f,false);
        ivory=Material("M_IvoryArmor",new Color(.76f,.80f,.81f,1),.62f,.30f,false);
        bronze=Material("M_BronzeTrim",new Color(.58f,.31f,.11f,1),.76f,.35f,false);
        cyan=Material("M_CyanEnergy",new Color(.02f,.70f,1f,1),.42f,.58f,true);
        orange=Material("M_OrangeEnergy",new Color(1f,.30f,.03f,1),.42f,.58f,true);
        crystal=Material("M_CrystalGlass",new Color(.08f,.78f,1f,.30f),.15f,.90f,true);
    }

    private static Material Material(string name,Color color,float metallic,float smooth,bool emissive)
    {
        string path=MatRoot+"/"+name+".mat"; Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){ Shader s=Shader.Find("Universal Render Pipeline/Lit"); if(s==null)s=Shader.Find("Standard"); m=new Material(s); AssetDatabase.CreateAsset(m,path); }
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color); if(m.HasProperty("_Color"))m.SetColor("_Color",color);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic); if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);
        if(emissive && m.HasProperty("_EmissionColor")){ m.EnableKeyword("_EMISSION"); Color e=new Color(color.r*2.6f,color.g*2.6f,color.b*2.6f,1); m.SetColor("_EmissionColor",e); }
        EditorUtility.SetDirty(m); return m;
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return; int slash=path.LastIndexOf('/'); string parent=slash>0?path.Substring(0,slash):"Assets"; string folder=slash>0?path.Substring(slash+1):path; if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent); AssetDatabase.CreateFolder(parent,folder);
    }
}
#endif
