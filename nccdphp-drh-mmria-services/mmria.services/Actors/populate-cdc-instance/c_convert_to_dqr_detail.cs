using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.server.utils;

public sealed class c_convert_to_dqr_detail
{


    string source_json;

    common.couchdb.DBConfigurationDetail connection;
    string metadata_release_version_name;
    string data_type = "overdose";

    private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> List_Look_Up;

    private int blank_value = 9999;

    public c_convert_to_dqr_detail (string p_source_json, common.couchdb.DBConfigurationDetail p_connection, string p_metadata_release_version_name, string p_type = "dqr-detail")
    {

        source_json = p_source_json;
        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        this.data_type = p_type;
    }

    public string execute ()
    {
        string result = null;

        var gs = new migrate.C_Get_Set_Value(new ());
        
        string metadata_url = connection.url + $"/metadata/version_specification-{metadata_release_version_name}/metadata";
        var metadata_curl = new mmria.getset.cURL("GET", null, metadata_url, null, connection.user_name, connection.user_value);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());


        List_Look_Up = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var child in metadata.children)
        {
            Get_List_Look_Up(List_Look_Up, metadata.lookup, child, "/" + child.name);
        }



        //migrate.C_Get_Set_Value.get_grid_value_result grid_value_result = null;
        migrate.C_Get_Set_Value.get_value_result value_result = null;
        var dqr_detail = new mmria.server.model.dqr.DQRDetail();

        //dqr_detail.case_folder = "";
        //dqr_detail.record_id = ""; //'OR-2019-4806',
        //dqr_detail.dt_death = ""; //: '05/27/2009',
        //dqr_detail.dt_com_rev = ""; //: '07/19/2021',
        //dqr_detail._id = ""; //: 'd1632b47-4950-a4d1-fa17-e7368eaeefe',

