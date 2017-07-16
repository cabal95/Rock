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
using System.Data.Entity.Infrastructure.Interception;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;

namespace Rock.Data
{
    /// <summary>
    /// Manually set precision to 3 to deal with the fact that EF seems to assume DateTime2
    /// even though the columns were created DateTime (on Mono this causes errors).
    /// </summary>
    public class DateTime2Interceptor : DbCommandInterceptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryHintDbCommandInterceptor"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="hint">The hint.</param>
        public DateTime2Interceptor()
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="interceptionContext"></param>
        /// <inheritdoc />
        public override void ReaderExecuting( System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext )
        {
            int count = 0;
            for ( int i = 0; i < command.Parameters.Count; i++ )
            {
                if ( command.Parameters[i].DbType == System.Data.DbType.DateTime2 )
                {
                    count += 1;
                    command.Parameters[i].Precision = 3;
                }
            }
            System.Diagnostics.Debug.WriteLine( string.Format( "Found {0} datetime2 types", count ) );

            base.ReaderExecuting( command, interceptionContext );
        }

        public override void NonQueryExecuting( DbCommand command, DbCommandInterceptionContext<int> interceptionContext )
        {
            int count = 0;
            for ( int i = 0; i < command.Parameters.Count; i++ )
            {
                if ( command.Parameters[i].DbType == System.Data.DbType.DateTime2 )
                {
                    count += 1;
                    command.Parameters[i].Precision = 3;
                }
            }
            System.Diagnostics.Debug.WriteLine( string.Format( "Found {0} datetime2 types", count ) );

            base.NonQueryExecuting( command, interceptionContext );
        }

        public override void ScalarExecuting( DbCommand command, DbCommandInterceptionContext<object> interceptionContext )
        {
            int count = 0;
            for ( int i = 0; i < command.Parameters.Count; i++ )
            {
                if ( command.Parameters[i].DbType == System.Data.DbType.DateTime2 )
                {
                    count += 1;
                    command.Parameters[i].Precision = 3;
                }
            }
            System.Diagnostics.Debug.WriteLine( string.Format( "Found {0} datetime2 types", count ) );

            base.ScalarExecuting( command, interceptionContext );
        }
    }
}
