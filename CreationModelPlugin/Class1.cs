﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModelPlugin : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = LevelUtils.SelectLevel(doc, "Этаж 1");

            Level level2 = LevelUtils.SelectLevel(doc, "Этаж 2");

            WallUtils.WallCreate(doc, 10000, 5000, level1, level2);

            DoorUtils.AddDoor(doc, level1);

            WindowUtils.AddWindow(doc, level1, 3);

            
            return Result.Succeeded;
        }
    }
    public class LevelUtils
    {
        public static Level SelectLevel (Document doc, string name)
        {
            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()                
                .Where(x => x.Name.Equals(name))
                .FirstOrDefault();
            return level;
        }
    }
    public class WallUtils
    {
        public static void WallCreate (Document doc, int width, int depth, Level level1, Level level2)
        {
            double width1 = UnitUtils.ConvertToInternalUnits(width, DisplayUnitType.DUT_MILLIMETERS);
            double depth1 = UnitUtils.ConvertToInternalUnits(depth, DisplayUnitType.DUT_MILLIMETERS);
            double dx = width1 / 2;
            double dy = depth1 / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            transaction.Commit();
        }

    }
    public class DoorUtils
    {
        public static void AddDoor(Document doc, Level level)
        {            
            var walls = new FilteredElementCollector(doc)
            .OfClass(typeof(Wall))
            .WhereElementIsNotElementType()
            .Cast<Wall>()
            .ToList();
            Wall wall = walls[1];

            FamilySymbol  doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()                
                .Where(x=>x.Name.Equals("ДГ 21-9Л"))
                .Where(x=>x.FamilyName.Equals("ГОСТ - 6629-88 (Внутренние двери)"))
                .FirstOrDefault();            

            Transaction tr = new Transaction(doc, "Размещение дверей");

        tr.Start();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        tr.Commit();
        }
    }
    public class WindowUtils
    {
        public static void AddWindow(Document doc, Level level, int count)
        {
            var walls = new FilteredElementCollector(doc)
            .OfClass(typeof(Wall))
            .WhereElementIsNotElementType()
            .Cast<Wall>()
            .ToList();           

            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("790х1700"))
                .Where(x => x.FamilyName.Equals("Универсальное окно"))
                .FirstOrDefault();

            LocationCurve hostCurve = walls[0].Location as LocationCurve;

            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            List<XYZ> points = new List<XYZ>();
            for (int i = 1; i < count + 1; i++)
            {
                double x = point1.X - (((point1.X - point2.X) / (count + 1) * i));
                double y = point1.Y - (((point1.Y - point2.Y) / (count + 1) * i));
                double z = point1.Z;
                XYZ point = new XYZ(x, y, z);
                points.Add(point);
            }


            Transaction tr = new Transaction(doc, "Размещение окон");

            tr.Start();
            foreach (XYZ point in points)
            {
                windowType.Activate();
                FamilyInstance window =  doc.Create.NewFamilyInstance(point, windowType, walls[0], level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);   

            }
            
            tr.Commit();
        }
    }
}

