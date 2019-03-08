﻿using System;

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rock.CoreShims
{
    /// <summary>
    /// The base SqlServerMigrationsSqlGenerator has a timeout of 1 second when trying to
    /// split large SQL statements using RegEx. This causes problems with, for example, the
    /// CreateDatabase migration. Pre-split the strings into smaller chunks.
    /// </summary>
    public class RockSqlServerMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
    {
        public RockSqlServerMigrationsSqlGenerator( MigrationsSqlGeneratorDependencies dependencies, IMigrationsAnnotationProvider migrationsAnnotations )
            : base( dependencies, migrationsAnnotations )
        {
        }
        protected override void Generate( SqlOperation operation, Microsoft.EntityFrameworkCore.Metadata.IModel model, MigrationCommandListBuilder builder )
        {
            // EFTODO: This does not take into account "GO ###" statements that are meant to repeat.

            var batches = operation.Sql.Split( new[] { "GO\r", "GO\n", "go\r", "go\n", "Go\r", "Go\n", "gO\r", "gO\n" }, StringSplitOptions.RemoveEmptyEntries );
            for ( var i = 0; i < batches.Length; i++ )
            {
                builder.AppendLine( batches[i] );
                EndStatement( builder, operation.SuppressTransaction );
            }
        }
    }
}
