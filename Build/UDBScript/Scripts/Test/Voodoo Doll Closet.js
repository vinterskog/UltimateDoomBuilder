var UDB = importNamespace('CodeImp.DoomBuilder');
var General = UDB.General;
var Map = UDB.General.Map.Map;
var Grid = UDB.General.Map.Grid;
var Vector2D = UDB.Geometry.Vector2D;
var DrawnVertex = UDB.Geometry.DrawnVertex;

var basepos = Grid.SnappedToGrid(General.Editing.Mode.MouseMapPos);

var params = QueryParameters([
	[ 'Closet size', 'size', 256 ]
]);

log('param size: ' + params.size)

// Vertices for voodoo doll closet
var v1 = basepos;
var v2 = new Vector2D(basepos.x + 64, basepos.y);
var v3 = new Vector2D(basepos.x + 64, basepos.y + params.size);
var v4 = new Vector2D(basepos.x, basepos.y + params.size);

// Get two new tags, one for the scrolling, one for the blocking sector
var tags = Map.GetMultipleNewTags(2);

// Draw the closet
DrawLines([ v1, v2, v3, v4, v1 ]);

// Get the new sectors and assign a tag
var sector = Map.GetMarkedSectors(true)[0]
sector.Tag = tags[0];
sector.FloorHeight = 0;
sector.CeilHeight = 128;

// Draw the carrying line
DrawLines([ v1, v4 ]);
var line = Map.GetMarkedLinedefs(true)[0];
line.Action = 253;
line.Tag = tags[0];

// Vertices for the sector blocking the player
v1 = new Vector2D(basepos.x + 16, basepos.y + 64);
v2 = new Vector2D(basepos.x + 48, basepos.y + 64);
v3 = new Vector2D(basepos.x + 48, basepos.y + 72);
v4 = new Vector2D(basepos.x + 16, basepos.y + 72);

// Draw the sector blocking the player
DrawLines([ v1, v2, v3, v4, v1 ]);

// Get the new sectors and assign a tag
sector = Map.GetMarkedSectors(true)[0]
sector.Tag = tags[1];
sector.FloorHeight = 25;
sector.CeilHeight = 128;

// Create the voodoo doll
var t = Map.CreateThing();
t.Type = 1;
t.Move(basepos.x + 32, basepos.y + 32, 0);
t.UpdateConfiguration();