using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;

using GraphQl_Client.Model;

namespace GraphQl_Client.Utils
{
    public static class Lines
    {
        public static class Generate
        {
            public static async Task OnServer(Extremum ext, int lineCount)
            {
                string ss = Config.uri;
                using (var httpClient = new HttpClient())
                {

                    StringBuilder sb = new StringBuilder();
                    StringWriter textWriter = new StringWriter(sb);
                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Serialize(textWriter, new
                    {
                        query = @"mutation GenerateLines($input: GenerateLinesInput) {
                            generateLines(input: $input) {
                                point1 {
                                    x
                                    y
                                    z
                                }
                                point2 {
                                    x
                                    y
                                    z
                                }
                            }
                        }",
                        variables = new
                        {
                            input = new
                            {
                                lineCount,
                                ext.minX,
                                ext.maxX,
                                ext.minY,
                                ext.maxY,
                                ext.minZ,
                                ext.maxZ
                            }
                        }
                    });

                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json"),
                        RequestUri = new Uri(Config.uri),
                    };

                    HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                    string responseString = await response.Content.ReadAsStringAsync();
                    JObject parsedRes = JObject.Parse(responseString);
                    JObject errors = (JObject)parsedRes["errors"];
                }
            }

            public static class InBox
            {
                public static async Task New(Model.Box box, int countLines)
                {
                    DBObjectCollection lines = CreateLines(box, countLines);
                    Shared.DrawGroup(lines, "GroupLinesOfBox", "LinesOfBox_" + box.ID);
                    await LinesToDB(lines);
                }
                public static async Task ByID(string BoxID, int countLines)
                {
                    Model.Box box = await Shared.BoxFromDb(BoxID);
                    Shared.ChangeCurrentUcs(box.cs);
                    await New(box, countLines);
                }
            }
        }

        public static class DrawFromDB
        {
            //public static class IncludedNewBox
            //{
            //    public static class Jig
            //    {
            //        public static void Part(Editor ed)
            //        {
            //            ed.WriteMessage("\nВыполнение - Lines_DrawFromDB_IncludedNewBox_Jig_Part");
            //        }
            //        public static void Full(Editor ed)
            //        {
            //            ed.WriteMessage("\nВыполнение - Lines_DrawFromDB_IncludedNewBox_Jig_Full");
            //        }
            //    }
            //    public static class Size
            //    {
            //        public static void Part(Editor ed)
            //        {
            //            ed.WriteMessage("\nВыполнение - Lines_DrawFromDB_IncludedNewBox_Size_Part");
            //        }
            //        public static void Full(Editor ed)
            //        {
            //            ed.WriteMessage("\nВыполнение - Lines_DrawFromDB_IncludedNewBox_Size_Full");
            //        }

            //    }
            //}
            public static class IncludedBoxByID
            {
                public static async Task Part(Editor ed, Model.Box box)
                {
                    await LoadAndDraw(ed, box, false);
                }
                public static async Task Full(Editor ed, Model.Box box)
                {
                    await LoadAndDraw(ed, box, true);
                }
            }

            private static async Task LoadAndDraw(Editor ed, Model.Box box, bool isFull)
            {
                Extremum ext = Utils.Box.GetExt(box);
                DBObjectCollection lines = await IncludedExt(ext, isFull);
                double eps = 0.0001;
                if (Math.Abs(box.cs.Xaxis.DotProduct(new Vector3d(1.0, 0.0, 0.0)) - 1.0) > eps ||
                    Math.Abs(box.cs.Yaxis.DotProduct(new Vector3d(0.0, 1.0, 0.0)) - 1.0) > eps)
                {
                    lines = CheckLines(box, lines, isFull);
                }
                if (lines.Count > 0)
                {
                    Shared.DrawGroup(lines, "GroupLinesOfBox", "LinesOfBox_" + box.ID);
                    Utils.Box.DrawBox(box);
                }
                else
                {
                    if (isFull)
                        ed.WriteMessage("\nВ базе нет линий целиком попадающих в этот 3dBox");
                    else
                        ed.WriteMessage("\nВ базе нет линий хотя бы одной из конечных точек попадающих в этот 3dBox");
                }
            }

            public static async Task All()
            {
                var sb = new StringBuilder();
                var textWriter = new StringWriter(sb);
                var serializer = new JsonSerializer();
                serializer.Serialize(textWriter, new
                {
                    query = @"query Lines{ 
                    lines{ 
                        point1{
                            x
                            y
                            z
                        }
                        point2{
                            x
                            y
                            z
                        }
                    }
                }"
                });
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json"),
                    RequestUri = new Uri(Config.uri),
                };
                HttpResponseMessage response = await new HttpClient().SendAsync(httpRequestMessage);
                string responseString = await response.Content.ReadAsStringAsync();
                JObject rss = JObject.Parse(responseString);
                JObject errors = (JObject)rss["errors"];
                JObject data = (JObject)rss["data"];
                JArray lines = (JArray)data["lines"];

                var ents = new DBObjectCollection();
                foreach (var lin in lines)
                {
                    ents.Add(
                        new Line(
                            new Point3d(
                                lin["point1"]["x"].Value<double>(),
                                lin["point1"]["y"].Value<double>(),
                                lin["point1"]["z"].Value<double>()
                            ),
                            new Point3d(
                                lin["point2"]["x"].Value<double>(),
                                lin["point2"]["y"].Value<double>(),
                                lin["point2"]["z"].Value<double>()
                            )
                        )
                    );
                }

                Shared.DrawGroup(ents, "Все линии из базы", null);

            }

            public static async Task<DBObjectCollection> IncludedExt(Extremum ext, bool isFull)
            {
                var sb = new StringBuilder();
                var textWriter = new StringWriter(sb);
                var serializer = new JsonSerializer();
                if(isFull)
                    serializer.Serialize(textWriter, new
                    {
                        query = @"query Linesf($input: FilterLinesInput) {
                        linesf(input: $input) {
                            id
                            point1 {
                                x
                                y
                                z
                            }
                            point2 {
                                x
                                y
                                z
                            }
                        }
                    }",
                        variables = new
                        {
                            input = new
                            {
                                ext.minX,
                                ext.maxX,
                                ext.minY,
                                ext.maxY,
                                ext.minZ,
                                ext.maxZ
                            }
                        }
                    });
                else
                    serializer.Serialize(textWriter, new
                    {
                        query = @"query Linesp($input: FilterLinesInput) {
                        linesp(input: $input) {
                            id
                            point1 {
                                x
                                y
                                z
                            }
                            point2 {
                                x
                                y
                                z
                            }
                        }
                    }",
                        variables = new
                        {
                            input = new
                            {
                                ext.minX,
                                ext.maxX,
                                ext.minY,
                                ext.maxY,
                                ext.minZ,
                                ext.maxZ
                            }
                        }
                    });

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json"),
                    RequestUri = new Uri(Config.uri),
                };
                HttpResponseMessage response = await new HttpClient().SendAsync(httpRequestMessage);
                string responseString = await response.Content.ReadAsStringAsync();
                JObject rss = JObject.Parse(responseString);
                JObject errors = (JObject)rss["errors"];
                JObject data = (JObject)rss["data"];
                string dataName = isFull ? "linesf" : "linesp";
                JArray lines = (JArray)data[dataName];

                //var lines = data["lines"];
                var ents = new DBObjectCollection();
                foreach (var lin in lines)
                {
                    ents.Add(
                        new Line(
                            new Point3d(
                                lin["point1"]["x"].Value<double>(),
                                lin["point1"]["y"].Value<double>(),
                                lin["point1"]["z"].Value<double>()
                            ),
                            new Point3d(
                                lin["point2"]["x"].Value<double>(),
                                lin["point2"]["y"].Value<double>(),
                                lin["point2"]["z"].Value<double>()
                            )
                        )
                    );
                }

                return ents;
            }

            public static DBObjectCollection CheckLines(Model.Box box, DBObjectCollection lines, bool isFull)
            {
                DBObjectCollection checkedLines = new DBObjectCollection();
                Model.Extremum extBoxUcs = new Extremum()
                {
                    minX = Math.Min(0d, box.SizeX),
                    maxX = Math.Max(0d, box.SizeX),
                    minY = Math.Min(0d, box.SizeY),
                    maxY = Math.Max(0d, box.SizeY),
                    minZ = Math.Min(0d, box.SizeZ),
                    maxZ = Math.Max(0d, box.SizeZ)
                };

                Matrix3d wcs2ucs = Matrix3d.AlignCoordinateSystem(
                    box.cs.Origin,
                    box.cs.Xaxis,
                    box.cs.Yaxis,
                    box.cs.Xaxis.CrossProduct(box.cs.Yaxis),
                    new Point3d(0, 0, 0),
                    new Vector3d(1, 0, 0),
                    new Vector3d(0, 1, 0),
                    new Vector3d(0, 0, 1));


                Point3d pnt1, pnt2;
                if(isFull)
                foreach (Line line in lines)
                {
                    pnt1 = line.StartPoint.TransformBy(wcs2ucs);
                    if ((pnt1.X > extBoxUcs.minX && pnt1.X < extBoxUcs.maxX) &&
                        (pnt1.Y > extBoxUcs.minY && pnt1.Y < extBoxUcs.maxY) &&
                        (pnt1.Z > extBoxUcs.minZ && pnt1.Z < extBoxUcs.maxZ))
                    {
                        pnt2 = line.EndPoint.TransformBy(wcs2ucs);
                        if ((pnt2.X > extBoxUcs.minX && pnt2.X < extBoxUcs.maxX) &&
                            (pnt2.Y > extBoxUcs.minY && pnt2.Y < extBoxUcs.maxY) &&
                            (pnt2.Z > extBoxUcs.minZ && pnt2.Z < extBoxUcs.maxZ))
                        {
                            checkedLines.Add(line);
                        }
                    }
                } 
                else
                    foreach (Line line in lines)
                    {
                        pnt1 = line.StartPoint.TransformBy(wcs2ucs);
                        if ((pnt1.X > extBoxUcs.minX && pnt1.X < extBoxUcs.maxX) &&
                            (pnt1.Y > extBoxUcs.minY && pnt1.Y < extBoxUcs.maxY) &&
                            (pnt1.Z > extBoxUcs.minZ && pnt1.Z < extBoxUcs.maxZ)) 
                        {
                            checkedLines.Add(line);
                        }
                        else
                        {
                            pnt2 = line.EndPoint.TransformBy(wcs2ucs);
                            if ((pnt2.X > extBoxUcs.minX && pnt2.X < extBoxUcs.maxX) &&
                                (pnt2.Y > extBoxUcs.minY && pnt2.Y < extBoxUcs.maxY) &&
                                (pnt2.Z > extBoxUcs.minZ && pnt2.Z < extBoxUcs.maxZ))
                            {
                                checkedLines.Add(line);
                            }

                        }
                    }

                return checkedLines;
            }
        }

        public static async Task DeleteFromDB()
        {
            using (var httpClient = new HttpClient())
            {
                StringBuilder sb = new StringBuilder();
                StringWriter textWriter = new StringWriter(sb);
                JsonSerializer serializer = new JsonSerializer();

                serializer.Serialize(textWriter, new
                {
                    query = @"query ClearLines {
                        clearLines {
                            id
                        }
                    }"
                });

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json"),
                    RequestUri = new Uri(Config.uri),
                };

                HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                string responseString = await response.Content.ReadAsStringAsync();
            }
        }

        private static DBObjectCollection CreateLines(Model.Box box, int countLines)
        {
            double minX, maxX, minY, maxY, minZ, maxZ;
            DBObjectCollection ents;

            minX = Math.Min(0.0, box.SizeX);
            maxX = Math.Max(0.0, box.SizeX);
            minY = Math.Min(0.0, box.SizeY);
            maxY = Math.Max(0.0, box.SizeY);
            minZ = Math.Min(0.0, box.SizeZ);
            maxZ = Math.Max(0.0, box.SizeZ);

            Matrix3d ucsToWcs = Application.DocumentManager.MdiActiveDocument
                .Editor.CurrentUserCoordinateSystem;

            Random random = new Random();

            ents = new DBObjectCollection();
            for (int i = 0; i < countLines; i++)
            {
                ents.Add(
                    new Line(
                        new Point3d(
                            NextDouble(random, minX, maxX),
                            NextDouble(random, minY, maxY),
                            NextDouble(random, minZ, maxZ)
                        ).TransformBy(ucsToWcs),
                        new Point3d(
                            NextDouble(random, minX, maxX),
                            NextDouble(random, minY, maxY),
                            NextDouble(random, minZ, maxZ)
                        ).TransformBy(ucsToWcs)
                    )
                );
            }
            return ents;
        }

        private static double NextDouble(this Random random, double minValue, double maxValue)
        {
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }

        private static async Task LinesToDB(DBObjectCollection Lines)
        {
            using (var httpClient = new HttpClient())
            {
                foreach (Line line in Lines)
                {
                    StringBuilder sb = new StringBuilder();
                    StringWriter textWriter = new StringWriter(sb);
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(textWriter, new
                    {
                        query = @"mutation CreateLine($input: LineInput!) {
                            createLine(input: $input) {
                                id
                            }
                        }",
                        variables = new
                        {
                            input = new
                            {
                                point1 = new
                                {
                                    x = line.StartPoint.X,
                                    y = line.StartPoint.Y,
                                    z = line.StartPoint.Z
                                },
                                point2 = new
                                {
                                    x = line.EndPoint.X,
                                    y = line.EndPoint.Y,
                                    z = line.EndPoint.Z
                                }
                            }
                        }
                    });

                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json"),
                        RequestUri = new Uri(Config.uri),
                    };

                    HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                    string responseString = await response.Content.ReadAsStringAsync();
                    JObject parsedRes = JObject.Parse(responseString);
                    JObject errors = (JObject)parsedRes["errors"];
                    if (errors != null)
                    {

                    }

                }

            }

        }
    }
}
