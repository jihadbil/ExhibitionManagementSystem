using System.Text.Json;
using ExhibitionManagementSystem.Models.DTOs.Booth;

namespace ExhibitionManagementSystem.Services.Common;

internal readonly record struct DrawingPoint(decimal X, decimal Y);
internal static class BoothDrawingGeometry
{
    private const decimal Epsilon = .000001m;
    public static IReadOnlyList<DrawingPoint> Rectangle(decimal x, decimal y, decimal width, decimal height, decimal rotation)
    {
        var angle = (double)rotation * Math.PI / 180;
        var cosine = (decimal)Math.Cos(angle); var sine = (decimal)Math.Sin(angle);
        return new[] { new DrawingPoint(0,0), new DrawingPoint(width,0), new DrawingPoint(width,height), new DrawingPoint(0,height) }
            .Select(p => new DrawingPoint(x+width/2+(p.X-width/2)*cosine-(p.Y-height/2)*sine, y+height/2+(p.X-width/2)*sine+(p.Y-height/2)*cosine)).ToArray();
    }
    public static IReadOnlyList<DrawingPoint> BoothPoints(BoothLayoutItemDto item)
    {
        if (!string.Equals(item.ShapeType, "Polygon", StringComparison.OrdinalIgnoreCase)) return Rectangle(item.PosX,item.PosY,item.Width,item.Height,item.RotationAngle);
        if (string.IsNullOrWhiteSpace(item.ShapePolygonJSON)) throw new JsonException("Missing polygon points.");
        using var document = JsonDocument.Parse(item.ShapePolygonJSON);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("points", out var pointsProperty)) root = pointsProperty;
        var local = ReadPoints(root, item.Width, item.Height);
        var angle=(double)item.RotationAngle*Math.PI/180; var c=(decimal)Math.Cos(angle); var s=(decimal)Math.Sin(angle);
        return local.Select(p=>new DrawingPoint(item.PosX+item.Width/2+(p.X-item.Width/2)*c-(p.Y-item.Height/2)*s,item.PosY+item.Height/2+(p.X-item.Width/2)*s+(p.Y-item.Height/2)*c)).ToArray();
    }
    private static IReadOnlyList<DrawingPoint> ReadPoints(JsonElement root, decimal width, decimal height)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() is <3 or >512) throw new JsonException("Invalid polygon points.");
        var result=new List<DrawingPoint>();
        foreach(var point in root.EnumerateArray())
        {
            if(point.ValueKind!=JsonValueKind.Object||!point.TryGetProperty("x",out var xp)||!point.TryGetProperty("y",out var yp)||!xp.TryGetDecimal(out var x)||!yp.TryGetDecimal(out var y)||x<0||y<0||x>width+.005m||y>height+.005m)throw new JsonException("Invalid polygon vertex.");
            result.Add(new(x,y));
        }
        if(Area(result)<.1m)throw new JsonException("Degenerate polygon.");
        for(var i=0;i<result.Count;i++)for(var j=i+1;j<result.Count;j++)
        {
            if(result[i]==result[j])throw new JsonException("Duplicate polygon vertex.");
            if(j==i+1||(i==0&&j==result.Count-1))continue;
            if(Intersects(result[i],result[(i+1)%result.Count],result[j],result[(j+1)%result.Count]))throw new JsonException("Self intersecting polygon.");
        }
        return result;
    }
    public static (decimal Width,decimal Height,IReadOnlyList<DrawingPoint> Points) HallBoundary(string? siteJson,int hallId,decimal width,decimal height)
    {
        if(!string.IsNullOrWhiteSpace(siteJson))
        {
            using var document=JsonDocument.Parse(siteJson);
            var root=document.RootElement;
            if(root.TryGetProperty("schemaVersion",out var version)&&version.TryGetInt32(out var number)&&number==4&&root.TryGetProperty("shapes",out var shapes))
            foreach(var shape in shapes.EnumerateArray())
            {
                if(!shape.TryGetProperty("hallID",out var id)||!id.TryGetInt32(out var value)||value!=hallId||shape.GetProperty("kind").GetString()!="hall")continue;
                width=shape.GetProperty("width").GetDecimal();height=shape.GetProperty("height").GetDecimal();
                return(width,height,ReadPoints(shape.GetProperty("points"),width,height));
            }
        }
        return(width,height,Rectangle(0,0,width,height,0));
    }
    public static IReadOnlyList<IReadOnlyList<DrawingPoint>> SiteObstacles(string? siteJson,int hallId,IReadOnlyList<DrawingPoint> boundary)
    {
        if(string.IsNullOrWhiteSpace(siteJson))return [];
        using var document=JsonDocument.Parse(siteJson);var root=document.RootElement;
        if(!root.TryGetProperty("schemaVersion",out var version)||!version.TryGetInt32(out var n)||n!=4||!root.TryGetProperty("shapes",out var shapes))return [];
        JsonElement hall=default;
        foreach(var shape in shapes.EnumerateArray())if(shape.TryGetProperty("hallID",out var id)&&id.TryGetInt32(out var value)&&value==hallId&&shape.GetProperty("kind").GetString()=="hall"){hall=shape;break;}
        if(hall.ValueKind==JsonValueKind.Undefined)return [];
        var hx=hall.GetProperty("x").GetDecimal();var hy=hall.GetProperty("y").GetDecimal();var hw=hall.GetProperty("width").GetDecimal();var hh=hall.GetProperty("height").GetDecimal();var angle=-(double)hall.GetProperty("rotation").GetDecimal()*Math.PI/180;var cosine=(decimal)Math.Cos(angle);var sine=(decimal)Math.Sin(angle);
        DrawingPoint ToLocal(DrawingPoint p)=>new(hw/2+(p.X-hx-hw/2)*cosine-(p.Y-hy-hh/2)*sine,hh/2+(p.X-hx-hw/2)*sine+(p.Y-hy-hh/2)*cosine);
        var result=new List<IReadOnlyList<DrawingPoint>>();
        foreach(var shape in shapes.EnumerateArray())
        {
            var kind=shape.GetProperty("kind").GetString();if(kind is not("wall" or "column"))continue;
            var x=shape.GetProperty("x").GetDecimal();var y=shape.GetProperty("y").GetDecimal();var width=shape.GetProperty("width").GetDecimal();var height=shape.GetProperty("height").GetDecimal();var rotation=shape.GetProperty("rotation").GetDecimal();
            if(kind=="column"){var p=Rectangle(x,y,width,height,rotation).Select(ToLocal).ToArray();if(Contains(boundary,ToLocal(new(x+width/2,y+height/2))))result.Add(p);continue;}
            var wallPoints=shape.GetProperty("points").EnumerateArray().Select(p=>new DrawingPoint(p.GetProperty("x").GetDecimal(),p.GetProperty("y").GetDecimal())).ToArray();
            var r=(double)rotation*Math.PI/180;var c=(decimal)Math.Cos(r);var s=(decimal)Math.Sin(r);
            var world=wallPoints.Select(p=>ToLocal(new(x+width/2+(p.X-width/2)*c-(p.Y-height/2)*s,y+height/2+(p.X-width/2)*s+(p.Y-height/2)*c))).ToArray();
            var thickness=shape.TryGetProperty("strokeWidth",out var stroke)?stroke.GetDecimal():.3m;
            for(var i=1;i<world.Length;i++){var a=world[i-1];var b=world[i];var len=(decimal)Math.Sqrt((double)((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y)));if(len<=0)continue;
                var nx=-(b.Y-a.Y)/len*thickness/2;var ny=(b.X-a.X)/len*thickness/2;var mid=new DrawingPoint((a.X+b.X)/2,(a.Y+b.Y)/2);if(Contains(boundary,mid))result.Add(new[]{new DrawingPoint(a.X+nx,a.Y+ny),new(b.X+nx,b.Y+ny),new(b.X-nx,b.Y-ny),new(a.X-nx,a.Y-ny)});}
        }
        return result;
    }
    public static decimal Area(IReadOnlyList<DrawingPoint> polygon)=>Math.Abs(polygon.Select((p,i)=>p.X*polygon[(i+1)%polygon.Count].Y-p.Y*polygon[(i+1)%polygon.Count].X).Sum())/2;
    private static decimal Cross(DrawingPoint a,DrawingPoint b,DrawingPoint p)=>(b.X-a.X)*(p.Y-a.Y)-(b.Y-a.Y)*(p.X-a.X);
    private static bool OnSegment(DrawingPoint a,DrawingPoint b,DrawingPoint p)=>Math.Abs(Cross(a,b,p))<Epsilon&&p.X>=Math.Min(a.X,b.X)-Epsilon&&p.X<=Math.Max(a.X,b.X)+Epsilon&&p.Y>=Math.Min(a.Y,b.Y)-Epsilon&&p.Y<=Math.Max(a.Y,b.Y)+Epsilon;
    private static bool Proper(DrawingPoint a,DrawingPoint b,DrawingPoint c,DrawingPoint d)=>Cross(a,b,c)*Cross(a,b,d)<-Epsilon&&Cross(c,d,a)*Cross(c,d,b)<-Epsilon;
    private static bool Intersects(DrawingPoint a,DrawingPoint b,DrawingPoint c,DrawingPoint d)=>Proper(a,b,c,d)||OnSegment(a,b,c)||OnSegment(a,b,d)||OnSegment(c,d,a)||OnSegment(c,d,b);
    private static bool OnEdge(IReadOnlyList<DrawingPoint> polygon,DrawingPoint p)=>polygon.Select((a,i)=>OnSegment(a,polygon[(i+1)%polygon.Count],p)).Any(x=>x);
    public static bool Contains(IReadOnlyList<DrawingPoint> polygon,DrawingPoint p)
    {
        if(OnEdge(polygon,p))return true;var inside=false;
        for(int i=0,j=polygon.Count-1;i<polygon.Count;j=i++){var a=polygon[i];var b=polygon[j];if((a.Y>p.Y)!=(b.Y>p.Y)&&p.X<(b.X-a.X)*(p.Y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;}
        return inside;
    }
    public static bool Inside(IReadOnlyList<DrawingPoint> polygon,IReadOnlyList<DrawingPoint> boundary)
    {
        if(polygon.Any(p=>!Contains(boundary,p)))return false;
        for(var i=0;i<polygon.Count;i++)
        {
            var a=polygon[i];var b=polygon[(i+1)%polygon.Count];var cuts=new List<decimal>{0,1};
            for(var j=0;j<boundary.Count;j++){var c=boundary[j];var d=boundary[(j+1)%boundary.Count];var dx=b.X-a.X;var dy=b.Y-a.Y;var ex=d.X-c.X;var ey=d.Y-c.Y;var den=dx*ey-dy*ex;if(Math.Abs(den)<Epsilon)continue;var t=((c.X-a.X)*ey-(c.Y-a.Y)*ex)/den;var u=((c.X-a.X)*dy-(c.Y-a.Y)*dx)/den;if(t>0&&t<1&&u>=-Epsilon&&u<=1+Epsilon)cuts.Add(t);}
            cuts.Sort();for(var j=1;j<cuts.Count;j++){var t=(cuts[j-1]+cuts[j])/2;if(!Contains(boundary,new(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t)))return false;}
        }
        return true;
    }
    public static bool Overlap(IReadOnlyList<DrawingPoint> a,IReadOnlyList<DrawingPoint> b)
    {
        if (a.Max(p=>p.X)<=b.Min(p=>p.X)+Epsilon || b.Max(p=>p.X)<=a.Min(p=>p.X)+Epsilon || a.Max(p=>p.Y)<=b.Min(p=>p.Y)+Epsilon || b.Max(p=>p.Y)<=a.Min(p=>p.Y)+Epsilon) return false;
        if(a.Select((p,i)=>b.Select((q,j)=>Proper(p,a[(i+1)%a.Count],q,b[(j+1)%b.Count])).Any(x=>x)).Any(x=>x)||a.Any(p=>Contains(b,p)&&!OnEdge(b,p))||b.Any(p=>Contains(a,p)&&!OnEdge(a,p)))return true;
        for(var i=0;i<a.Count;i++){var p=a[i];var q=a[(i+1)%a.Count];var mid=new DrawingPoint((p.X+q.X)/2,(p.Y+q.Y)/2);if(Contains(b,mid)&&!OnEdge(b,mid))return true;var len=(decimal)Math.Sqrt((double)((q.X-p.X)*(q.X-p.X)+(q.Y-p.Y)*(q.Y-p.Y)));if(len<=0)continue;
            foreach(var sign in new[]{-1,1}){var probe=new DrawingPoint(mid.X-sign*(q.Y-p.Y)/len*.0001m,mid.Y+sign*(q.X-p.X)/len*.0001m);if(Contains(a,probe)&&Contains(b,probe)&&!OnEdge(b,probe))return true;}}
        return false;
    }
}
