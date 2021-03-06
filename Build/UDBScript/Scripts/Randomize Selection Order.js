var Map = UDB.General.Map.Map;

var elements;

switch(UDB.General.Editing.Mode.Attributes.DisplayName) {
	case 'Sectors Mode':
		Map.MarkSelectedSectors(true, true);
		Map.ClearSelectedSectors();
		elements = Map.GetMarkedSectors(true);
		break;
	case 'Things Mode':
		Map.MarkSelectedThings(true, true);
		Map.ClearSelectedThings();
		elements = Map.GetMarkedThings(true);
		break;
	case 'Linedefs Mode':
		Map.MarkSelectedLinedefs(true, true);
		Map.ClearSelectedLinedefs();
		elements = Map.GetMarkedLinedefs(true);
		break;
	case 'Vertices Mode':
		Map.MarkSelectedVertices(true, true);
		Map.ClearSelectedVertices();
		elements = Map.GetMarkedVertices(true);
		break;		
	default:
		throw 'Unsupported mode!';
}

var rnd = new System.Random();

while(elements.Count > 0) {
	ar e = elements[rnd.Next(0, elements.Count)];
	e.Selected = true;
	elements.Remove(e);
}

//while(elements.length > 0) {
//	var e = elements[rnd.Next(0, elements.length)];
//	e.Selected = true;
//	elements.splice(elements.indexOf(e), 1);
//}