/*

                        rec_id: 'OR-2019-4806',
                        dt_death: '05/27/2009',
                        dt_com_rev: '07/19/2021',
                        ia_id: 'd1632b47-4950-a4d1-fa17-e7368eaeefe',
*/

        dqr_detail.hrcpr_bcp_secti_is_2 = 0;

        dqr_detail.is_preventable_death = 0;

        dqr_detail.n01 = 0;
        dqr_detail.n02 = 0;
        dqr_detail.n03[0] = 0;
        dqr_detail.n03[1] = 0;
        dqr_detail.n03[2] = 0;
        dqr_detail.n03[3] = 0;
        dqr_detail.n03[4] = 0;
        dqr_detail.n03[5] = 0;
        dqr_detail.n03[6] = 0;
        dqr_detail.n03[7] = 0;

        dqr_detail.n04 = 0;
        dqr_detail.n05 = 0;
        dqr_detail.n06 = 0;
        dqr_detail.n07 = 0;
        dqr_detail.n08 = 0;
        dqr_detail.n09 = 0;

        dqr_detail.n10.m = 0;
        dqr_detail.n10.u = 0;

        dqr_detail.n11.m = 0;
        dqr_detail.n11.u = 0;

        dqr_detail.n12.m = 0;
        dqr_detail.n12.u = 0;
        dqr_detail.n13.m = 0;
        dqr_detail.n13.u = 0;
        dqr_detail.n14.m = 0;
        dqr_detail.n14.u = 0;
        dqr_detail.n15.m = 0;
        dqr_detail.n15.u = 0;
        dqr_detail.n16.m = 0;
        dqr_detail.n16.u = 0;
        dqr_detail.n17.m = 0;
        dqr_detail.n17.u = 0;
        dqr_detail.n18.m = 0;
        dqr_detail.n18.u = 0;
        dqr_detail.n19.m = 0;
        dqr_detail.n19.u = 0;
        
        dqr_detail.n20.m = 0;
        dqr_detail.n20.u = 0;
        dqr_detail.n21.m = 0;
        dqr_detail.n21.u = 0;
        dqr_detail.n22.m = 0;
        dqr_detail.n22.u = 0;
        dqr_detail.n23.m = 0;
        dqr_detail.n23.u = 0;
        dqr_detail.n24.m = 0;
        dqr_detail.n24.u = 0;
        dqr_detail.n25.m = 0;
        dqr_detail.n25.u = 0;
        dqr_detail.n26.m = 0;
        dqr_detail.n26.u = 0;
        dqr_detail.n27.m = 0;
        dqr_detail.n27.u = 0;
        dqr_detail.n28.m = 0;
        dqr_detail.n28.u = 0;
        dqr_detail.n29.m = 0;
        dqr_detail.n29.u = 0;
        
        dqr_detail.n30.m = 0;
        dqr_detail.n30.u = 0;
        dqr_detail.n31.m = 0;
        dqr_detail.n31.u = 0;
        dqr_detail.n32.m = 0;
        dqr_detail.n32.u = 0;
        dqr_detail.n33.m = 0;
        dqr_detail.n33.u = 0;
        dqr_detail.n34.m = 0;
        dqr_detail.n34.u = 0;
        dqr_detail.n35.m = 0;
        dqr_detail.n35.u = 0;
        dqr_detail.n36.m = 0;
        dqr_detail.n36.u = 0;
        dqr_detail.n37.m = 0;
        dqr_detail.n37.u = 0;
        dqr_detail.n38.m = 0;
        dqr_detail.n38.u = 0;
        dqr_detail.n39.m = 0;
        dqr_detail.n39.u = 0;
        
        dqr_detail.n40.m = 0;
        dqr_detail.n40.u = 0;
        dqr_detail.n41.m = 0;
        dqr_detail.n41.u = 0;
        dqr_detail.n42.m = 0;
        dqr_detail.n42.u = 0;
        dqr_detail.n43.m = 0;
        dqr_detail.n43.u = 0;

        dqr_detail.n44.t = 0;
        dqr_detail.n44.p = 0;
        dqr_detail.n45.t = 0;
        dqr_detail.n45.p = 0;
        dqr_detail.n46.t = 0;
        dqr_detail.n46.p = 0;
        dqr_detail.n47.t = 0;
        dqr_detail.n47.p = 0;
        dqr_detail.n48.t = 0;
        dqr_detail.n48.p = 0;
        dqr_detail.n49.t = 0;
        dqr_detail.n49.p = 0;

        bool cr_do_revie_is_date = false;
        bool cr_p_relat_is_1 = false;
        bool hrcpr_bcp_secti_is_2 = false;

        System.Dynamic.ExpandoObject source_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (source_json);

        int get_list_value_by_path(string p_path)
        {
            
            int result = -1;
            int test_int = -1;
            var value_result = gs.get_value(source_object, p_path);
            if
            (
                !value_result.is_error &&
                value_result.result != null && 
                int.TryParse(value_result.result.ToString(), out test_int)
            )
            {
                result = test_int;
            }

            return result;

        }
        
        List<int> get_mutilist_value_by_path(string p_path)
        {
            
            List<int> result = new();

            int test_int = -1;
            var value_result = gs.get_value(source_object, p_path);
            if
            (
                !value_result.is_error &&
                value_result.result != null && 
                value_result.result is List<object> 
            )
            {
                var value_result_list = value_result.result as List<object>;
                foreach(var item in value_result_list)
                {
                    if(int.TryParse(item.ToString(), out test_int))
                    result.Add(test_int);
                }
            }

            return result;

        }

        int? get_integer_value_by_path(string p_path)
        {
            int? result = null;

            int test_int = -1;

            var value_result = gs.get_value(source_object, p_path);
            if(value_result.result != null)
            {
                if(int.TryParse(value_result.result.ToString(), out test_int))
                {
                    result = test_int;
                }
            }

            return result;
        }

        float? get_float_value_by_path(string p_path)
        {
            float? result = null;

            float test_int = -1;

            var value_result = gs.get_value(source_object, p_path);
            if(value_result.result != null)
            {
                if(float.TryParse(value_result.result.ToString(), out test_int))
                {
                    result = test_int;
                }
            }

            return result;
        }

        string get_string_value_by_path(string p_path)
        {
            string result = null;

            float test_int = -1;

            var value_result = gs.get_value(source_object, p_path);
            if(value_result.result != null)
            {
                if( !string.IsNullOrWhiteSpace(value_result.result.ToString()))
                {
                    result = value_result.result?.ToString();
                }
            }

            return result;
        }


        value_result = gs.get_value(source_object, "_id");
    
        dqr_detail._id  = ((object)value_result.result).ToString();

        value_result = gs.get_value(source_object, "home_record/jurisdiction_id");
        dqr_detail.case_folder = ((object)value_result.result).ToString();

        value_result = gs.get_value(source_object, "home_record/record_id");
        dqr_detail.record_id = value_result.result != null? ((object)value_result.result).ToString() : ""; //'OR-2019-4806',
    
        dqr_detail._id  = value_result.result != null ? ((object)value_result.result).ToString(): "/";

        value_result = gs.get_value(source_object, "addquarter");
        var obj = (object)value_result.result;


        if(obj != null)
        {
            dqr_detail.add_quarter_name  = ((object)value_result.result).ToString();

            if(! string.IsNullOrWhiteSpace(dqr_detail.add_quarter_name))
            {
                var arr = dqr_detail.add_quarter_name.Split("-");
                dqr_detail.add_quarter_number = double.Parse($"{arr[1]}.{((int.Parse(arr[0].Replace("Q","")) - 1) * .25D).ToString().Replace("0.","")}");
            }
        }



        value_result = gs.get_value(source_object, "cmpquarter");
        obj = (object)value_result.result;
        if(obj != null)
        {
            dqr_detail.cmp_quarter_name  = ((object)value_result.result).ToString();

            if(! string.IsNullOrWhiteSpace(dqr_detail.cmp_quarter_name))
            {
                var arr = dqr_detail.cmp_quarter_name.Split("-");
                dqr_detail.cmp_quarter_number = double.Parse($"{arr[1]}.{((int.Parse(arr[0].Replace("Q","")) - 1) * .25D).ToString().Replace("0.","")}");
            }
        }

        dqr_detail.n01 = 1;

        int test_int = -1;

        value_result = gs.get_value(source_object, "home_record/how_was_this_death_identified");
        if(value_result.is_error)
        {

        }
        else if
        (
            
            value_result.result != null &&
            value_result.result is IList<object>
    
        )
        {
            var list = value_result.result as IList<object>;
            if(list.Count == 0)
            {
                dqr_detail.n02 = 1;
            }
            else
            {
                foreach(var item in list)
                {
                    int.TryParse(item.ToString(), out test_int);
                    if
                    (
                        test_int == 9999 ||
                        test_int == 7777
                    )
                    {
                        dqr_detail.n02 = 1;
                        break;
                    }
                    
                }
            }

        }
        /*
        else
        {
            dqr_detail.n02 = 1;
        }*/




        value_result = gs.get_value(source_object, "home_record/case_status/overall_case_status");
        if
        (
            !value_result.is_error &&
            value_result.result != null                
        )
        {

            int.TryParse(value_result.result.ToString(), out test_int);
            switch(test_int)
            {
                case 1:
                dqr_detail.n03[0] = 1;
                break;
                case 2:
                dqr_detail.n03[1] = 1;
                break;
                case 3:
                dqr_detail.n03[2] = 1;
                break;
                case 4:
                dqr_detail.n03[3] = 1;
                break;
                case 5:
                dqr_detail.n03[4] = 1;
                break;
                case 6:
                dqr_detail.n03[5] = 1;
                break;
                case 0:
                dqr_detail.n03[6] = 1;
                break;
                case 9999:
                dqr_detail.n03[7] = 1;
                break;
            
            }
            
        }
        else
        {
            dqr_detail.n03[7] = 1;
        }



    //DS2. All Quarters. Limit to Selected quarter or before
