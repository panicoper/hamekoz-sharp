//
//  DB.cs
//
//  Author:
//       Claudio Rodrigo Pereyra Diaz <rodrigo@hamekoz.com.ar>
//
//  Copyright (c) 2010 Hamekoz
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;

namespace Hamekoz.Data
{
	public static class SqlExtension
	{
		public static string SqlClean (this string sql)
		{
			//HACK refactoriar para que sea mas eficiente y contemple mas caracteres

			string aux = sql;
			// eliminamos caracteres que nos puedan estorbar
			var simbolos = new char[7];
			simbolos.SetValue (',', 0);
			simbolos.SetValue (';', 1);
			simbolos.SetValue ('.', 2);
			simbolos.SetValue (':', 3);
			simbolos.SetValue ('"', 4);
			simbolos.SetValue ('\'', 5);
			simbolos.SetValue (char.Parse ("'"), 6);

			for (int i = 0; i < simbolos.Length; i++) {
				aux = aux.Replace (simbolos [i], ' ');
			}

			return aux;
		}
	}

	/// <summary>
	/// Data Base access class
	/// </summary>
	public class DB
	{
		public static void PrintProviderFactoryClasses ()
		{
			// Retrieve the installed providers and factories.
			DataTable table = DbProviderFactories.GetFactoryClasses ();

			// Display each row and column value.
			foreach (DataRow row in table.Rows) {
				Console.WriteLine (row [0]);
				Console.WriteLine (row [1]);
				Console.WriteLine (row [2]);
				Console.WriteLine (row [3]);
				Console.WriteLine ();
			}
		}

		DbProviderFactory factory;

		#region Propiedades

		static DB instancia;

		public static DB Instancia {
			get {
				if (instancia == null) {
					instancia = new DB ();
					instancia.ConnectionName = "Hamekoz.Data.DB";
				}
				return instancia;
			}
		}

		public static void SetInstancia (DB db)
		{
			instancia = db;
		}

		public int DefaultCommandTimeOut {
			get {
				return  30;
			}
		}

		int commandTimeOut = 30;

		/// <summary>
		/// Gets or sets the command time out.
		/// </summary>
		/// <value>The command time out.</value>
		public int CommandTimeOut {
			get { return commandTimeOut; }
			set { commandTimeOut = value; }
		}

		string providerName;

		/// <summary>
		/// Gets or sets the invarian name of the provider.
		/// </summary>
		/// <value>The invarian name of the provider.</value>
		public string ProviderName {
			get { return providerName; }
			set {
				providerName = value;
				factory = DbProviderFactories.GetFactory (providerName);
			}
		}

		/// <summary>
		/// Gets or sets the connection string.
		/// </summary>
		/// <value>The connection string.</value>
		public string ConnectionString { get; set; }

		string connectionName;

		/// <summary>
		/// Gets or sets the name of the connection in the app.config.
		/// </summary>
		/// <value>The name of the connection.</value>
		public string ConnectionName {
			get { return connectionName; }
			set {
				ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings [value];
				ProviderName = settings.ProviderName;
				ConnectionString = settings.ConnectionString;
				connectionName = value;
			}
		}

		#endregion

		#region Metodos Privados

		/// <summary>
		/// Asigna los parametros al comando de acuerdo al motor utilizado
		/// </summary>
		void cargarParametros (DbCommand comando, object[] parametros)
		{
			DbParameter p;
			for (int i = 0; i < parametros.Length; i = i + 2) {
				p = factory.CreateParameter ();
				p.ParameterName = parametros [i].ToString ();
				p.Value = parametros [i + 1];
				comando.Parameters.Add (p);
			}
		}

		#endregion

		#region Metodos Publicos

		public int SP (string sp, params object[] parameters)
		{
			using (DbConnection conexion = factory.CreateConnection ()) {
				conexion.ConnectionString = ConnectionString;
				conexion.Open ();
				using (DbCommand comando = factory.CreateCommand ()) {
					comando.Connection = conexion;
					comando.CommandType = CommandType.StoredProcedure;
					comando.CommandText = sp;
					comando.CommandTimeout = CommandTimeOut;
					cargarParametros (comando, parameters);
					return comando.ExecuteNonQuery ();
				}
			}
		}

		public DataSet SPToDataSet (string sp, params object[] parameters)
		{
			var dataset = new DataSet ();
			return SPToDataSet (dataset, sp, parameters);
		}

