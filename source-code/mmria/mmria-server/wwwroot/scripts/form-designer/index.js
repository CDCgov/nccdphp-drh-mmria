function get_new_ui_specification(p_name)
{
    return {
        "name": p_name,
        "_id": $mmria.get_new_guid(),
        "data_type": "ui-specification",
        "date_created": new Date().toISOString(),
        "created_by": g_uid,
        "date_last_updated": new Date().toISOString(),
        "last_updated_by": g_uid,
        "dimension": { 
            "width": 8.5,
            "height": null
        },
        "form_design": {}       
    };
}