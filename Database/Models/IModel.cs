using System;

namespace TNRD.Zeepkist.GTR.Auth.Database.Models;

public interface IModel
{
    int Id { get; set; }
    DateTime? DateCreated { get; set; }
    DateTime? DateUpdated { get; set; }
}
