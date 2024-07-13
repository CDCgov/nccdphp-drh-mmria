@tool
extends Node2D
class_name GroupField

var is_debug_mode: bool = false

var label: Label 
var is_label_edit_mode: bool = false

signal size_changed(
	object_id,
	old_width_value, 
	new_width_value,
	old_height_value, 
	new_height_value
)


signal edit_mode_changed(
	object_id,
	edit_mode_value
)

@export var width:float = 40
@export var height:float = 20
@export var top:float = 20
@export var left:float = 20

var start:Vector2 = Vector2.ZERO
var end:Vector2 = Vector2.ZERO

var old_width:float = 40
var old_height:float = 20

@export var color:Color = Color.RED
@export var selected_color:Color = Color.GHOST_WHITE

@export var dragging_color:Color = Color.GRAY

@export var is_drag_mode:bool = false

@export var is_selected_mode:bool = false

var is_move_by:bool = false

var draw_color:Color

var Area:Area2D
var CollisionShap:RectangleShape2D


var TopLeftHandleBar:HandleBar
var BottomRightHandleBar:HandleBar



var drag_offset:Vector2 = Vector2.ZERO
var origin_drag_position:Vector2 = Vector2.ZERO
var target_drag_position:Vector2 = Vector2.ZERO

var point_list:PackedVector2Array = []
var t:float = 0.0

func _enter_tree():

	#print("group field enter_tree")
	pass

func _ready():
	#print("group field ready")
	TopLeftHandleBar = $TopLeftHandleBar
	BottomRightHandleBar = $BottomRightHandleBar
	label = $Label

	Area = get_node("Area2D")
	CollisionShap = $Area2D/CollisionShape2D.shape
	
	Area.connect("area_entered", _area_on_enter)
	Area.connect("area_exited", _area_on_exited)

	TopLeftHandleBar.connect("mouse_engaged", top_handle_bar_start)
	TopLeftHandleBar.connect("mouse_move", top_handle_bar_changed)
	TopLeftHandleBar.connect("mouse_disengaged", top_handle_bar_end)
	
	BottomRightHandleBar.connect("mouse_engaged", bottom_handle_bar_start)
	BottomRightHandleBar.connect("mouse_move", bottom_handle_bar_changed)
	BottomRightHandleBar.connect("mouse_disengaged", bottom_handle_bar_end)

	label.connect("mouse_entered", label_mouse_entered)
	label.connect("mouse_exited", label_mouse_exited)
	label.connect("gui_input", label_gui_input)

	_calc_positions()
	
	TopLeftHandleBar.update_position(start)
	BottomRightHandleBar.update_position(end)
	label.position = start
	
	CollisionShap.size = Vector2(width, height)
	
	draw_color = color
	
	queue_redraw()
	
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	
	
	if is_move_by:
		t += delta * 0.4
		if t >= 1.0:
			is_move_by = false
			t = 0.0
			return
			
		position.x = lerp (origin_drag_position.x, target_drag_position.x, t)
		position.y = lerp (origin_drag_position.y, target_drag_position.y, t)
		
		
		


func _calc_positions():
	var label_size = label.size
	
	if width < label_size.x:
		width = label_size.x
		
	if height < label_size.y:
		height = label_size.y
	
	var x_diff = width / 2.0
	var y_diff = height / 2.0
	start = Vector2(- x_diff, -y_diff)
	end = Vector2(x_diff, y_diff)

func _calc_resize(
	top_left:Vector2, 
	bottom_right:Vector2
	):

	var _width = abs(top_left.x - bottom_right.x)
	var _height = abs(top_left.y - bottom_right.y)
	
	var label_size = label.size
	
	if _width < label_size.x:
		return
		
	if _height < label_size.y:
		return

	
	
	width = _width
	height = _height
	
	start = top_left
	end = bottom_right
	
	CollisionShap.size = end - start
	$Area2D.position = (start + end) / 2 
	
		
	print("area 2d center: %s", $Area2D.position)
	TopLeftHandleBar.update_position(start)
	BottomRightHandleBar.update_position(end)
	label.position = start
	queue_redraw()
	

