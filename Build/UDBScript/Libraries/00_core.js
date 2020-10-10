Array.prototype.ToList = function(cls)
{
	var list = new (System.Collections.Generic.List(cls))();
	
	this.forEach(e => {
		list.Add(e);
	});
	
	return list;
}