//Valid Review Date: IsDate([cr_do_revie]) = True



        value_result = gs.get_value(source_object, "committee_review/date_of_review");
        if
        (
            !value_result.is_error &&
            value_result.result != null
    
        )
        {
            DateTime test_time = DateTime.MinValue;
            var data_string = value_result.result.ToString();
            if
            (
                DateTime.TryParse(data_string, out test_time)
            )
            {
                dqr_detail.n04 = 1;
                cr_do_revie_is_date = true;
                dqr_detail.dt_com_rev = $"{test_time.Month}/{test_time.Day}/{test_time.Year}"; //: '07/19/2021',
                
            }
        }

        value_result = gs.get_value(source_object, "committee_review/pregnancy_relatedness");
        if
        (
            !value_result.is_error &&
            value_result.result != null

        )
        {
            if
            (
                cr_do_revie_is_date &&
                int.TryParse(value_result.result.ToString(), out test_int) &&
                test_int == 1
            )
            {
                dqr_detail.n05 = 1;
                dqr_detail.n06 = 1;
                cr_p_relat_is_1 = true;
                
            }
        }



        value_result = gs.get_value(source_object, "home_record/case_progress_report/birth_certificate_parent_section");
        if
        (
            !value_result.is_error &&
            value_result.result != null

        )
        {
            if
            (
                cr_p_relat_is_1 &&
                int.TryParse(value_result.result.ToString(), out test_int) &&
                test_int == 2
            )
            {
                dqr_detail.n07 = 1;
                dqr_detail.n09 = 1;
                hrcpr_bcp_secti_is_2 = true;
                dqr_detail.hrcpr_bcp_secti_is_2 = 1;
            }
        }


        if(cr_do_revie_is_date && cr_p_relat_is_1)
        {
            dqr_detail.n08 = 1;
        }



        if
        (
            cr_do_revie_is_date && 
            cr_p_relat_is_1 &&
            hrcpr_bcp_secti_is_2
        )
        {
            dqr_detail.n09 = 1;
        }


        //n10
        //hr_abs_dth_timing: /home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status
        value_result = gs.get_value(source_object, "home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            !value_result.is_error &&
            value_result.result != null
        )
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                if(test_int == 9999)
                {
                    dqr_detail.n10.m = 1;
                }
                else if(test_int == 88)
                {
                    dqr_detail.n10.u = 1;
                }
            }
            else
            {
                dqr_detail.n10.m = 1;
            }
        }
        else if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            (
                value_result.is_error ||
                value_result.result == null
            )
        )
        {
            dqr_detail.n10.m = 1;
        }


        
        /*
                    //n11
                    hrdod_month = '9999' OR hrdod_day = '9999' OR hrdod_year = '9999'

        hrdod_month: /home_record/date_of_death/month
        hrdod_day:  /home_record/date_of_death/day
        hrdod_year: /home_record/date_of_death/year
        */

        int hrdod_month = -1;
        int hrdod_day = -1;
        int hrdod_year = -1;
        
        value_result = gs.get_value(source_object, "home_record/date_of_death/month");
        if(value_result.result != null)
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                hrdod_month = test_int;
            }
        }

        value_result = gs.get_value(source_object, "home_record/date_of_death/day");
        if(value_result.result != null)
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                hrdod_day = test_int;
            }
        }


        value_result = gs.get_value(source_object, "home_record/date_of_death/year");
        if(value_result.result != null)
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                hrdod_year = test_int;
            }
        }

        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            (
                hrdod_month == 9999 ||
                hrdod_day == 9999 ||
                hrdod_year == 9999
            )
        )
        {
            dqr_detail.n11.m = 1;
        }

        dqr_detail.dt_death = $"{hrdod_month}/{hrdod_day}/{hrdod_year}"; //: '05/27/2009',

        /*
        n12

        hrcpr_bcp_secti = '2' AND (bfdcpfodddod_month = '9999' OR bfdcpfodddod_day = '9999' OR bfdcpfodddod_year  = '9999')
        hrcpr_bcp_secti:  /home_record/case_progress_report/birth_certificate_parent_section
bfdcpfodddod_month: /birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month
bfdcpfodddod_day:  /birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day
bfdcpfodddod_year:   /birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year

*/
int hrcpr_bcp_secti = -1;
int bfdcpfodddod_month = -1;
int bfdcpfodddod_day = -1;
int bfdcpfodddod_year = -1;

        value_result = gs.get_value(source_object, "home_record/case_progress_report/birth_certificate_parent_section");
        if(value_result.result != null)
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                hrcpr_bcp_secti = test_int;
            }
        }

        value_result = gs.get_value(source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month");
        if(value_result.result != null)
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                bfdcpfodddod_month = test_int;
            }
        }


        value_result = gs.get_value(source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day");
        if(value_result.result != null)
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                bfdcpfodddod_day = test_int;
            }
        }


        value_result = gs.get_value(source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year");
        if(value_result.result != null)
        {
            if(int.TryParse(value_result.result.ToString(), out test_int))
            {
                bfdcpfodddod_year = test_int;
            }
        }

        
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            hrcpr_bcp_secti == 2 && 
            (
                bfdcpfodddod_month == 9999 || 
                bfdcpfodddod_day == 9999 ||
                bfdcpfodddod_year  == 9999 ||
                bfdcpfodddod_month == -1 || 
                bfdcpfodddod_day == -1 ||
                bfdcpfodddod_year  == -1
            )
        )
        {
            dqr_detail.n12.m = 1;
        }



        var dcdi_p_statu = get_list_value_by_path("death_certificate/death_information/pregnancy_status");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                dcdi_p_statu == -1 ||
                dcdi_p_statu == 9999 
            )
            {
                dqr_detail.n13.m = 1;
            }


            if
            (
                dcdi_p_statu == 88 ||
                dcdi_p_statu == 8888
            )
            {
                dqr_detail.n13.u = 1;
            }

        }
