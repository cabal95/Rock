using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace System.Data.Entity.Validation
{
    public class DbEntityValidationException : Exception
    {
        public IEnumerable<DbEntityValidationResult> EntityValidationErrors { get; protected set; }
    }

    public class DbEntityValidationResult
    {
        public EntityEntry Entry { get; private set; }

        public bool IsValid { get; protected set; }

        public ICollection<ValidationError> ValidationErrors { get; protected set; }
    }

    public class ValidationError
    {
        public string PropertyName { get; protected set; }

        public string ErrorMessage { get; protected set; }
    }
}
