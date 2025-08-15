using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;



namespace mmria.mmrds.data
{
    public class cData
    {

        private string ConnectionString = null;
        //private OleDbConnection conn;
        private OleDbConnection transaction_connection;
        private OleDbTransaction transaction;
        private const int TimeoutConst = 1800; // 1800 Seconds = 30 minutes
        public cData(string pConnectionString)
        {

            this.ConnectionString = pConnectionString;
            //this.conn = new OleDbConnection(pConnectionString);
            //this.transaction_connection = new OleDbConnection(pConnectionString);
            //this.conn.Close();
            //this.transaction_connection.Close();
        }

		public DataTable GetDataTable(string pSQL, List<System.Data.OleDb.OleDbParameter> OleDbParameterList = null, bool Is_Stored_Procedure = false)
        {
            System.Data.DataTable result = new System.Data.DataTable();
            OleDbCommand command = null;
			OleDbDataAdapter Adapter = null;
            OleDbConnection conn = null;

			try
			{

                conn = new OleDbConnection(this.ConnectionString);
                conn.Open();
                command = new OleDbCommand(pSQL, conn);
				command.CommandTimeout = TimeoutConst; 
				
				if (Is_Stored_Procedure)
				{
					command.CommandType = System.Data.CommandType.StoredProcedure;
				}
				else
				{
					command.CommandType = System.Data.CommandType.Text;
				}
				
				if (OleDbParameterList != null)
				{
                    foreach (var parameter in OleDbParameterList)
                    {
                        command.Parameters.Add(parameter);
                    }
				}
				
				Adapter = new OleDbDataAdapter(command);
				Adapter.Fill(result);
				
			
			}
			catch(Exception ex)
			{
				throw ex;
			}
			finally
			{
				if(command != null)
				{
					command.Dispose();
					command = null;
				}

				if(Adapter != null)
				{
					Adapter.Dispose();
					Adapter = null;
				}

                if(conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
			}

            return result;
        }


        public List<DataTable> GetDatTableList(List<string> QueryList)
        {
            var result = new List<DataTable>();
            OleDbCommand command = null;
            OleDbDataAdapter Adapter = null;
            OleDbConnection conn = null;

            try
            {
                conn = new OleDbConnection(this.ConnectionString);
                conn.Open();

                foreach (string query in QueryList)
                {
                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    command = conn.CreateCommand();

                    command.CommandTimeout = TimeoutConst;
                    command.CommandText = query;

                    Adapter = new OleDbDataAdapter(command);

                    Adapter.Fill(dataTable);


                    result.Add(dataTable);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }

                if(Adapter != null)
                {
                    Adapter.Dispose();
                    Adapter = null;
                }

                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
            return result;
        }


        public bool ExecuteSQL(string pSQL, List<System.Data.OleDb.OleDbParameter> OleDbParameterList = null, bool Is_Stored_Procedure = false, int TimeOut = 30)
        {
            bool result = false;
            OleDbCommand command = null;
            OleDbConnection conn = null;
            try
            {

                if (this.transaction == null)
                {
                    conn = new OleDbConnection(this.ConnectionString);
                    conn.Open();
                    command = new OleDbCommand(pSQL, conn);

                    command.CommandTimeout = TimeOut;

                    if (Is_Stored_Procedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }
                    else
                    {
                        command.CommandType = CommandType.Text;
                    }

                    if (OleDbParameterList != null)
                    {

                        foreach (var parameter in OleDbParameterList)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }
                    command.ExecuteNonQuery();
                }
                else
                {

                    if(this.transaction_connection.State == ConnectionState.Closed)
                    {
                        this.transaction_connection.Open();
                    }

                    command = new OleDbCommand(pSQL, this.transaction_connection);
                    command.Transaction = this.transaction;
                    command.CommandTimeout = TimeOut;

                    if (Is_Stored_Procedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }
                    else
                    {
                        command.CommandType = CommandType.Text;
                    }

                    if (OleDbParameterList != null)
                    {

                        foreach (var parameter in OleDbParameterList)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }
                    
                    command.ExecuteNonQuery();
                }

            }
            catch(Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }

                if(this.transaction == null)
                {
                    if (conn != null)
                    {
                        conn.Close();
                        conn.Dispose();
                    }
                }
            }
            result = true;


            return result;
        }

        public void BeginTransaction()
        {
            if (this.transaction_connection == null)
            {
                this.transaction_connection = new OleDbConnection(this.ConnectionString);
                this.transaction_connection.Open();
            }
                
            if (this.transaction_connection.State == ConnectionState.Closed)
            {
                transaction_connection.Open();
            }

            if (this.transaction == null)
            {
                this.transaction = transaction_connection.BeginTransaction();
            }

        }

        public void RollbackTransaction()
        {
            try
            {
                if (this.transaction != null)
                {
                    this.transaction.Rollback();
                }
                else
                {
                    throw new Exception("cData.Transaction is null");
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (this.transaction != null)
                {
                    this.transaction.Dispose();
                    this.transaction = null;
                }

                if (this.transaction_connection != null)
                {
                    if (this.transaction_connection.State == ConnectionState.Open)
                    {
                        this.transaction_connection.Close();
                    }
                    this.transaction_connection.Dispose();
                    this.transaction_connection = null;
                }
            }
        }

        public void CommitTransaction()
        {
            

            try
            {
                //this.transaction.Rollback();
                if(this.transaction != null)
                {
                    this.transaction.Commit();
                }
                else
                {
                    throw new Exception("cData.Transaction is null");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

                if (this.transaction != null)
                {
                    this.transaction.Dispose();
                    this.transaction = null;
                }

                if (this.transaction_connection != null)
                {
                    if (this.transaction_connection.State == ConnectionState.Open)
                    {
                        this.transaction_connection.Close();
                    }
                    this.transaction_connection.Dispose();
                    this.transaction_connection = null;
                }

            }
            /*
            this.transaction.Commit();
            this.conn.Close();
            this.transactionCommand.Dispose();
            this.transaction.Dispose();
            this.transaction = null;
            this.transactionCommand = null;*/
        }

        public string SafeString(string Input)
        {

            var result = Input.Replace("'", "''");

            return result;
        }
    }
}