/*

        if(dqr_detail._id.Equals("FL-2020-0176",StringComparison.OrdinalIgnoreCase))
        {
            System.Console.WriteLine("here");
        }
*/

        // hrcpr_bcp_secti: /home_record/case_progress_report/birth_certificate_parent_section
        var bfdcpr_ro_mothe = get_mutilist_value_by_path("birth_fetal_death_certificate_parent/race/race_of_mother");
        var bfdcpdom_ioh_origi = get_list_value_by_path("birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin");

        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                hrcpr_bcp_secti == 2 &&
                (
                    bfdcpr_ro_mothe.Count == 0 || 
                    bfdcpr_ro_mothe.IndexOf(9999) > -1 ||
                    bfdcpr_ro_mothe.IndexOf(8888) > -1 ||
                    (
                        bfdcpdom_ioh_origi == -1 ||
                        bfdcpdom_ioh_origi == 9999 ||
                        bfdcpdom_ioh_origi == 8888 ||
                        bfdcpdom_ioh_origi == 7777
                    )
                )
            )
            {
                dqr_detail.n14.m = 1;
            }
        }


        var dcr_race = get_mutilist_value_by_path("death_certificate/race/race");
        var dcd_ioh_origi = get_list_value_by_path("death_certificate/demographics/is_of_hispanic_origin");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                dcr_race.Count == 0 || 
                dcr_race.IndexOf(9999) > -1||
                dcr_race.IndexOf(8888) > -1||
                
                dcd_ioh_origi == -1 ||
                dcd_ioh_origi == 9999 ||
                dcd_ioh_origi == 8888 ||
                dcd_ioh_origi == 7777
            )
            {
                dqr_detail.n15.m = 1;
            }
        }



        var dcd_age = get_integer_value_by_path("death_certificate/demographics/age");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if(dcd_age == null)
            {
                dqr_detail.n16.m = 1;
            }
        }


        //hrcpr_bcp_secti:  /home_record/case_progress_report/birth_certificate_parent_section
        var bfdcpdom_e_level = get_list_value_by_path("birth_fetal_death_certificate_parent/demographic_of_mother/education_level");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            hrcpr_bcp_secti_is_2
        )
        {
            if
            (
                bfdcpdom_e_level == -1 ||
                bfdcpdom_e_level == 9999
            )
            {
                dqr_detail.n17.m = 1;
            }

            if
            (
                bfdcpdom_e_level == 7777 ||
                bfdcpdom_e_level == 8888
            )
            {
                dqr_detail.n17.u = 1;
            }
        }


        var dcd_e_level = get_list_value_by_path("death_certificate/demographics/education_level");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                dcd_e_level == -1 ||
                dcd_e_level == 9999                
            )
            {
                dqr_detail.n18.m = 1;
            }

            if
            (
                dcd_e_level == 7777 ||
                dcd_e_level == 8888
            )
            {
                dqr_detail.n18.u = 1;
            }
        }

        var saepsoes_eosoe_stres = get_mutilist_value_by_path("social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                saepsoes_eosoe_stres.Count == 0 ||
                saepsoes_eosoe_stres.IndexOf(9999) > -1               
            )
            {
                dqr_detail.n19.m = 1;
            }

            if
            (
                saepsoes_eosoe_stres.IndexOf(7777) > -1 
            )
            {
                dqr_detail.n19.u = 1;
            }
        }

        var saepsec_cl_arran = get_list_value_by_path("social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                saepsec_cl_arran == -1 ||
                saepsec_cl_arran == 9999
            )
            {
                dqr_detail.n20.m = 1;
            }

            if
            (
                saepsec_cl_arran == 7777 ||
                saepsec_cl_arran == 8888
            )
            {
                dqr_detail.n20.u = 1;
            }
        }

        var dcaod_eddf_resid = get_float_value_by_path("death_certificate/address_of_death/estimated_death_distance_from_residence");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if(dcaod_eddf_resid == null)
            {
                dqr_detail.n21.m = 1;
            }
        }

        //hrcpr_bcp_secti:  /home_record/case_progress_report/birth_certificate_parent_section
        var bfdcplor_edf_resid = get_float_value_by_path("birth_fetal_death_certificate_parent/location_of_residence/estimated_distance_from_residence");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            hrcpr_bcp_secti_is_2
        )
        {
            if(bfdcplor_edf_resid == null)
            {
                dqr_detail.n22.m = 1;
            }
        }


        var dcaod_u_statu = get_string_value_by_path("death_certificate/address_of_death/urban_status");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 
        )
        {
            if(dcaod_u_statu == null)
            {
                dqr_detail.n23.m = 1;
            }

            if(dcaod_u_statu == "Undetermined")
            {
                dqr_detail.n23.u = 1;
            }
        }

        var dcpolr_u_statu = get_string_value_by_path("death_certificate/place_of_last_residence/urban_status");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if(dcpolr_u_statu == null)
            {
                dqr_detail.n24.m = 1;
            }

            if(dcpolr_u_statu == "Undetermined")
            {
                dqr_detail.n24.u = 1;
            }
        }


        var bfdcpfodl_u_statu = get_string_value_by_path("birth_fetal_death_certificate_parent/facility_of_delivery_location/urban_status");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            hrcpr_bcp_secti_is_2
        )
        {
            if(bfdcpfodl_u_statu == null)
            {
                dqr_detail.n25.m = 1;
            }

            if(bfdcpfodl_u_statu == "Undetermined")
            {
                dqr_detail.n25.u = 1;
            }
        }
        var bfdcplor_u_statu = get_string_value_by_path("birth_fetal_death_certificate_parent/location_of_residence/urban_status");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            hrcpr_bcp_secti_is_2
        )
        {
            if(bfdcplor_u_statu == null)
            {
                dqr_detail.n26.m = 1;
            }

            if(bfdcplor_u_statu == "Undetermined")
            {
                dqr_detail.n26.u = 1;
            }
        }

        var cdi_wa_perfo = get_list_value_by_path("death_certificate/death_information/was_autopsy_performed");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cdi_wa_perfo == -1 ||
                cdi_wa_perfo == 9999                
            )
            {
                dqr_detail.n27.m = 1;
            }

            if
            (
                cdi_wa_perfo == 7777 
            )
            {
                dqr_detail.n27.u = 1;
            }
        }


        var ar_autopsy_type = get_list_value_by_path("autopsy_report/type_of_autopsy_or_examination");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                ar_autopsy_type == -1 ||
                ar_autopsy_type == 9999                
            )
            {
                dqr_detail.n28.m = 1;
            }

            if
            (
                ar_autopsy_type == 7777 
            )
            {
                dqr_detail.n28.u = 1;
            }
        }


        var pppcf_pso_payme = get_list_value_by_path("prenatal/primary_prenatal_care_facility/principal_source_of_payment");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                pppcf_pso_payme == -1 ||
                pppcf_pso_payme == 9999                
            )
            {
                dqr_detail.n29.m = 1;
            }

            if
            (
                pppcf_pso_payme == 7777 
            )
            {
                dqr_detail.n29.u = 1;
            }
        }

        var bfdcppc_psopft_deliv = get_list_value_by_path("birth_fetal_death_certificate_parent/prenatal_care/principal_source_of_payment_for_this_delivery");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1 &&
            hrcpr_bcp_secti_is_2
        )
        {
            if
            (
                bfdcppc_psopft_deliv == -1 ||
                bfdcppc_psopft_deliv == 9999                
            )
            {
                dqr_detail.n30.m = 1;
            }

            if
            (
                bfdcppc_psopft_deliv == 7777 
            )
            {
                dqr_detail.n30.u = 1;
            }
        }

        var saephcs_np_care = get_list_value_by_path("social_and_environmental_profile/health_care_system/no_prenatal_care");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                saephcs_np_care == -1 ||
                saephcs_np_care == 9999                
            )
            {
                dqr_detail.n31.m = 1;
            }

            if
            (
                saephcs_np_care == 7777 
            )
            {
                dqr_detail.n31.u = 1;
            }
        }


        var saephca_bthc_acces = get_mutilist_value_by_path("social_and_environmental_profile/health_care_access/barriers_to_health_care_access");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                saephca_bthc_acces.Count == 0 ||
                saephca_bthc_acces.IndexOf(9999) > -1
            )
            {
                dqr_detail.n32.m = 1;
            }

            if
            (
                saephca_bthc_acces.IndexOf(7777) > -1
            )
            {
                dqr_detail.n32.u = 1;
            }
        }


        var saep_ds_use = get_list_value_by_path("social_and_environmental_profile/documented_substance_use");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                saep_ds_use == -1 ||
                saep_ds_use == 9999                
            )
            {
                dqr_detail.n33.m = 1;
            }

            if
            (
                saep_ds_use == 7777 ||
                saep_ds_use == 8888
            )
            {
                dqr_detail.n33.u = 1;
            }
        }


        var mhp_wtdpmh_condi = get_list_value_by_path("mental_health_profile/were_there_documented_preexisting_mental_health_conditions");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                mhp_wtdpmh_condi == -1 ||
                mhp_wtdpmh_condi == 9999                
            )
            {
                dqr_detail.n34.m = 1;
            }

            if
            (
                mhp_wtdpmh_condi == 7777 ||
                mhp_wtdpmh_condi == 8888
            )
            {
                dqr_detail.n34.u = 1;
            }
        }


        var cr_p_mm = get_float_value_by_path("committee_review/pmss_mm");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_p_mm == null ||
                cr_p_mm == -1 ||
                cr_p_mm == 9999                
            )
            {
                dqr_detail.n35.m = 1;
            }

            if
            (
                cr_p_mm == 999F ||
                cr_p_mm == 999.1F
            )
            {
                dqr_detail.n35.u = 1;
            }
        }


        var cr_wtd_preve = get_list_value_by_path("committee_review/was_this_death_preventable");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_wtd_preve == -1 ||
                cr_wtd_preve == 9999
            )
            {
                dqr_detail.n36.m = 1;
            }
        }

        var cr_cta_outco = get_list_value_by_path("committee_review/chance_to_alter_outcome");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_cta_outco == -1 ||
                cr_cta_outco == 9999
            )
            {
                dqr_detail.n37.m = 1;
            }

            if
            (
                cr_cta_outco == 3                
            )
            {
                dqr_detail.n37.u = 1;
            }
        }

        var cr_doctt_death = get_list_value_by_path("committee_review/did_obesity_contribute_to_the_death");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_doctt_death == -1 ||
                cr_doctt_death == 9999
            )
            {
                dqr_detail.n38.m = 1;
            }

            if
            (
                cr_doctt_death == 7777                
            )
            {
                dqr_detail.n38.u = 1;
            }
        }


        var cr_ddctt_death = get_list_value_by_path("committee_review/did_discrimination_contribute_to_the_death");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_ddctt_death == -1 ||
                cr_ddctt_death == 9999
            )
            {
                dqr_detail.n39.m = 1;
            }

            if
            (
                cr_ddctt_death == 7777                
            )
            {
                dqr_detail.n39.u = 1;
            }
        }

        var cr_dmhcctt_death = get_list_value_by_path("committee_review/did_mental_health_conditions_contribute_to_the_death");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_dmhcctt_death == -1 ||
                cr_dmhcctt_death == 9999
            )
            {
                dqr_detail.n40.m = 1;
            }

            if
            (
                cr_dmhcctt_death == 7777                
            )
            {
                dqr_detail.n40.u = 1;
            }
        }


        var cr_dsudctt_death = get_list_value_by_path("committee_review/did_substance_use_disorder_contribute_to_the_death");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_dsudctt_death == -1 ||
                cr_dsudctt_death == 9999
            )
            {
                dqr_detail.n41.m = 1;
            }

            if
            (
                cr_dsudctt_death == 7777                
            )
            {
                dqr_detail.n41.u = 1;
            }
        }

        var cr_wtda_sucid = get_list_value_by_path("committee_review/was_this_death_a_sucide");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (
                cr_wtda_sucid == -1 ||
                cr_wtda_sucid == 9999
            )
            {
                dqr_detail.n42.m = 1;
            }

            if
            (
                cr_wtda_sucid == 7777                
            )
            {
                dqr_detail.n42.u = 1;
            }
        }

        var cr_wtda_homic = get_list_value_by_path("committee_review/was_this_death_a_homicide");
        if
        (
            cr_do_revie_is_date &&
            cr_p_relat_is_1
        )
        {
            if
            (

                cr_wtda_homic == -1 ||
                cr_wtda_homic == 9999                
            )
            {
                dqr_detail.n43.m = 1;
            }

            if
            (
                cr_wtda_homic == 7777                
            )
            {
                dqr_detail.n43.u = 1;
            }
        }

