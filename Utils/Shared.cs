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
using HostMgd.GraphicsSystem;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;

using GraphQl_Client.Model;

namespace GraphQl_Client.Utils
{
    public static class Shared
    {
        public static void DrawGroup(DBObjectCollection ents, string groupDescr, string groupName)
        {
            Document dc = Application.DocumentManager.MdiActiveDocument;
            Database db = dc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                DBDictionary groupDictionary = (DBDictionary)tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite);
                Group group = new Group(groupDescr, true);
                ObjectId groupId = groupDictionary.SetAt(groupName, group);
                tr.AddNewlyCreatedDBObject(group, true);

                ObjectIdCollection ids = new ObjectIdCollection();
                foreach (Entity ent in ents)
                {
                    ObjectId id = modelSpace.AppendEntity(ent);
                    ids.Add(id);
                    tr.AddNewlyCreatedDBObject(ent, true);
                }
                group.InsertAt(0, ids);

                tr.Commit();
            }
        }
        public static async Task<Model.Box> BoxFromDb(string id)
        {
            Model.Box box;
            using (var httpClient = new HttpClient())
            {
                StringBuilder sb = new StringBuilder();
                StringWriter textWriter = new StringWriter(sb);
                JsonSerializer serializer = new JsonSerializer();

                serializer.Serialize(textWriter, new
                {
                    query = @"query Box($boxId: ID!) {
                        box(id: $boxId) {
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
                    }",
                    variables = new
                    {
                        boxId = id
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

                var b = parsedRes["data"]["box"];

                box = new Model.Box()
                {
                    ID = b["id"].Value<string>(),
                    cs = new CoordinateSystem3d(
                        new Point3d(
                            b["ucs"]["originPoint"]["x"].Value<double>(),
                            b["ucs"]["originPoint"]["y"].Value<double>(),
                            b["ucs"]["originPoint"]["z"].Value<double>()
                        ),
                        new Vector3d(
                            b["ucs"]["axisX"]["x"].Value<double>(),
                            b["ucs"]["axisX"]["y"].Value<double>(),
                            b["ucs"]["axisX"]["z"].Value<double>()
                        ),
                        new Vector3d(
                            b["ucs"]["axisY"]["x"].Value<double>(),
                            b["ucs"]["axisY"]["y"].Value<double>(),
                            b["ucs"]["axisY"]["z"].Value<double>()
                        )
                    ),
                    SizeX = b["size"]["x"].Value<double>(),
                    SizeY = b["size"]["y"].Value<double>(),
                    SizeZ = b["size"]["z"].Value<double>()
                };
            }
            return box;
        }
        public static void ChangeCurrentUcs(CoordinateSystem3d TargetCs)
        {
            Document dc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = dc.Editor;
            Database db = dc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                UcsTable ut = tr.GetObject(db.UcsTableId, OpenMode.ForRead) as UcsTable;
                UcsTableRecord utr;
                if (ut.Has("NewBox") == false)
                {
                    utr = new UcsTableRecord();
                    utr.Name = "NewBox";
                    ut.UpgradeOpen();
                    ut.Add(utr);
                    tr.AddNewlyCreatedDBObject(utr, true);
                }
                else
                {
                    utr = tr.GetObject(ut["NewBox"], OpenMode.ForWrite) as UcsTableRecord;
                }
                utr.Origin = TargetCs.Origin;
                utr.XAxis = TargetCs.Xaxis;
                utr.YAxis = TargetCs.Yaxis;

                ViewportTableRecord vtr = tr.GetObject(ed.ActiveViewportId, OpenMode.ForWrite) as ViewportTableRecord;
                vtr.IconAtOrigin = true;
                vtr.IconEnabled = true;
                vtr.SetUcs(utr.ObjectId);
                ed.UpdateTiledViewportsFromDatabase();

                tr.Commit();
            }
        }
        public static bool IsContinue(string Message)
        {
            bool res = false;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions(Message)
            {
                AllowNone = false
            };
            pko.Keywords.Add("Да");
            pko.Keywords.Add("Нет");
            pko.Keywords.Default = "Да";
            PromptResult pr = ed.GetKeywords(pko);
            if (pr.StringResult.Equals("Да")) res = true;

            return res;
        }
        public static Matrix3d DCS2WCS(Viewport vp)
        {
            return
                Matrix3d.Rotation(-vp.TwistAngle, vp.ViewDirection, vp.ViewTarget) *
                Matrix3d.Displacement(vp.ViewTarget - Point3d.Origin) *
                Matrix3d.PlaneToWorld(vp.ViewDirection);
        }
    }



}
