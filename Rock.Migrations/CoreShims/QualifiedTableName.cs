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

namespace Rock.Migrations.CoreShims
{
    public class QualifiedTableName
    {
        public string Schema { get; private set; }
        public string Table { get; private set; }

        public string FullName
        {
            get
            {
                return string.IsNullOrWhiteSpace( Schema ) ? Table : $"{Schema}.{Table}";
            }
        }

        public QualifiedTableName( string fullName )
        {
            if ( fullName.Contains( '.' ) )
            {
                Schema = fullName.Split( '.' )[0];
                Table = fullName.Split( '.' )[1];
            }
            else
            {
                Schema = null;
                Table = fullName;
            }
        }

        public QualifiedTableName( string schema, string table )
        {
            Schema = schema;
            Table = table;
        }
    }
}