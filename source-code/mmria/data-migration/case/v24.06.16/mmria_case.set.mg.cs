
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.v240616;

public sealed partial class mmria_case
{


    public bool SetMG_String(string path, int form_index, int grid_index, string value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "birth_certificate_infant_fetal_section/causes_of_death/type":
                    birth_certificate_infant_fetal_section[form_index].causes_of_death[grid_index].type = value;
                    result = true;
            break;
            case "birth_certificate_infant_fetal_section/causes_of_death/class":
                    birth_certificate_infant_fetal_section[form_index].causes_of_death[grid_index].@class = value;
                    result = true;
            break;
            case "birth_certificate_infant_fetal_section/causes_of_death/complication_subclass":
                    birth_certificate_infant_fetal_section[form_index].causes_of_death[grid_index].complication_subclass = value;
                    result = true;
            break;
            case "birth_certificate_infant_fetal_section/causes_of_death/other_specify":
                    birth_certificate_infant_fetal_section[form_index].causes_of_death[grid_index].other_specify = value;
                    result = true;
            break;
            case "birth_certificate_infant_fetal_section/causes_of_death/icd_code":
                    birth_certificate_infant_fetal_section[form_index].causes_of_death[grid_index].icd_code = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/internal_transfers/from_unit":
                    er_visit_and_hospital_medical_records[form_index].internal_transfers[grid_index].from_unit = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/internal_transfers/to_unit":
                    er_visit_and_hospital_medical_records[form_index].internal_transfers[grid_index].to_unit = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/internal_transfers/comments":
                    er_visit_and_hospital_medical_records[form_index].internal_transfers[grid_index].comments = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/exam_evaluation":
                    er_visit_and_hospital_medical_records[form_index].physical_exam_and_evaluations[grid_index].exam_evaluation = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/findings":
                    er_visit_and_hospital_medical_records[form_index].physical_exam_and_evaluations[grid_index].findings = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/performed_by":
                    er_visit_and_hospital_medical_records[form_index].physical_exam_and_evaluations[grid_index].performed_by = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/exam_assessments":
                    er_visit_and_hospital_medical_records[form_index].psychological_exam_and_assesments[grid_index].exam_assessments = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/findings":
                    er_visit_and_hospital_medical_records[form_index].psychological_exam_and_assesments[grid_index].findings = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/performed_by":
                    er_visit_and_hospital_medical_records[form_index].psychological_exam_and_assesments[grid_index].performed_by = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/labratory_tests/specimen":
                    er_visit_and_hospital_medical_records[form_index].labratory_tests[grid_index].specimen = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/labratory_tests/test_name":
                    er_visit_and_hospital_medical_records[form_index].labratory_tests[grid_index].test_name = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/labratory_tests/result":
                    er_visit_and_hospital_medical_records[form_index].labratory_tests[grid_index].result = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/labratory_tests/comments":
                    er_visit_and_hospital_medical_records[form_index].labratory_tests[grid_index].comments = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/pathology/specimen":
                    er_visit_and_hospital_medical_records[form_index].pathology[grid_index].specimen = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/pathology/exam_type":
                    er_visit_and_hospital_medical_records[form_index].pathology[grid_index].exam_type = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/pathology/findings":
                    er_visit_and_hospital_medical_records[form_index].pathology[grid_index].findings = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/comments":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].comments = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/birth_attendant/specify_other":
                    er_visit_and_hospital_medical_records[form_index].birth_attendant[grid_index].specify_other = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/birth_attendant/npi":
                    er_visit_and_hospital_medical_records[form_index].birth_attendant[grid_index].npi = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/anesthesia/method":
                    er_visit_and_hospital_medical_records[form_index].anesthesia[grid_index].method = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/anesthesia/complications":
                    er_visit_and_hospital_medical_records[form_index].anesthesia[grid_index].complications = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/list_of_medications/medication":
                    er_visit_and_hospital_medical_records[form_index].list_of_medications[grid_index].medication = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/list_of_medications/dose_frequency_duration":
                    er_visit_and_hospital_medical_records[form_index].list_of_medications[grid_index].dose_frequency_duration = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/list_of_medications/adverse_reaction":
                    er_visit_and_hospital_medical_records[form_index].list_of_medications[grid_index].adverse_reaction = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/list_of_medications/comments":
                    er_visit_and_hospital_medical_records[form_index].list_of_medications[grid_index].comments = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/surgical_procedures/hospital_unit":
                    er_visit_and_hospital_medical_records[form_index].surgical_procedures[grid_index].hospital_unit = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/surgical_procedures/procedure":
                    er_visit_and_hospital_medical_records[form_index].surgical_procedures[grid_index].procedure = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/surgical_procedures/performed_by":
                    er_visit_and_hospital_medical_records[form_index].surgical_procedures[grid_index].performed_by = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/surgical_procedures/outcome":
                    er_visit_and_hospital_medical_records[form_index].surgical_procedures[grid_index].outcome = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/blood_product_grid/product":
                    er_visit_and_hospital_medical_records[form_index].blood_product_grid[grid_index].product = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/blood_product_grid/number_of_units":
                    er_visit_and_hospital_medical_records[form_index].blood_product_grid[grid_index].number_of_units = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/blood_product_grid/reaction_complications":
                    er_visit_and_hospital_medical_records[form_index].blood_product_grid[grid_index].reaction_complications = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/procedure":
                    er_visit_and_hospital_medical_records[form_index].diagnostic_imaging_grid[grid_index].procedure = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/target":
                    er_visit_and_hospital_medical_records[form_index].diagnostic_imaging_grid[grid_index].target = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/finding":
                    er_visit_and_hospital_medical_records[form_index].diagnostic_imaging_grid[grid_index].finding = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/referrals_and_consultations/specialist_type":
                    er_visit_and_hospital_medical_records[form_index].referrals_and_consultations[grid_index].specialist_type = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/referrals_and_consultations/reason":
                    er_visit_and_hospital_medical_records[form_index].referrals_and_consultations[grid_index].reason = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/referrals_and_consultations/recommendations":
                    er_visit_and_hospital_medical_records[form_index].referrals_and_consultations[grid_index].recommendations = value;
                    result = true;
            break;
            case "other_medical_office_visits/relevant_medical_history/finding":
                    other_medical_office_visits[form_index].relevant_medical_history[grid_index].finding = value;
                    result = true;
            break;
            case "other_medical_office_visits/relevant_medical_history/comments":
                    other_medical_office_visits[form_index].relevant_medical_history[grid_index].comments = value;
                    result = true;
            break;
            case "other_medical_office_visits/relevant_family_history/finding":
                    other_medical_office_visits[form_index].relevant_family_history[grid_index].finding = value;
                    result = true;
            break;
            case "other_medical_office_visits/relevant_family_history/comments":
                    other_medical_office_visits[form_index].relevant_family_history[grid_index].comments = value;
                    result = true;
            break;
            case "other_medical_office_visits/relevant_social_history/finding":
                    other_medical_office_visits[form_index].relevant_social_history[grid_index].finding = value;
                    result = true;
            break;
            case "other_medical_office_visits/relevant_social_history/comments":
                    other_medical_office_visits[form_index].relevant_social_history[grid_index].comments = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/comments":
                    other_medical_office_visits[form_index].vital_signs[grid_index].comments = value;
                    result = true;
            break;
            case "other_medical_office_visits/laboratory_tests/specimen":
                    other_medical_office_visits[form_index].laboratory_tests[grid_index].specimen = value;
                    result = true;
            break;
            case "other_medical_office_visits/laboratory_tests/test_name":
                    other_medical_office_visits[form_index].laboratory_tests[grid_index].test_name = value;
                    result = true;
            break;
            case "other_medical_office_visits/laboratory_tests/result":
                    other_medical_office_visits[form_index].laboratory_tests[grid_index].result = value;
                    result = true;
            break;
            case "other_medical_office_visits/laboratory_tests/comments":
                    other_medical_office_visits[form_index].laboratory_tests[grid_index].comments = value;
                    result = true;
            break;
            case "other_medical_office_visits/diagnostic_imaging_and_other_technology/procedure":
                    other_medical_office_visits[form_index].diagnostic_imaging_and_other_technology[grid_index].procedure = value;
                    result = true;
            break;
            case "other_medical_office_visits/diagnostic_imaging_and_other_technology/target_procedure":
                    other_medical_office_visits[form_index].diagnostic_imaging_and_other_technology[grid_index].target_procedure = value;
                    result = true;
            break;
            case "other_medical_office_visits/diagnostic_imaging_and_other_technology/finding":
                    other_medical_office_visits[form_index].diagnostic_imaging_and_other_technology[grid_index].finding = value;
                    result = true;
            break;
            case "other_medical_office_visits/physical_exam/finding":
                    other_medical_office_visits[form_index].physical_exam[grid_index].finding = value;
                    result = true;
            break;
            case "other_medical_office_visits/physical_exam/comment":
                    other_medical_office_visits[form_index].physical_exam[grid_index].comment = value;
                    result = true;
            break;
            case "other_medical_office_visits/referrals_and_consultations/speciality":
                    other_medical_office_visits[form_index].referrals_and_consultations[grid_index].speciality = value;
                    result = true;
            break;
            case "other_medical_office_visits/referrals_and_consultations/reason":
                    other_medical_office_visits[form_index].referrals_and_consultations[grid_index].reason = value;
                    result = true;
            break;
            case "other_medical_office_visits/referrals_and_consultations/recommendations":
                    other_medical_office_visits[form_index].referrals_and_consultations[grid_index].recommendations = value;
                    result = true;
            break;
            case "other_medical_office_visits/medications/medication_name":
                    other_medical_office_visits[form_index].medications[grid_index].medication_name = value;
                    result = true;
            break;
            case "other_medical_office_visits/medications/dose_frequeny_duration":
                    other_medical_office_visits[form_index].medications[grid_index].dose_frequeny_duration = value;
                    result = true;
            break;
            case "other_medical_office_visits/medications/adverse_reaction":
                    other_medical_office_visits[form_index].medications[grid_index].adverse_reaction = value;
                    result = true;
            break;
            case "other_medical_office_visits/medications/comments":
                    other_medical_office_visits[form_index].medications[grid_index].comments = value;
                    result = true;
            break;
            case "other_medical_office_visits/new_grid/abnormal_findings":
                    other_medical_office_visits[form_index].new_grid[grid_index].abnormal_findings = value;
                    result = true;
            break;
            case "other_medical_office_visits/new_grid/recommendations_and_action_plans":
                    other_medical_office_visits[form_index].new_grid[grid_index].recommendations_and_action_plans = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/comments":
                    medical_transport[form_index].transport_vital_signs[grid_index].comments = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }


        
        return result;
    }

    public bool SetMG_Double(string path, int form_index, int grid_index, double? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/body_system":
                    er_visit_and_hospital_medical_records[form_index].physical_exam_and_evaluations[grid_index].body_system = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/labratory_tests/diagnostic_level":
                    er_visit_and_hospital_medical_records[form_index].labratory_tests[grid_index].diagnostic_level = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/temperature":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].temperature = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/pulse":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].pulse = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/respiration":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].respiration = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/bp_systolic":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].bp_systolic = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/bp_diastolic":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].bp_diastolic = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/oxygen_saturation":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].oxygen_saturation = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/birth_attendant/title":
                    er_visit_and_hospital_medical_records[form_index].birth_attendant[grid_index].title = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/temperature":
                    other_medical_office_visits[form_index].vital_signs[grid_index].temperature = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/pulse":
                    other_medical_office_visits[form_index].vital_signs[grid_index].pulse = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/respiration":
                    other_medical_office_visits[form_index].vital_signs[grid_index].respiration = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/bp_systolic":
                    other_medical_office_visits[form_index].vital_signs[grid_index].bp_systolic = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/bp_diastolic":
                    other_medical_office_visits[form_index].vital_signs[grid_index].bp_diastolic = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/oxygen_saturation":
                    other_medical_office_visits[form_index].vital_signs[grid_index].oxygen_saturation = value;
                    result = true;
            break;
            case "other_medical_office_visits/laboratory_tests/diagnostic_level":
                    other_medical_office_visits[form_index].laboratory_tests[grid_index].diagnostic_level = value;
                    result = true;
            break;
            case "other_medical_office_visits/physical_exam/body_system":
                    other_medical_office_visits[form_index].physical_exam[grid_index].body_system = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/gestational_weeks":
                    medical_transport[form_index].transport_vital_signs[grid_index].gestational_weeks = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/gestational_days":
                    medical_transport[form_index].transport_vital_signs[grid_index].gestational_days = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/systolic_bp":
                    medical_transport[form_index].transport_vital_signs[grid_index].systolic_bp = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/diastolic_bp":
                    medical_transport[form_index].transport_vital_signs[grid_index].diastolic_bp = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/heart_rate":
                    medical_transport[form_index].transport_vital_signs[grid_index].heart_rate = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/oxygen_saturation":
                    medical_transport[form_index].transport_vital_signs[grid_index].oxygen_saturation = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/blood_sugar":
                    medical_transport[form_index].transport_vital_signs[grid_index].blood_sugar = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetMG_Boolean(string path, int form_index, int grid_index, bool? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetMG_List_Of_Double(string path, int form_index, int grid_index, List<double> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    
    public bool SetMG_List_Of_String(string path, int form_index, int grid_index, List<string> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetMG_Datetime(string path, int form_index, int grid_index, DateTime? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "er_visit_and_hospital_medical_records/internal_transfers/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].internal_transfers[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].physical_exam_and_evaluations[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].psychological_exam_and_assesments[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/labratory_tests/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].labratory_tests[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/pathology/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].pathology[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/vital_signs/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].vital_signs[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/anesthesia/date_time":
                    er_visit_and_hospital_medical_records[form_index].anesthesia[grid_index].date_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/list_of_medications/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].list_of_medications[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/surgical_procedures/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].surgical_procedures[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/blood_product_grid/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].blood_product_grid[grid_index].date_and_time = value;
                    result = true;
            break;
            case "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/date_and_time":
                    er_visit_and_hospital_medical_records[form_index].diagnostic_imaging_grid[grid_index].date_and_time = value;
                    result = true;
            break;
            case "other_medical_office_visits/vital_signs/date_and_time":
                    other_medical_office_visits[form_index].vital_signs[grid_index].date_and_time = value;
                    result = true;
            break;
            case "other_medical_office_visits/laboratory_tests/date_and_time":
                    other_medical_office_visits[form_index].laboratory_tests[grid_index].date_and_time = value;
                    result = true;
            break;
            case "other_medical_office_visits/diagnostic_imaging_and_other_technology/date_and_time":
                    other_medical_office_visits[form_index].diagnostic_imaging_and_other_technology[grid_index].date_and_time = value;
                    result = true;
            break;
            case "other_medical_office_visits/medications/date_and_time":
                    other_medical_office_visits[form_index].medications[grid_index].date_and_time = value;
                    result = true;
            break;
            case "medical_transport/transport_vital_signs/date_and_time":
                    medical_transport[form_index].transport_vital_signs[grid_index].date_and_time = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


    public bool SetMG_Date_Only(string path, int form_index, int grid_index, DateOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "er_visit_and_hospital_medical_records/referrals_and_consultations/date":
                    er_visit_and_hospital_medical_records[form_index].referrals_and_consultations[grid_index].date = value;
                    result = true;
            break;
            case "other_medical_office_visits/referrals_and_consultations/date":
                    other_medical_office_visits[form_index].referrals_and_consultations[grid_index].date = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


    public bool SetMG_Time_Only(string path, int form_index, int grid_index, TimeOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


}