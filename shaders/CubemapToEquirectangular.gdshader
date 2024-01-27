shader_type sky;

uniform sampler2D front;
uniform sampler2D left;
uniform sampler2D right;
uniform sampler2D back;
uniform sampler2D top;
uniform sampler2D bottom;

void sky() {
	COLOR.x = 1.0;
	float theta = SKY_COORDS.y*PI;
	float phi = SKY_COORDS.x*PI*2.0;
	vec3 unit = vec3(0,0,0);
	
	unit.x = sin(phi) * sin(theta) * 1.0;
	unit.y = cos(theta) * 1.0;
	unit.z = cos(phi) * sin(theta) * 1.0;
	unit = normalize(unit);
	vec2 st = vec2(0.0);
	if (-unit.y>=abs(unit.x) && -unit.y>=abs(unit.z)) {
		st = ((vec2(1.0, -1.0)*unit.xz)/abs(unit.y)+1.0)/2.0;
		COLOR = texture(bottom, st).rgb;
	} if (unit.y>=abs(unit.x) && unit.y>=abs(unit.z)) {
		st = ((unit.xz)/abs(unit.y)+1.0)/2.0;
		COLOR = texture(top, st).rgb;
	} if (unit.x>=abs(unit.y) && unit.x>=abs(unit.z)) {
		st = (-unit.zy/abs(unit.x)+1.0)/2.0;
		COLOR = texture(right, st).rgb;
	} if (-unit.x>=abs(unit.y) && -unit.x>=abs(unit.z)) {
		st = ((vec2(1.0, -1.0)*unit.zy)/abs(unit.x)+1.0)/2.0;
		COLOR = texture(left, st).rgb;
	} if (unit.z>=abs(unit.x) && unit.z>=abs(unit.y)) {
		st = ((vec2(1.0, -1.0)*unit.xy)/abs(unit.z)+1.0)/2.0;
		COLOR = texture(back, st).rgb;
	} if (-unit.z>=abs(unit.x) && -unit.z>=abs(unit.y)) {
		st = ((vec2(-1.0)*unit.xy)/abs(unit.z)+1.0)/2.0;
		COLOR = texture(front, st).rgb;
	}
}