func _draw():
	var point_array:PackedVector2Array = []
	point_array.append(start)
	point_array.append(Vector2(end.x, start.y))
	point_array.append(end)
	point_array.append(Vector2(start.x, end.y))
	
	#raw_line(point_array[0], point_array[3], Color.BLUE)
	
	draw_line(point_array[0], point_array[1], draw_color)
	draw_line(point_array[1], point_array[2], draw_color)
	draw_line(point_array[2], point_array[3], draw_color)
	draw_line(point_array[3], point_array[0], draw_color)
	
	if is_debug_mode:
		var zero_to_global = to_global(Vector2.ZERO)
		
		draw_circle(to_local(position), 5, Color.RED)
		print("group_field local(position): %s width: %s height: %s" % [to_local(position), width, height])
		
		draw_circle(position, 5, Color.GREEN)
		print("group_field position: %s width: %s height: %s" % [position, width, height])
		
		draw_circle(Vector2.ZERO, 5, Color.CORNFLOWER_BLUE)
		
		print("Vector.ZERO to_global %s %s %s" % [zero_to_global, to_local(position) - zero_to_global, position - zero_to_global])
		
	#draw_circle(Vector2.ZERO, 3, Color.ALICE_BLUE)

func _area_on_enter(_area: Area2D):
	#print("Lasso Area Enterd")
	pass
	
func _area_on_exited(_area: Area2D):
	#print("Lasso Area Exited")
	pass
	
	
func top_handle_bar_start(_p_position:Vector2):
	old_width = width
	old_height = height
	#print("top_handle_bar_start G:%s P:%s", get_global_mouse_position(), p_position)
	pass
	
func top_handle_bar_changed(p_position:Vector2):
	#print("top_handle_bar_changed G:%s P:%s", get_global_mouse_position(), p_position)
	_calc_resize(to_local(p_position), BottomRightHandleBar.position)
	pass	
	
func top_handle_bar_end(_p_position:Vector2):
	#print("top_handle_bar_end G:%s P:%s", get_global_mouse_position(), p_position)
	size_changed.emit(
		self.get_instance_id(),
		old_width, 
		width,
		old_height, 
		height
		)
	pass
	
func bottom_handle_bar_start(_p_position:Vector2):
	old_width = width
	old_height = height
	#print("bottom_handle_bar_start G:%s P:%s", get_global_mouse_position(), p_position)
	pass

func bottom_handle_bar_changed(p_position:Vector2):
	#print("bottom_handle_bar_changed G:%s P:%s", get_global_mouse_position(), p_position)
	_calc_resize(TopLeftHandleBar.position, to_local(p_position))
	
	pass
	
func bottom_handle_bar_end(_p_position:Vector2):
	#print("bottom_handle_bar_end G:%s P:%s", get_global_mouse_position(), p_position)
	size_changed.emit(
		self.get_instance_id(),
		old_width, 
		width,
		old_height, 
		height
		)

	pass


func set_to_selected():
	draw_color = selected_color
	is_selected_mode = true
	queue_redraw()
	
func unset_to_selected():	
	draw_color = color
	is_selected_mode = false
	queue_redraw()

func set_to_drag(target_position:Vector2):
	#drag_offset = to_local(target_position) - position
	var red_position = to_local(position)
	drag_offset = target_position - position + red_position
	origin_drag_position = position 
	draw_color = selected_color
	is_selected_mode = true
	queue_redraw()

func move_by(difference_position:Vector2):
	#drag_offset = to_local(target_position) - position
	var red_position = to_local(position)
	drag_offset = difference_position - position + red_position
	origin_drag_position = position 
	target_drag_position = origin_drag_position - difference_position
	draw_color = selected_color
	is_move_by = true
	queue_redraw()

