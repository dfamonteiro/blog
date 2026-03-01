import drawsvg as draw
from typing import Tuple, Dict, Union
from math import sin, cos, degrees, radians, pi, acos

CIRCLE_POS = (0, 80)
CIRCLE_RADIUS = 80

D_THICKNESS = 20

APEX_POS = (0, -150)

M_BOTTOM = D_THICKNESS

M_TOP = D_THICKNESS * 5

CANVAS_SIZE = 400

GUIDELINES = False
COLOR_SHAPES = True
COLOR = "white"
FAVICON = True
HOME_PAGE = False

def get_tangent_points(circle_pos : Tuple[float, float], circle_radius : float, apex_pos : Tuple[float, float]) -> Dict[str, Union[float, Tuple[float, float]]]:
    "WARNING: this assumes that the circle and the apex are vertically aligned"
    hypotenuse = abs(circle_pos[1] - apex_pos[1])
    adjacent = circle_radius

    angle = pi/2 - acos(adjacent/hypotenuse)

    x = circle_pos[0] + cos(angle) * circle_radius
    y = circle_pos[1] - sin(angle) * circle_radius

    return {
        "angle" : degrees(angle),
        "T1" : (x, y),
        "T2" : (-x, y),
    }

def line_intersection(line1, line2):
    xdiff = (line1[0][0] - line1[1][0], line2[0][0] - line2[1][0])
    ydiff = (line1[0][1] - line1[1][1], line2[0][1] - line2[1][1])

    def det(a, b):
        return a[0] * b[1] - a[1] * b[0]

    div = det(xdiff, ydiff)
    if div == 0:
       raise Exception('lines do not intersect')

    d = (det(*line1), det(*line2))
    x = det(d, xdiff) / div
    y = det(d, ydiff) / div
    return x, y

def red_circle(d : draw.Drawing, pos : Tuple[float, float]):
    d.append(draw.Circle(*pos, 3, fill="red"))

d = draw.Drawing(210 if HOME_PAGE else CANVAS_SIZE, 325 if HOME_PAGE else CANVAS_SIZE, origin='center')
group = draw.Group(transform='scale(1.9) translate(0, -56)' if FAVICON else "")

angle_data = get_tangent_points(CIRCLE_POS, CIRCLE_RADIUS, APEX_POS)
t1, t2, angle = angle_data["T1"], angle_data["T2"], angle_data["angle"]

if GUIDELINES:
    group.append(draw.Circle(
        *CIRCLE_POS, CIRCLE_RADIUS, fill = "transparent", stroke_width = 1, stroke = COLOR
    ))
    group.append(draw.Circle(
        *CIRCLE_POS, CIRCLE_RADIUS - D_THICKNESS, fill = "transparent", stroke_width = 1, stroke = COLOR
    ))
    group.append(draw.Rectangle(
        *t2, t1[0] * 2, D_THICKNESS, fill = "transparent", stroke_width = 1, stroke = COLOR
    ))

if COLOR_SHAPES:
    #Draw the arc of the D
    group.append(draw.Arc(
        *CIRCLE_POS, CIRCLE_RADIUS - D_THICKNESS/2, 180 + angle, 360 - angle,
        fill = "transparent", stroke_width = D_THICKNESS, stroke = COLOR
        )
    )
    #Draw the straight part of the D
    group.append(draw.Rectangle(
        *t2, t1[0] * 2, D_THICKNESS, fill=COLOR
    ))

horizontal_spacing = D_THICKNESS / cos(radians(angle))
if GUIDELINES:
    #Apex lines
    group.append(draw.Line(*t1, *APEX_POS, stroke=COLOR, stroke_width=1))
    group.append(draw.Line(*t2, *APEX_POS, stroke=COLOR, stroke_width=1))
    #Apex spaced lines
    group.append(draw.Line(t1[0] - horizontal_spacing, t1[1], APEX_POS[0] - horizontal_spacing, APEX_POS[1], stroke=COLOR, stroke_width=1))
    group.append(draw.Line(t2[0] + horizontal_spacing, t2[1], APEX_POS[0] + horizontal_spacing, APEX_POS[1], stroke=COLOR, stroke_width=1))
    #Horizontal Guidelines
    group.append(draw.Line(-CANVAS_SIZE/4, t2[1] - M_BOTTOM, CANVAS_SIZE/4, t2[1] - M_BOTTOM, stroke=COLOR, stroke_width=1))
    group.append(draw.Line(-CANVAS_SIZE/4, t2[1] - M_TOP, CANVAS_SIZE/4, t2[1] - M_TOP, stroke=COLOR, stroke_width=1))
