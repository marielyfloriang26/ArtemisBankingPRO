using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Application.Exceptions;

public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(List<string> errors) : base("Se encontraron errores de validación.")
    {
        Errors = errors;
    }
}
