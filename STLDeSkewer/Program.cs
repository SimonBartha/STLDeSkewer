using IxMilia.Stl;
using System;
using System.Globalization;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        var files = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        string diagPath = "diag.ini";

        double xyac = 100.0, xybd = 100.0, yzac = 100.0, yzbd = 100.0, zxac = 100.0, zxbd = 100; // default ideal

        // Read skewing diagonals from ini.
        // Measure like this
        // B               C
        //
        //
        //
        //
        // A               B
        var diagValues = File.ReadAllLines(diagPath)
                             .Select(line => line.Split(','))
                             .ToDictionary(parts => parts[0].Trim(), parts => double.Parse(parts[1], CultureInfo.InvariantCulture));

        xyac = diagValues["xyac"];
        xybd = diagValues["xybd"];
        yzac = diagValues["yzac"];
        yzbd = diagValues["yzbd"];
        zxac = diagValues["zxac"];
        zxbd = diagValues["zxbd"];

        double xyAngle = (180.0 / Math.PI) * Math.Atan((xybd / 2) / (xyac / 2)) * 2;
        double xyAngleTick = 90 - xyAngle;
        double xytan = Math.Tan((Math.PI / 180.0) * xyAngleTick);

        double yzAngle = (180.0 / Math.PI) * Math.Atan((yzbd / 2) / (yzac / 2)) * 2;
        double yzAngleTick = 90 - yzAngle;
        double yztan = Math.Tan((Math.PI / 180.0) * yzAngleTick);

        double zxAngle = (180.0 / Math.PI) * Math.Atan((zxbd / 2) / (zxac / 2)) * 2;
        double zxAngleTick = 90 - zxAngle;
        double zxtan = Math.Tan((Math.PI / 180.0) * zxAngleTick);

        Console.WriteLine($"Using xytan={xytan:F6}, yztan={yztan:F6}, zxtan={zxtan:F6}");

        foreach (var f in files.Where(x => x.ToLower().EndsWith(".stl") && !x.ToLower().EndsWith("-unskewed.stl")).ToList())
        {
            var stlFile = StlFile.Load(f);

            foreach (var triangle in stlFile.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    var v = GetVertex(triangle, i);

                    double xin = v.X;
                    double yin = v.Y;
                    double zin = v.Z;

                    double xout = xin - yin * xytan;
                    double yout = yin - zin * yztan;
                    xout = xout - zin * zxtan;
                    double zout = zin;

                    SetVertex(triangle, i, new StlVertex((float)xout, (float)yout, (float)zout));
                }
            }

            var outputPath = f[..^4] + "-Unskewed.stl";

            using (var fs = File.Create(outputPath))
            {
                stlFile.Save(fs, false);
            }

            Console.WriteLine($"Unskewed STL saved to {outputPath}");
        }
    }

    public static StlVertex GetVertex(StlTriangle triangle, int index)
    {
        if (index == 0) return triangle.Vertex1;
        else if (index == 1) return triangle.Vertex2;
        else if (index == 2) return triangle.Vertex3;
        else throw new IndexOutOfRangeException($"Triangle has only 3 vertices. Index {index} makes no sense.");
    }

    public static void SetVertex(StlTriangle triangle, int index, StlVertex vertex)
    {
        if (index == 0) triangle.Vertex1 = vertex;
        else if (index == 1) triangle.Vertex2 = vertex;
        else if (index == 2) triangle.Vertex3 = vertex;
        else throw new IndexOutOfRangeException($"Triangle has only 3 vertices. Index {index} makes no sense.");
        triangle.Normal = triangle.GetValidNormal();
    }
}