		public DataSet SPToDataSet (DataSet dataset, string sp, params object[] parameters)
		{
			using (DbConnection conexion = factory.CreateConnection ()) {
				conexion.ConnectionString = ConnectionString;
				conexion.Open ();
				using (DbCommand comando = factory.CreateCommand ()) {
					comando.Connection = conexion;
					comando.CommandType = CommandType.StoredProcedure;
					comando.CommandText = sp;
					comando.CommandTimeout = CommandTimeOut;
					cargarParametros (comando, parameters);
					using (DbDataAdapter adaptador = factory.CreateDataAdapter ()) {
						adaptador.SelectCommand = comando;
						adaptador.Fill (dataset, sp);
						return dataset;
					}
				}
			}
		}

		public DbDataReader SPToDbDataReader (string sp, params object[] parameters)
		{
			DbConnection conexion = factory.CreateConnection ();
			conexion.ConnectionString = ConnectionString;
			conexion.Open ();
			DbCommand comando = factory.CreateCommand ();
			comando.Connection = conexion;
			comando.CommandType = CommandType.StoredProcedure;
			comando.CommandText = sp;
			comando.CommandTimeout = CommandTimeOut;
			cargarParametros (comando, parameters);
			return comando.ExecuteReader (CommandBehavior.CloseConnection);
		}

		public object SPToScalar (string sp, params object[] parameters)
		{
			using (DbConnection conexion = factory.CreateConnection ()) {
				conexion.ConnectionString = ConnectionString;
				conexion.Open ();
				using (DbCommand comando = factory.CreateCommand ()) {
					comando.Connection = conexion;
					comando.CommandType = CommandType.StoredProcedure;
					comando.CommandText = sp;
					comando.CommandTimeout = CommandTimeOut;
					cargarParametros (comando, parameters);
					return comando.ExecuteScalar ();
				}
			}
		}

		public int Sql (string sql)
		{
			using (DbConnection conexion = factory.CreateConnection ()) {
				conexion.ConnectionString = ConnectionString;
				conexion.Open ();
				using (DbCommand comando = factory.CreateCommand ()) {
					comando.Connection = conexion;
					comando.CommandType = CommandType.Text;
					comando.CommandText = sql;
					comando.CommandTimeout = CommandTimeOut;
					return comando.ExecuteNonQuery ();
				}
			}
		}

		public DataSet SqlToDataSet (string sql)
		{
			var dataset = new DataSet ();
			return SqlToDataSet (dataset, sql);
		}

		public DataSet SqlToDataSet (DataSet dataset, string sql)
		{
			using (DbConnection conexion = factory.CreateConnection ()) {
				conexion.ConnectionString = ConnectionString;
				conexion.Open ();
				using (DbCommand comando = factory.CreateCommand ()) {
					comando.Connection = conexion;
					comando.CommandType = CommandType.Text;
					comando.CommandText = sql;
					comando.CommandTimeout = CommandTimeOut;
					using (DbDataAdapter adaptador = factory.CreateDataAdapter ()) {
						adaptador.SelectCommand = comando;
						adaptador.Fill (dataset);
						return dataset;
					}
				}
			}
		}

		/// <summary>
		/// Qwery database with sql string and return a DbDataReader
		/// </summary>
		/// <param name="sql">Sql qwery</param>
		/// <returns>DbDataReader with data</returns>
		/// <remarks>Must wrap this method with using sentence </remarks>
		public DbDataReader SqlToDbDataReader (string sql)
		{
			DbConnection conexion = factory.CreateConnection ();
			conexion.ConnectionString = ConnectionString;
			conexion.Open ();
			DbCommand comando = factory.CreateCommand ();
			comando.Connection = conexion;
			comando.CommandType = CommandType.Text;
			comando.CommandText = sql;
			comando.CommandTimeout = CommandTimeOut;
			return comando.ExecuteReader (CommandBehavior.CloseConnection);
		}

		public object SqlToScalar (string sql)
		{
			using (DbConnection conexion = factory.CreateConnection ()) {
				conexion.ConnectionString = ConnectionString;
				conexion.Open ();
				using (DbCommand comando = factory.CreateCommand ()) {
					comando.Connection = conexion;
					comando.CommandType = CommandType.Text;
					comando.CommandText = sql;
					comando.CommandTimeout = CommandTimeOut;
					return comando.ExecuteScalar ();
				}
			}
		}

