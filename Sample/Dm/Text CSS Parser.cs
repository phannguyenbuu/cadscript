using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;


using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;

namespace AcadScript
{
	public class CssParser
	{
		private List<string> _styleSheets;
		private SortedList<string, StyleClass> _scc;
		public SortedList<string, StyleClass> Styles
		{
			get { return this._scc; }
			set { this._scc = value; }
		}

		public CssParser()
		{
			this._styleSheets = new List<string>();
			this._scc = new SortedList<string, StyleClass>();
		}

		public void AddStyleSheetFile(string path)
		{
			this._styleSheets.Add(path);
			ProcessStyleSheet(path);

			_collectGroup();
		}

		public void AddStyleSheetContent(string content)
		{
			string[] parts = content.Split('}');
			foreach (string s in parts)
			{
				if (CleanUp(s).IndexOf('{') > -1)
				{
					FillStyleClass(s);
				}
			}

			_collectGroup();
		}

		void _collectGroup()
        {
			for (int i = 0; i < _scc.Count; i++)
				for (int j = 0; j < i; j++)
					if (_scc.Keys[i].Upper().Contains(_scc.Keys[j].Upper()))
					{
						//for (int k = 0; k < _scc.Values[j].Attributes; j++)
						foreach (var val in _scc.Values[j].Attributes)
							if(!_scc.Values[i].Attributes.ContainsKey(val.Key))
								_scc.Values[i].Attributes.Add(val.Key, val.Value);
						else
								_scc.Values[i].Attributes[val.Key] = val.Value;
					}
		}

		public string GetStyleSheet(int index)
		{
			return this._styleSheets[index];
		}

		private void ProcessStyleSheet(string path)
		{
			string content = CleanUp(File.ReadAllText(path));
			string[] parts = content.Split('}');
			
			foreach (string s in parts)
			{
				if (CleanUp(s).IndexOf('{') > -1)
				{
					FillStyleClass(s);
				}
			}

			
		}

		private void FillStyleClass(string s)
		{
			StyleClass sc = null;
			string[] parts = s.Split('{');
			string styleName = CleanUp(parts[0]).Trim().ToLower();

			if (this._scc.ContainsKey(styleName))
			{
				sc = this._scc[styleName];
				this._scc.Remove(styleName);
			}
			else
			{
				sc = new StyleClass();
			}

			sc.Name = styleName;

			string[] atrs = CleanUp(parts[1]).Replace("}", "").Split(';');

			foreach (string a in atrs)
			{
				if (a.Contains(":"))
				{
					string _key = a.Split(':')[0].Trim().ToLower();
					if (sc.Attributes.ContainsKey(_key))
					{
						sc.Attributes.Remove(_key);
					}
					sc.Attributes.Add(_key, a.Split(':')[1].Trim().ToLower());
				}
			}
			this._scc.Add(sc.Name, sc);
		}

		private string CleanUp(string s)
		{
			string temp = s;
			string reg = "(/\\*(.|[\r\n])*?\\*/)|(//.*)";
			Regex r = new Regex(reg);
			temp = r.Replace(temp, "");
			temp = temp.Replace("\r", "").Replace("\n", "");
			return temp;
		}

		public class StyleClass
		{
			private string _name = string.Empty;
			public string Name
			{
				get { return _name; }
				set { _name = value; }
			}

			private SortedList<string, string> _attributes = new SortedList<string, string>();
			public SortedList<string, string> Attributes
			{
				get { return _attributes; }
				set { _attributes = value; }
			}

			public override string ToString()
			{
				string res = "";
				foreach (var itm in _attributes)
					res += itm.Key + " is " + itm.Value + ";";
				return res;
			}
		}

		
	}

	public class TextCSSParserCLS
    {
		
		static string _getParentname(string blockname, string[] blocklist)
        {
			 

			string[] ar = blockname.filter(".");
			string res = null;

			for(int i = ar.Length - 2; i >= 0; i--)
            {
				string s = DE.NumericArray(0, i).Select(n => ar[n]).ToTextStr(".");

				//ACD.WR(s);

				if (blocklist.Contains(s))
				{
					res = s;
					break;
				}
            }

			return res;
        }

		

