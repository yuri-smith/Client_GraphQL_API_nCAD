using System;
using System.Drawing;
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
    public static class Box
    {
        public static class CreateNew
        {
            public static class Empty
            {
                public async static Task<Model.Box> Jig(Point3d point)
                {
                    Model.Box box = CreateBoxJig(point);
                    box.ID = await BoxToDB(box);
                    DrawBox(box);
                    return box;
                }
            }




            private static Model.Box CreateBoxJig(Point3d point)
            {
                Model.Box createdBox = null;
                Document dc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = dc.Editor;
                Database db = dc.Database;

                Model.BoxJigXY boxJigXY;
                Model.BoxJigZ boxJigZ;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    boxJigXY = new BoxJigXY(point);
                    ed.Drag(boxJigXY);
                    tr.Commit();
                }

                CoordinateSystem3d curCs = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;
                CoordinateSystem3d targetCs = new CoordinateSystem3d(boxJigXY.Point1, curCs.Zaxis.Negate(), curCs.Yaxis);
                Shared.ChangeCurrentUcs(targetCs);

                boxJigZ = new Model.BoxJigZ(boxJigXY.Point1, boxJigXY.Point2, boxJigXY.Point3, boxJigXY.Point4);
                ed.Drag(boxJigZ);
                createdBox = new Model.Box
                {
                    cs = new CoordinateSystem3d(targetCs.Origin, targetCs.Zaxis, targetCs.Yaxis),
                    SizeX = boxJigXY.dX,
                    SizeY = boxJigXY.dY,
                    SizeZ = -boxJigZ.dZ,
                    ID = null
                };

                Shared.ChangeCurrentUcs(curCs);

                return createdBox;
            }

            public static async Task<string> BoxToDB(Model.Box box)
            {
                string boxID;
                using (var httpClient = new HttpClient())
                {
                    StringBuilder sb = new StringBuilder();
                    StringWriter textWriter = new StringWriter(sb);
                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Serialize(textWriter, new
                    {
                        query = @"mutation CreateBox($input: BoxInput!) {
                            createBox(input: $input) {
                                id
                            }
                        }",
                        variables = new
                        {
                            input = new
                            {
                                ucs = new
                                {
                                    originPoint = new
                                    {
                                        x = box.cs.Origin.X,
                                        y = box.cs.Origin.Y,
                                        z = box.cs.Origin.Z,
                                    },
                                    axisX = new
                                    {
                                        x = box.cs.Xaxis.X,
                                        y = box.cs.Xaxis.Y,
                                        z = box.cs.Xaxis.Z,
                                    },
                                    axisY = new
                                    {
                                        x = box.cs.Yaxis.X,
                                        y = box.cs.Yaxis.Y,
                                        z = box.cs.Yaxis.Z,
                                    }
                                },
                                size = new
                                {
                                    x = box.SizeX,
                                    y = box.SizeY,
                                    z = box.SizeZ
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

                    //возврат значений из тасков
                    //https://csharp.hotexamples.com/ru/examples/-/HttpClient/SendAsync/php-httpclient-sendasync-method-examples.html
                    //https://docs.microsoft.com/ru-ru/dotnet/standard/parallel-programming/how-to-return-a-value-from-a-task
                    HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                    string responseString = await response.Content.ReadAsStringAsync();
                    JObject parsedRes = JObject.Parse(responseString);
                    JObject errors = (JObject)parsedRes["errors"];

                    boxID = parsedRes["data"]["createBox"]["id"].Value<string>();
                }
                return boxID;
            }




            /// <summary>
            /// точки бокса в мировых коорднатах
            /// первые четыре - основание в плоскости box.cs
            /// остальные - соответственные на противоположной грани
            /// </summary>
            /// <param name="box"></param>
            /// <returns></returns>
            private static Point3d[] GetPointsBoxWCS(Model.Box box)
            {
                CoordinateSystem3d cs = box.cs;
                Point3d[] pnts = new Point3d[8];//точки бокса в мировых коорднатах
                pnts[0] = cs.Origin;
                pnts[1] = pnts[0].Add(cs.Xaxis.MultiplyBy(box.SizeX));
                pnts[2] = pnts[0].Add(cs.Yaxis.MultiplyBy(box.SizeY));
                pnts[3] = pnts[2].Add(cs.Xaxis.MultiplyBy(box.SizeX));

                pnts[4] = pnts[0].Add(cs.Zaxis.MultiplyBy(box.SizeZ));
                pnts[5] = pnts[4].Add(cs.Xaxis.MultiplyBy(box.SizeX));
                pnts[6] = pnts[4].Add(cs.Yaxis.MultiplyBy(box.SizeY));
                pnts[7] = pnts[6].Add(cs.Xaxis.MultiplyBy(box.SizeX));

                return pnts;
            }

        }

        public static class DrawFromDB
        {
            public static async Task ByID(string id)
            {
                Model.Box box = await Shared.BoxFromDb(id);
                DrawBox(box);
            }
            public static async void All()
            {
                using (var httpClient = new HttpClient())
                {
                    StringBuilder sb = new StringBuilder();
                    StringWriter textWriter = new StringWriter(sb);
                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Serialize(textWriter, new
                    {
                        query = @"query Boxes {
                            boxes {
                                id
                                ucs {
                                    originPoint {
                                        x
                                        y
                                        z
                                    }
                                    axisX {
                                        x
                                        y
                                        z
                                    }
                                    axisY {
                                        x
                                        y
                                        z
                                    }
                                }
                                size {
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

                    HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                    string responseString = await response.Content.ReadAsStringAsync();
                    JObject parsedRes = JObject.Parse(responseString);
                    JObject errors = (JObject)parsedRes["errors"];

                    JArray boxes = (JArray)parsedRes["data"]["boxes"];

                    for (int i = 0; i < boxes.Count; i++)
                    {
                        DrawBox(
                            new Model.Box()
                            {
                                ID = boxes[i]["id"].Value<string>(),
                                cs = new CoordinateSystem3d(
                                    new Point3d(
                                        boxes[i]["ucs"]["originPoint"]["x"].Value<double>(),
                                        boxes[i]["ucs"]["originPoint"]["y"].Value<double>(),
                                        boxes[i]["ucs"]["originPoint"]["z"].Value<double>()
                                    ),
                                    new Vector3d(
                                        boxes[i]["ucs"]["axisX"]["x"].Value<double>(),
                                        boxes[i]["ucs"]["axisX"]["y"].Value<double>(),
                                        boxes[i]["ucs"]["axisX"]["z"].Value<double>()
                                    ),
                                    new Vector3d(
                                        boxes[i]["ucs"]["axisY"]["x"].Value<double>(),
                                        boxes[i]["ucs"]["axisY"]["y"].Value<double>(),
                                        boxes[i]["ucs"]["axisY"]["z"].Value<double>()
                                    )
                                ),
                                SizeX = boxes[i]["size"]["x"].Value<double>(),
                                SizeY = boxes[i]["size"]["y"].Value<double>(),
                                SizeZ = boxes[i]["size"]["z"].Value<double>()
                            });
                    }

                }
            }

        }

        public static class DeleteFromDB
        {
            public static async Task ByID(string id)
            {
                using (var httpClient = new HttpClient())
                {
                    StringBuilder sb = new StringBuilder();
                    StringWriter textWriter = new StringWriter(sb);
                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Serialize(textWriter, new
                    {
                        query = @"mutation DeleteBox($deleteBoxId: ID!) {
                            deleteBox(id: $deleteBoxId) {
                                id  
                            }
                        }",
                        variables = new
                        {
                            deleteBoxId = id
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
                }
            }
            public static async Task All()
            {
                using (var httpClient = new HttpClient())
                {
                    StringBuilder sb = new StringBuilder();
                    StringWriter textWriter = new StringWriter(sb);
                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Serialize(textWriter, new
                    {
                        query = @"query ClearBoxes {
                          clearBoxes {
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

        }


        public static void DrawBox(Model.Box box)
        {
            Shared.ChangeCurrentUcs(box.cs);

            Point3d[] pnts = GetPointsBoxWCS(box);

            DBObjectCollection ents = new DBObjectCollection
                {
                    new Line(pnts[0], pnts[1]),
                    new Line(pnts[0], pnts[2]),
                    new Line(pnts[2], pnts[3]),
                    new Line(pnts[1], pnts[3]),

                    new Line(pnts[0], pnts[4]),
                    new Line(pnts[1], pnts[5]),
                    new Line(pnts[2], pnts[6]),
                    new Line(pnts[3], pnts[7]),

                    new Line(pnts[4], pnts[5]),
                    new Line(pnts[4], pnts[6]),
                    new Line(pnts[6], pnts[7]),
                    new Line(pnts[5], pnts[7])
                };

            Shared.DrawGroup(ents, "GroupBox", "Box_" + box.ID);
        }

        public static Extremum GetExt(Model.Box box)
        {
            Extremum ext = new Extremum();

            Point3d[] pnts = GetPointsBoxWCS(box);

            List<double> xxx = (from p in pnts
                                orderby p.X
                                select p.X).ToList<double>();
            ext.minX = xxx.First();
            ext.maxX = xxx.Last();

            List<double> yyy = (from p in pnts
                                orderby p.Y
                                select p.Y).ToList<double>();
            ext.minY = yyy.First();
            ext.maxY = yyy.Last();

            List<double> zzz = (from p in pnts
                                orderby p.Z
                                select p.Z).ToList<double>();
            ext.minZ = zzz.First();
            ext.maxZ = zzz.Last();

            return ext;
        }

        private static Point3d[] GetPointsBoxWCS(Model.Box box)
        {
            CoordinateSystem3d cs = box.cs;
            Point3d[] pnts = new Point3d[8];//точки бокса в мировых коорднатах
            pnts[0] = cs.Origin;
            pnts[1] = pnts[0].Add(cs.Xaxis.MultiplyBy(box.SizeX));
            pnts[2] = pnts[0].Add(cs.Yaxis.MultiplyBy(box.SizeY));
            pnts[3] = pnts[2].Add(cs.Xaxis.MultiplyBy(box.SizeX));

            pnts[4] = pnts[0].Add(cs.Zaxis.MultiplyBy(box.SizeZ));
            pnts[5] = pnts[4].Add(cs.Xaxis.MultiplyBy(box.SizeX));
            pnts[6] = pnts[4].Add(cs.Yaxis.MultiplyBy(box.SizeY));
            pnts[7] = pnts[6].Add(cs.Xaxis.MultiplyBy(box.SizeX));

            return pnts;
        }
    }
}
