w = 150; h = 100; d = 20; r = 10; wh_offset = 7;

/*color("tomato") difference(){
    base(w, h, r=r, depth=d);
    translate([0, 0, d/2]){
        color("lime") base(w - wh_offset, h - wh_offset, r=r, depth=5);
    }
}*/
nSquares = 5;
squareWidth = 20;
pieceOffset = 1;
depthCulled = 5;
depthBoard = 15;
startHeight = depthBoard/2;
marginBoard = 10;
radiusBoard = marginBoard * 0.8;

color("tomato") difference(){
    widthBoard = nSquares * squareWidth + (marginBoard * 2);
    color("tomato") base(x_width=widthBoard, y_height=widthBoard, r=radiusBoard, depth=depthBoard);
    
    squares(nSquares=nSquares, squareWidth=squareWidth, pieceOffset=pieceOffset, depth=depthCulled, startHeight=startHeight);
}

module base(x_width, y_height, r, depth) {
    union(){
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

module squares(nSquares, squareWidth, pieceOffset, depth, startHeight){
    slide = (pieceOffset / 2) - (squareWidth * (nSquares / 2));
    translate([slide, slide, startHeight - depth +0.1])
    //translate([0, 0, startHeight - depth +2])
    for ( row = [0 : nSquares - 1] )
    {
        for ( col = [0 : nSquares - 1] )
        {
            translate([col*squareWidth, row*squareWidth, 0]){
                cube([squareWidth - pieceOffset, squareWidth - pieceOffset, depth]);
            }
        }
    }
}

//color("tomato")