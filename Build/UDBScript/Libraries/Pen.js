function Pen(pos) {
	this.angle = 0;
	this.snaptogrid = false;
	this.ListOfDrawnVertex = System.Collections.Generic.List(UDB.Geometry.DrawnVertex);
	this.vertices = new this.ListOfDrawnVertex();
	
	if(typeof pos !== 'undefined')
		this.curpos = pos;
	else
		this.curpos = new UDB.Geometry.Vector2D(0, 0);
}

Pen.prototype.DrawVertex = function() {
	var v = new UDB.Geometry.DrawnVertex();
	v.pos = this.curpos;
	v.stitch = true;
	v.stitchline = true;
	this.vertices.Add(v);
}

Pen.prototype.FinishDrawing = function() {
	this.vertices.Add(this.vertices[0]);
	
	var result = UDB.Geometry.Tools.DrawLines(this.vertices);
	
	this.vertices = new this.ListOfDrawnVertex();
	
	return result;
}

Pen.prototype.MoveForward = function(distance) {
	this.curpos = new UDB.Geometry.Vector2D(
		this.curpos.x + Math.cos(this.angle) * distance,
		this.curpos.y + Math.sin(this.angle) * distance
	);
}

Pen.prototype.MoveTo = function(pos) {
	this.curpos = pos;
}

Pen.prototype.TurnRight = function(radians) {
	if(typeof radians !== 'undefined')
		this.angle -= radians;
	else
		this.angle -= Math.PI / 2;
	
	while(this.angle < 0)
		this.angle += Math.PI * 2;
}

Pen.prototype.TurnLeft = function(radians) {
	if(typeof radians !== 'undefined')
		this.angle += radians;
	else
		this.angle += Math.PI / 2;
	
	while(this.angle > Math.PI * 2)
		this.angle -= Math.PI * 2;
}

Pen.prototype.TurnRightDegrees = function(degrees) {
	this.angle += degrees * Math.PI / 180.0;
	
	while(this.angle < 0)
		this.angle += Math.PI * 2;
}

Pen.prototype.TurnLeftDegrees = function(degrees) {
	this.angle -= degrees * Math.PI / 180.0;
	
	while(this.angle > Math.PI * 2)
		this.angle -= Math.PI * 2;
}

Pen.prototype.SetAngle = function(radians) {
	this.angle = radians;
}

Pen.prototype.SetAngleDegrees = function(degrees) {
	this.angle = degrees * Math.PI / 180.0;
}