# Points of the right leg of the M
top_outer_right = line_intersection([t1, APEX_POS], [[-CANVAS_SIZE/4, t2[1] - M_TOP], [CANVAS_SIZE/4, t2[1] - M_TOP]])
top_inner_right = line_intersection([(t1[0] - horizontal_spacing, t1[1]), (APEX_POS[0] - horizontal_spacing, APEX_POS[1])], [[-CANVAS_SIZE/4, t2[1] - M_TOP], [CANVAS_SIZE/4, t2[1] - M_TOP]])
bot_outer_right = line_intersection([t1, APEX_POS], [[-CANVAS_SIZE/4, t2[1] - M_BOTTOM], [CANVAS_SIZE/4, t2[1] - M_BOTTOM]])
bot_inner_right = line_intersection([(t1[0] - horizontal_spacing, t1[1]), (APEX_POS[0] - horizontal_spacing, APEX_POS[1])], [[-CANVAS_SIZE/4, t2[1] - M_BOTTOM], [CANVAS_SIZE/4, t2[1] - M_BOTTOM]])
# Points of the left leg of the M
top_outer_left = line_intersection([t2, APEX_POS], [[-CANVAS_SIZE/4, t2[1] - M_TOP], [CANVAS_SIZE/4, t2[1] - M_TOP]])
top_inner_left = line_intersection([(t2[0] + horizontal_spacing, t1[1]), (APEX_POS[0] + horizontal_spacing, APEX_POS[1])], [[-CANVAS_SIZE/4, t2[1] - M_TOP], [CANVAS_SIZE/4, t2[1] - M_TOP]])
bot_outer_left = line_intersection([t2, APEX_POS], [[-CANVAS_SIZE/4, t2[1] - M_BOTTOM], [CANVAS_SIZE/4, t2[1] - M_BOTTOM]])
bot_inner_left = line_intersection([(t2[0] + horizontal_spacing, t1[1]), (APEX_POS[0] + horizontal_spacing, APEX_POS[1])], [[-CANVAS_SIZE/4, t2[1] - M_BOTTOM], [CANVAS_SIZE/4, t2[1] - M_BOTTOM]])
if GUIDELINES:
    # Guidelines for the mid points
    group.append(draw.Line(*top_outer_right, top_outer_right[0] + cos(radians(angle + 90)) * M_TOP, top_outer_right[1] + sin(radians(angle + 90)) * M_TOP, stroke=COLOR, stroke_width=1))
    group.append(draw.Line(*top_inner_right, top_inner_right[0] + cos(radians(angle + 90)) * M_TOP, top_inner_right[1] + sin(radians(angle + 90)) * M_TOP, stroke=COLOR, stroke_width=1))
    group.append(draw.Line(*top_outer_left, top_outer_left[0] + cos(radians(-angle + 90)) * M_TOP, top_outer_left[1] + sin(radians(-angle + 90)) * M_TOP, stroke=COLOR, stroke_width=1))
    group.append(draw.Line(*top_inner_left, top_inner_left[0] + cos(radians(-angle + 90)) * M_TOP, top_inner_left[1] + sin(radians(-angle + 90)) * M_TOP, stroke=COLOR, stroke_width=1))
# Points of the "middle legs" of the M
mid_right = line_intersection(
    [top_outer_right, (top_outer_right[0] + cos(radians(angle + 90)) * M_TOP, top_outer_right[1] + sin(radians(angle + 90)) * M_TOP)],
    [top_inner_left, (top_inner_left[0] + cos(radians(-angle + 90)) * M_TOP, top_inner_left[1] + sin(radians(-angle + 90)) * M_TOP)]
)
mid_left = line_intersection(
    [top_inner_right, (top_inner_right[0] + cos(radians(angle + 90)) * M_TOP, top_inner_right[1] + sin(radians(angle + 90)) * M_TOP)],
    [top_outer_left, (top_outer_left[0] + cos(radians(-angle + 90)) * M_TOP, top_outer_left[1] + sin(radians(-angle + 90)) * M_TOP)]
)
mid_top = line_intersection(
    [top_inner_right, (top_inner_right[0] + cos(radians(angle + 90)) * M_TOP, top_inner_right[1] + sin(radians(angle + 90)) * M_TOP)],
    [top_inner_left, (top_inner_left[0] + cos(radians(-angle + 90)) * M_TOP, top_inner_left[1] + sin(radians(-angle + 90)) * M_TOP)],
)
if COLOR_SHAPES:
    # Let's draw the M
    group.append(draw.Lines(*top_outer_right, *top_inner_right, *bot_inner_right, *bot_outer_right, close=True, fill=COLOR))
    group.append(draw.Lines(*top_outer_left, *top_inner_left, *bot_inner_left, *bot_outer_left, close=True, fill=COLOR))
    group.append(draw.Lines(*top_outer_left, *top_inner_left, *mid_top, *top_inner_right, *top_outer_right, *mid_right, *mid_left, close=True, fill=COLOR))

d.append(group)

# red_circle(d, mid_right)
d.set_pixel_scale(1)
d.save_svg('logo.svg')
d.save_png('logo.png')
