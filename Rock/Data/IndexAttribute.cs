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

namespace Rock.Data
{
    /// <summary>
    /// Represents an attribute that is placed on a property to indicate that the
    /// database column to which the property is mapped has an index.
    /// From: https://github.com/jsakamoto/EntityFrameworkCore.IndexAttribute
    /// </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = true )]
    public class IndexAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the index name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets a number that determines the column ordering for
        /// multi-column indexes. This will be -1 if no column order has been specified.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate whether to define a unique index.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Initializes a new IndexAttribute instance for an index that will be
        /// named by convention and has no column order, uniqueness specified.
        /// </summary>
        public IndexAttribute() : this( "", -1 )
        {
        }

        /// <summary>
        /// Initializes a new IndexAttribute instance for an index with the given name
        /// and has no column order, uniqueness specified.
        /// </summary>
        /// <param name="name">The index name.</param>
        public IndexAttribute( string name ) : this( name, -1 )
        {
        }

        /// <summary>
        /// Initializes a new IndexAttribute instance for an index with the given name
        /// and column order, but with no uniqueness specified.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="order">A number which will be used to determine column ordering for multi-column indexes.</param>
        public IndexAttribute( string name, int order )
        {
            this.Name = name;
            this.Order = order;
        }
    }
}
