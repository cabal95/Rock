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

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Rock.Migrations.CoreShims
{
    public class LegacyColumnsBuilder
    {
        private readonly bool _nullableDefault;
        private readonly bool _isAlter;
        private ColumnsBuilder _builder;

        public LegacyColumnsBuilder( ColumnsBuilder builder, bool isAlter )
        {
            _builder = builder;
            _nullableDefault = true;
            _isAlter = isAlter;
        }

        public OperationBuilder<AddColumnOperation> Boolean( bool? nullable = null, bool? defaultValue = null, string defaultValueSql = null, string name = null )
        {
            nullable = nullable ?? _nullableDefault;

            if ( _isAlter && !nullable.Value && !defaultValue.HasValue )
            {
                defaultValue = false;
            }

            var column = _builder.Column<bool>( null, null, null, false, name, nullable.Value, defaultValue, defaultValueSql, null, null );

            return column;
        }

        public OperationBuilder<AddColumnOperation> Int( bool? nullable = null, bool identity = false, int? defaultValue = null, string defaultValueSql = null, string name = null )
        {
            nullable = nullable ?? _nullableDefault;

            if ( _isAlter && !nullable.Value && !defaultValue.HasValue )
            {
                defaultValue = 0;
            }

            var column = _builder.Column<int>( null, null, null, false, name, nullable.Value, defaultValue, defaultValueSql, null, null );

            if ( identity )
            {
                column = column.Annotation( "SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn );
            }

            return column;
        }

        public OperationBuilder<AddColumnOperation> Decimal( bool? nullable = null, byte? precision = null, byte? scale = null, decimal? defaultValue = null, string defaultValueSql = null, string name = null, string storeType = null )
        {
            nullable = nullable ?? _nullableDefault;

            if ( _isAlter && !nullable.Value && !defaultValue.HasValue )
            {
                defaultValue = 0;
            }

            if ( string.IsNullOrEmpty( storeType ) )
            {
                storeType = $"decimal({precision ?? 18}, {scale ?? 0})";
            }

            var column = _builder.Column<decimal>( storeType, null, null, false, name, nullable.Value, defaultValue, defaultValueSql, null, null );

            return column;
        }

        public OperationBuilder<AddColumnOperation> String( bool? nullable = null, int? maxLength = null, bool? fixedLength = null, bool? unicode = null, string defaultValue = null, string defaultValueSql = null, string name = null )
        {
            nullable = nullable ?? _nullableDefault;

            if ( _isAlter && !nullable.Value && defaultValue == null )
            {
                defaultValue = string.Empty;
            }

            var column = _builder.Column<string>( null, unicode, maxLength, false, name, nullable.Value, defaultValue, defaultValueSql, null, fixedLength );

            return column;
        }

        public OperationBuilder<AddColumnOperation> DateTime( bool? nullable = null, byte? precision = null, DateTime? defaultValue = null, string defaultValueSql = null, string name = null, string storeType = null )
        {
            nullable = nullable ?? _nullableDefault;

            if ( precision.HasValue )
            {
                throw new NotImplementedException( "precision" );
            }

            var column = _builder.Column<DateTime>( storeType, null, null, false, name, nullable.Value, defaultValue, defaultValueSql, null, null );

            return column;
        }

        public OperationBuilder<AddColumnOperation> Guid( bool? nullable = null, bool identity = false, Guid? defaultValue = null, string defaultValueSql = null, string name = null )
        {
            nullable = nullable ?? _nullableDefault;

            var column = _builder.Column<Guid>( null, null, null, false, name, nullable.Value, defaultValue, defaultValueSql, null, null );

            return column;
        }
    }
}