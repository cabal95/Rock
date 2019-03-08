// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
#if !IS_NET_CORE
using System.Data.Entity.Migrations;
#endif
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
#if !IS_NET_CORE
using System.Web.Hosting;

#else
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Rock.Migrations.CoreShims;
#endif
using Rock;
using Rock.Data;
using Rock.Model;


namespace Rock.Migrations
{
    /// <summary>
    /// Custom Migration methods
    /// </summary>
#if IS_NET_CORE
    public abstract class RockMigration : Rock.Data.IMigration, Rock.CoreShims.ILegacyMigration
#else
    public abstract class RockMigration : DbMigration, Rock.Data.IMigration
#endif
    {
        /// <summary>
        /// Gets the migration helper.
        /// </summary>
        /// <value>
        /// The migration helper.
        /// </value>
        public Rock.Data.MigrationHelper RockMigrationHelper
        {
            get
            {
                if (_migrationHelper == null)
                {
                    _migrationHelper = new Rock.Data.MigrationHelper( this );
                }
                return _migrationHelper;
            }
        }
        private Rock.Data.MigrationHelper _migrationHelper = null;

        /// <summary>
        ///  Contains embedded SQL files that be used in migrations
        ///  to Add SQL files, name the SQL files so it starts with the Migration name, 
        ///  put them in the migrations folder, then add it to RockMigrationSQL.resx
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        public class MigrationSQL : Rock.Migrations.Migrations.RockMigrationSQL { };

        /// <summary>
        /// Adds an operation to execute a SQL command.  Entity Framework Migrations
        /// APIs are not designed to accept input provided by untrusted sources (such
        /// as the end user of an application). If input is accepted from such sources
        /// it should be validated before being passed to these APIs to protect against
        /// SQL injection attacks etc.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        public void Sql(string sql)
        {
#if IS_NET_CORE
            Sql( sql, false );
#else
            Sql(sql, false, null);
#endif
        }

#if IS_NET_CORE
        public void Sql( string sql, bool suppressTransaction )
        {
            MigrationBuilder.Sql( sql, suppressTransaction );
        }
#endif

        /// <summary>
        /// Runs the SQL found in a file.
        /// </summary>
        /// <param name="sqlFile">The file the SQL can be found it relative to the application path.</param>
        public void SqlFile(string sqlFile)
        {
            // append application root
            sqlFile = EfMapPath(sqlFile);

            string script = File.ReadAllText( sqlFile );
            using ( var rockContext = new RockContext() )
            {
                Sql( script );  

                // delete file if being run in 'production'
                if ( HttpContext.Current != null )
                {
                    File.Delete( sqlFile );

                    // delete directory if it's empty
                    if (Directory.GetFiles(Path.GetDirectoryName(sqlFile)).Length == 0){
                        Directory.Delete( Path.GetDirectoryName( sqlFile ) );
                    }
                }
            }
        }

        /// <summary>
        /// Efs the map path.
        /// </summary>
        /// <param name="seedFile">The seed file.</param>
        /// <returns></returns>
        private string EfMapPath( string seedFile )
        {
#if !IS_NET_CORE
            if ( HttpContext.Current != null )
            {
                return HostingEnvironment.MapPath( seedFile );
            }
#endif

            var absolutePath = new Uri( Assembly.GetExecutingAssembly().CodeBase ).AbsolutePath;
            var directoryName = Path.GetDirectoryName( absolutePath ).Replace( "Rock.Migrations\\bin", "RockWeb" );
            var path = Path.Combine( directoryName, ".." + seedFile.TrimStart( '~' ).Replace( '/', '\\' ) );

            return path;
        }

#if IS_NET_CORE
        public MigrationBuilder MigrationBuilder { get; set; }

        public virtual void Up() { }

        public virtual void Down() { }

        public LegacyCreateTableBuilder<TColumns> CreateTable<TColumns>( string name, Func<LegacyColumnsBuilder, TColumns> columns )
        {
            string schema = null;

            if ( name.Contains( '.' ) )
            {
                schema = name.Split( '.' )[0];
                name = name.Split( '.' )[1];
            }

            var createTableBuilder = MigrationBuilder.CreateTable( name, ( cb ) => columns( new LegacyColumnsBuilder( cb, false ) ), schema, null );

            return new LegacyCreateTableBuilder<TColumns>( createTableBuilder, this, schema, name );
        }

        public void CreateIndex( string table, string column, bool unique = false, string name = null )
        {
            CreateIndex( table, new[] { column }, unique, name );
        }

        public void CreateIndex( string table, string[] columns, bool unique = false, string name = null )
        {
            var qTable = new QualifiedTableName( table );

            if ( name == null )
            {
                name = $"IX_{string.Join( '_', columns )}";
            }

            if ( columns.Length == 1 )
            {
                MigrationBuilder.CreateIndex( name, qTable.Table, columns[0], qTable.Schema, unique );
            }
            else
            {
                MigrationBuilder.CreateIndex( name, qTable.Table, columns, qTable.Schema, unique );
            }
        }

        public void AddColumn( string table, string name, Func<LegacyColumnsBuilder, OperationBuilder<AddColumnOperation>> columnAction )
        {
            var operation = ExtractOperation( columnAction( new LegacyColumnsBuilder( new ColumnsBuilder( new CreateTableOperation() ), true ) ) );
            var qTable = new QualifiedTableName( table );

            operation.Name = name;
            operation.Table = qTable.Table;
            operation.Schema = qTable.Schema;

            MigrationBuilder.Operations.Add( operation );
        }

        public void AddForeignKey( string dependentTable, string dependentColumn, string principalTable, string principalColumn, bool cascadeDelete = false, string name = null )
        {
            AddForeignKey( dependentTable, new[] { dependentColumn }, principalTable, new string[] { principalColumn }, cascadeDelete, name );
        }

        public void AddForeignKey( string dependentTable, string[] dependentColumns, string principalTable, string[] principalColumns, bool cascadeDelete = false, string name = null )
        {
            var principal = new QualifiedTableName( principalTable );
            var dependent = new QualifiedTableName( dependentTable );

            if ( name == null )
            {
                name = $"FK_{dependent.FullName}_{principal.FullName}_{string.Join( '_', dependentColumns )}";
            }

            var operation = new AddForeignKeyOperation
            {
                Schema = dependent.Schema,
                Table = dependent.Table,
                Name = name,
                Columns = dependentColumns,
                PrincipalSchema = principal.Schema,
                PrincipalTable = principal.Table,
                PrincipalColumns = principalColumns,
                OnUpdate = ReferentialAction.NoAction,
                OnDelete = cascadeDelete ? ReferentialAction.Cascade : ReferentialAction.NoAction
            };

            MigrationBuilder.Operations.Add( operation );
        }

        public void AlterColumn( string table, string name, Func<LegacyColumnsBuilder, OperationBuilder<AddColumnOperation>> columnAction )
        {
            var op = ExtractOperation( columnAction( new LegacyColumnsBuilder( new ColumnsBuilder( new CreateTableOperation() ), true ) ) );
            var qTable = new QualifiedTableName( table );

            var operation = new AlterColumnOperation
            {
                Schema = qTable.Schema,
                Table = qTable.Table,
                Name = name,
                ClrType = op.ClrType,
                ColumnType = op.ColumnType,
                IsUnicode = op.IsUnicode,
                MaxLength = op.MaxLength,
                IsRowVersion = op.IsRowVersion,
                IsNullable = op.IsNullable,
                DefaultValue = op.DefaultValue,
                DefaultValueSql = op.DefaultValueSql,
                ComputedColumnSql = op.ComputedColumnSql,
                IsFixedLength = op.IsFixedLength,
                OldColumn = new ColumnOperation
                {
                    ClrType = op.ClrType,
                    ColumnType = null,
                    IsUnicode = null,
                    MaxLength = null,
                    IsRowVersion = false,
                    IsNullable = false, //?
                    DefaultValue = null,
                    DefaultValueSql = null,
                    ComputedColumnSql = null,
                    IsFixedLength = null
                }
            };

            MigrationBuilder.Operations.Add( operation );
        }

        public void DropColumn( string table, string name )
        {
            var qTable = new QualifiedTableName( table );

            MigrationBuilder.DropColumn( name, qTable.Table, qTable.Schema );
        }

        public void DropIndex( string table, string name )
        {
            var qTable = new QualifiedTableName( table );

            MigrationBuilder.DropIndex( name, qTable.Table, qTable.Schema );
        }

        public void DropIndex( string table, string[] columns )
        {
            var name = $"IX_{string.Join( '_', columns )}";

            DropIndex( table, name );
        }

        public void DropForeignKey( string dependentTable, string dependentColumn, string principalTable )
        {
            DropForeignKey( dependentTable, new[] { dependentColumn }, principalTable );
        }

        public void DropForeignKey( string dependentTable, string[] dependentColumns, string principalTable )
        {
            var principal = new QualifiedTableName( principalTable );
            var dependent = new QualifiedTableName( dependentTable );
            var name = $"FK_{dependent.FullName}_{principal.FullName}_{string.Join( '_', dependentColumns )}";

            MigrationBuilder.DropForeignKey( name, dependent.Table, dependent.Schema );
        }

        public void DropTable( string name )
        {
            var table = new QualifiedTableName( name );

            MigrationBuilder.DropTable( table.Table, table.Schema );
        }

        public void RenameTable( string name, string newName )
        {
            var qName = new QualifiedTableName( name );
            var qNewName = new QualifiedTableName( newName );

            MigrationBuilder.RenameTable( qName.Table, qName.Schema, qNewName.Table, qNewName.Schema );
        }

        public T ExtractOperation<T>( OperationBuilder<T> builder )
            where T : MigrationOperation
        {
            return ( T ) builder.GetType().GetProperty( "Operation", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( builder );
        }
#endif
   }
}