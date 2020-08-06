var UDB = importNamespace('CodeImp.DoomBuilder');
var Map = UDB.General.Map.Map;
var Vector2D = UDB.Geometry.Vector2D;
var Line2D = UDB.Geometry.Line2D;

var damaging = 0.0;
var notdamaging = 0.0;

Map.Sectors.forEach(function(s) {
	var i=0;
	
	while(i < s.Triangles.Vertices.Count)
	{
		var line = Line2D(s.Triangles.Vertices[i], s.Triangles.Vertices[i+1]);
		var distance = line.GetDistanceToLine(s.Triangles.Vertices[i+2], false);

		var area = 0.5 * line.GetLength() * distance / 1000;

		i += 3;

		switch(s.Effect) {
			case 4:
			case 5:
			case 7:
			case 11:
			case 16:
				damaging += area;
				break;
			default:
				notdamaging += area;
				break;
		}
	}
});

var damagingpercent = (damaging / (damaging + notdamaging)) * 100;

log('damaging: ' + damaging);
log('not damaging: ' + notdamaging);
log('damaging %: ' + damagingpercent);

ShowMessage('Damaging %: ' + damagingpercent);