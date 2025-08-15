extends TextEdit

var lifted = false
var offset = Vector2.ZERO

var is_entered:bool = false

# Called when the node enters the scene tree for the first time.
func _ready():
	mouse_entered.connect(_on_mouse_enter)
	mouse_exited.connect(_on_mouse_exited)
	#focus_entered.connect(_focus_entered2)
	#Input.dset_custom_mouse_cursor(beam, Input.CURSOR_IBEAM)
	


func _mouse_exited():
	print("_mouse_exited")

func _mouse_entered2():
	print("_mouse_entered2")
# Called every frame. 'delta' is the elapsed time since the previous frame.

func _focus_entered2():
	print("_focus_entered2")

func _process(_delta):
	if lifted:
		#print("lifted")
		global_position = get_global_mouse_position() - offset

func _gui_input(event):
	#print("_gui_input")
	if event is InputEventMouseButton and event.pressed:
		lifted = true
		offset = get_global_mouse_position() - global_position
		#print("_gui_input_lifted")
	if event is InputEventMouseButton and not event.pressed:
		lifted = false
	#	print("here")
	#if lifted and event is InputEventMouseMotion:
	#	position += event.relative
	#	print("_gui_input")

func _unhandled_input(_event):
	#print("_unhandled_input")
	#if event is InputEventMouseButton and not event.pressed:
	#	lifted = false
	#if lifted and event is InputEventMouseMotion:
	#	position += event.relative
	pass

func _input_event(_viewport, _event, _shape_idx):
	#print("_input_event")
	#if event is InputEventMouseButton and event.pressed:
	#	lifted = true
	pass


func _on_mouse_enter():
	#print("TextEdit mouse Enterd")
	is_entered = true
	
	
func _on_mouse_exited():
	#print("TextEdit mouse Exited")
	is_entered = false
	