		static void RunCSS(CssParser css)
        {
			ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
			ObjectIdCollection blockIds = IR.SelectedIds;
			string[] blocklist = ACD.DB.ListBlock();

			foreach (var obj in css.Styles)
			{
				string contain_block = _getParentname(obj.Key, blocklist);

				if (blocklist.Contains(obj.Key) && !contain_block.empty())
				{
					//ACD.DB.GetEntities(null, EN_SELECT.AC_DXF_AND_NAME, "INSERT", contain_block);
					//if (IR.SelectedIds.Count > 0)
					
					
					ObjectId parentId = blockIds.ToList().FirstOrDefault(id
						=> ACD.DB._getIdName(id) == contain_block);

					ACD.WR("- Item {0} value {1} contain_block {2} parent {3}",
						obj.Key, obj.Value, contain_block, parentId);

					if (!parentId.IsNull)
					{
						pPos basept = ACD.DB._getPoint(parentId);

						ACD.DB.BlockEntitiesAction(parentId, ids =>
						{
							string point_blockname = null;
							ObjectIdCollection pointIds = new ObjectIdCollection();

							foreach (var itm in obj.Value.Attributes)
							{
								string key = itm.Key.Upper();

								if (key.StartsWith("POINT.SELECTOR"))
								{
									point_blockname = itm.Value.Upper();


								}
							}

							//---START REPLACE------//

							
							ObjectId srcId = blockIds.ToList().FirstOrDefault(id => ACD.DB._getIdName(id) == obj.Key);

							ObjectIdCollection subIds = ACD.DB.CloneObjects(ids);
							
							pointIds = subIds.ToList().Where(id => ACD.DB._isBlock(id)
							&& ACD.DB._getIdName(id).Upper().Contains(point_blockname)).ToCollection();

							ACD.WR("- POINTIDS {0}", pointIds.Count);

							if (pointIds.Count > 0)
							{
								pointIds = pointIds.ToList()
								.OrderBy(id => ACD.DB._getPoint(id).Y.roundNumber(1000))
									.ThenBy(id => ACD.DB._getPoint(id).X).ToCollection();

								foreach (var itm in obj.Value.Attributes)
								{
									string key = itm.Key.Upper();

									if (key.Contains("POINTS[") && key.Contains("]") && key.Contains("."))
									{
										if (!key._getInComma("[]").empty())
										{
											string s_index = key._getInComma("[]");

											int index = (int)s_index.ToNumber();
											if (s_index.Upper() == "F") index = 0;
											if (s_index.Upper() == "L") index = pointIds.Count - 1;

											ObjectId objId = pointIds[index];

											double v = itm.Value.ToNumber();
											pPos cur_p = ACD.DB._getPoint(objId);
											pPos p = cur_p.Clone();

											if (key.EndsWith(".MX"))
												p.X += v;
											else if (key.EndsWith(".MY"))
												p.Y += v;
											else if (key.EndsWith(".MZ"))
												p.Z += v;
											else if (key.EndsWith(".X"))
												p.X = v - basept.X;
											else if (key.EndsWith(".Y"))
												p.Y = v - basept.Y;
											else if (key.EndsWith(".Z"))
												p.Z = v  - basept.Z;

											ACD.DB.MoveObject(objId, p - cur_p);
											ACD.WR("------------> Block children {0} index {1} pos {2}",
											point_blockname, index, p);
										}
									}
								}
							}

							ACD.DB.NewBlock(subIds, "$" + obj.Key, true, false, basept);

							ReplaceBlock(ACD.DB, blockIds, obj.Key, "$" + obj.Key);
						});
					}
				}
			}
		}


		static void ReplaceBlock(Database db, ObjectIdCollection blockIds, string fromBlockName, string toBlockName)
		{
			using (Transaction tr = db.TransactionManager.StartTransaction())
			{
				BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

				BlockTableRecord btr_from = (BlockTableRecord)tr.GetObject(bt[fromBlockName], OpenMode.ForRead);
				BlockTableRecord btr_to = (BlockTableRecord)tr.GetObject(bt[toBlockName], OpenMode.ForWrite);

				ObjectIdCollection srcId = blockIds.ToList().Where(id => ACD.DB._getIdName(id) == fromBlockName).ToCollection();

				foreach (ObjectId id in srcId)
				{
					BlockReference ent = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
					ent.BlockTableRecord = bt[toBlockName];
				}

				btr_from.Name = ACD.DB.uniqueBlockName(fromBlockName);
				btr_to.Name = fromBlockName;

				btr_from.Erase();

				tr.Commit();
			}
		}

		public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ACD.DB.GetEntities(null,EN_SELECT.AC_DXF,"MTEXT");

                foreach (ObjectId txtId in IR.SelectedIds)
                {
					string content = ACD.DB._getContent(txtId);
					if (content.StartsWith("[CSS]"))
					{
						CssParser css = new CssParser();
						css.AddStyleSheetContent(content.Substring(5));

						ACD.WR("Number of CSS Elements {0}", css.Styles.Count);

						RunCSS(css);
					}
                }

                ACD.Focus();
            }
        }
    }
}

