using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HostMgd.ApplicationServices;
//using NcApp = HostMgd.ApplicationServices.Application;
using HostMgd.EditorInput;
using Teigha.Runtime;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using TrManager = Teigha.DatabaseServices.TransactionManager;
using Teigha.GraphicsInterface;

using GraphQl_Client.Model;
using GraphQl_Client.Utils;



namespace GraphQl_Client
{

    public class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            if (Utils.Shared.IsContinue("\nТестовое задание. Плагин \"Интеграция с GraphQL-API (NodeJS+MongoDB)\". Продолжить?: "))
            {
                string uri;
                string pathToConfig = Application.GetSystemVariable("MYDOCUMENTSPREFIX").ToString() + @"\GraphQl_Client.config.json";
                if (!File.Exists(pathToConfig))
                {
                    //PromptStringOptions pso = new PromptStringOptions("URI сервера?: ")
                    //{
                    //    AllowSpaces = false,
                    //    DefaultValue = "http://localhost:8000/graphql",
                    //    UseDefaultValue = true
                    //};
                    PromptStringOptions pso = new PromptStringOptions("URI сервера?: ")
                    {
                        AllowSpaces = false,
                        DefaultValue = "http://ys-node-ncad.herokuapp.com/graphql",
                        UseDefaultValue = true
                    };
                    uri = Application.DocumentManager.MdiActiveDocument.Editor.GetString(pso).StringResult;

                    JObject cfg =
                        new JObject(
                            new JProperty("config",
                                new JObject(
                                    new JProperty("uri", uri)
                                )
                            )
                        );

                    File.WriteAllText(pathToConfig, cfg.ToString());
                }
                JObject jo = JObject.Parse(File.ReadAllText(pathToConfig));
                Config.uri = jo["config"]["uri"].Value<string>() ;

