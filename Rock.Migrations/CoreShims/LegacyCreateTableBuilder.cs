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
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;


namespace Rock.Migrations.CoreShims
{
    public class LegacyCreateTableBuilder<TColumns>
    {
        public CreateTableBuilder<TColumns> TableBuilder { get; private set; }

        protected MigrationBuilder MigrationBuilder { get; private set; }

        protected RockMigration RockMigration { get; private set; }

        protected QualifiedTableName TableName { get; set; }

        public LegacyCreateTableBuilder( CreateTableBuilder<TColumns> tableBuilder, RockMigration rockMigration, string schema, string name )
        {
            TableBuilder = tableBuilder;
            MigrationBuilder = rockMigration.MigrationBuilder;
            RockMigration = rockMigration;
            TableName = new QualifiedTableName( schema, name );
        }

        public LegacyCreateTableBuilder<TColumns> PrimaryKey( Expression<Func<TColumns, object>> columns )
        {
            TableBuilder.PrimaryKey( $"PK_{TableName.FullName}", columns );

            return this;
        }

        public LegacyCreateTableBuilder<TColumns> ForeignKey( string principalTable, Expression<Func<TColumns, object>> dependentKeyExpression, bool cascadeDelete = false, string name = null )
        {
            var columns = ( ( LambdaExpression ) dependentKeyExpression ).GetPropertyAccessList().Select( c => c.Name ).ToArray();

            var tableOp = RockMigration.ExtractOperation<CreateTableOperation>( TableBuilder );
            var dependent = new QualifiedTableName( tableOp.Schema, tableOp.Name );

            RockMigration.AddForeignKey( dependent.FullName, columns, principalTable, new[] { "Id" }, cascadeDelete, name );

            return this;
        }

        public LegacyCreateTableBuilder<TColumns> Index(Expression<Func<TColumns, object>> indexExpression, string name = null, bool unique = false, bool clustered = false )
        {
            var columns = ( ( LambdaExpression ) indexExpression ).GetPropertyAccessList().Select( c => c.Name ).ToArray();

            if ( name == null )
            {
                name = $"IX_{string.Join( '_', columns )}";
            }

            if ( columns.Length == 1 )
            {
                MigrationBuilder.CreateIndex( name, TableName.Table, columns[0], TableName.Schema, unique );
            }
            else
            {
                MigrationBuilder.CreateIndex( name, TableName.Table, columns, TableName.Schema, unique );
            }

            return this;
        }
    }
}