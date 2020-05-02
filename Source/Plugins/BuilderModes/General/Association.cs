
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

		/// <summary>
		/// Sets the association to a map element. Only works with an instance of Thing, Sector, or Linedef.
		/// Also gets the forward and reverse associations
		/// </summary>
		/// <param name="element">An instance of Thing, Sector, or Linedef</param>
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
			}
			else if(element is Linedef)
			{
				Linedef ld = element as Linedef;
				center = ld.GetCenterPoint();

				type = UniversalType.LinedefTag;
				tags = new HashSet<int>(ld.Tags);
			}
			else if(element is Thing)
			{
				Thing t = element as Thing;
				center = t.Position;

				ThingTypeInfo ti = General.Map.Data.GetThingInfoEx(t.Type);
				directlinktype = ti.ThingLink;

				type = UniversalType.ThingTag;
				tags = new HashSet<int>(new int[] { t.Tag });
			}

			// Remove the tag 0, because nothing sensible will come from it
			tags.Remove(0);

			// Get forward and reverse associations
			GetAssociations();
		}

		/// <summary>
		/// Clears out all lists so that the association appears empty
		/// </summary>
		public void Clear()
		{
			tags = new HashSet<int>();
			things = new List<Thing>();
			sectors = new List<Sector>();
			linedefs = new List<Linedef>();
			eventlines = new List<EventLine>();
		}

		/// <summary>
		/// Get the forward and reverse associations between the element and other map elements
		/// </summary>
		private void GetAssociations()
		{
			Dictionary<int, HashSet<int>> actiontags = new Dictionary<int, HashSet<int>>();
			bool showforwardlabel = BuilderPlug.Me.EventLineLabelVisibility == 1 || BuilderPlug.Me.EventLineLabelVisibility == 3;
			bool showreverselabel = BuilderPlug.Me.EventLineLabelVisibility == 2 || BuilderPlug.Me.EventLineLabelVisibility == 3;

			// Special handling for Doom format maps where there the linedef's tag references sectors
			if (General.Map.Config.LineTagIndicatesSectors)
			{
				if (tags.Count == 0)
					return;

				// Forward association from linedef to sector
				if (element is Linedef)
				{
					foreach (Sector s in General.Map.Map.Sectors)
					{
						if (tags.Contains(s.Tag))
						{
							Vector2D sectorcenter = (s.Labels.Count > 0 ? s.Labels[0].position : new Vector2D(s.BBox.X + s.BBox.Width / 2, s.BBox.Y + s.BBox.Height / 2));

							sectors.Add(s);

							eventlines.Add(new EventLine(center, sectorcenter, showforwardlabel ? GetActionDescription(element) : null));
						}
					}
				}
				else if(element is Sector)
				{
					foreach(Linedef ld in General.Map.Map.Linedefs)
					{
						if(tags.Contains(ld.Tag))
						{
							linedefs.Add(ld);

							eventlines.Add(new EventLine(ld.GetCenterPoint(), center, showreverselabel ? GetActionDescription(ld) : null));
						}
					}
				}

				return;
			}

			// Get tags of map elements the element is referencing. This is used for the forward associations
			if (element is Linedef || element is Thing)
				actiontags = GetTagsByType();

			// Store presence of different types once, so that we don't have to do a lookup for each map element
			bool hassectortags = actiontags.ContainsKey((int)UniversalType.SectorTag);
			bool haslinedeftags = actiontags.ContainsKey((int)UniversalType.LinedefTag);
			bool hasthingtag = actiontags.ContainsKey((int)UniversalType.ThingTag);

			// Process all sectors in the map
			foreach (Sector s in General.Map.Map.Sectors)
			{
				bool addforward = false;
				bool addreverse = false;

				// Check for forward association (from the element to the sector)
				if (hassectortags && actiontags[(int)UniversalType.SectorTag].Overlaps(s.Tags))
					addforward = true;

				// Check the reverse association (from the sector to the element)
				// Nothing here yet

				if (addforward || addreverse)
				{
					Vector2D sectorcenter = (s.Labels.Count > 0 ? s.Labels[0].position : new Vector2D(s.BBox.X + s.BBox.Width / 2, s.BBox.Y + s.BBox.Height / 2));

					sectors.Add(s);

					if (addforward)
						eventlines.Add(new EventLine(center, sectorcenter, showforwardlabel ? GetActionDescription(element) : null));

					if (addreverse)
						eventlines.Add(new EventLine(sectorcenter, center, showreverselabel ? GetActionDescription(element) : null));
				}
			}

			// Process all linedefs in the map
			foreach(Linedef ld in General.Map.Map.Linedefs)
			{
				bool addforward = false;
				bool addreverse = false;

				// Check the forward association (from the element to the linedef)
				if (haslinedeftags && actiontags[(int)UniversalType.LinedefTag].Overlaps(ld.Tags))
					addforward = true;

				// Check the reverse association (from the linedef to the element)
				if (IsAssociatedToLinedef(ld))
					addreverse = true;

				if (addforward || addreverse)
				{
					linedefs.Add(ld);

					if (addforward)
						eventlines.Add(new EventLine(center, ld.GetCenterPoint(), showforwardlabel ? GetActionDescription(element) : null));

					if (addreverse)
						eventlines.Add(new EventLine(ld.GetCenterPoint(), center, showreverselabel ? GetActionDescription(ld) : null));
				}
			}

			// Doom format only knows associations between linedefs and sectors, but not thing, so stop here
			if (General.Map.DOOM)
				return;

			// Process all things in the map
			foreach(Thing t in General.Map.Map.Things)
			{
				bool addforward = false;
				bool addreverse = false;

				// Check the forward association (from the element to the thing)
				if (hasthingtag && actiontags[(int)UniversalType.ThingTag].Contains(t.Tag))
					addforward = true;

				// Check the reverse association (from the thing to the element). Only works for Hexen and UDMF,
				// as Doom format doesn't have any way to reference other map elements
				if (IsAssociatedToThing(t))
					addreverse = true;

				if (addforward || addreverse)
				{
					things.Add(t);

					if (addforward)
						eventlines.Add(new EventLine(center, t.Position, showforwardlabel ? GetActionDescription(element) : null));

					if (addreverse)
						eventlines.Add(new EventLine(t.Position, center, GetActionDescription(t)));
				}
			}
		}

		/// <summary>
		/// Gets a dictionary of sector tags, linedef tags, and thing tags, grouped by their type, that the map element is referencing
		/// </summary>
		/// <returns>Dictionary of sector tags, linedef tags, and thing tags that the map element is referencing</returns>
		private Dictionary<int, HashSet<int>> GetTagsByType()
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
			else if (element is Thing)
			{
				Thing t = element as Thing;

				if (t.Action > 0 && General.Map.Config.LinedefActions.ContainsKey(t.Action))
					action = General.Map.Config.LinedefActions[t.Action];

				actionargs = t.Args;
			}
			else // element is a Sector
			{
				return actiontags;
			}

			if (action != null)
			{
				// Collect what map element the action arguments are referencing. Ignore the argument if it's 0, so that they
				// are not associated to everything untagged
				for (int i = 0; i < Linedef.NUM_ARGS; i++)
				{
					if ((action.Args[i].Type == (int)UniversalType.SectorTag ||
						action.Args[i].Type == (int)UniversalType.LinedefTag ||
						action.Args[i].Type == (int)UniversalType.ThingTag) &&
						actionargs[i] > 0)
					{
						if (!actiontags.ContainsKey(action.Args[i].Type))
							actiontags[action.Args[i].Type] = new HashSet<int>();

						actiontags[action.Args[i].Type].Add(actionargs[i]);
					}
				}
			}
			else if (element is Thing && directlinktype >= 0 && Math.Abs(directlinktype) != ((Thing)element).Type)
			{
				// The direct link shenanigans if the thing doesn't have an action, but still reference something through
				// the action parameters
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

			return actiontags;
		}

		/// <summary>
		/// Checks if there's an association between the element and a Linedef
		/// </summary>
		/// <param name="linedef">Linedef to check the association against</param>
		/// <returns>true if the Linedef and the element are associated, false if not</returns>
		private bool IsAssociatedToLinedef(Linedef linedef)
		{
			// Doom style reference from linedef to sector?
			if (General.Map.Config.LineTagIndicatesSectors && element is Sector)
			{
				if (linedef.Action > 0 && tags.Overlaps(linedef.Tags))
					return true;
			}

			// Known action on this line?
			if ((linedef.Action > 0) && General.Map.Config.LinedefActions.ContainsKey(linedef.Action))
			{
				LinedefActionInfo action = General.Map.Config.LinedefActions[linedef.Action];
				if (((action.Args[0].Type == (int)type) && (linedef.Args[0] != 0) && (tags.Contains(linedef.Args[0]))) ||
					((action.Args[1].Type == (int)type) && (linedef.Args[1] != 0) && (tags.Contains(linedef.Args[1]))) ||
					((action.Args[2].Type == (int)type) && (linedef.Args[2] != 0) && (tags.Contains(linedef.Args[2]))) ||
					((action.Args[3].Type == (int)type) && (linedef.Args[3] != 0) && (tags.Contains(linedef.Args[3]))) ||
					((action.Args[4].Type == (int)type) && (linedef.Args[4] != 0) && (tags.Contains(linedef.Args[4]))))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if there's an association between the element and a Thing
		/// </summary>
		/// <param name="thing">Thing to check the association against</param>
		/// <returns>true if the Thing and the element are associated, false if not</returns>
		private bool IsAssociatedToThing(Thing thing)
		{
			// Get the thing type info
			ThingTypeInfo ti = General.Map.Data.GetThingInfoEx(thing.Type);

			// Known action on this thing?
			if ((thing.Action > 0) && General.Map.Config.LinedefActions.ContainsKey(thing.Action))
			{
				//Do not draw the association if this is a child link.
				//  This prevents a reverse link to a thing via an argument, when it should be a direct tag-to-tag link instead.
				if (ti != null && directlinktype < 0 && directlinktype != -thing.Type)
					return false;

				LinedefActionInfo action = General.Map.Config.LinedefActions[thing.Action];
				if (((action.Args[0].Type == (int)type) && (tags.Contains(thing.Args[0]))) ||
					 ((action.Args[1].Type == (int)type) && (tags.Contains(thing.Args[1]))) ||
					 ((action.Args[2].Type == (int)type) && (tags.Contains(thing.Args[2]))) ||
					 ((action.Args[3].Type == (int)type) && (tags.Contains(thing.Args[3]))) ||
					 ((action.Args[4].Type == (int)type) && (tags.Contains(thing.Args[4]))))
				{
					return true;
				}

				//If there is a link setup on this thing, and it matches the association, then draw a direct link to any matching tag
				if (ti != null && directlinktype == thing.Type && tags.Contains(thing.Tag))
				{
					return true;
				}
			}
			//mxd. Thing action on this thing?
			else if (thing.Action == 0)
			{
				// Gets the association, unless it is a child link.
				// This prevents a reverse link to a thing via an argument, when it should be a direct tag-to-tag link instead.
				if (ti != null && directlinktype >= 0 && Math.Abs(directlinktype) != thing.Type)
				{
					if (((ti.Args[0].Type == (int)type) && (tags.Contains(thing.Args[0]))) ||
						 ((ti.Args[1].Type == (int)type) && (tags.Contains(thing.Args[1]))) ||
						 ((ti.Args[2].Type == (int)type) && (tags.Contains(thing.Args[2]))) ||
						 ((ti.Args[3].Type == (int)type) && (tags.Contains(thing.Args[3]))) ||
						 ((ti.Args[4].Type == (int)type) && (tags.Contains(thing.Args[4]))))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a string that contains the description of the action and its arguments, based on the given Linedef or Thing
		/// </summary>
		/// <param name="se">An instance of Thing or Linedef</param>
		/// <returns>String that contains the description of the action and its arguments for a given Linedef or Thing</returns>
		private string GetActionDescription(SelectableElement se)
		{
			int action = 0;
			int[] actionargs = new int[5];

			if (se is Thing)
			{
				action = ((Thing)se).Action;
				actionargs = ((Thing)se).Args;
			}
			else if(se is Linedef)
			{
				action = ((Linedef)se).Action;
				actionargs = ((Linedef)se).Args;
			}

			if (action > 0)
			{
				LinedefActionInfo lai = General.Map.Config.GetLinedefActionInfo(action);
				List<string> argdescription = new List<string>();

				string description = lai.Index + ": " + lai.Title;

				// Label style: only action, or if the element can't have any parameters
				if (BuilderPlug.Me.EventLineLabelStyle == 0 || General.Map.Config.LineTagIndicatesSectors)
					return description;

				for (int i=0; i < 5; i++)
				{
					if(lai.Args[i].Used)
					{
						string argstring = "";

						if(BuilderPlug.Me.EventLineLabelStyle == 2) // Label style: full arguments
							argstring = lai.Args[i].Title + ": ";

						EnumItem ei = lai.Args[i].Enum.GetByEnumIndex(actionargs[i].ToString());

						if (ei != null && BuilderPlug.Me.EventLineLabelStyle == 2) // Label style: full arguments
							argstring += ei.ToString();
						else // Argument has no EnumItem or label style: short arguments
							argstring += actionargs[i].ToString();

						argdescription.Add(argstring);
					}
				}

				description += " (" + string.Join(", ", argdescription) + ")";

				return description;
			}

			return null;
		}

		/// <summary>
		/// Renders associated things and sectors in the indication color.
		/// Also renders event lines, if that option is enabled
		/// </summary>
		public void Render()
		{
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

			if (General.Settings.GZShowEventLines)
			{
				List<Line3D> lines = new List<Line3D>(eventlines.Count);
				List<TextLabel> labels = new List<TextLabel>(eventlines.Count);

				foreach (EventLine el in eventlines)
				{
					lines.Add(el.Line);

					if (el.Label != null && !string.IsNullOrEmpty(el.Label.Text))
						labels.Add(el.Label);
				}

				renderer.RenderArrows(lines);
				renderer.RenderText(labels.ToArray());
			}
		}

		/// <summary>
		/// Plots associated linedefs and sectors
		/// </summary>
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
