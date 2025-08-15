var path_to_onblur_map = [];
var path_to_onclick_map = [];
var path_to_onfocus_map = [];
var path_to_onchange_map = [];
var path_to_source_validation = [];
var path_to_derived_validation = [];
var path_to_validation_description = [];

function x6_of(x) {
    if ((!this.record_id || this.record_id == '') && this.state_of_death_record && this.state_of_death_record != '' && this.date_of_death.year && this.date_of_death.year > 2000) {
        this.record_id = this.state_of_death_record + '-' + this.date_of_death.year + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6);
        x.value = this.record_id;
    }
}
path_to_onfocus_map['/children/4/children/0']='x6_of';
function x7_ob(p_control) {
    if (p_control.value == '') {
        this.first_name = 'bubba';
    }
}
path_to_onblur_map['/children/4/children/1']='x7_ob';
path_to_source_validation['/children/4/children/2']='x8_sv';

 function x8_dv (value)
{
 
 return x8_sv(value);

}
path_to_derived_validation['/children/4/children/2']='x8_dv';