                YS();
            }
            else
            {
                Terminate();
            }
                    
        }


        [CommandMethod("YS")]
        public void YS()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nКоманды плагина:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("Box");
            pko.Keywords.Add("Lines");
            pko.Keywords.Default = "Box";
            PromptResult pr = ed.GetKeywords(pko);
            if (pr.Status != PromptStatus.OK) return;
            if (pr.StringResult.Equals("Box")) Box(true);
            if (pr.StringResult.Equals("Lines")) Lines(true);

        }

        #region YS-Box

        [CommandMethod("YS_Box")]
        public void Box()
        {
            Box(false);
        }

        private void Box(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nBox:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("CreateNew");
            pko.Keywords.Add("DrawFromDB");
            pko.Keywords.Add("DeleteFromDB");
            pko.Keywords.Default = "CreateNew";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) YS();
            if (pr.StringResult.Equals("CreateNew")) Box_CreateNew(true);
            if (pr.StringResult.Equals("DrawFromDB")) Box_DrawFromDB(true);
            if (pr.StringResult.Equals("DeleteFromDB")) Box_DeleteFromDB(true);
            YS();
        }

        [CommandMethod("YS_Box_CreateNew")]
        public void Box_CreateNew()
        {
            Box_CreateNew(false);
        }

        private void Box_CreateNew(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nBox_CreateNew:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("Empty");
            pko.Keywords.Add("WithLines");
            pko.Keywords.Default = "Empty";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Box(true);
            if (pr.StringResult.Equals("Empty")) Box_CreateNew_Empty(true);
            if (pr.StringResult.Equals("WithLines")) Box_CreateNew_WithLines(true);
            Box(true);
        }

        [CommandMethod("YS_Box_CreateNew_Empty")]
        public void Box_CreateNew_Empty()
        {
            Box_CreateNew_Empty(false);
        }

        private async void Box_CreateNew_Empty(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nBox_CreateNew_Empty:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("Jig");
            pko.Keywords.Add("Size");
            pko.Keywords.Default = "Jig";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Box_CreateNew(true);
            if (pr.StringResult.Equals("Jig"))
            {
                Model.Box box = await Utils.Box.CreateNew.Empty.Jig(BasePointInput(ed));
                ed.WriteMessage("newBox.ID = " + box.ID);
            }
            if (pr.StringResult.Equals("Size"))
            {
                Model.Box box = BoxBasePointInput(ed);
                box.ID = await Utils.Box.CreateNew.BoxToDB(box);
                Utils.Box.DrawBox(box);
                ed.WriteMessage("newBox.ID = " + box.ID);
            }
            Box_CreateNew(true);
        }

        [CommandMethod("YS_Box_CreateNew_WithLines")]
        public void Box_CreateNew_WithLines()
        {
            Box_CreateNew_WithLines(false);
        }

        private async void Box_CreateNew_WithLines(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nBox_CreateNew_WithLines:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("Jig");
            pko.Keywords.Add("Size");
            pko.Keywords.Default = "Jig";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Box_CreateNew(true);
            if (pr.StringResult.Equals("Jig"))
            {
                Model.Box box = await Utils.Box.CreateNew.Empty.Jig(BasePointInput(ed));
                await Utils.Lines.Generate.InBox.New(box, CountLinesInput(ed));
            }
            if (pr.StringResult.Equals("Size"))
            {
                Model.Box box = BoxBasePointInput(ed);
                await Utils.Box.CreateNew.BoxToDB(box);
                box.ID = await Utils.Box.CreateNew.BoxToDB(box);
                Utils.Box.DrawBox(box);
                await Utils.Lines.Generate.InBox.New(box, CountLinesInput(ed));
            }

            Box_CreateNew(true);
        }

        [CommandMethod("YS_Box_DrawFromDB")]
        public void Box_DrawFromDB()
        {
            Box_DrawFromDB(false);
        }

        private async void Box_DrawFromDB(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nBox_DrawFromDB:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("ByID");
            pko.Keywords.Add("All");
            pko.Keywords.Default = "ByID";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Box(true);
            if (pr.StringResult.Equals("ByID"))
            {
                ed.WriteMessage("На обратотку Box_DrawFromDB_ByID");
                await Utils.Box.DrawFromDB.ByID(IDinput(ed));

            }
            if (pr.StringResult.Equals("All"))
                Utils.Box.DrawFromDB.All();
            Box(true);
        }

        [CommandMethod("YS_Box_DeleteFromDB")]
        public void Box_DeleteFromDB()
        {
            Box_DeleteFromDB(false);
        }

        private async void Box_DeleteFromDB(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nBox_DeleteFromDB:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("ByID");
            pko.Keywords.Add("All");
            pko.Keywords.Default = "ByID";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Box(true);
            if (pr.StringResult.Equals("ByID"))
                await Utils.Box.DeleteFromDB.ByID(
                    ed.GetString("\n3dBox.ID:").StringResult);
            if (pr.StringResult.Equals("All"))
                await Utils.Box.DeleteFromDB.All();
            Box(true);
        }

        #endregion

        [CommandMethod("YS_Lines")]
        public void Lines()
        {
            Lines(false);
        }

        private void Lines(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("Generate");
            pko.Keywords.Add("DrawFromDB");
            pko.Keywords.Add("DeleteFromDB");
            pko.Keywords.Default = "Generate";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) YS();
            if (pr.StringResult.Equals("Generate")) Lines_Generate(true);
            if (pr.StringResult.Equals("DrawFromDB")) Lines_DrawFromDB(true);
            if (pr.StringResult.Equals("DeleteFromDB")) Lines_DeleteFromDB(true);
            YS();
        }

        [CommandMethod("YS_Lines_Generate")]
        public void Lines_Generate()
        {
            Lines_Generate(false);
        }

        private void Lines_Generate(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_Generate:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("OnServer");
            pko.Keywords.Add("InBox");
            pko.Keywords.Default = "OnServer";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines(true);
            if (pr.StringResult.Equals("OnServer")) Lines_Generate_OnServer(true);
            if (pr.StringResult.Equals("InBox")) Lines_Generate_InBox(true);
            Lines(true);
        }

        [CommandMethod("YS_Lines_Generate_OnServer")]
        public void Lines_Generate_OnServer()
        {
            Lines_Generate_OnServer(false);
        }

        private async void Lines_Generate_OnServer(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //проверка на коллениарность векторов текущей и мировой систем координат
            double eps = 0.0001;
            CoordinateSystem3d curCS = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;
            if (Math.Abs(curCS.Xaxis.DotProduct(Vector3d.XAxis) - 1.0) > eps ||
                Math.Abs(curCS.Yaxis.DotProduct(Vector3d.YAxis) - 1.0) > eps)
            {
                ed.WriteMessage("\nДля генерации линий на стороне сервера одноименные вектора осей" +
                    "\nтекущей и мировой систем координат д.б. коллинеарными," +
                    "\nт.е. - лежать на одной и той же или на параллельных линиях!" +
                    "\nПри этом они могут быть противоположно направленны." +
                    "\nАльтернатива - 'Generate-InBox' - генерятся в CADe, а потом на сервер и в базу данных");
                return;
            } 

            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_Generate_OnServer:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("Cube");
            pko.Keywords.Add("BoxSize");
            pko.Keywords.Add("BoxBasePoint");
            pko.Keywords.Default = "Cube";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines_Generate(true);
            string res = pr.StringResult;
            if (res.Equals("Cube") || 
                res.Equals("BoxSize") ||
                res.Equals("BoxBasePoint"))
            {
                Model.Box box = new Model.Box();
                //создает коробку по введенным параметрам
                if (pr.StringResult.Equals("Cube"))
                    box = CubeInput(ed);
                if (pr.StringResult.Equals("BoxSize"))
                    box = BoxSizeInput(ed);
                if (pr.StringResult.Equals("BoxBasePoint"))
                    box = BoxBasePointInput(ed);

                box.ID = await Utils.Box.CreateNew.BoxToDB(box);//сохраняет коробку в базе и берет ее ID
                Extremum ext = Utils.Box.GetExt(box);           //вычисляет экстремальные значения поординат
                await Utils.Lines.Generate.OnServer(ext, CountLinesInput(ed));//генерирует на сервере случ.линии внутри области
                //берет линии попадающие в данную коробку из базы (не только созданные в этом сеансе)
                DBObjectCollection lines = await Utils.Lines.DrawFromDB.IncludedExt(ext, true);
                Shared.DrawGroup(lines, "GeneratedLinesOnServer", "LinesOfBox_" + box.ID);//рисует линии 
                Utils.Box.DrawBox(box);//рисует коробку
            }
            Lines_Generate(true);
        }

        [CommandMethod("YS_Lines_Generate_InBox")]
        public void Lines_Generate_InBox()
        {
            Lines_Generate_InBox(false);
        }

        private async void Lines_Generate_InBox(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_Generate_InBox:")
            {
                AllowNone = false
            };
            pko.Keywords.Add("New");
            pko.Keywords.Add("ByID");
            pko.Keywords.Default = "New";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines_Generate(true);
            if (pr.StringResult.Equals("New"))
            {
                Model.Box box = await Utils.Box.CreateNew.Empty.Jig(BasePointInput(ed));
                await Utils.Lines.Generate.InBox.New(box, CountLinesInput(ed));
            }
            if (pr.StringResult.Equals("ByID"))
            {
                await Utils.Lines.Generate.InBox.ByID(
                    IDinput(ed),
                    CountLinesInput(ed));
            } 
            Lines_Generate(true);
        }

        [CommandMethod("YS_Lines_DrawFromDB")]
        public void Lines_DrawFromDB()
        {
            Lines_DrawFromDB(false);
        }

        private async void Lines_DrawFromDB(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_DrawFromDB:")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false
            };
            pko.Keywords.Add("IncludedNewBox");
            pko.Keywords.Add("IncludedBoxByID");
            pko.Keywords.Add("All");
            pko.Keywords.Default = "IncludedBoxByID";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines(true);
            if (pr.StringResult.Equals("IncludedNewBox"))
                Lines_DrawFromDB_IncludedNewBox(true);
            if (pr.StringResult.Equals("IncludedBoxByID")) 
                Lines_DrawFromDB_IncludedBoxByID(true);
            if (pr.StringResult.Equals("All")) 
                await Utils.Lines.DrawFromDB.All();
            Lines(true);
        }

        [CommandMethod("YS_Lines_DrawFromDB_IncludedNewBox")]
        public void Lines_DrawFromDB_IncludedNewBox()
        {
            Lines_DrawFromDB_IncludedNewBox(false);
        }
        private void Lines_DrawFromDB_IncludedNewBox(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_DrawFromDB_IncludedNewBox:")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false
            };
            pko.Keywords.Add("Jig");
            pko.Keywords.Add("Size");
            pko.Keywords.Default = "Jig";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines_DrawFromDB(true);
            if (pr.StringResult.Equals("Jig"))
                Lines_DrawFromDB_IncludedNewBox_Jig(true);
            if (pr.StringResult.Equals("Size"))
                Lines_DrawFromDB_IncludedNewBox_Size(true);

            Lines_DrawFromDB(true);
        }

        [CommandMethod("YS_Lines_DrawFromDB_IncludedNewBox_Jig")]
        public void Lines_DrawFromDB_IncludedNewBox_Jig()
        {
            Lines_DrawFromDB_IncludedNewBox_Jig(false);
        }
        private async void Lines_DrawFromDB_IncludedNewBox_Jig(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_DrawFromDB_IncludedNewBox_Jig:")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false
            };
            pko.Keywords.Add("Part");
            pko.Keywords.Add("Full");
            pko.Keywords.Default = "Part";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines_DrawFromDB_IncludedNewBox(true);

            Model.Box box = await Utils.Box.CreateNew.Empty.Jig(BasePointInput(ed));
            ed.WriteMessage("newBox.ID = " + box.ID);

            if (pr.StringResult.Equals("Part"))
                await Utils.Lines.DrawFromDB.IncludedBoxByID.Part(ed, box);
            if (pr.StringResult.Equals("Full"))
                await Utils.Lines.DrawFromDB.IncludedBoxByID.Full(ed, box);

            Lines_DrawFromDB_IncludedNewBox(true);
        }

        [CommandMethod("YS_Lines_DrawFromDB_IncludedNewBox_Size")]
        public void Lines_DrawFromDB_IncludedNewBox_Size()
        {
            Lines_DrawFromDB_IncludedNewBox_Size(false);
        }
        private async void Lines_DrawFromDB_IncludedNewBox_Size(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_DrawFromDB_IncludedNewBox_Size:")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false
            };
            pko.Keywords.Add("Part");
            pko.Keywords.Add("Full");
            pko.Keywords.Default = "Part";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines_DrawFromDB_IncludedNewBox(true);

            Model.Box box = BoxBasePointInput(ed);

            box.ID = await Utils.Box.CreateNew.BoxToDB(box);
            Utils.Box.DrawBox(box);
            if (pr.StringResult.Equals("Part"))
                await Utils.Lines.DrawFromDB.IncludedBoxByID.Part(ed, box);
            if (pr.StringResult.Equals("Full"))
                await Utils.Lines.DrawFromDB.IncludedBoxByID.Full(ed, box);

            Utils.Shared.ChangeCurrentUcs(box.cs);
            Lines_DrawFromDB_IncludedNewBox(true);
        }



        [CommandMethod("YS_Lines_DrawFromDB_IncludedBoxByID")]
        public void Lines_DrawFromDB_IncludedBoxByID()
        {
            Lines_DrawFromDB_IncludedBoxByID(false);
        }

        private async void Lines_DrawFromDB_IncludedBoxByID(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_DrawFromDB_IncludedBoxByID:")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false
            };
            pko.Keywords.Add("Part");
            pko.Keywords.Add("Full");
            pko.Keywords.Default = "Part";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines(true);
            Model.Box box = await Utils.Shared.BoxFromDb(IDinput(ed));
            if (pr.StringResult.Equals("Part"))
            {
                await Utils.Lines.DrawFromDB.IncludedBoxByID.Part(ed, box);
            }
            if (pr.StringResult.Equals("Full"))
            {
                await Utils.Lines.DrawFromDB.IncludedBoxByID.Full(ed, box);
            }

            Lines_DrawFromDB(true);
        }

        [CommandMethod("YS_Lines_DeleteFromDB")]
        public void Lines_DeleteFromDB()
        {
            Lines_DeleteFromDB(false);
        }

        private async void Lines_DeleteFromDB(bool isParent)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptKeywordOptions pko = new PromptKeywordOptions("\nLines_DeleteFromDB:")
            {
                AppendKeywordsToMessage = true,
                Message = "Удалить все линии из базы данных?",
                AllowNone = false
            };
            pko.Keywords.Add("Да");
            pko.Keywords.Add("Нет");
            pko.Keywords.Default = "Нет";
            PromptResult pr = ed.GetKeywords(pko);
            if (isParent && pr.Status != PromptStatus.OK) Lines(true);
            if (pr.StringResult.Equals("Нет")) Lines(true);
            if (pr.StringResult.Equals("Да")) await Utils.Lines.DeleteFromDB();
            Lines(true);
        }



        #region Input(ы)

        private int CountLinesInput(Editor ed)
        {
            PromptIntegerOptions pio = new PromptIntegerOptions("\nЗадайте количество линий:")
            {
                AllowNegative = false,
                AllowNone = false,
                AllowZero = false,
                DefaultValue = 100,
                LowerLimit = 2,
                UpperLimit = 500,
                UseDefaultValue = true
            };
            PromptIntegerResult pir = ed.GetInteger(pio);
            return pir.Value;
        }
        private Model.Box CubeInput(Editor ed)
        {
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nРазмер ребра куба:")
            {
                AllowNegative = true,
                AllowNone = false,
                AllowZero = false,
                DefaultValue = 1000,
                UseDefaultValue = true
            };
            double s = ed.GetDouble(pdo).Value;
            return new Model.Box()
            {
                cs = new CoordinateSystem3d(
                    new Point3d(-s * 0.5, -s * 0.5, -s * 0.5),
                    new Vector3d(1, 0, 0),
                    new Vector3d(0, 1, 0)
                ),
                SizeX = s,
                SizeY = s,
                SizeZ = s
            };
        }
        private Model.Box BoxSizeInput(Editor ed)
        {
            PromptDoubleOptions pdo = new PromptDoubleOptions("")
            {
                AllowNegative = true,
                AllowNone = false,
                AllowZero = false,
                DefaultValue = 1000,
                UseDefaultValue = true
            };
            pdo.Message = "\nРазмер вдоль оси X:";
            double x = ed.GetDouble(pdo).Value;
            pdo.Message = "\nРазмер вдоль оси Y:";
            double y = ed.GetDouble(pdo).Value;
            pdo.Message = "\nРазмер вдоль оси Z:";
            double z = ed.GetDouble(pdo).Value;
            //return new Model.Box(
            //    new Point3d(-x * 0.5, -y * 0.5, -z * 0.5),
            //    new Vector3d(1, 0, 0),
            //    new Vector3d(0, 1, 0),
            //    x, y, z);
            return new Model.Box()
            {
                cs = new CoordinateSystem3d(
                    new Point3d(-x * 0.5, -y * 0.5, -z * 0.5),
                    new Vector3d(1, 0, 0),
                    new Vector3d(0, 1, 0)
                ),
                SizeX = x,
                SizeY = y,
                SizeZ = z
            };

        }
        private Model.Box BoxBasePointInput(Editor ed)
        {
            Point3d firstCorner = BasePointInput(ed);
            CoordinateSystem3d cs = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;
            Shared.ChangeCurrentUcs(new CoordinateSystem3d(firstCorner, cs.Xaxis, cs.Yaxis));

            PromptCornerOptions pco = new PromptCornerOptions("\nВторая точка:", firstCorner)
            {
                AllowNone = false,
                LimitsChecked = false
            };
            Point3d secondCorner = ed.GetCorner(pco).Value;

            PromptDoubleOptions pdo = new PromptDoubleOptions("")
            {
                AllowNegative = true,
                AllowNone = false,
                AllowZero = false,
                DefaultValue = Convert.ToDouble(-int.MaxValue),
                UseDefaultValue = true
            };
            pdo.Message = "\nРазмер вдоль оси Z:";
            double z = ed.GetDouble(pdo).Value;


            return new Model.Box()
            {
                cs = new CoordinateSystem3d(
                    firstCorner,
                    cs.Xaxis,
                    cs.Yaxis),
                SizeX = secondCorner.X - firstCorner.X,
                SizeY = secondCorner.Y - firstCorner.Y,
                SizeZ = z
            };

        }
        private Point3d BasePointInput(Editor ed)
        {
            PromptPointOptions ppo = new PromptPointOptions("Базовая точка:");
            PromptPointResult ppr = ed.GetPoint(ppo);
            return ppr.Value;
        }
        private string IDinput(Editor ed)
        {
            PromptStringOptions pso = new PromptStringOptions("\n3dBox.ID:") 
            { AllowSpaces = false };
            return ed.GetString(pso).StringResult;
        }
        #endregion

        public void Terminate() { }

    }
}
