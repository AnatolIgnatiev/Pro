using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Pro.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Не указан логин")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Не указан пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароль не совпадает")]
        public string ConfirmPassword { get; set; }
    }
}
