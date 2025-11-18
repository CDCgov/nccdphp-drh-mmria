var g_de_identified_list = null;
var g_selected_list = null;
var g_selected_index = -1;
var g_selected_clone_source = null;
var g_release_version = null;
var g_metadata = null;
var g_form_map = new Map();

$(function() {
	'use strict';
	load_report_set();

	$(document).keydown(function(evt) {
		if (evt.keyCode == 83 && evt.ctrlKey) {
			evt.preventDefault();
		}
	});

	window.onhashchange = function(e) {
		if (e.isTrusted) {
			var new_url = e.newURL || window.location.href;
			g_ui.url_state = url_monitor.get_url_state(new_url);
		}
	};
});

async function load_report_set() {
	const release_version = await $.ajax({
		url: `${location.protocol}//${location.host}/api/version/release-version`,
	});

	g_release_version = release_version;

	const metadata_response = await $.ajax({
		url: `${location.protocol}//${location.host}/api/version/${g_release_version}/metadata`,
	});

	g_metadata = metadata_response;

	create_metadata_map(g_form_map, g_metadata, "");

	const g_de_identified_list_response = await $.ajax({
		url: location.protocol + '//' + location.host + '/api/export_list_manager',
	});

	g_de_identified_list = g_de_identified_list_response;
	g_selected_list = Object.keys(g_de_identified_list.name_path_list)[0];

	if (g_de_identified_list.sort_order == null || g_de_identified_list.sort_order.length == 0) {
		g_de_identified_list.sort_order = Object.keys(g_de_identified_list.name_path_list);
	}

	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function on_clone_source_change(p_value) {
	g_selected_clone_source = p_value;
}

function on_export_list_type_change(p_value) {
	g_selected_list = p_value;
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function render_de_identified_list() {
	var result = [];
	result.push(`<div class="row mb-2">
		<div class="col-md-6" style="font-size:24px;">Custom Lists</div>
		<div class='col-md-6'>
			<button class='secondary-button float-right' aria-label='Add New List' onclick='add_name_path_list_click()'>
				<span class='x16 cdc-icon-plus pl-2'>
					<span style='padding-left: 4px;'>Add New List</span>
				</span>
			</button>
		</div>
	</div>
	<table class='table'>
		<thead>
			<tr class='header-level-2'>
				<th></th>
				<th>List Name</th>
				<th>Action</th>
			</tr>
		</thead>
		<tbody>`);

	for (const sort_index in g_de_identified_list.sort_order) {
		const list_name = g_de_identified_list.sort_order[sort_index];
		result.push(`<tr style="cursor:grab;" draggable='true' ondragstart='handle_drag_start(event, "${list_name}")' ondragover='handle_drag_over(event)' ondrop='handle_drop(event, "${list_name}")' ondragend='handle_drag_end(event)'>
			<td><img id='drag_${list_name}' src='./img/icon_drag_drop.svg'/></td>
			<td><input size="115" type='text' class='form-control' value='${list_name}' onchange='update_list_name("${list_name}", this.value)'></input></td>
			<td><button class='delete-button' onclick='remove_name_path_list_click("${list_name}")'>Delete List</button></td>
		</tr>`);
	}

	result.push(`</tbody>
	</table>
	<div class="row mb-2">
		<div class="col-md-6">
			<button class='primary-button mt-3' onclick='server_save()'>Save Lists</button>
		</div>
		<div class='col-md-6'>
			
		</div>
	</div>
	<hr/>
	<div style='font-size:24px;' class='mb-2'>Export Field List</div>
	<div class="row mb-2 mt-2">
		<div class="col-md-4 horizontal-control">
			<label style='width:175px;' for='export-list-type'>Selected list:</label>
			<select id='export-list-type' onchange='on_export_list_type_change(this.value)' class='form-select form-control'>`);

	for (const sort_index in g_de_identified_list.sort_order) {
		const list_name = g_de_identified_list.sort_order[sort_index];
		if (list_name == g_selected_list) {
			result.push(`<option value='${list_name}' selected>${list_name}</option>`);
		} else {
			result.push(`<option value='${list_name}'>${list_name}</option>`);
		}
	}

	result.push(`</select>
		</div>
		<div class='col-md-6'></div>
	</div>
	<div class="row mb-2 mt-2">
		<div class="col-md-4 horizontal-control">
			<label style='width:175px;' for='clone-source'>Clone source:</label>
			<select class='form-select form-control' id='clone-source' onchange='on_clone_source_change(this.value)'>
				<option value='9999' disabled='' style='font-weight:bold;'>lists</option>`);

	for (let [key, value] of Object.entries(g_de_identified_list.name_path_list)) {
		if (key == g_selected_clone_source) {
			result.push(`<option value='${key}' selected>${key}</option>`);
		} else {
			result.push(`<option value='${key}'>${key}</option>`);
		}
	}

	result.push(`<option value='9999' disabled='' style='font-weight:bold;'>form</option>`);

	g_form_map.forEach((value, key) => {
		if (key == g_selected_clone_source) {
			result.push(`<option value='${key}' selected>${key}</option>`);
		} else {
			result.push(`<option value='${key}'>${key}</option>`);
		}
	});

	result.push(`</select>
		</div>
		<div class='col-md-6 mb-2'>
			<button class='secondary-button' onclick='clone_list_click()'>Clone Fields</button>
		</div>
		<div class='col-md-6'>
	test
		</div>        
	</div>`);

	let selected_list = g_de_identified_list.name_path_list[g_selected_list];

	result.push(`<table class='table'>
		<thead>
			<tr class='header-level-2'>
				<th></th>
				<th>Field Path/Name</th>
				<th>Actions</th>
			</tr>
		</thead>
		<tbody>`);

	for (let i in selected_list) {
		let item = selected_list[i];
		let row_number = parseInt(i) + 1;
		let bgColor = (i % 2) ? " bgcolor='#CCCCCC'" : "";
		result.push(`<tr style='cursor:grab;'${bgColor} draggable='true' ondragstart='handle_field_drag_start(event, ${i})' ondragover='handle_drag_over(event)' ondrop='handle_field_drop(event, ${i})' ondragend='handle_drag_end(event)'>
			<td><img src='./img/icon_drag_drop.svg' /></td>
			<td><input id='row_${row_number}' class='form-control' size='98' type='text' title='${item}' aria-labelledby='path_label' value='${item}' onblur='update_item(${i}, this.value)'/></td>
			<td>
				<button class='secondary-button' onclick='cut_selected(${row_number})'>Copy Field</button>
				<button class='secondary-button' onclick='paste_selected(${row_number})'>Paste Field</button>
				<button class='secondary-button' onclick='delete_item(${i})'>Delete Field</button>
			</td>
		</tr>`);
	}

	result.push(`</tbody>
	</table>
	<div class="row mb-2">
		<div class="col-md-6">
			<button class='primary-button mt-3' onclick='server_save()'>Save Lists</button>
		</div>
		<div class='col-md-6'></div>
	</div>`);

	return result;
}

function update_item(p_index, p_value) {
	g_de_identified_list.name_path_list[g_selected_list][p_index] = p_value;
}

function update_list_name(p_old_name, p_new_name) {
	if (p_old_name === p_new_name || p_new_name.trim() === '') {
		return;
	}

	// Update the name_path_list with new key
	g_de_identified_list.name_path_list[p_new_name] = g_de_identified_list.name_path_list[p_old_name];
	delete g_de_identified_list.name_path_list[p_old_name];

	// Update sort_order array
	const index = g_de_identified_list.sort_order.indexOf(p_old_name);
	if (index > -1) {
		g_de_identified_list.sort_order[index] = p_new_name;
	}

	// Update selected list if it was the renamed one
	if (g_selected_list === p_old_name) {
		g_selected_list = p_new_name;
	}

	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function delete_item(p_index) {
	g_de_identified_list.name_path_list[g_selected_list].splice(p_index, 1);
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function add_new_item_click() {
	g_de_identified_list.name_path_list[g_selected_list].splice(0, 0, "");
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function server_save() {
	$.ajax({
		url: location.protocol + '//' + location.host + '/api/export_list_manager',
		contentType: 'application/json; charset=utf-8',
		dataType: 'json',
		data: JSON.stringify(g_de_identified_list),
		type: "POST"
	}).done(function(response) {
		var response_obj = eval(response);
		if (response_obj.ok) {
			g_de_identified_list._rev = response_obj.rev;
			document.getElementById('output').innerHTML = render_de_identified_list().join("");
		}
	});
}





function remove_name_path_list_click(list_name) {
	var answer = prompt("Are you sure you want to remove the " + list_name + " list?", "Enter yes to confirm");
	if (answer == "yes") {
		delete g_de_identified_list.name_path_list[list_name];

		const current_index = g_de_identified_list.sort_order.indexOf(list_name);
		g_de_identified_list.sort_order.splice(current_index, 1);

		if (Object.keys(g_de_identified_list.name_path_list).length > 0) {
			g_selected_list = Object.keys(g_de_identified_list.name_path_list)[0];
		}

		document.getElementById('output').innerHTML = render_de_identified_list().join("");
	}
}

function clone_list_click() {
	const clone_target = document.getElementById("clone-source").value;
	var answer = prompt("Are you sure you want to clone [" + clone_target + "] ?", "Enter yes to confirm");
	if (answer == "yes") {
		let list = g_de_identified_list.name_path_list[clone_target];
		if (list == null) {
			list = g_form_map.get(clone_target);
		}

		if (list != null && g_de_identified_list.name_path_list[g_selected_list] != null) {
			const target_list = g_de_identified_list.name_path_list[g_selected_list];

			for (let i = 0; i < list.length; i++) {
				const new_path = list[i];
				if (target_list.indexOf(new_path) < 0) {
					target_list.push(new_path);
				}
			}
		}

		document.getElementById('output').innerHTML = render_de_identified_list().join("");
	}
}

function add_name_path_list_click() {
	let new_name = '';
	g_de_identified_list.name_path_list[new_name] = [];
	g_selected_list = new_name;
	g_de_identified_list.sort_order.unshift(new_name);
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function cut_selected(p_value) {
	g_selected_index = p_value;
}

function paste_selected(p_value) {
	let x = p_value - 1;
	let y = g_selected_index - 1;
	const list = g_de_identified_list.name_path_list[g_selected_list];

	if (g_de_identified_list != null && g_selected_list != null && list != null &&
		g_selected_index > -1 && x < list.length && y < list.length) {
		let y_value = list[y];
		list.splice(y, 1);
		list.splice(x, 0, y_value);
		document.getElementById('output').innerHTML = render_de_identified_list().join("");
	}
}


function create_metadata_map(p_result, p_metadata, p_path, p_current_key) {
	let next_path = p_path + "/" + p_metadata.name;
	if (p_metadata.type == "app") {
		next_path = "/";
	} else if (next_path.startsWith("//")) {
		next_path = next_path.substring(2);
	}

	if (p_metadata.children && p_metadata.children.length > 0) {
		if (p_metadata.type == "form") {
			p_result.set(p_metadata.name, []);
			p_current_key = p_metadata.name;
		}

		for (var i = 0; i < p_metadata.children.length; i++) {
			var child = p_metadata.children[i];
			if (child.type.toLowerCase() != "grid" &&
				child.type.toLowerCase() != "label" &&
				child.type.toLowerCase() != "button" &&
				child.type.toLowerCase() != "chart") {
				create_metadata_map(p_result, child, next_path, p_current_key);
			}
		}
	} else if (p_current_key != null) {
		p_result.get(p_current_key).push(next_path);
	}
}

async function update_sort_order(p_list_name, p_desired_index) {
	let sort_index = p_desired_index - 1;
	if (sort_index < 0) {
		sort_index = 0;
	} else if (sort_index > g_de_identified_list.sort_order.length - 1) {
		sort_index = g_de_identified_list.sort_order.length - 1;
	}

	const current_index = g_de_identified_list.sort_order.indexOf(p_list_name);
	g_de_identified_list.sort_order.splice(current_index, 1);
	g_de_identified_list.sort_order.splice(sort_index, 0, p_list_name);
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

var g_drag_source_list_name = null;
var g_drag_source_field_index = null;

function handle_drag_start(event, list_name) {
	g_drag_source_list_name = list_name;
	g_drag_source_field_index = null;
	event.dataTransfer.effectAllowed = 'move';
	event.dataTransfer.setData('text/html', list_name);
	event.currentTarget.style.opacity = '0.4';
}

function handle_drag_over(event) {
	if (event.preventDefault) {
		event.preventDefault();
	}
	event.dataTransfer.dropEffect = 'move';
	return false;
}

function handle_drop(event, target_list_name) {
	if (event.preventDefault) {
		event.preventDefault();
	}
	if (event.stopPropagation) {
		event.stopPropagation();
	}

	// Only allow drop if we're dragging a list (not a field)
	if (g_drag_source_list_name !== null && g_drag_source_list_name !== target_list_name) {
		const source_index = g_de_identified_list.sort_order.indexOf(g_drag_source_list_name);
		const target_index = g_de_identified_list.sort_order.indexOf(target_list_name);

		g_de_identified_list.sort_order.splice(source_index, 1);
		g_de_identified_list.sort_order.splice(target_index, 0, g_drag_source_list_name);

		setTimeout(() => {
			document.getElementById('output').innerHTML = render_de_identified_list().join("");
		}, 0);
	}

	return false;
}

function handle_drag_end(event) {
	event.currentTarget.style.opacity = '1';
	g_drag_source_list_name = null;
}

function handle_field_drag_start(event, field_index) {
	g_drag_source_field_index = field_index;
	g_drag_source_list_name = null;
	event.dataTransfer.effectAllowed = 'move';
	event.dataTransfer.setData('text/html', field_index);
	event.currentTarget.style.opacity = '0.4';
}

function handle_field_drop(event, target_field_index) {
	if (event.preventDefault) {
		event.preventDefault();
	}
	if (event.stopPropagation) {
		event.stopPropagation();
	}

	// Only allow drop if we're dragging a field (not a list)
	if (g_drag_source_field_index !== null && g_drag_source_field_index !== target_field_index) {
		const list = g_de_identified_list.name_path_list[g_selected_list];
		const source_index = parseInt(g_drag_source_field_index);
		const target_index = parseInt(target_field_index);

		const item = list[source_index];
		list.splice(source_index, 1);
		list.splice(target_index, 0, item);

		setTimeout(() => {
			document.getElementById('output').innerHTML = render_de_identified_list().join("");
		}, 0);
	}

	g_drag_source_field_index = null;
	return false;
}