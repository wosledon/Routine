using Routine.Api.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Routine.Api.ValidationAttributes;

namespace Routine.Api.Models
{
    [EmployeeNoMustDifferentFromFirstName(ErrorMessage = "员工编号和名必须不一样")]
    public class EmployeeAddDto: EmployeeAddOrUpdateDto
    {
       
    }
}