func move_to(new_position:Vector2):
	position = new_position
	queue_redraw()

func set_size(p_width: float, p_height: float):
	width = p_width
	height = p_height
	
	_calc_positions()
	TopLeftHandleBar.update_position(start)
	BottomRightHandleBar.update_position(end)
	label.position = start
	CollisionShap.size = end - start
	$Area2D.position = (start + end) / 2 
	queue_redraw()


func unset_to_drag():
	drag_offset = Vector2.ZERO
	draw_color = color
	is_selected_mode = false
	queue_redraw()
	
func set_drag_motion():
	is_drag_mode = true
	#drag_offset = Vector2.ZERO

	#origin_drag_position = get_global_mouse_position()
	#drag_offset = origin_drag_position - to_global(position)
	
	point_list.append(drag_offset)
	draw_color = dragging_color
	queue_redraw()
	
func addpoint_list(value:Vector2):
	point_list.append(value)
	

func  label_mouse_entered():
		#print("******* label mouse entered *******")	
		label.modulate = Color.YELLOW

func  label_mouse_exited():
		#print("******* label mouse exited *******")	
		label.modulate = Color.WHITE
		
func  label_gui_input(event: InputEvent):
		#label.modulate = Color.WHITE
		
	if event is InputEventKey:
		var iek = event as InputEventKey
		print("group_field.gui_event_input(iek) = %s" % iek.as_text_key_label())        
	
	if not event is InputEventMouseButton:
		return

	if event.button_index != MOUSE_BUTTON_LEFT:
		return
		
	if event.pressed:	
		print("******* group field label gui input  left click pressed *******")	
	else:
		print("******* group field label gui input  left click released *******")	
		is_label_edit_mode = not is_label_edit_mode
		edit_mode_changed.emit(self.get_instance_id(), is_label_edit_mode)
		
	#print("is_label_edit_mode: %s" % is_label_edit_mode)
		
func set_input_key_event(iek:InputEventKey):
	var is_shift = false
	var value:String
	
	#iek.as_text_key_label().split("+")
	#"Shift"
	#"A" => "a"
	#"Shift+J"
	#"Semicolon"
	var array = iek.as_text_key_label().split("+")
	if array.size() < 1 or array.size() > 2: return
	if array[0] == "Ctrl":
		return
		
	if array.size() == 1:
		value = array[0]
		if array[0] == "Shift":
			return

	else:
		if array[0] != "Shift":
			return
		else:
			is_shift = true			
			value = array[1]
	
	if value == "Backspace":
		if label.text.length() > 0:
			label.text.erase(label.text.length() - 1, 1)
			
		return
		
	if value == "Escape" or (
			value == "Enter" and 
			is_shift
		):
		is_label_edit_mode = false
		edit_mode_changed.emit(self.get_instance_id(), is_label_edit_mode)
		return
	
	if value == "Tab":
		label.text = label.text + "\t"
	elif value == "Space":
		label.text = label.text + " "
	elif value == "Enter":
		label.text = label.text + "\n"
	else:
		label.text = label.text + value.to_lower()
	
	if width < label.size.x or height < label.size.y:
		
		var size_difference =Vector2(10,0)
		BottomRightHandleBar.position = BottomRightHandleBar.position + size_difference
		_calc_resize(TopLeftHandleBar.position, BottomRightHandleBar.position)
		queue_redraw()
		
		#_calc_resize(top_left:Vector2, bottom_right:Vector2)
		#_calc_resize(TopLeftHandleBar.position, to_local(p_position))
		#_calc_resize(to_local(p_position), BottomRightHandleBar.position)
		#_calc_resize(top_left, bottom_right)
	
		#_calc_positions()
		
		#TopLeftHandleBar.update_position(start)
		#BottomRightHandleBar.update_position(end)
		#label.position = start
		
		#CollisionShap.size = Vector2(width, height)
		#queue_redraw()
		
