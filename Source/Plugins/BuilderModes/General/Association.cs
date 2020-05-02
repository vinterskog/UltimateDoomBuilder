
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System.Collections.Generic;
using System;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Types;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	public class Association
	{
		private HashSet<int> tags;
		private Vector2D center;
		private UniversalType type;
		private int directlinktype;
		private List<EventLine> eventlines;
		private IRenderer2D renderer;
		private SelectableElement element;

		// Map elements that are associated
		private List<Thing> things;
		private List<Sector> sectors;
		private List<Linedef> linedefs;

		public HashSet<int> Tags { get { return tags; } }
		public Vector2D Center { get { return center; } }
		public UniversalType Type { get { return type; } }
		public int DirectLinkType { get { return directlinktype; } }
		public List<Thing> Things { get { return things; } }
		public List<Sector> Sectors { get { return sectors; } }
		public List<Linedef> Linedefs { get { return linedefs; } }
		public List<EventLine> EventLines { get { return eventlines; } }
		public bool IsEmpty { get { return things.Count == 0 && sectors.Count == 0 && linedefs.Count == 0; } }

		//mxd. This sets up the association
		public Association(IRenderer2D renderer)
		{
			this.tags = new HashSet<int> { 0 };
			this.renderer = renderer;

			things = new List<Thing>();
			sectors = new List<Sector>();
			linedefs = new List<Linedef>();
			eventlines = new List<EventLine>();
		}

		public void Set(SelectableElement element)
		{
			this.element = element;
			things = new List<Thing>();
			sectors = new List<Sector>();
			linedefs = new List<Linedef>();
			eventlines = new List<EventLine>();

			if(element is Sector)
			{
				Sector s = element as Sector;
				center = (s.Labels.Count > 0 ? s.Labels[0].position : new Vector2D(s.BBox.X + s.BBox.Width / 2, s.BBox.Y + s.BBox.Height / 2));

				type = UniversalType.SectorTag;
				tags = new HashSet<int>(s.Tags);

				GetAssociationsTo();
			}
			else if(element is Linedef)
			{
				Linedef ld = element as Linedef;
				center = ld.GetCenterPoint();

				type = UniversalType.LinedefTag;
				tags = new HashSet<int>(ld.Tags);

				GetAssociationsFrom();
				GetAssociationsTo();
			}
			else if(element is Thing)
			{
				Thing t = element as Thing;
				center = t.Position;

				ThingTypeInfo ti = General.Map.Data.GetThingInfoEx(t.Type);
				directlinktype = ti.ThingLink;

				type = UniversalType.ThingTag;
				tags = new HashSet<int>(new int[] { t.Tag });

				GetAssociationsFrom();
				GetAssociationsTo();
			}
		}

		public void Clear()
		{
			tags = new HashSet<int>();
			things = new List<Thing>();
			sectors = new List<Sector>();
			linedefs = new List<Linedef>();
			eventlines = new List<EventLine>();
		}

		private void GetAssociationsTo()
		{
			// Tag must be above zero
			if (General.GetByIndex(tags, 0) < 1) return;

			// Doom style referencing to sectors?
			if (General.Map.Config.LineTagIndicatesSectors && (type == UniversalType.SectorTag))
			{
				// Linedefs
				foreach (Linedef l in General.Map.Map.Linedefs)
				{
					// Any action on this line?
					if (l.Action <= 0 || !tags.Overlaps(l.Tags)) continue;
					if (!linedefs.Contains(l)) linedefs.Add(l);
					eventlines.Add(new EventLine(l.GetCenterPoint(), center));
				}
			}

			// Reverse association to Linedefs
			foreach (Linedef l in General.Map.Map.Linedefs)
			{
				// Known action on this line?
				if ((l.Action > 0) && General.Map.Config.LinedefActions.ContainsKey(l.Action))
				{
					LinedefActionInfo action = General.Map.Config.LinedefActions[l.Action];
					if (((action.Args[0].Type == (int)type) && (tags.Contains(l.Args[0]))) ||
						((action.Args[1].Type == (int)type) && (tags.Contains(l.Args[1]))) ||
						((action.Args[2].Type == (int)type) && (tags.Contains(l.Args[2]))) ||
						((action.Args[3].Type == (int)type) && (tags.Contains(l.Args[3]))) ||
						((action.Args[4].Type == (int)type) && (tags.Contains(l.Args[4]))))
					{
						if (!linedefs.Contains(l)) linedefs.Add(l);
						eventlines.Add(new EventLine(l.GetCenterPoint(), center));
					}
				}
			}

			// Doom format things don't have actions or tags, so stop here
			if (General.Map.DOOM)
				return;

			foreach (Thing t in General.Map.Map.Things)
			{
				// Get the thing type info
				ThingTypeInfo ti = General.Map.Data.GetThingInfoEx(t.Type);

				// Known action on this thing?
				if ((t.Action > 0) && General.Map.Config.LinedefActions.ContainsKey(t.Action))
				{
					//Do not draw the association if this is a child link.
					//  This prevents a reverse link to a thing via an argument, when it should be a direct tag-to-tag link instead.
					if (ti != null && directlinktype < 0 && directlinktype != -t.Type)
						continue;

					LinedefActionInfo action = General.Map.Config.LinedefActions[t.Action];
					if (((action.Args[0].Type == (int)type) && (tags.Contains(t.Args[0]))) ||
						 ((action.Args[1].Type == (int)type) && (tags.Contains(t.Args[1]))) ||
						 ((action.Args[2].Type == (int)type) && (tags.Contains(t.Args[2]))) ||
						 ((action.Args[3].Type == (int)type) && (tags.Contains(t.Args[3]))) ||
						 ((action.Args[4].Type == (int)type) && (tags.Contains(t.Args[4]))))
					{
						if (!things.Contains(t)) things.Add(t);
						eventlines.Add(new EventLine(t.Position, center));
					}

					//If there is a link setup on this thing, and it matches the association, then draw a direct link to any matching tag
					if (ti != null && directlinktype == t.Type && tags.Contains(t.Tag))
					{
						if (!things.Contains(t)) things.Add(t);
						eventlines.Add(new EventLine(t.Position, center));
					}
				}
				//mxd. Thing action on this thing?
				else if (t.Action == 0)
				{
					// Gets the association, unless it is a child link.
					// This prevents a reverse link to a thing via an argument, when it should be a direct tag-to-tag link instead.
					if (ti != null && directlinktype >= 0 && Math.Abs(directlinktype) != t.Type)
					{
						if (((ti.Args[0].Type == (int)type) && (tags.Contains(t.Args[0]))) ||
							 ((ti.Args[1].Type == (int)type) && (tags.Contains(t.Args[1]))) ||
							 ((ti.Args[2].Type == (int)type) && (tags.Contains(t.Args[2]))) ||
							 ((ti.Args[3].Type == (int)type) && (tags.Contains(t.Args[3]))) ||
							 ((ti.Args[4].Type == (int)type) && (tags.Contains(t.Args[4]))))
						{
							if (!things.Contains(t)) things.Add(t);
							eventlines.Add(new EventLine(t.Position, center));
						}
					}
				}
			}
		}

		private void GetAssociationsFrom()
		{
			// Use the line tag to highlight sectors (Doom style)
			if (General.Map.Config.LineTagIndicatesSectors && element is Linedef)
			{
				foreach(Sector s in General.Map.Map.Sectors)
				{
					if (tags.Contains(s.Tag))
						sectors.Add(s);
				}
			}
			else
			{
				LinedefActionInfo action = null;
				int[] actionargs = new int[5];
				Dictionary<int, HashSet<int>> actiontags = new Dictionary<int, HashSet<int>>();

				// Get the action and its arguments from a linedef or a thing, if they have them
				if (element is Linedef)
				{
					Linedef ld = element as Linedef;

					if (ld.Action > 0 && General.Map.Config.LinedefActions.ContainsKey(ld.Action))
						action = General.Map.Config.LinedefActions[ld.Action];

					actionargs = ld.Args;
				}
				else if(element is Thing)
				{
					Thing t = element as Thing;

					if (t.Action > 0 && General.Map.Config.LinedefActions.ContainsKey(t.Action))
						action = General.Map.Config.LinedefActions[t.Action];

					actionargs = t.Args;
				}

				if (action != null)
				{
					// Collect what map element the action arguments are referencing
					for (int i = 0; i < Linedef.NUM_ARGS; i++)
					{
						if ((action.Args[i].Type == (int)UniversalType.SectorTag ||
							action.Args[i].Type == (int)UniversalType.LinedefTag ||
							action.Args[i].Type == (int)UniversalType.ThingTag))
						{
							if (!actiontags.ContainsKey(action.Args[i].Type))
								actiontags[action.Args[i].Type] = new HashSet<int>();

							actiontags[action.Args[i].Type].Add(actionargs[i]);
						}
					}
				}
				else if (element is Thing && directlinktype >= 0 && Math.Abs(directlinktype) != ((Thing)element).Type)
				{
					Thing t = element as Thing;

					ThingTypeInfo ti = General.Map.Data.GetThingInfoEx(t.Type);

					if (ti != null && directlinktype >= 0 && Math.Abs(directlinktype) != t.Type)
					{
						for (int i = 0; i < Linedef.NUM_ARGS; i++)
						{
							if ((ti.Args[i].Type == (int)UniversalType.SectorTag ||
								ti.Args[i].Type == (int)UniversalType.LinedefTag ||
								ti.Args[i].Type == (int)UniversalType.ThingTag))
							{
								if (!actiontags.ContainsKey(ti.Args[i].Type))
									actiontags[ti.Args[i].Type] = new HashSet<int>();

								actiontags[ti.Args[i].Type].Add(actionargs[i]);
							}

						}
					}
				}

				foreach(KeyValuePair<int, HashSet<int>> kvp in actiontags)
				{
					if(kvp.Key == (int)UniversalType.SectorTag)
					{
						foreach(Sector s in General.Map.Map.Sectors)
						{
							if (kvp.Value.Overlaps(s.Tags) && !sectors.Contains(s))
							{
								sectors.Add(s);
								Vector2D sectorcenter = (s.Labels.Count > 0 ? s.Labels[0].position : new Vector2D(s.BBox.X + s.BBox.Width / 2, s.BBox.Y + s.BBox.Height / 2));
								eventlines.Add(new EventLine(center, sectorcenter));
							}
						}
					}
					else if(kvp.Key == (int)UniversalType.LinedefTag)
					{
						foreach(Linedef ld in General.Map.Map.Linedefs)
						{
							if(kvp.Value.Overlaps(ld.Tags))
							{
								linedefs.Add(ld);
								eventlines.Add(new EventLine(center, ld.GetCenterPoint()));
							}
						}
					}
					else if(kvp.Key == (int)UniversalType.ThingTag)
					{
						foreach(Thing t in General.Map.Map.Things)
						{
							if(kvp.Value.Contains(t.Tag) && !things.Contains(t))
							{
								things.Add(t);
								eventlines.Add(new EventLine(center, t.Position));
							}
						}
					}
				}
			}
		}

		public void Render()
		{
			List<Line3D> lines = new List<Line3D>(eventlines.Count);

			foreach (Thing t in things)
				renderer.RenderThing(t, General.Colors.Indication, General.Settings.ActiveThingsAlpha);

			// There must be a better way to do this
			foreach(Sector s in sectors)
			{
				int highlightedColor = General.Colors.Highlight.WithAlpha(128).ToInt();
				FlatVertex[] verts = new FlatVertex[s.FlatVertices.Length];
				s.FlatVertices.CopyTo(verts, 0);
				for (int i = 0; i < verts.Length; i++) verts[i].c = highlightedColor;
				renderer.RenderGeometry(verts, null, true);
			}

			foreach (EventLine el in eventlines)
				lines.Add(el.Line);

			renderer.RenderArrows(lines);
		}

		public void Plot()
		{
			foreach(Linedef ld in linedefs)
				renderer.PlotLinedef(ld, General.Colors.Indication);

			foreach (Sector s in sectors)
				renderer.PlotSector(s, General.Colors.Indication);
		}

		// This compares an association
		public static bool operator ==(Association a, Association b)
		{
			if(!(a is Association) || !(b is Association)) return false; //mxd
			return (a.type == b.type) && a.tags.SetEquals(b.tags);
		}

		// This compares an association
		public static bool operator !=(Association a, Association b)
		{
			if(!(a is Association) || !(b is Association)) return true; //mxd
			return (a.type != b.type) || !a.tags.SetEquals(b.tags);
		}

		//mxd 
		public override int GetHashCode() 
		{
			return base.GetHashCode();
		}

		//mxd
		public override bool Equals(object obj) 
		{
			if(!(obj is Association)) return false;

			Association b = (Association)obj;
			return (type == b.type) && tags.SetEquals(b.tags);
		}
	}
}