// *************

        if(cr_do_revie_is_date && cr_p_relat_is_1)
        {
            dqr_detail.n44.t = 1;
            dqr_detail.n45.t = 1;

            // n44 (cr_wtd_preve IN ('1','0') OR cr_cta_outco IN ('0','1','2'))
            if
            (
                cr_wtd_preve == 0 ||
                cr_wtd_preve == 1 ||
                cr_cta_outco == 0 ||
                cr_cta_outco == 1 ||
                cr_cta_outco == 2
            )
            {
                dqr_detail.n44.p = 1;
            }

                /*n45 ( 
                    (
                        cr_wtd_preve = '1' AND 
                        cr_cta_outco IN ('0','1','3','9999')
                    ) OR 
                    (
                        cr_wtd_preve='0' AND
                        cr_cta_outco IN ('2', '3','9999')
                    ) 
                    OR 
                    (
                        cr_wtd_preve='9999' AND 
                        cr_cta_outco IN ('3', '9999')
                    )
                    )

                */
            if
            ( 
                (
                    cr_wtd_preve == 1 && 
                    (
                        cr_cta_outco == 0 ||
                        cr_cta_outco == 1 ||
                        cr_cta_outco == 3 ||
                        cr_cta_outco == 9999
                    )
                ) 
                || 
                (
                    cr_wtd_preve==0 &&
                    (
                        cr_cta_outco == 2 ||
                        cr_cta_outco == 3 ||
                        cr_cta_outco == 9999
                    )
                ) 
                || 
                (
                    cr_wtd_preve==9999 && 
                    (
                        cr_cta_outco == 3 ||
                        cr_cta_outco == 9999
                    )
                )
            )
            {
                dqr_detail.n45.p = 1;
            }
        }
        


        /*
        Valid Review Date: IsDate([cr_do_revie]) = True  AND
        (
            A3.cr_wtd_preve='1' OR 
            A3.cr_cta_outco='0' OR 
            A3.cr_cta_outco='1'
        )
            AND 
            A2.cr_p_relat = '1' 
        */
        if 
        (
            cr_do_revie_is_date && 
            (
                cr_wtd_preve == 1 ||
                cr_cta_outco == 0 ||
                cr_cta_outco == 1
            )
            && cr_p_relat_is_1
        )
        {

            dqr_detail.is_preventable_death = 1;

            //n46
            //cr_ddctt_death IN ('1', '2')
            if
            (
                cr_ddctt_death == 1 ||
                cr_ddctt_death == 2
            )
            {
                dqr_detail.n46.t = 1;
            }
            

            //n47
            // cr_dmhcctt_death IN ('1', '2')
            if
            (
                cr_dmhcctt_death == 1 ||
                cr_dmhcctt_death == 2
            )
            {
                dqr_detail.n47.t = 1;
            }

            //n48
            // cr_dsudctt_death IN ('1', '2')
            if
            (
                cr_dsudctt_death == 1 ||
                cr_dsudctt_death == 2
            )
            {
                dqr_detail.n48.t = 1;
            }
            


            // n49
            dqr_detail.n49.t = 1;
            


            var grid_value_result = gs.get_grid_value(source_object, "committee_review/critical_factors_worksheet/class");
            if
            (
                !grid_value_result.is_error &&
                grid_value_result.result != null 
            )
            {

                foreach(var (index, value) in grid_value_result.result)
                {
                    if
                    (
                        value != null &&
                        int.TryParse(value.ToString(), out test_int)
                    )
                    {
                        //n46
                        //cr_ddctt_death IN ('1', '2') AND (CDF_26 = 1 OR CDF_27 = 1 OR CDF_28 = 1)
                        if
                        (
                            (
                                cr_ddctt_death == 1 ||
                                cr_ddctt_death == 2 
                            )
                            &&
                            (
                                test_int == 26 ||
                                test_int == 27 ||
                                test_int == 28
                            )
                        )
                        {
                    
                            dqr_detail.n46.p = 1;
                        }
                        //n47
                        // cr_dmhcctt_death IN ('1', '2') AND (CDF_06 = 1)
                        if
                        (
                            (
                                cr_dmhcctt_death == 1 ||
                                cr_dmhcctt_death == 2
                                )
                                && test_int == 6
                        )
                        {
                            dqr_detail.n47.p = 1;
                        }
                        //n48
                        // cr_dsudctt_death IN ('1', '2') AND (CDF_07 = 1)
                        if
                        (
                            (
                                cr_dsudctt_death == 1 ||
                                cr_dsudctt_death == 2
                            )
                            && test_int == 7
                        )
                        {
                            dqr_detail.n48.p = 1;
                        }
                    }
                }

                    // n49
                // ([crcfw_class] <> '9999' AND [crcfw_descr] IS NOT NULL AND [crcfw_descr] <> '' AND [crcfw_c_recom] IS NOT NULL AND [crcfw_c_recom] <> '')
                //crcfw_class:  /committee_review/critical_factors_worksheet/class
                //crcfw_descr:  /committee_review/critical_factors_worksheet/description
                //crcfw_c_recom:  /committee_review/critical_factors_worksheet/committee_recommendations

                if(gs.get_form(source_object, "committee_review") is var form)
                {
                    if(gs.get_grid(form, "critical_factors_worksheet") is var grid_result)
                    {
                        var is_pass = true;
                        foreach(IDictionary<string, object> item_object in grid_result)
                        {
                            var crcfw_class = gs.get_number(item_object, "class");
                            var crcfw_descr = gs.get_string(item_object, "description");
                            var crcfw_c_recom = gs.get_string(item_object, "committee_recommendations");

                            if
                            (
                                crcfw_class != null && crcfw_class != 9999 && 
                                !string.IsNullOrWhiteSpace(crcfw_descr) && 
                                ! string.IsNullOrWhiteSpace(crcfw_c_recom)
                            )
                            {
                                is_pass = is_pass && true;
                                dqr_detail.n49.p += 1;
                            }
                            else
                            {
                                is_pass = is_pass && false;
                            }
                        }

                        if(is_pass)
                        {
                            //dqr_detail.n49.p = 1;
                        }
                    }
                }
            }
            
            dqr_detail.n49.t = dqr_detail.n49.p;
        }


        /*
            ( 
                (cr_wtd_preve='0' AND cr_cta_outco IN ('2', '3','9999')) OR 
                (cr_wtd_preve = '1' AND cr_cta_outco IN ('0','1','3','9999')) OR
                (cr_wtd_preve='9999' AND cr_cta_outco IN ('3', '9999')) 
            )

        */



        //n46




        //cr_ddctt_death IN ('1', '2')
        /*
        (
            A3.cr_wtd_preve='1' OR 
            A3.cr_cta_outco='0' OR 
            A3.cr_cta_outco='1'
        ) 
        AND A2.cr_p_relat = '1'
        //cr_ddctt_death IN ('1', '2')
        //cr_ddctt_death:  /committee_review/did_discrimination_contribute_to_the_death

        */ 



        //n47
        /*
        Valid Review Date: IsDate([cr_do_revie]) = True  AND
(A3.cr_wtd_preve='1' OR A3.cr_cta_outco='0' OR A3.cr_cta_outco='1') AND 
A2.cr_p_relat = '1'

n = cr_dmhcctt_death IN ('1', '2')
p = cr_dmhcctt_death IN ('1', '2') AND (CDF_06 = 1)

        cr_dmhcctt_death:  /committee_review/did_mental_health_conditions_contribute_to_the_death
        */



        //n48
        /*
        Valid Review Date: IsDate([cr_do_revie]) = True  AND
(A3.cr_wtd_preve='1' OR A3.cr_cta_outco='0' OR A3.cr_cta_outco='1') AND 
A2.cr_p_relat = '1'

n = cr_dsudctt_death IN ('1', '2')
p = cr_dsudctt_death IN ('1', '2') AND (CDF_07 = 1)

        cr_dsudctt_death: /committee_review/did_substance_use_disorder_contribute_to_the_death
        */



        //n49
        /*
        Valid Review Date: IsDate([cr_do_revie]) = True  AND
(A3.cr_wtd_preve='1' OR A3.cr_cta_outco='0' OR A3.cr_cta_outco='1') AND 
A2.cr_p_relat = '1'

n = 
p = ([crcfw_class] <> '9999' AND [crcfw_descr] IS NOT NULL AND [crcfw_descr] <> '' AND [crcfw_c_recom] IS NOT NULL AND [crcfw_c_recom] <> '')

        cr_dsudctt_death: /committee_review/did_substance_use_disorder_contribute_to_the_death
        */



        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        //settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        result = Newtonsoft.Json.JsonConvert.SerializeObject(dqr_detail, settings);

        return result;
    }

    private void Get_List_Look_Up
    (
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> p_result,
        mmria.common.metadata.node[] p_lookup,
        mmria.common.metadata.node p_metadata,
        string p_path
    )
    {
        switch (p_metadata.type.ToLower())
        {
            case "form":
            case "group":
            case "grid":
            foreach (mmria.common.metadata.node node in p_metadata.children)
            {
                Get_List_Look_Up(p_result, p_lookup, node, p_path + "/" + node.name.ToLower());
            }
            break;
            case "list":
            if
            (
                p_metadata.control_style != null &&
                p_metadata.control_style.ToLower() == "editable"
            )
            {
                break;
            }

            p_result.Add(p_path, new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            var value_node_list = p_metadata.values;
            if
            (
                !string.IsNullOrWhiteSpace(p_metadata.path_reference)
            )
            {
                var name = p_metadata.path_reference.Replace("lookup/", "");
                foreach (var item in p_lookup)
                {
                if (item.name.ToLower() == name.ToLower())
                {
                    value_node_list = item.values;
                    break;
                }
                }
            }

            foreach (var value in value_node_list)
            {
                p_result[p_path].Add(value.value, value.display);
            }

            break;
            default:
            break;
        }
    }





}


