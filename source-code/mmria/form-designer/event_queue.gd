extends Object
class_name event_queue

#var stack : Array[move_event] = []
var stack : Array = []


class target_move_object:
	var from_position = Vector2.ZERO
	var to_position = Vector2.ZERO
	var object_id : int
		
class move_event:
	var move_difference = Vector2.ZERO
	var target_object_list : Array[target_move_object] = []

class resize_event:
	var width_diff : float
	var height_diff : float
	var target_object_list : Array[target_resize_object] = []

class target_resize_object:
	var from_width : float
	var to_width : float
	var from_height : float
	var to_height : float
	var object_id : int

func push_move_event(value: move_event):
	stack.push_back(value)
	print("event.push queue size %s" % self.size())
	
	
func push_resize_event(value: resize_event):
	stack.push_back(value)
	print("event.push queue size %s" % self.size())

func pop()-> Object:
	var result = stack.pop_back()
	
	print("event.pop queue size %s" % self.size())
	
	return result

func size()-> int:
	return stack.size()
