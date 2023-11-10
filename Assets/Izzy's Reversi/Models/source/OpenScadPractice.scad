w = 150; h = 100; d = 10; wh_offset = 7;

color("tomato") difference(){
    base(w, h, r=15, depth=d);
    translate([0, 0, d/2]){
        color("lime") base(w - wh_offset, h - wh_offset, r=15, depth=5);
    }
}

module base(x_width, y_height, r, depth) {
    color("tomato") union(){
        color("lime") cube([x_width, y_height-(2*r), depth], center = true);
        color("aqua") cube([x_width-(2*r), y_height, depth], center = true);
        translate([(x_width/2 - r), (y_height/2 - r), 0]){
            color("red") cylinder(h=depth, r1=r, r2=r, center = true);
        }
        translate([(x_width/2 - r), -(y_height/2 - r), 0]){
            color("red") cylinder(h=depth, r1=r, r2=r, center = true);
        }
        translate([-(x_width/2 - r), (y_height/2 - r), 0]){
            color("red") cylinder(h=depth, r1=r, r2=r, center = true);
        }
        translate([-(x_width/2 - r), -(y_height/2 - r), 0]){
            color("red") cylinder(h=depth, r1=r, r2=r, center = true);
        }
    }
}