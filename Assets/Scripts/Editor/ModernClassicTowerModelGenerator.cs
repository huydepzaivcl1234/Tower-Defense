#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates clean modern-classic tower models directly inside Unity using primitive meshes.
/// Output: Assets/TowerPrefabs/GeneratedModernClassic/<TowerName>.prefab
/// Each prefab contains Phase1..Phase4, and Tower.ApplyVisualPhase swaps shape on upgrade.
/// This does not overwrite existing TowerData references automatically.
/// </summary>
public static class ModernClassicTowerModelGenerator
{
    private const string Root = "Assets/TowerPrefabs/GeneratedModernClassic";
    private const string MatRoot = Root + "/Materials";

    private static Material dark, ivory, bronze, cyan, orange, gold, wood, glass;

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
        EditorUtility.DisplayDialog("Tower Models",
            "Đã tạo 8 tower × 4 phase trong:\n" + Root + "\n\n" +
            "Mỗi prefab có sẵn Tower, BoxCollider, AudioSource, visualPhases, TurretHead và FirePoint.\n" +
            "Muốn Phase 4 xuất hiện trong gameplay thì TowerData tương ứng cần có 4 level stats.", "OK");
    }

    private static void Generate(string name, Action<Transform,int> builder)
    {
        GameObject root = new GameObject(name + "_Generated");
        Tower tower = root.AddComponent<Tower>();
        root.AddComponent<AudioSource>().playOnAwake = false;
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 1.15f, 0);
        col.size = new Vector3(2.4f, 2.3f, 2.4f);

        tower.visualPhases = new GameObject[4];
        for (int p = 1; p <= 4; p++)
        {
            GameObject phase = new GameObject("Phase" + p);
            phase.transform.SetParent(root.transform, false);
            tower.visualPhases[p - 1] = phase;
            builder(phase.transform, p);
            phase.SetActive(p == 1);
        }
        tower.ApplyVisualPhase();

        string path = Root + "/" + name.Replace("/", "_") + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void BuildArcher(Transform phase, int p)
    {
        BuildBase(phase, p, true);
        Transform head = New("TurretHead", phase, new Vector3(0, 1.45f, 0));
        int limbs = p >= 4 ? 4 : 2;
        float span = 1.25f + p * .12f;
        for (int i = 0; i < limbs; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float z = i < 2 ? .15f : -.15f;
            Beam(head, new Vector3(side * .55f, .12f, z), new Vector3(.09f, .09f, span * .48f), bronze, new Vector3(0, side * -18f, 0));
        }
        Beam(head, new Vector3(0,.08f,.48f), new Vector3(.18f,.16f,.95f + .12f*p), dark);
        Beam(head, new Vector3(0,.08f,.95f + .1f*p), new Vector3(.07f,.07f,.5f), cyan);
        Cube(head, new Vector3(0,.18f,.15f), new Vector3(.34f,.28f,.45f), ivory);
        if (p >= 2) { Cylinder(head,new Vector3(-.42f,.2f,.05f),new Vector3(.16f,.16f,.16f),bronze); Cylinder(head,new Vector3(.42f,.2f,.05f),new Vector3(.16f,.16f,.16f),bronze); }
        if (p >= 3) { Cube(head,new Vector3(0,.38f,.18f),new Vector3(.28f,.18f,.42f),cyan); }
        FirePoint(head, new Vector3(0,.08f,1.55f + .1f*p));
    }

    private static void BuildXbow(Transform phase, int p)
    {
        BuildBase(phase,p,true);
        Transform head = New("TurretHead", phase, new Vector3(0,1.42f,0));
        Beam(head,new Vector3(0,.12f,.45f),new Vector3(.2f,.16f,1.1f+.12f*p),dark);
        Beam(head,new Vector3(-.58f,.12f,.18f),new Vector3(.08f,.1f,.78f+.08f*p),ivory,new Vector3(0,-22,0));
        Beam(head,new Vector3(.58f,.12f,.18f),new Vector3(.08f,.1f,.78f+.08f*p),ivory,new Vector3(0,22,0));
        Cylinder(head,new Vector3(0,.18f,.05f),new Vector3(.25f,.18f,.25f),bronze);
        if(p>=2){ Cylinder(head,new Vector3(-.32f,.3f,.05f),new Vector3(.18f,.18f,.18f),dark); Cylinder(head,new Vector3(.32f,.3f,.05f),new Vector3(.18f,.18f,.18f),dark); }
        if(p>=3){ Beam(head,new Vector3(0,.28f,.5f),new Vector3(.08f,.08f,1.0f),cyan); }
        if(p>=4){ Cube(head,new Vector3(0,.42f,.15f),new Vector3(.6f,.18f,.5f),ivory); }
        FirePoint(head,new Vector3(0,.12f,1.65f+.1f*p));
    }

    private static void BuildCannon(Transform phase, int p)
    {
        BuildBase(phase,p,true);
        Transform head=New("TurretHead",phase,new Vector3(0,1.45f,0));
        CylinderZ(head,new Vector3(0,.18f,.55f),new Vector3(.42f+.05f*p,.42f+.05f*p,1.35f+.18f*p),dark);
        CylinderZ(head,new Vector3(0,.18f,1.25f+.15f*p),new Vector3(.5f+.05f*p,.5f+.05f*p,.18f),bronze);
        Cube(head,new Vector3(0,-.02f,.0f),new Vector3(.8f,.28f,.75f),ivory);
        if(p>=2) Cylinder(head,new Vector3(.52f,.1f,.15f),new Vector3(.18f,.18f,.18f),cyan);
        if(p>=3) Cylinder(head,new Vector3(-.52f,.1f,.15f),new Vector3(.18f,.18f,.18f),cyan);
        if(p>=4) CylinderZ(head,new Vector3(0,.18f,1.45f+.18f*p),new Vector3(.32f,.32f,.28f),cyan);
        FirePoint(head,new Vector3(0,.18f,1.55f+.22f*p));
    }

    private static void BuildBigCannon(Transform phase, int p)
    {
        BuildBase(phase,p,true,1.15f);
        Transform head=New("TurretHead",phase,new Vector3(0,1.52f,0));
        CylinderZ(head,new Vector3(0,.24f,.65f),new Vector3(.62f+.07f*p,.62f+.07f*p,1.55f+.2f*p),dark);
        CylinderZ(head,new Vector3(0,.24f,1.45f+.18f*p),new Vector3(.72f+.07f*p,.72f+.07f*p,.26f),bronze);
        Cube(head,new Vector3(0,-.08f,.0f),new Vector3(1.15f,.38f,1.0f),ivory);
        for(int i=0;i<p;i++) Cylinder(head,new Vector3(-.48f+i*.32f,.2f,-.15f),new Vector3(.18f,.18f,.18f),cyan);
        if(p>=4) CylinderZ(head,new Vector3(0,.24f,1.7f+.2f*p),new Vector3(.48f,.48f,.22f),cyan);
        FirePoint(head,new Vector3(0,.24f,1.85f+.22f*p));
    }

    private static void BuildBomb(Transform phase, int p)
    {
        BuildBase(phase,p,true);
        Transform head=New("TurretHead",phase,new Vector3(0,1.45f,0));
        Transform mortar=New("Mortar",head,new Vector3(0,.18f,.15f),new Vector3(42,0,0));
        CylinderZ(mortar,new Vector3(0,0,.45f),new Vector3(.55f+.06f*p,.55f+.06f*p,1.0f+.12f*p),dark);
        CylinderZ(mortar,new Vector3(0,0,.95f+.1f*p),new Vector3(.66f+.05f*p,.66f+.05f*p,.2f),bronze);
        Cube(head,new Vector3(0,-.1f,0),new Vector3(.95f,.35f,.95f),ivory);
        if(p>=3) Sphere(head,new Vector3(0,.55f,.05f),new Vector3(.38f,.38f,.38f),dark);
        if(p>=4) Sphere(head,new Vector3(0,.55f,.05f),new Vector3(.2f,.2f,.2f),orange);
        FirePoint(head,new Vector3(0,.68f,.95f+.12f*p));
    }

    private static void BuildBurning(Transform phase, int p)
    {
        BuildBase(phase,p,true);
        Transform head=New("TurretHead",phase,new Vector3(0,1.42f,0));
        CylinderZ(head,new Vector3(0,.12f,.7f),new Vector3(.27f+.03f*p,.27f+.03f*p,1.5f+.15f*p),dark);
        CylinderZ(head,new Vector3(0,.12f,1.45f+.1f*p),new Vector3(.34f,.34f,.22f),orange);
        for(int i=0;i<Mathf.Min(3,p);i++) Cylinder(head,new Vector3(-.42f+i*.42f,.28f,-.15f),new Vector3(.28f,.55f,.28f),bronze);
        Cube(head,new Vector3(0,-.12f,.0f),new Vector3(1.0f,.3f,.85f),ivory);
        if(p>=3) Beam(head,new Vector3(0,.36f,.65f),new Vector3(.08f,.08f,1.1f),orange);
        FirePoint(head,new Vector3(0,.12f,1.65f+.16f*p));
    }

    private static void BuildUltimate(Transform phase, int p)
    {
        BuildBase(phase,p,true,1.1f);
        Transform head=New("TurretHead",phase,new Vector3(0,1.5f,0));
        CylinderZ(head,new Vector3(0,.18f,.7f),new Vector3(.45f+.05f*p,.45f+.05f*p,1.45f+.18f*p),dark);
        for(int i=0;i<p;i++) CylinderZ(head,new Vector3(0,.18f,.35f+i*.28f),new Vector3(.58f,.58f,.10f),cyan);
        Cube(head,new Vector3(0,-.05f,.05f),new Vector3(1.05f,.4f,.9f),ivory);
        Sphere(head,new Vector3(0,.18f,1.45f+.16f*p),new Vector3(.32f+.04f*p,.32f+.04f*p,.32f+.04f*p),cyan);
        if(p>=4){ Beam(head,new Vector3(-.62f,.25f,.5f),new Vector3(.12f,.12f,.9f),bronze); Beam(head,new Vector3(.62f,.25f,.5f),new Vector3(.12f,.12f,.9f),bronze); }
        FirePoint(head,new Vector3(0,.18f,1.75f+.18f*p));
    }

    private static void BuildGoldMine(Transform phase, int p)
    {
        BuildBase(phase,p,false,1.05f);
        Transform core=New("Core",phase,new Vector3(0,1.45f,0));
        int crystals=2+p;
        for(int i=0;i<crystals;i++)
        {
            float a=i*Mathf.PI*2/crystals;
            Vector3 pos=new Vector3(Mathf.Cos(a)*.42f,.05f+(i%2)*.18f,Mathf.Sin(a)*.42f);
            Cube(core,pos,new Vector3(.22f,.65f+(i%2)*.2f,.22f),cyan,new Vector3(8+i*3,i*17,8));
        }
        if(p>=2) Cylinder(core,new Vector3(.62f,.2f,-.18f),new Vector3(.18f,.9f,.18f),bronze);
        if(p>=2) Beam(core,new Vector3(.32f,.72f,-.18f),new Vector3(.1f,.1f,.75f),bronze,new Vector3(0,90,-30));
        if(p>=3){ Cylinder(core,new Vector3(-.62f,.2f,-.18f),new Vector3(.18f,.9f,.18f),bronze); Cylinder(core,new Vector3(0,.2f,.62f),new Vector3(.18f,.9f,.18f),dark); }
        if(p>=4) Sphere(core,new Vector3(0,.95f,0),new Vector3(.34f,.34f,.34f),glass);
        GameObject fp=new GameObject("FirePoint"); fp.transform.SetParent(phase,false); fp.transform.localPosition=new Vector3(0,2.4f,0);
    }

    private static void BuildBase(Transform parent,int p,bool turret,float mul=1f)
    {
        float s=(1f+.05f*(p-1))*mul;
        Cylinder(parent,new Vector3(0,.28f,0),new Vector3(1.45f*s,.55f,1.45f*s),dark);
        Cylinder(parent,new Vector3(0,.58f,0),new Vector3(1.18f*s,.38f,1.18f*s),ivory);
        Cylinder(parent,new Vector3(0,.82f,0),new Vector3(.92f*s,.18f,.92f*s),bronze);
        for(int i=0;i<4;i++)
        {
            float a=i*Mathf.PI*.5f;
            Vector3 pos=new Vector3(Mathf.Cos(a)*1.02f*s,.28f,Mathf.Sin(a)*1.02f*s);
            Cube(parent,pos,new Vector3(.34f,.55f,.34f),p>=3?bronze:dark,new Vector3(0,-i*90,0));
        }
        if(p>=2) Cylinder(parent,new Vector3(0,.55f,0),new Vector3(1.25f*s,.08f,1.25f*s),cyan);
        if(p>=3)
        {
            Cube(parent,new Vector3(.0f,.38f,-1.0f*s),new Vector3(.42f,.45f,.12f),bronze);
            Cube(parent,new Vector3(.0f,.38f,1.0f*s),new Vector3(.42f,.45f,.12f),bronze);
        }
        if(p>=4) Cylinder(parent,new Vector3(0,.98f,0),new Vector3(.78f*s,.12f,.78f*s),cyan);
    }

    private static Transform New(string name,Transform parent,Vector3 pos,Vector3 euler=default)
    {
        GameObject go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localEulerAngles=euler; return go.transform;
    }

    private static GameObject Cube(Transform p,Vector3 pos,Vector3 scale,Material m,Vector3 euler=default)=>Prim(PrimitiveType.Cube,p,pos,scale,m,euler);
    private static GameObject Sphere(Transform p,Vector3 pos,Vector3 scale,Material m)=>Prim(PrimitiveType.Sphere,p,pos,scale,m,default);
    private static GameObject Cylinder(Transform p,Vector3 pos,Vector3 scale,Material m)=>Prim(PrimitiveType.Cylinder,p,pos,scale,m,default);
    private static GameObject CylinderZ(Transform p,Vector3 pos,Vector3 scale,Material m)=>Prim(PrimitiveType.Cylinder,p,pos,new Vector3(scale.x,scale.z*.5f,scale.y),m,new Vector3(90,0,0));
    private static GameObject Beam(Transform p,Vector3 pos,Vector3 scale,Material m,Vector3 euler=default)=>Prim(PrimitiveType.Cube,p,pos,scale,m,euler);

    private static GameObject Prim(PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material mat,Vector3 euler)
    {
        GameObject go=GameObject.CreatePrimitive(type);
        go.name=type.ToString(); go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale; go.transform.localEulerAngles=euler;
        Collider c=go.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.DestroyImmediate(c);
        Renderer r=go.GetComponent<Renderer>(); if(r!=null) r.sharedMaterial=mat;
        return go;
    }

    private static void FirePoint(Transform head,Vector3 pos)
    {
        GameObject fp=new GameObject("FirePoint"); fp.transform.SetParent(head,false); fp.transform.localPosition=pos;
    }

    private static void CreateMaterials()
    {
        dark=Mat("DarkMetal",new Color(.055f,.075f,.09f),.7f,.15f);
        ivory=Mat("IvorySteel",new Color(.58f,.61f,.62f),.75f,.22f);
        bronze=Mat("Bronze",new Color(.34f,.20f,.09f),.8f,.28f);
        cyan=Mat("CyanEnergy",new Color(.02f,.55f,.72f),.35f,.15f,true,new Color(0,.75f,1f)*2.2f);
        orange=Mat("OrangeEnergy",new Color(.65f,.22f,.025f),.3f,.18f,true,new Color(1f,.25f,.02f)*2f);
        gold=Mat("Gold",new Color(.78f,.5f,.07f),.85f,.2f);
        wood=Mat("Wood",new Color(.18f,.09f,.035f),.05f,.45f);
        glass=Mat("GlassCore",new Color(.05f,.4f,.52f),.2f,.08f,true,new Color(0,.6f,1f)*1.5f);
    }

    private static Material Mat(string name,Color color,float metallic,float smooth,bool emission=false,Color emissionColor=default)
    {
        string path=MatRoot+"/"+name+".mat";
        Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null)
        {
            Shader sh=Shader.Find("Universal Render Pipeline/Lit"); if(sh==null) sh=Shader.Find("Standard");
            m=new Material(sh){name=name}; AssetDatabase.CreateAsset(m,path);
        }
        if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",color); else if(m.HasProperty("_Color")) m.color=color;
        if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",metallic);
        if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
        if(emission && m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",emissionColor); }
        EditorUtility.SetDirty(m); return m;
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        int slash=path.LastIndexOf('/'); string parent=path.Substring(0,slash); string name=path.Substring(slash+1);
        if(!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,name);
    }
}
#endif