		/// <summary>
		/// Registrar los errores generados al intentar realizar las consultas.
		/// </summary>
		/// <param name="error"></param>
		static void RegistrarError (Exception error)
		{
			System.IO.StreamWriter w = System.IO.File.AppendText ("DBNet.log");
			w.Write ("\r\nRegistro de error: ");
			w.WriteLine ("{0} {1}", DateTime.Now.ToShortDateString (), DateTime.Now.ToShortTimeString ());
			w.WriteLine ("Message: {0}", error.Message);
			w.WriteLine ("Source: {0}", error.Source);
			w.WriteLine ("StackTrace:");
			w.WriteLine (error.StackTrace);
			w.WriteLine ("-------------------------------");
			w.Flush ();
			w.Close ();
		}

		#endregion

		#region Legacy

		/// <summary>
		/// Tipo de comando que se pueden utilizar en consultas de Base de Datos
		/// </summary>
		[Obsolete ("Ya nos se requiere")]
		public enum TipoComando
		{
			/// <summary>
			/// Procedimiento almacenado
			/// </summary>
			SP,
			/// <summary>
			/// Consulta por linea de texto
			/// </summary>
			Texto
		}

		[Obsolete ("No se usa mas")]
		DbConnection conexionLegacy;

		/// <summary>
		/// Crea un comando de ejecucion en base de datos
		/// </summary>
		/// <param name="tipo">Tipo del comando</param>
		/// <param name="cadenaSql">Cadena del comando</param>
		[Obsolete ("No se usa mas")]
		DbCommand crearComando (TipoComando tipo, string cadenaSql)
		{
			DbCommand comando = factory.CreateCommand ();

			//asigno los valores del comando a ejecutar
			comando.Connection = conexionLegacy;
			switch (tipo) {
			case TipoComando.SP:
				{
					comando.CommandType = CommandType.StoredProcedure;
					break;
				}
			case TipoComando.Texto:
				{
					comando.CommandType = CommandType.Text;
					break;
				}
			}
			comando.CommandText = cadenaSql;
			comando.CommandTimeout = CommandTimeOut;
			return comando;
		}

		/// <summary>
		/// Conect this instance.
		/// </summary>
		[Obsolete ("No se usa mas")]
		public bool Conectar ()
		{
			try {
				//Simulo una clase singleton para que solo exista una conexion a la base de datos
				if (conexionLegacy == null) {
					conexionLegacy = factory.CreateConnection ();
				} else
					conexionLegacy.Close ();
				conexionLegacy.ConnectionString = ConnectionString;
				conexionLegacy.Open ();
				return true;
			} catch (Exception ex) {
				RegistrarError (ex);
				return false;
			}
		}

		/// <summary>
		/// Disconect this instance.
		/// </summary>
		[Obsolete ("No se usa mas, los dataReader deben estar dentro de una sentencia Using")]
		public void Desconectar ()
		{
			while (conexionLegacy.State != ConnectionState.Closed)
				conexionLegacy.Close ();
		}

		/// <summary>
		/// Realiza una consulta a una base de datos y devuelve un dataset nuevo
		/// </summary>
		/// <param name="tipo">Define el tipo de consulta a realizar. SP, texto, TablaEntera</param>
		/// <param name="cadenaSql">SP: nombre SP, Texto: Cadena de consulta</param>
		/// <param name="parametros">Pares Nombre parametros, Valor parametro</param>
		/// <returns>Devuelve resultas de la consulta en tablas contenidas en un DataSet</returns>
		[Obsolete ("Usar SPToDataSet o SqlToDataSet")]
		DataSet ejecutarDataSet (TipoComando tipo, string cadenaSql, params object[] parametros)
		{
			DbCommand comando;
			DbDataAdapter adaptador = factory.CreateDataAdapter ();
			var resultado = new DataSet ();

			Conectar ();

			//Instancio los objetos de acuerdo al tipo de motor de bases de datos que se utilice
			try {
				comando = crearComando (tipo, cadenaSql);
				//  comando.CommandTimeout = CommandTimeOut;
				cargarParametros (comando, parametros);
				//Ejecuto la consulta

				adaptador.SelectCommand = comando;
				switch (tipo) {
				case TipoComando.SP:
					{
						adaptador.Fill (resultado, cadenaSql);
						break;
					}
				default:
					{
						adaptador.Fill (resultado);
						break;
					}
				}
				//if (!enlazado)
				Desconectar ();
			} catch (DbException ex) {
				RegistrarError (ex);
			}
			return resultado;
		}

