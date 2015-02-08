module GeometryUtils

open System.Collections.Generic
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

let inline v3 (x: ^a, y: ^b, z:^c) = Vector3(float32 x, float32 y, float32 z)
let inline v2 (x: ^a, y: ^b) = Vector2(float32 x, float32 y)
let vpc(c, v:Vector3) = VertexPositionColor(Color = c, Position = v)
let pi_4 = MathHelper.PiOver4 
let myVertexArray = [|vpc(Color.Red, v3(0,1,0));vpc(Color.Red, v3(0, 1, 0))|]

let intToShort (ilist:int[]): int16 [] = [|for i in ilist -> int16 i|]

let inline rectangle(x:^a,  y :^a, w:^a, h:^a) = Rectangle(int x, int y, int w, int h)

// CSG stuff

let GetNormal(a:Vector3, b:Vector3, c:Vector3) =
    let n = Vector3.Cross((b - a), (c - a))
    n.Normalize()
    n

type Plane(a:Vector3, b:Vector3, c:Vector3) =
    let mutable normal = GetNormal(a,b,c)
    let  mutable w = Vector3.Dot(a, normal)
    member p.Normal = normal
    member p.W = w
    member p.Ok = normal.Length() > 0.f
    member p.Flip() = 
        normal <- normal * (-1.0f)
        w <- -w

let eps = 0.0000f
let v0 = v3(0,0,0)
let v1 = v3(0,1,0)
let v2 = v3(0,0,1)

let lerp v0 v1 t = Vector3.Lerp(v0,v1, t)
        

let Interpolate (a:VertexPositionColor) (b:VertexPositionColor) (t:float32) = 
    vpc(Color.Lerp(a.Color, b.Color, t), Vector3.Lerp(a.Position, b.Position, t))

let dot a b = Vector3.Dot(a,b)

type PolygonVPC() =     
    member val Vertices  = List<VertexPositionColor>() with get, set
    member val Plane  = Plane(v0,v1,v2) with get, set
    member p.Flip() =
        p.Vertices.Reverse()
        p.Plane.Flip()

    static member Gen(l:List<VertexPositionColor>) =
        let mutable p = PolygonVPC()
        p.Vertices <- l  
        p.Plane <- Plane(l.[0].Position, l.[1].Position, l.[2].Position)
        p


type PolyList = List<PolygonVPC>

type Node =
    | EmptyNode
    | LiveNode of PolyList * Node * Node


let Coplanar = 0
let Front = 1
let Back = 2
let Spanning = 3

let Classify(p:Plane, v:Vector3) =
       let t = (dot  p.Normal v) - p.W
       if (t < eps) then Back else (if(t > eps) then Front else Coplanar)

let Merge (i:int, j:int) =
        if (i = j) then j else 
        match i with
        | 0 -> j
        |_ -> Spanning

let SplitPolygon (p:Plane) (poly:PolygonVPC) (coFront:PolyList) (coBack: PolyList) (front:PolyList) (back:PolyList) =
    let mutable polytype = Coplanar
    let types = List<int>()

    let split() =
        let mutable f = List<VertexPositionColor>()
        let mutable b  = List<VertexPositionColor>()
        for i in 0 .. poly.Vertices.Count do
            let j = (i + 1)  % poly.Vertices.Count
            let ti, tj = types.[i], types.[j]
            let vi,vj = poly.Vertices.[i], poly.Vertices.[j]
            if ti <> Back then f.Add(vi)
            if ti <> Front then b.Add(vi)
            if (ti ||| tj) = Spanning then
                let t = (p.W - (dot p.Normal vi.Position)) / (dot p.Normal (vj.Position - vi.Position))
                let v = Interpolate vi vj t
                f.Add(v)
                b.Add(v)
        if f.Count >= 3 then front.Add(PolygonVPC.Gen(f))
        if b.Count >= 3 then back.Add(PolygonVPC.Gen(b))

    for v in poly.Vertices do
        let typ = Classify(p, v.Position)
        polytype <- Merge(polytype, typ)
        types.Add(typ)

    match polytype with
    | COPLANAR -> if (dot p.Normal poly.Plane.Normal) > 0.f then coFront.Add(poly) else coBack.Add(poly)
    | FRONT -> front.Add(poly)
    | BACK -> back.Add(poly)
    | SPANNING -> split()
    ()
    


    