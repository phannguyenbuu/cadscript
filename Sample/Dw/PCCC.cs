using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace AcadScript
{
    public class PCCCCls
    {
        /// <summary>
        /// Đọc block được chọn → set tất cả layer của objects bên trong = màu 8 (Cyan)
        /// </summary>
        

        /// <summary>
        /// Core function: Duyệt block → collect layers → set color 8
        /// </summary>
        static void SetBlockLayersColor(ObjectId blockId, short colorIndex)
        {
            HashSet<string> uniqueLayers = new HashSet<string>();

            ACD.DB.BlockEntitiesAction(blockId, ids =>
            {
                foreach (ObjectId id in ids)
                {
                    string layerName = ACD.DB._getLayer(id).ToUpper();
                    if (!string.IsNullOrEmpty(layerName))
                        uniqueLayers.Add(layerName);
                }
            });

            // Set color cho từng layer
            foreach (string layerName in uniqueLayers)
            {
                ACD.DB._setLayerColor(layerName, colorIndex);
                ACD.WR("Layer {0} → Color 8", layerName);
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection blockIds = ACD.GetSelection(); // Hàm của bạn [file:74]

                foreach (ObjectId blockId in blockIds)
                {
                    if (ACD.DB._isBlock(blockId)) // Chỉ xử lý block
                    {
                        SetBlockLayersColor(blockId, 8); // Màu 8 = Cyan
                        ACD.WR("Processed block: {0}", ACD.DB._getIdName(blockId));
                    }
                }
                ACD.WR("Done! {0} blocks processed.", blockIds.Count);
            }

            ACD.Focus();
        }
    
    }
}