		/// <summary>
		/// Realiza una consulta a una base de datos y llena con el resultado un dataset parasado por referencia
		/// </summary>
		/// <param name="dataset">DataSet instanciado a ser llenado</param>
		/// <param name="tipo">Define el tipo de consulta a realizar. SP, texto, TablaEntera</param>
		/// <param name="cadenaSql">SP: nombre SP, Texto: Cadena de consulta</param>
		/// <param name="parametros">Pares Nombre parametros, Valor parametro</param>
		[Obsolete ("Usar SPToDataSet o SqlToDataSet")]
		void ejecutarDataSet (DataSet dataset, TipoComando tipo, string cadenaSql, params object[] parametros)
		{
			DbCommand comando;
			DbDataAdapter adaptador = factory.CreateDataAdapter ();
			//if (!enlazado)
			Conectar ();

			//Instancio los objetos de acuerdo al tipo de motor de bases de datos que se utilice
			try {
				comando = crearComando (tipo, cadenaSql);
				// comando.CommandTimeout = CommandTimeOut;
				cargarParametros (comando, parametros);
				//Ejecuto la consulta

				adaptador.SelectCommand = comando;
				switch (tipo) {
				case TipoComando.SP:
					{
						adaptador.Fill (dataset, cadenaSql);
						break;
					}
				default:
					{
						adaptador.Fill (dataset);
						break;
					}
				}
				//if (!enlazado)
				Desconectar ();
			} catch (DbException ex) {
				RegistrarError (ex);
			}
		}

		/// <summary>
		/// Realiza una consulta a una base de datos
		/// </summary>
		/// <param name="tipo">Define el tipo de consulta a realizar. SP, texto, TablaEntera</param>
		/// <param name="cadenaSql">SP: nombre SP, Texto: Cadena de consulta</param>
		/// <param name="parametros">Pares Nombre parametros, Valor parametro</param>
		/// <returns>Devuelve resultas de la consulta en DataReader</returns>
		/// <remarks>IMPORTANTE CERRAR EL DATAREADER AL FINALIZAR SU UTILIZACION</remarks>
		[Obsolete ("Usar SPToDbDataReader o SqlToDbDataReader")]
		DbDataReader ejecutarDataReader (TipoComando tipo, string cadenaSql, params object[] parametros)
		{
			DbCommand comando;
			DbDataReader resultado;
			//if (!enlazado)
			Conectar ();
			comando = crearComando (tipo, cadenaSql);
			cargarParametros (comando, parametros);
			//Ejecuto la consulta
			resultado = comando.ExecuteReader (CommandBehavior.CloseConnection);

			return resultado;
		}

		/// <summary>
		/// Ejecuta una consulta escalar.
		/// </summary>
		/// <param name="tipo">Tipo de consulta</param>
		/// <param name="cadenaSql">SP: nombre SP, Texto: Cadena de consulta</param>
		/// <param name="parametros">Pares Nombre parametros, Valor parametro</param>
		/// <returns>Devuelve un objeto con un valor escalar a que necesitar ser casteado</returns>
		[Obsolete ("Usar SPToScalar o SqlToScalar")]
		object ejecutarScalar (TipoComando tipo, string cadenaSql, params object[] parametros)
		{
			object id;
			DbCommand comando;
			//if (!enlazado)
			Conectar ();
			comando = crearComando (tipo, cadenaSql);
			cargarParametros (comando, parametros);
			id = comando.ExecuteScalar ();
			//if (!enlazado)
			Desconectar ();
			return id;
		}

		/// <summary>
		/// Ejecuta una consulta sin devolucion de resultados.
		/// </summary>
		/// <param name="tipo">Tipo de consulta</param>
		/// <param name="cadenaSql">SP: nombre SP, Texto: Cadena de consulta</param>
		/// <param name="parametros">Pares Nombre parametros, Valor parametro</param>
		/// <returns>Devuelve cantidad de registros afectados</returns>
		[Obsolete ("Usar SP o Sql")]
		public int ejecutarProceso (TipoComando tipo, string cadenaSql, params object[] parametros)
		{
			int cant;
			DbCommand comando;
			//if (!enlazado)
			Conectar ();
			comando = crearComando (tipo, cadenaSql);
			//  comando.CommandTimeout = 240;
			cargarParametros (comando, parametros);
			cant = comando.ExecuteNonQuery ();
			//cpereyra - Verificar que si se elemina el comando luego de utilizarlo sigue andando
			comando.Dispose ();
			//if (!enlazado)
			Desconectar ();
			return cant;
		}

		#endregion
	}
}
