using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Rock.Data.Validation
{
    public class EntityValidationException : Exception
    {
        public IEnumerable<EntityValidationResult> EntityValidationErrors { get; protected set; }
    }

    public class EntityValidationResult
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
