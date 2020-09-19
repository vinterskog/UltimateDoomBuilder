var Vector2D = UDB.Geometry.Vector2D;
var mousemappos = UDB.General.Editing.Mode.MouseMapPos;

UDB.General.Map.Map.Vertices.forEach(function(v) {
	var curdist = Vector2D.Distance(mousemappos, v.Position);
	
	if(curdist < ScriptOptions.distance) {
		var normal = (new Vector2D(v.Position.x - mousemappos.x, v.Position.y - mousemappos.y)).GetNormal();
		v.Move(new Vector2D(mousemappos.x + normal.x * ScriptOptions.distance, mousemappos.y + normal.y * ScriptOptions.distance));
	}
});