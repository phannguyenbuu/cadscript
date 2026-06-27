using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    public class BatchConvertDXFCLS
    {
        [CommandMethod("BatchDXF")]
        public static void Main()
        {
            string dirPath = @"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details";
            if (!Directory.Exists(dirPath))
            {
                ACD.WR("Directory not found: {0}", dirPath);
                return;
            }

            string[] files = Directory.GetFiles(dirPath, "*.dwg");
            ACD.WR("Found {0} DWG files to process.", files.Length);

            foreach (string file in files)
            {
                // Skip temporary or already processed helper files
                string fileName = Path.GetFileName(file);
                if (fileName.Contains(".tmp") || fileName.Contains(".dxf.dwg") || fileName.StartsWith("__probe")) 
                    continue;

                try
                {
                    using (Database db = new Database(false, true))
                    {
                        db.ReadDwgFile(file, FileShare.ReadWrite, true, "");
                        
                        // Extract Text Content
                        string textContent = "";
                        // Use IR.GetEntities to find Text and MText
                        db.GetEntities(null, EN_SELECT.AC_DXF, "TEXT", "MTEXT");
                        if (IR.SelectedIds.Count > 0)
                        {
                            foreach (ObjectId tid in IR.SelectedIds)
                            {
                                string c = db._getContent(tid);
                                if (!string.IsNullOrWhiteSpace(c) && !c.Contains("%%") && c.Length < 50) // Basic filter for "content" text
                                {
                                    textContent = c.Trim();
                                    break;
                                }
                            }
                        }

                        // Extract Circle Diameter
                        double diameter = 0;
                        db.GetEntities(null, EN_SELECT.AC_DXF, "CIRCLE");
                        if (IR.SelectedIds.Count > 0)
                        {
                            // Get diameter of the first circle found
                            diameter = db._getRadius(IR.SelectedIds[0]) * 2;
                        }

                        // Sanitize textContent for filename
                        string cleanText = string.Concat(textContent.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                        
                        // Build suffix
                        string suffix = "";
                        if (!string.IsNullOrEmpty(cleanText)) 
                            suffix += "_" + cleanText;
                        
                        if (diameter > 0) 
                            suffix += "_D" + Math.Round(diameter).ToString();

                        string newName = Path.Combine(dirPath, Path.GetFileNameWithoutExtension(file) + suffix + ".dxf");
                        
                        if (File.Exists(newName)) 
                            File.Delete(newName);
                        
                        // Save as DXF (AutoCAD 2007 format for compatibility)
                        db.DxfOut(newName, 16, DwgVersion.AC1021);
                        ACD.WR("Processed: {0} -> {1}", fileName, Path.GetFileName(newName));
                    }
                }
                catch (Exception ex)
                {
                    ACD.WR("Error processing {0}: {1}", fileName, ex.Message);
                }
            }
            ACD.WR("Batch conversion finished.");
        }
    }
}
